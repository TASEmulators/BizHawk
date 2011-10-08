using System;
using System.Collections.Generic;
using System.IO;
using BizHawk.Emulation.CPUs.M68K;
using BizHawk.Emulation.CPUs.Z80;
using BizHawk.Emulation.Sound;

namespace BizHawk.Emulation.Consoles.Sega
{
	public sealed partial class Genesis : IEmulator
	{
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
			YM2612 = new YM2612();
			PSG = new SN76489();
			VDP = new GenVDP();
			VDP.DmaReadFrom68000 = ReadW;
			SoundMixer = new SoundMixer(YM2612, PSG);

			MainCPU.ReadByte = ReadB;
			MainCPU.ReadWord = ReadW;
			MainCPU.ReadLong = ReadL;
			MainCPU.WriteByte = WriteB;
			MainCPU.WriteWord = WriteW;
			MainCPU.WriteLong = WriteL;

			SoundCPU.ReadMemory = ReadMemoryZ80;
			SoundCPU.WriteMemory = WriteMemoryZ80;
			SoundCPU.WriteHardware = (a, v) => { Console.WriteLine("Z80: Attempt I/O Write {0:X2}:{1:X2}", a, v); };
			SoundCPU.ReadHardware = x => 0xFF;
			SoundCPU.IRQCallback = () => SoundCPU.Interrupt = false;
			Z80Reset = true;
            RomData = new byte[0x400000];
            for (int i = 0; i < rom.Length; i++)
                RomData[i] = rom[i];

            MainCPU.Reset();
		}

		public void FrameAdvance(bool render)
		{
			Frame++;
			PSG.BeginFrame(SoundCPU.TotalExecutedCycles);
			for (VDP.ScanLine = 0; VDP.ScanLine < 262; VDP.ScanLine++)
			{
				Log.Error("VDP","FRAME {0}, SCANLINE {1}", Frame, VDP.ScanLine);

				if (VDP.ScanLine < 224)
					VDP.RenderLine();

				MainCPU.ExecuteCycles(487); // 488??
				if (Z80Runnable)
				{
					//Console.WriteLine("running z80");
					SoundCPU.ExecuteCycles(228);
					SoundCPU.Interrupt = false;
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
		public int LagCount { get { return -1; } set { return; } } //TODO: Implement
		public bool IsLagFrame { get { return false; } }
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
			throw new NotImplementedException();
		}

		public void LoadStateText(TextReader reader)
		{
			throw new NotImplementedException();
		}

		public void SaveStateBinary(BinaryWriter writer)
		{
			throw new NotImplementedException();
		}

		public void LoadStateBinary(BinaryReader reader)
		{
			throw new NotImplementedException();
		}

		public byte[] SaveStateBinary()
		{
			return new byte[0];
		}

		public IList<MemoryDomain> MemoryDomains { get { throw new NotImplementedException(); } }
		public MemoryDomain MainMemory { get { throw new NotImplementedException(); } }

		public void Dispose() { }
	}
}