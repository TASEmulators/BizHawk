using System;

namespace BizHawk.Emulation.Computers.Commodore64.MOS
{
	// emulates the PLA
	// which handles all bank switching

	public class MOSPLA
	{
		// ------------------------------------

		public Func<int, byte> PeekBasicRom;
		public Func<int, byte> PeekCartridgeLo;
		public Func<int, byte> PeekCartridgeHi;
		public Func<int, byte> PeekCharRom;
		public Func<int, byte> PeekCia0;
		public Func<int, byte> PeekCia1;
		public Func<int, byte> PeekColorRam;
		public Func<int, byte> PeekExpansionLo;
		public Func<int, byte> PeekExpansionHi;
		public Func<int, byte> PeekKernalRom;
		public Func<int, byte> PeekMemory;
		public Func<int, byte> PeekSid;
		public Func<int, byte> PeekVic;
		public Action<int, byte> PokeCartridgeLo;
		public Action<int, byte> PokeCartridgeHi;
		public Action<int, byte> PokeCia0;
		public Action<int, byte> PokeCia1;
		public Action<int, byte> PokeColorRam;
		public Action<int, byte> PokeExpansionLo;
		public Action<int, byte> PokeExpansionHi;
		public Action<int, byte> PokeMemory;
		public Action<int, byte> PokeSid;
		public Action<int, byte> PokeVic;
        public Func<bool> ReadAEC;
        public Func<bool> ReadBA;
		public Func<ushort, byte> ReadBasicRom;
		public Func<ushort, byte> ReadCartridgeLo;
		public Func<ushort, byte> ReadCartridgeHi;
		public Func<bool> ReadCharen;
		public Func<ushort, byte> ReadCharRom;
		public Func<ushort, byte> ReadCia0;
		public Func<ushort, byte> ReadCia1;
		public Func<ushort, byte> ReadColorRam;
		public Func<ushort, byte> ReadExpansionLo;
		public Func<ushort, byte> ReadExpansionHi;
		public Func<bool> ReadExRom;
		public Func<bool> ReadGame;
		public Func<bool> ReadHiRam;
		public Func<ushort, byte> ReadKernalRom;
		public Func<bool> ReadLoRam;
		public Func<ushort, byte> ReadMemory;
		public Func<ushort, byte> ReadSid;
		public Func<ushort, byte> ReadVic;
		public Action<ushort, byte> WriteCartridgeLo;
		public Action<ushort, byte> WriteCartridgeHi;
		public Action<ushort, byte> WriteCia0;
		public Action<ushort, byte> WriteCia1;
		public Action<ushort, byte> WriteColorRam;
		public Action<ushort, byte> WriteExpansionLo;
		public Action<ushort, byte> WriteExpansionHi;
		public Action<ushort, byte> WriteMemory;
		public Action<ushort, byte> WriteSid;
		public Action<ushort, byte> WriteVic;
	
		// ------------------------------------

		private enum PLABank
		{
			None,
			RAM,
			BasicROM,
			KernalROM,
			CharROM,
			IO,
			CartridgeLo,
			CartridgeHi,
			Vic,
			Sid,
			ColorRam,
			Cia0,
			Cia1,
			Expansion0,
			Expansion1
		}

		// ------------------------------------

        bool p0;
        bool p1;
        bool p2;
        bool p3;
        bool p4;
        bool p5;
        bool p6;
        bool p7;
        bool p8;
        bool p9;
        bool p10;
        bool p11;
        bool p12;
        bool p13;
        bool p14;
        bool p15;
        bool p16;
        bool p17;
        bool p18;
        bool p19;
        bool p20;
        bool p21;
        bool p22;
        bool p23;
        bool p24;
        bool p25;
        bool p26;
        bool p27;
        bool p28;
        bool p29;
        bool p30;
        bool p31;
        bool loram;
        bool hiram;
        bool game;
        bool exrom;
        bool charen;
        bool a15;
        bool a14;
        bool a13;
        bool a12;
        bool va14;
        bool va13;
        bool va12;
        bool aec;
        bool ba;
        bool cas;
        bool casram;
        bool basic;
        bool kernal;
        bool charrom;
        //bool grw;
        bool io;
        bool roml;
        bool romh;

		private PLABank Bank(ushort addr, bool read)
		{
            loram = ReadLoRam();
            hiram = ReadHiRam();
            game = ReadGame();
            exrom = ReadExRom();
            charen = ReadCharen();

            a15 = (addr & 0x8000) != 0;
            a14 = (addr & 0x4000) != 0;
            a13 = (addr & 0x2000) != 0;
            a12 = (addr & 0x1000) != 0;
            va14 = a14;
            va13 = a13;
            va12 = a12;
            aec = !ReadAEC(); //active low
            ba = ReadBA();
            cas = !true; //active low

            p0 = loram && hiram && a15 && !a14 && a13 && !aec && read && game;
            p1 = hiram && a15 && a14 && a13 && !aec && read && game;
            p2 = hiram && a15 && a14 && a13 && !aec && read && !exrom && !game;
            p3 = hiram && !charen && a15 && a14 && !a13 && a12 && !aec && read && game;
            p4 = loram && !charen && a15 && a14 && !a13 && a12 && !aec && read && game;
            p5 = hiram && !charen && a15 && a14 && !a13 && a12 && !aec && read && !exrom && !game;
            p6 = va14 && !va13 && va12 && aec && game;
            p7 = va14 && !va13 && va12 && aec && !exrom && !game;
            p9 = hiram && charen && a15 && a14 && !a13 && a12 && !aec && read && game;
            p10 = hiram && charen && a15 && a14 && !a13 && a12 && !aec && !read && game;
            p11 = loram && charen && a15 && a14 && !a13 && a12 && !aec && read && game;
            p12 = loram && charen && a15 && a14 && !a13 && a12 && !aec && !read && game;
            p13 = hiram && charen && a15 && a14 && !a13 && a12 && !aec && read && !exrom && !game;
            p14 = hiram && charen && a15 && a14 && !a13 && a12 && !aec && !read && !exrom && !game;
            p15 = loram && charen && a15 && a14 && !a13 && a12 && !aec && read && !exrom && !game;
            p16 = loram && charen && a15 && a14 && !a13 && a12 && !aec && !read && !exrom && !game;
            p17 = a15 && a14 && !a13 && a12 && !aec && read && exrom && !game;
            p18 = a15 && a14 && !a13 && a12 && !aec && !read && exrom && !game;
            p19 = loram && hiram && a15 && !a14 && !a13 && !aec && read && !exrom;
            p20 = a15 && !a14 && !a13 && !aec && exrom && !game;
            p21 = hiram && a15 && !a14 && a13 && !aec && read && !exrom && !game;
            p22 = a15 && a14 && a13 && !aec && exrom && !game;
            p23 = va13 && va12 && aec && exrom && !game;
            p24 = !a15 && !a14 && a12 && exrom && !game;
            p25 = !a15 && !a14 && a13 && exrom && !game;
            p26 = !a15 && a14 && exrom && !game;
            p27 = a15 && !a14 && a13 && exrom && !game;
            p28 = a15 && a14 && !a13 && !a12 && exrom && !game;
            p30 = cas;
            //p31 = !cas && a15 && a14 && !a13 && a12 && !aec && !read;
            casram = !(p0 || p1 || p2 || p3 || p4 || p5 || p6 || p7 || p9 || p10 || p11 || p12 || p13 || p14 || p15 || p16 || p17 || p18 || p19 || p20 || p21 || p22 || p23 || p24 || p25 || p26 || p27 || p28 || p30);
            basic = p0;
            kernal = (p1 || p2);
            charrom = (p3 || p4 || p5 || p6 || p7);
            //grw = p31;
            io = (p9 || p10 || p11 || p12 || p13 || p14 || p15 || p16 || p17 || p18);
            roml = (p19 || p20);
            romh = (p21 || p22 || p23);

            if (basic)
                return PLABank.BasicROM;
            if (kernal)
                return PLABank.KernalROM;
            if (charrom)
                return PLABank.CharROM;
            if (io)
            {
                switch (addr & 0x0F00)
                {
                    case 0x000:
                    case 0x100:
                    case 0x200:
                    case 0x300:
                        return PLABank.Vic;
                    case 0x400:
                    case 0x500:
                    case 0x600:
                    case 0x700:
                        return PLABank.Sid;
                    case 0x800:
                    case 0x900:
                    case 0xA00:
                    case 0xB00:
                        return PLABank.ColorRam;
                    case 0xC00:
                        return PLABank.Cia0;
                    case 0xD00:
                        return PLABank.Cia1;
                    case 0xE00:
                        return PLABank.Expansion0;
                    case 0xF00:
                        return PLABank.Expansion1;
                }
                return PLABank.IO;
            }
            if (roml)
                return PLABank.CartridgeLo;
            if (romh)
                return PLABank.CartridgeHi;
            if (casram)
                return PLABank.RAM;
            return PLABank.None;
        }

		public byte Peek(int addr)
		{
			switch (Bank((ushort)(addr & 0xFFFF), true))
			{
				case PLABank.BasicROM:
					return PeekBasicRom(addr);
				case PLABank.CartridgeHi:
					return PeekCartridgeHi(addr);
				case PLABank.CartridgeLo:
					return PeekCartridgeLo(addr);
				case PLABank.CharROM:
					return PeekCharRom(addr);
				case PLABank.Cia0:
					return PeekCia0(addr);
				case PLABank.Cia1:
					return PeekCia1(addr);
				case PLABank.ColorRam:
					return PeekColorRam(addr);
				case PLABank.Expansion0:
					return PeekExpansionLo(addr);
				case PLABank.Expansion1:
					return PeekExpansionHi(addr);
				case PLABank.KernalROM:
					return PeekKernalRom(addr);
				case PLABank.None:
					return 0xFF;
				case PLABank.RAM:
					return PeekMemory(addr);
				case PLABank.Sid:
					return PeekSid(addr);
				case PLABank.Vic:
					return PeekVic(addr);
			}
			return 0xFF;
		}

		public void Poke(int addr, byte val)
		{
			switch (Bank((ushort)(addr & 0xFFFF), false))
			{
				case PLABank.BasicROM:
					break;
				case PLABank.CartridgeHi:
					PokeCartridgeHi(addr, val);
					break;
				case PLABank.CartridgeLo:
					PokeCartridgeLo(addr, val);
					break;
				case PLABank.CharROM:
					break;
				case PLABank.Cia0:
					PokeCia0(addr, val);
					break;
				case PLABank.Cia1:
					PokeCia1(addr, val);
					break;
				case PLABank.ColorRam:
					PokeColorRam(addr, val);
					break;
				case PLABank.Expansion0:
					PokeExpansionLo(addr, val);
					break;
				case PLABank.Expansion1:
					PokeExpansionHi(addr, val);
					break;
				case PLABank.KernalROM:
					break;
				case PLABank.None:
					break;
				case PLABank.RAM:
					PokeMemory(addr, val);
					break;
				case PLABank.Sid:
					PokeSid(addr, val);
					break;
				case PLABank.Vic:
					PokeVic(addr, val);
					break;
			}
		}

		public byte Read(ushort addr)
		{
			switch (Bank(addr, true))
			{
				case PLABank.BasicROM:
					return ReadBasicRom(addr);
				case PLABank.CartridgeHi:
					return ReadCartridgeHi(addr);
				case PLABank.CartridgeLo:
					return ReadCartridgeLo(addr);
				case PLABank.CharROM:
					return ReadCharRom(addr);
				case PLABank.Cia0:
					return ReadCia0(addr);
				case PLABank.Cia1:
					return ReadCia1(addr);
				case PLABank.ColorRam:
					return ReadColorRam(addr);
				case PLABank.Expansion0:
					return ReadExpansionLo(addr);
				case PLABank.Expansion1:
					return ReadExpansionHi(addr);
				case PLABank.KernalROM:
					return ReadKernalRom(addr);
				case PLABank.RAM:
					return ReadMemory(addr);
				case PLABank.Sid:
					return ReadSid(addr);
				case PLABank.Vic:
					return ReadVic(addr);
			}
			return 0xFF;
		}

		public void Write(ushort addr, byte val)
		{
			switch (Bank(addr, false))
			{
				case PLABank.BasicROM:
					break;
				case PLABank.CartridgeHi:
					WriteCartridgeHi(addr, val);
			        WriteMemory(addr, val);
					break;
				case PLABank.CartridgeLo:
					WriteCartridgeLo(addr, val);
			        WriteMemory(addr, val);
					break;
				case PLABank.CharROM:
					break;
				case PLABank.Cia0:
					WriteCia0(addr, val);
					break;
				case PLABank.Cia1:
					WriteCia1(addr, val);
					break;
				case PLABank.ColorRam:
					WriteColorRam(addr, val);
					break;
				case PLABank.Expansion0:
					WriteExpansionLo(addr, val);
					return;
				case PLABank.Expansion1:
					WriteExpansionHi(addr, val);
                    return;
				case PLABank.KernalROM:
					break;
				case PLABank.None:
					break;
				case PLABank.RAM:
			        WriteMemory(addr, val);
					break;
				case PLABank.Sid:
					WriteSid(addr, val);
					break;
				case PLABank.Vic:
					WriteVic(addr, val);
					break;
			}
		}
	}
}
