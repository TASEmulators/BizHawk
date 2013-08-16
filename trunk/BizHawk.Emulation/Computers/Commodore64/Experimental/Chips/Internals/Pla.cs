using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Experimental.Chips.Internals
{
    sealed public class Pla
    {
        #region CACHE
        bool a12;
        bool a13;
        bool a14;
        bool a15;
        int addr;
        bool aec;
        bool ba;
        bool cachedBasic;
        bool cachedCASRam;
        bool cachedCharRom;
        bool cachedGraphicsRead;
        bool cachedIO;
        bool cachedKernal;
        bool cachedRomHi;
        bool cachedRomLo;
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
        int vaddr;
        bool va12;
        bool va13;
        bool va14;
        bool va15;
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

        public void Precache() 
        {
            // PLA line information is from the PDF titled "The C64 PLA Dissected"
            // Written by Thomas 'skoe' Giesel.

            addr = InputAddress();
            aec = InputAEC();
            ba = InputBA();
            cas = InputCAS();
            charen = InputCharen();
            exrom = InputExRom();
            game = InputGame();
            loram = InputLoRam();
            hiram = InputHiRam();
            read = InputRead();
            vaddr = InputVA();

            a15 = (addr & 0x08000) != 0;
            a14 = (addr & 0x04000) != 0;
            a13 = (addr & 0x02000) != 0;
            a12 = (addr & 0x01000) != 0;
            va15 = (vaddr & 0x08000) != 0;
            va14 = (vaddr & 0x04000) != 0;
            va13 = (vaddr & 0x02000) != 0;
            va12 = (vaddr & 0x01000) != 0;

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

            cachedCASRam = p0 || p1 || p2 || p3 || p4 || p5 || p6 || p7 || p9 || p10 || p11 || p12 || p13 || p14 || p15 || p16 || p17 || p18 || p19 || p20 || p21 || p22 || p23 || p24 || p25 || p26 || p27 || p28 || p30;
            cachedBasic = !p0;
            cachedKernal = !(p1 || p2);
            cachedCharRom = !(p3 || p4 || p5 || p6 || p7);
            cachedGraphicsRead = !p31;
            cachedIO = !(p9 || p10 || p11 || p12 || p13 || p14 || p15 || p16 || p17 || p18);
            cachedRomLo = !(p19 || p20);
            cachedRomHi = !(p21 || p22 || p23);
        }
    }
}
