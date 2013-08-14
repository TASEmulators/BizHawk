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
		public Func<int, byte> ReadBasicRom;
		public Func<int, byte> ReadCartridgeLo;
		public Func<int, byte> ReadCartridgeHi;
		public Func<bool> ReadCharen;
		public Func<int, byte> ReadCharRom;
		public Func<int, byte> ReadCia0;
		public Func<int, byte> ReadCia1;
		public Func<int, byte> ReadColorRam;
		public Func<int, byte> ReadExpansionLo;
		public Func<int, byte> ReadExpansionHi;
		public Func<bool> ReadExRom;
		public Func<bool> ReadGame;
		public Func<bool> ReadHiRam;
		public Func<int, byte> ReadKernalRom;
		public Func<bool> ReadLoRam;
		public Func<int, byte> ReadMemory;
		public Func<int, byte> ReadSid;
		public Func<int, byte> ReadVic;
		public Action<int, byte> WriteCartridgeLo;
		public Action<int, byte> WriteCartridgeHi;
		public Action<int, byte> WriteCia0;
		public Action<int, byte> WriteCia1;
		public Action<int, byte> WriteColorRam;
		public Action<int, byte> WriteExpansionLo;
		public Action<int, byte> WriteExpansionHi;
		public Action<int, byte> WriteMemory;
		public Action<int, byte> WriteSid;
		public Action<int, byte> WriteVic;
	
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
        bool p30;
        bool loram;
        bool hiram;
        bool game;
        bool exrom;
        bool charen;
        bool a15;
        bool a14;
        bool a13;
        bool a12;
        bool aec;
        bool cas;

		private PLABank Bank(int addr, bool read)
		{
            loram = ReadLoRam();
            hiram = ReadHiRam();
            game = ReadGame();

            a15 = (addr & 0x08000) != 0;
            a14 = (addr & 0x04000) != 0;
            a13 = (addr & 0x02000) != 0;
            a12 = (addr & 0x01000) != 0;
            aec = !ReadAEC(); //active low

            p0 = loram && hiram && a15 && !a14 && a13 && !aec && read && game;
            if (p0)
                return PLABank.BasicROM;

            exrom = ReadExRom();
            p1 = hiram && a15 && a14 && a13 && !aec && read && game;
            p2 = hiram && a15 && a14 && a13 && !aec && read && !exrom && !game;
            if (p1 || p2)
                return PLABank.KernalROM;

            charen = ReadCharen();
            p3 = hiram && !charen && a15 && a14 && !a13 && a12 && !aec && read && game;
            p4 = loram && !charen && a15 && a14 && !a13 && a12 && !aec && read && game;
            p5 = hiram && !charen && a15 && a14 && !a13 && a12 && !aec && read && !exrom && !game;
            p6 = a14 && !a13 && a12 && aec && game;
            p7 = a14 && !a13 && a12 && aec && !exrom && !game;
            if (p3 || p4 || p5 || p6 || p7)
                return PLABank.CharROM;

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
            if (p9 || p10 || p11 || p12 || p13 || p14 || p15 || p16 || p17 || p18)
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

            p19 = loram && hiram && a15 && !a14 && !a13 && !aec && read && !exrom;
            p20 = a15 && !a14 && !a13 && !aec && exrom && !game;
            if (p19 || p20)
                return PLABank.CartridgeLo;

            p21 = hiram && a15 && !a14 && a13 && !aec && read && !exrom && !game;
            p22 = a15 && a14 && a13 && !aec && exrom && !game;
            p23 = a13 && a12 && aec && exrom && !game;
            if (p21 || p22 || p23)
                return PLABank.CartridgeHi;

            cas = !true; //active low
            p24 = !a15 && !a14 && a12 && exrom && !game;
            p25 = !a15 && !a14 && a13 && exrom && !game;
            p26 = !a15 && a14 && exrom && !game;
            p27 = a15 && !a14 && a13 && exrom && !game;
            p28 = a15 && a14 && !a13 && !a12 && exrom && !game;
            p30 = cas;
            if (!(p24 || p25 || p26 || p27 || p28 || p30))
                return PLABank.RAM;

            //p31 = !cas && a15 && a14 && !a13 && a12 && !aec && !read;
            //grw = p31;

            return PLABank.None;
        }

		public byte Peek(int addr)
		{
            addr &= 0x0FFFF;
			switch (Bank(addr, true))
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
            addr &= 0x0FFFF;
			switch (Bank(addr, false))
			{
				case PLABank.CartridgeHi:
					PokeCartridgeHi(addr, val);
					break;
				case PLABank.CartridgeLo:
					PokeCartridgeLo(addr, val);
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

		public byte Read(int addr)
		{
            addr &= 0x0FFFF;
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

		public void Write(int addr, byte val)
		{
            addr &= 0x0FFFF;
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
