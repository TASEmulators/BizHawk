using System;
using System.Collections.Generic;
using System.IO;
using BizHawk.Emulation.CPUs.M68000;
using BizHawk.Emulation.CPUs.Z80;
using BizHawk.Emulation.Sound;

namespace BizHawk.Emulation.Consoles.Sega
{
	[CoreVersion("0.0.0.1", FriendlyName = "MegaHawk")]
	public sealed partial class Genesis : IEmulator
	{
		private int _lagcount = 0;
		private bool lagged = true;
		private bool islag = false;

		// ROM
		public byte[] RomData;

		// Machine stuff
		public MC68000 MainCPU;
		public Z80A SoundCPU;
		public GenVDP VDP;
		public SN76489 PSG;
		public YM2612 YM2612;
		public byte[] Ram = new byte[0x10000];
		public byte[] Z80Ram = new byte[0x2000];

		private bool M68000HasZ80Bus = false;
		private bool Z80Reset = false;
		private bool Z80Runnable { get { return (Z80Reset == false && M68000HasZ80Bus == false); } }

		private SoundMixer SoundMixer;

		public void ResetFrameCounter()
		{
			Frame = 0;
		}

		// Genesis timings:
		// 53,693,175   Machine clocks / sec
		//  7,670,454   Main 68000 cycles / sec (7 mclk divisor)
		//  3,579,545   Z80 cycles / sec (15 mclk divisor)

		// At 59.92 FPS:
		//    896,081   mclks / frame
		//    128,011   Main 68000 cycles / frame
		//     59,738   Z80 cycles / frame

		// At 262 lines/frame: 
		//       3420   mclks / line
		//      ~ 488.5 Main 68000 cycles / line
		//        228   Z80 cycles / line

		// Video characteristics:
		// 224 lines are active display. The remaining 38 lines are vertical blanking.
		// In H40 mode, the dot clock is 480 pixels per line. 
		// 320 are active display, the remaining 160 are horizontal blanking.
		// A total of 3420 mclks per line, but 2560 mclks are active display and 860 mclks are blanking.

		public Genesis(GameInfo game, byte[] rom)
		{
			CoreOutputComm = new CoreOutputComm();
			MainCPU = new MC68000();
			SoundCPU = new Z80A();
            YM2612 = new YM2612() { MaxVolume = 23405 };
            PSG = new SN76489() { MaxVolume = 4681 };
			VDP = new GenVDP();
			VDP.DmaReadFrom68000 = ReadWord;
			SoundMixer = new SoundMixer(YM2612, PSG);

			MainCPU.ReadByte = ReadByte;
			MainCPU.ReadWord = ReadWord;
			MainCPU.ReadLong = ReadLong;
			MainCPU.WriteByte = WriteByte;
			MainCPU.WriteWord = WriteWord;
			MainCPU.WriteLong = WriteLong;

			SoundCPU.ReadMemory = ReadMemoryZ80;
			SoundCPU.WriteMemory = WriteMemoryZ80;
			SoundCPU.WriteHardware = (a, v) => { Console.WriteLine("Z80: Attempt I/O Write {0:X2}:{1:X2}", a, v); };
			SoundCPU.ReadHardware = x => 0xFF;
			SoundCPU.IRQCallback = () => SoundCPU.Interrupt = false;
			Z80Reset = true;
			RomData = new byte[0x400000];
			for (int i = 0; i < rom.Length; i++)
				RomData[i] = rom[i];

			SetupMemoryDomains();
			MainCPU.Reset();
		}

		public void FrameAdvance(bool render)
		{
			lagged = true;

			Frame++;
			PSG.BeginFrame(SoundCPU.TotalExecutedCycles);
            YM2612.BeginFrame(SoundCPU.TotalExecutedCycles);
			for (VDP.ScanLine = 0; VDP.ScanLine < 262; VDP.ScanLine++)
			{
				//Log.Error("VDP","FRAME {0}, SCANLINE {1}", Frame, VDP.ScanLine);

				if (VDP.ScanLine < 224)
					VDP.RenderLine();

				MainCPU.ExecuteCycles(487); // 488??
                if (Z80Runnable)
                {
                    //Console.WriteLine("running z80");
                    SoundCPU.ExecuteCycles(228);
                    SoundCPU.Interrupt = false;
                } else {
                    SoundCPU.TotalExecutedCycles += 228; // I emulate the YM2612 synced to Z80 clock, for better or worse. Keep the timer going even if Z80 isn't running.
                }

				if (VDP.ScanLine == 224)
				{
					MainCPU.ExecuteCycles(16);// stupid crap to sync with genesis plus for log testing
					// End-frame stuff
					if (VDP.VInterruptEnabled)
						MainCPU.Interrupt = 6;

					if (Z80Runnable)
						SoundCPU.Interrupt = true;
				}
			}
			PSG.EndFrame(SoundCPU.TotalExecutedCycles);
            YM2612.EndFrame(SoundCPU.TotalExecutedCycles);

			Controller.UpdateControls(Frame++);
			if (lagged)
			{
				_lagcount++;
				islag = true;
			}
			else
				islag = false;
		}

		public CoreInputComm CoreInputComm { get; set; }
		public CoreOutputComm CoreOutputComm { get; private set; }

		public IVideoProvider VideoProvider
		{
			get { return VDP; }
		}

		public ISoundProvider SoundProvider
		{
			get { return SoundMixer; }
		}

		public int Frame { get; set; }
		public int LagCount { get { return _lagcount; } set { _lagcount = value; } }
		public bool IsLagFrame { get { return islag; } }
		public bool DeterministicEmulation { get; set; }
		public string SystemId { get { return "GEN"; } }

		public byte[] SaveRam
		{
			get { throw new NotImplementedException(); }
		}

		public bool SaveRamModified
		{
			get
			{
				return false; // TODO implement
			}
			set
			{
				throw new NotImplementedException();
			}
		}

        public void SaveStateText(TextWriter writer)
        {
            writer.WriteLine("[MegaDrive]");
            MainCPU.SaveStateText(writer, "Main68K");
            SoundCPU.SaveStateText(writer);
            PSG.SaveStateText(writer);
            VDP.SaveStateText(writer);
			writer.WriteLine("Frame {0}", Frame);
			writer.WriteLine("Lag {0}", _lagcount);
            writer.Write("MainRAM ");
            Ram.SaveAsHex(writer);
            writer.Write("Z80RAM ");
            Z80Ram.SaveAsHex(writer);
            

            writer.WriteLine("[/MegaDrive]");
        }

        public void LoadStateText(TextReader reader)
        {
            while (true)
            {
                string[] args = reader.ReadLine().Split(' ');
                if (args[0].Trim() == "") continue;
                if (args[0] == "[MegaDrive]") continue;
                if (args[0] == "[/MegaDrive]") break;
                if (args[0] == "MainRAM")
                    Ram.ReadFromHex(args[1]);
                else if (args[0] == "Z80RAM")
                    Z80Ram.ReadFromHex(args[1]);
                else if (args[0] == "[Main68K]")
                    MainCPU.LoadStateText(reader, "Main68K");
                else if (args[0] == "[Z80]")
                    SoundCPU.LoadStateText(reader);
				else if (args[0] == "Frame")
					Frame = int.Parse(args[1]);
				else if (args[0] == "Lag")
					_lagcount = int.Parse(args[1]);
                else if (args[0] == "[PSG]")
                    PSG.LoadStateText(reader);
                else if (args[0] == "[VDP]")
                    VDP.LoadStateText(reader);
                else
                    Console.WriteLine("Skipping unrecognized identifier " + args[0]);
            }
        }

		public void SaveStateBinary(BinaryWriter writer)
		{
			//throw new NotImplementedException();
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			//throw new NotImplementedException();
		}

		public byte[] SaveStateBinary()
		{
			return new byte[0];
		}

		IList<MemoryDomain> memoryDomains;

		void SetupMemoryDomains()
		{
			var domains = new List<MemoryDomain>(3);
			var MainMemoryDomain = new MemoryDomain("68000 RAM", Ram.Length, Endian.Big,
				addr => Ram[addr & 0xFFFF],
				(addr, value) => Ram[addr & 0xFFFF] = value);
			var Z80Domain = new MemoryDomain("Z80 RAM", Z80Ram.Length, Endian.Little,
				addr => Z80Ram[addr & 0x1FFF],
				(addr, value) => { Z80Ram[addr & 0x1FFF] = value; });

			var VRamDomain = new MemoryDomain("Video RAM", VDP.VRAM.Length, Endian.Big,
				addr => VDP.VRAM[addr & 0xFFFF],
				(addr, value) => VDP.VRAM[addr & 0xFFFF] = value);

			domains.Add(MainMemoryDomain);
			domains.Add(Z80Domain);
			domains.Add(VRamDomain);
			memoryDomains = domains.AsReadOnly();
		}

		public IList<MemoryDomain> MemoryDomains { get { return memoryDomains; } }
		public MemoryDomain MainMemory { get { return memoryDomains[0]; } }

		public void Dispose() { }
	}
}