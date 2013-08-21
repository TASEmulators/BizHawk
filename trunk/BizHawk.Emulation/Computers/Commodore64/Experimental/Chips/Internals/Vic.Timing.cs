using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Experimental.Chips.Internals
{
    sealed public partial class Vic
    {
        enum FetchState
        {
            None,
            Idle,
            Graphics,
            Character,
            Refresh,
            Sprite,
            Pointer
        }
        FetchState fetchState;

        bool characterBA;
        int characterBAEnd;
        int characterBAStart;
        bool characterFetch;
        int characterFetchStart;
        bool graphicsFetch;
        bool hBlank;
        int hBlankDelay;
        int rasterAdvance;
        int rasterCount;
        int rasterDelay;
        int rasterWidth;
        bool refreshFetch;
        int refreshStart;
        int screenXEnd;
        int screenXStart;
        int screenYEnd;
        int screenYStart;
        bool vBlank;

        void InitTiming(VicTiming timing)
        {
            int spriteBAStart = timing.SpriteBAStart;

            for (int i = 0; i < 8; i++)
            {
                sprites[i].BAStart = spriteBAStart % timing.HSize;
                sprites[i].BAEnd = (spriteBAStart + 40) % timing.HSize;
                sprites[i].FetchStart = (spriteBAStart + 24) % timing.HSize;
            }

            characterBAStart = timing.CharacterBAStart % timing.HSize;
            characterBAEnd = (characterBAStart + 344) % timing.HSize;
            characterFetchStart = (characterFetchStart + 24) % timing.HSize;
            screenXStart = timing.HBlankEnd;
            screenXEnd = timing.HBlankStart;
            screenYStart = timing.VBlankEnd;
            screenYEnd = timing.VBlankStart;
            rasterWidth = timing.HSize;
            rasterAdvance = timing.LineStart;
            rasterCount = timing.VSize;
        }
    }

    sealed public class VicTiming
    {
        public int CharacterBAStart; //VMBA
        public int HBlankDelay;
        public int HBlankEnd; //HBLANK
        public int HBlankStart; //HBLANK
        public int HSize; //BOL
        public int LineStart; //VINC
        public int RefreshStart; //REFW
        public int SpriteBAStart; //SPBA
        public int VBlankEnd; //VBLANK
        public int VBlankStart; //VBLANK
        public int VSize; //VRESET
    }
}
