#if WINDOWS
#define MUSASHI
#endif

using System;
using System.Collections.Generic;
using System.IO;
using BizHawk.Emulation.CPUs.M68000;
using BizHawk.Emulation.CPUs.Z80;
using BizHawk.Emulation.Sound;
using Native68000;
using System.Runtime.InteropServices;

namespace BizHawk.Emulation.Consoles.Sega
{
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
			_lagcount = 0;
			islag = false;
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

#if MUSASHI
        VdpCallback _vdp;
        ReadCallback read8;
        ReadCallback read16;
        ReadCallback read32;
        WriteCallback write8;
        WriteCallback write16;
        WriteCallback write32;
#endif

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
            MainCPU.IrqCallback = InterruptCallback;

            // ---------------------- musashi -----------------------
#if MUSASHI
            _vdp = vdpcallback;
            read8 = Read8;
            read16 = Read16;
            read32 = Read32;
            write8 = Write8;
            write16 = Write16;
            write32 = Write32;

            Musashi.RegisterVdpCallback(Marshal.GetFunctionPointerForDelegate(_vdp));
            Musashi.RegisterRead8(Marshal.GetFunctionPointerForDelegate(read8));
            Musashi.RegisterRead16(Marshal.GetFunctionPointerForDelegate(read16));
            Musashi.RegisterRead32(Marshal.GetFunctionPointerForDelegate(read32));
            Musashi.RegisterWrite8(Marshal.GetFunctionPointerForDelegate(write8));
            Musashi.RegisterWrite16(Marshal.GetFunctionPointerForDelegate(write16));
            Musashi.RegisterWrite32(Marshal.GetFunctionPointerForDelegate(write32));
#endif
            // ---------------------- musashi -----------------------

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
#if MUSASHI
            Musashi.Init();
            Musashi.Reset();
            VDP.GetPC = () => Musashi.PC;
#else
            MainCPU.Reset();
            VDP.GetPC = () => MainCPU.PC;
#endif
            InitializeCartHardware(game);
        }

        void InitializeCartHardware(GameInfo game)
        {
            LogCartInfo();
            InitializeEeprom(game);
            InitializeSaveRam(game);
        }

		public void FrameAdvance(bool render, bool rendersound)
		{
			lagged = true;

            Controller.UpdateControls(Frame++);
			PSG.BeginFrame(SoundCPU.TotalExecutedCycles);
            YM2612.BeginFrame(SoundCPU.TotalExecutedCycles);

            // Do start-of-frame events
            VDP.HIntLineCounter = VDP.Registers[10];
            //VDP.VdpStatusWord &= 
            unchecked { VDP.VdpStatusWord &= (ushort)~GenVDP.StatusVerticalBlanking; }

			for (VDP.ScanLine = 0; VDP.ScanLine < 262; VDP.ScanLine++)
			{
				//Log.Error("VDP","FRAME {0}, SCANLINE {1}", Frame, VDP.ScanLine);

				if (VDP.ScanLine < VDP.FrameHeight)
					VDP.RenderLine();

                Exec68k(365);
                RunZ80(171);

                // H-Int now?

                VDP.HIntLineCounter--;
                if (VDP.HIntLineCounter < 0 && VDP.ScanLine < 224) // FIXME
                {
                    VDP.HIntLineCounter = VDP.Registers[10];
                    VDP.VdpStatusWord |= GenVDP.StatusHorizBlanking;

                    if (VDP.HInterruptsEnabled)
                    {
                        Set68kIrq(4);
                        //Console.WriteLine("Fire hint!");
                    }

                }

                Exec68k(488 - 365);
                RunZ80(228 - 171);

                if (VDP.ScanLine == 224)
				{
                    VDP.VdpStatusWord |= GenVDP.StatusVerticalInterruptPending;
                    VDP.VdpStatusWord |= GenVDP.StatusVerticalBlanking;
                    Exec68k(16); // this is stupidly wrong.
					// End-frame stuff
                    if (VDP.VInterruptEnabled)
                        Set68kIrq(6);

					SoundCPU.Interrupt = true;
                    //The INT output is asserted every frame for exactly one scanline, and it can't be disabled. A very short Z80 interrupt routine would be triggered multiple times if it finishes within 228 Z80 clock cycles. I think (but cannot recall the specifics) that some games have delay loops in the interrupt handler for this very reason. 
				}
			}
			PSG.EndFrame(SoundCPU.TotalExecutedCycles);
            YM2612.EndFrame(SoundCPU.TotalExecutedCycles);

            

			if (lagged)
			{
				_lagcount++;
				islag = true;
			}
			else
				islag = false;
		}

        void Exec68k(int cycles)
        {
#if MUSASHI
            Musashi.Execute(cycles);
#else
            MainCPU.ExecuteCycles(cycles);
#endif
        }

        void RunZ80(int cycles)
        {
            // I emulate the YM2612 synced to Z80 clock, for better or worse.
            // So we still need to keep the Z80 cycle count accurate even if the Z80 isn't running.

            if (Z80Runnable)
                SoundCPU.ExecuteCycles(cycles);
            else
                SoundCPU.TotalExecutedCycles += cycles; 
        }

        void Set68kIrq(int irq)
        {
#if MUSASHI
            Musashi.SetIRQ(irq);
#else
            MainCPU.Interrupt = irq;
#endif
        }

        int vdpcallback(int level) // Musashi handler
        {
            InterruptCallback(level);
            return -1;
        }

        void InterruptCallback(int level)
        {
            unchecked { VDP.VdpStatusWord &= (ushort)~GenVDP.StatusVerticalInterruptPending; }
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
		public ISyncSoundProvider SyncSoundProvider { get { return new FakeSyncSound(SoundMixer, 735); } }
		public bool StartAsyncSound() { return true; }
		public void EndAsyncSound() { }

		public int Frame { get; set; }
		public int LagCount { get { return _lagcount; } set { _lagcount = value; } }
		public bool IsLagFrame { get { return islag; } }
		public bool DeterministicEmulation { get { return true; } }
		public string SystemId { get { return "GEN"; } }

		

        public void SaveStateText(TextWriter writer)
        {
            var buf = new byte[141501 + SaveRAM.Length];
            var stream = new MemoryStream(buf);
            var bwriter = new BinaryWriter(stream);
            SaveStateBinary(bwriter);
            
            writer.WriteLine("Version 1");
            writer.Write("BigFatBlob ");
            buf.SaveAsHex(writer);

            /*writer.WriteLine("[MegaDrive]");
            MainCPU.SaveStateText(writer, "Main68K");
            SoundCPU.SaveStateText(writer);
            PSG.SaveStateText(writer);
            VDP.SaveStateText(writer);
			writer.WriteLine("Frame {0}", Frame);
			writer.WriteLine("Lag {0}", _lagcount);
			writer.WriteLine("IsLag {0}", islag);
            writer.Write("MainRAM ");
            Ram.SaveAsHex(writer);
            writer.Write("Z80RAM ");
            Z80Ram.SaveAsHex(writer);
            writer.WriteLine("[/MegaDrive]");*/
        }

        public void LoadStateText(TextReader reader)
        {
            var buf = new byte[141501 + SaveRAM.Length];
            var version = reader.ReadLine();
            if (version != "Version 1")
                throw new Exception("Not a valid state vesrion! sorry! your state is bad! Robust states will be added later!");
            var omgstate = reader.ReadLine().Split(' ')[1];
            buf.ReadFromHex(omgstate);
            LoadStateBinary(new BinaryReader(new MemoryStream(buf)));

            /*while (true)
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
				else if (args[0] == "IsLag")
					islag = bool.Parse(args[1]);
                else if (args[0] == "[PSG]")
                    PSG.LoadStateText(reader);
                else if (args[0] == "[VDP]")
                    VDP.LoadStateText(reader);
                else
                    Console.WriteLine("Skipping unrecognized identifier " + args[0]);
            }*/
        }

		public void SaveStateBinary(BinaryWriter writer)
		{
#if MUSASHI
            Musashi.SaveStateBinary(writer);    // 124  
#endif
            SoundCPU.SaveStateBinary(writer);   // 46
            PSG.SaveStateBinary(writer);        // 15
            VDP.SaveStateBinary(writer);        // 65781
            YM2612.SaveStateBinary(writer);     // 1785
            
            writer.Write(Ram);                  // 65535
            writer.Write(Z80Ram);               // 8192

            writer.Write(Frame);                // 4
            writer.Write(M68000HasZ80Bus);      // 1
            writer.Write(Z80Reset);             // 1
            writer.Write(BankRegion);           // 4

            for (int i = 0; i < 3; i++)
            {
                writer.Write(IOPorts[i].Data);
                writer.Write(IOPorts[i].TxData);
                writer.Write(IOPorts[i].RxData);
                writer.Write(IOPorts[i].SCtrl);
            }

            if (SaveRAM.Length > 0)
                writer.Write(SaveRAM);

            // TODO: EEPROM/cart HW state
            // TODO: lag counter crap
		}

		public void LoadStateBinary(BinaryReader reader)
		{
#if MUSASHI
            Musashi.LoadStateBinary(reader);
#endif
			SoundCPU.LoadStateBinary(reader);
            PSG.LoadStateBinary(reader);
            VDP.LoadStateBinary(reader);
            YM2612.LoadStateBinary(reader);

            Ram = reader.ReadBytes(Ram.Length);
            Z80Ram = reader.ReadBytes(Z80Ram.Length);

            Frame = reader.ReadInt32();
            M68000HasZ80Bus = reader.ReadBoolean();
            Z80Reset = reader.ReadBoolean();
            BankRegion = reader.ReadInt32();

            for (int i = 0; i < 3; i++)
            {
                IOPorts[i].Data   = reader.ReadByte();
                IOPorts[i].TxData = reader.ReadByte();
                IOPorts[i].RxData = reader.ReadByte();
                IOPorts[i].SCtrl  = reader.ReadByte();
            }

            if (SaveRAM.Length > 0)
                SaveRAM = reader.ReadBytes(SaveRAM.Length);
		}

		public byte[] SaveStateBinary()
		{
            var buf = new byte[141501+SaveRAM.Length];
            var stream = new MemoryStream(buf);
            var writer = new BinaryWriter(stream);
            SaveStateBinary(writer);
            //Console.WriteLine("buf len = {0}", stream.Position);
            writer.Close();
            return buf;
		}

		IList<MemoryDomain> memoryDomains;

		void SetupMemoryDomains()
		{
			var domains = new List<MemoryDomain>(3);
			var MainMemoryDomain = new MemoryDomain("Main RAM", Ram.Length, Endian.Big,
				addr => Ram[addr & 0xFFFF],
				(addr, value) => Ram[addr & 0xFFFF] = value);
			var Z80Domain = new MemoryDomain("Z80 RAM", Z80Ram.Length, Endian.Little,
				addr => Z80Ram[addr & 0x1FFF],
				(addr, value) => { Z80Ram[addr & 0x1FFF] = value; });

			var VRamDomain = new MemoryDomain("Video RAM", VDP.VRAM.Length, Endian.Big,
				addr => VDP.VRAM[addr & 0xFFFF],
				(addr, value) => VDP.VRAM[addr & 0xFFFF] = value);

			var RomDomain = new MemoryDomain("Rom Data", RomData.Length, Endian.Big,
				addr => RomData[addr], //adelikat: For speed considerations, I didn't mask this, every tool that uses memory domains is smart enough not to overflow, if I'm wrong let me know!
				(addr, value) => RomData[addr & (RomData.Length - 1)] = value);

			var SystemBusDomain = new MemoryDomain("System Bus", 0x1000000, Endian.Big,
				addr => (byte)ReadByte(addr),
				(addr, value) => Write8((uint)addr, (uint)value));

			domains.Add(MainMemoryDomain);
			domains.Add(Z80Domain);
			domains.Add(VRamDomain);
			domains.Add(RomDomain);
			domains.Add(SystemBusDomain);
			memoryDomains = domains.AsReadOnly();
		}

		public IList<MemoryDomain> MemoryDomains { get { return memoryDomains; } }
		public MemoryDomain MainMemory { get { return memoryDomains[0]; } }

		public void Dispose() { }
	}
}