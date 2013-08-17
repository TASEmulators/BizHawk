using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Experimental.Chips.Internals
{
    sealed public class Pla
    {
        #region CACHE
        bool[] basicStates;
        bool[] casStates;
        bool[] charStates;
        bool[] grStates;
        bool[] ioStates;
        bool[] kernalStates;
        bool[] romHiStates;
        bool[] romLoStates;

        bool cachedBasic;
        bool cachedCASRam;
        bool cachedCharRom;
        bool cachedGraphicsRead;
        bool cachedIO;
        bool cachedKernal;
        bool cachedRomHi;
        bool cachedRomLo;
        #endregion

        #region INPUTS
        public Func<int> InputAddress;
        public Func<bool> InputAEC;
        public Func<bool> InputBA;
        public Func<bool> InputCAS;
        public Func<bool> InputCharen;
        public Func<bool> InputExRom;
        public Func<bool> InputGame;
        public Func<bool> InputHiRam;
        public Func<bool> InputLoRam;
        public Func<bool> InputRead;
        public Func<int> InputVA;
        #endregion

        #region OUTPUTS
        public bool Basic { get { return cachedBasic; } }
        public bool CASRam { get { return cachedCASRam; } }
        public bool CharRom { get { return cachedCharRom; } }
        public bool GraphicsRead { get { return cachedGraphicsRead; } }
        public bool IO { get { return cachedIO; } }
        public bool Kernal { get { return cachedKernal; } }
        public bool OutputBasic() { return Basic; }
        public bool OutputCASRam() { return CASRam; }
        public bool OutputCharRom() { return CharRom; }
        public bool OutputGraphicsRead() { return GraphicsRead; }
        public bool OutputIO() { return IO; }
        public bool OutputKernal() { return Kernal; }
        public bool OutputRomHi() { return RomHi; }
        public bool OutputRomLo() { return RomLo; }
        public bool RomHi { get { return cachedRomHi; } }
        public bool RomLo { get { return cachedRomLo; } }
        public void SyncState(Serializer ser) { }
        #endregion

        #region LOOKUP_TABLE_GENERATOR

        // PLA line information is from the PDF titled "The C64 PLA Dissected"
        // Written by Thomas 'skoe' Giesel.

        void GenerateLookup()
        {
            bool a12;
            bool a13;
            bool a14;
            bool a15;
            bool aec;
            bool ba;
            bool cas;
            bool charen;
            bool exrom;
            bool game;
            bool hiram;
            bool loram;
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
            bool p31;
            bool read;
            bool va12;
            bool va13;
            bool va14;

            basicStates = new bool[65536];
            casStates = new bool[65536];
            charStates = new bool[65536];
            grStates = new bool[65536];
            ioStates = new bool[65536];
            kernalStates = new bool[65536];
            romHiStates = new bool[65536];
            romLoStates = new bool[65536];

            for (int i = 0; i < 65536; i++)
            {
                aec = (i & 0x0001) != 0;
                ba = (i & 0x0002) != 0;
                cas = (i & 0x0004) != 0;
                charen = (i & 0x0008) != 0;
                exrom = (i & 0x0010) != 0;
                game = (i & 0x0020) != 0;
                loram = (i & 0x0040) != 0;
                hiram = (i & 0x0080) != 0;
                read = (i & 0x0100) != 0;
                va12 = (i & 0x0200) != 0;
                va13 = (i & 0x0400) != 0;
                va14 = (i & 0x0800) != 0;
                a12 = (i & 0x1000) != 0;
                a13 = (i & 0x2000) != 0;
                a14 = (i & 0x4000) != 0;
                a15 = (i & 0x8000) != 0;

                p0 = loram && hiram && a15 && !a14 && a13 && !aec && read && game;
                p1 = hiram && a15 && a14 && a13 && !aec && read && game;
                p2 = hiram && a15 && a14 && a13 && !aec && read && !exrom && !game;
                p3 = hiram && !charen && a15 && a14 && !a13 && a12 && !aec && read && game;
                p4 = loram && !charen && a15 && a14 && !a13 && a12 && !aec && read && game;
                p5 = hiram && !charen && a15 && a14 && !a13 && a12 && !aec && read && !exrom && !game;
                p6 = va14 && !va13 && va12 && aec && game;
                p7 = va14 && !va13 && va12 && aec && !exrom && !game;
                //p8 = cas && a15 && a14 && !a13 && a12 && !aec && !rd;
                p9 = hiram && charen && a15 && a14 && !a13 && a12 && !aec && ba && read && game;
                p10 = hiram && charen && a15 && a14 && !a13 && a12 && !aec && !read && game;
                p11 = loram && charen && a15 && a14 && !a13 && a12 && !aec && ba && read && game;
                p12 = loram && charen && a15 && a14 && !a13 && a12 && !aec && !read && game;
                p13 = hiram && charen && a15 && a14 && !a13 && a12 && !aec && ba && read && !exrom && !game;
                p14 = hiram && charen && a15 && a14 && !a13 && a12 && !aec && !read && !exrom && !game;
                p15 = loram && charen && a15 && a14 && !a13 && a12 && !aec && ba && read && !exrom && !game;
                p16 = loram && charen && a15 && a14 && !a13 && a12 && !aec && !read && !exrom && !game;
                p17 = a15 && a14 && !a13 && a12 && !aec && ba && read && exrom && !game;
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
                //p29 = !cas;
                p30 = cas;
                p31 = !cas && a15 && a14 && !a13 && a12 && !aec && !read;

                casStates[i] = (p0 || p1 || p2 || p3 || p4 || p5 || p6 || p7 || p9 || p10 || p11 || p12 || p13 || p14 || p15 || p16 || p17 || p18 || p19 || p20 || p21 || p22 || p23 || p24 || p25 || p26 || p27 || p28 || p30);
                basicStates[i] = (!p0);
                kernalStates[i] = (!(p1 || p2));
                charStates[i] = (!(p3 || p4 || p5 || p6 || p7));
                grStates[i] = (!p31);
                ioStates[i] = (!(p9 || p10 || p11 || p12 || p13 || p14 || p15 || p16 || p17 || p18));
                romLoStates[i] = (!(p19 || p20));
                romHiStates[i] = (!(p21 || p22 || p23));
            }
        }
        #endregion

        public Pla()
        {
            GenerateLookup();
        }

        public void Precache() 
        {
            int stateIndex = (
                (InputAEC() ? 0x1 : 0) |
                (InputBA() ? 0x2 : 0) |
                (InputCAS() ? 0x4 : 0) |
                (InputCharen() ? 0x8 : 0) |
                (InputExRom() ? 0x10 : 0) |
                (InputGame() ? 0x20 : 0) |
                (InputLoRam() ? 0x40 : 0) |
                (InputHiRam() ? 0x80 : 0) |
                (InputRead() ? 0x100 : 0) |
                ((InputVA() & 0x7000) >> 3) |
                (InputAddress() & 0xF000)
                );

            cachedBasic = basicStates[stateIndex];
            cachedCASRam = casStates[stateIndex];
            cachedCharRom = charStates[stateIndex];
            cachedGraphicsRead = grStates[stateIndex];
            cachedIO = ioStates[stateIndex];
            cachedKernal = kernalStates[stateIndex];
            cachedRomHi = romHiStates[stateIndex];
            cachedRomLo = romLoStates[stateIndex];
        }
    }
}
