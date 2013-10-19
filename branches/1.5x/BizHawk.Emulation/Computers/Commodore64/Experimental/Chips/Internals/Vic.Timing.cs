using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.Experimental.Chips.Internals
{
    sealed public class VicColumnState
    {
        public VicBAType BA;
        public VicFetchType Fetch;
        public bool HBlank;
        public int RasterX;
    }

    public enum VicActType
    {
        None,
        SpriteDMA,
        SpriteExpandY,
        RCAdvance,
        RasterAdvance,
        RasterAdvanceBottom,
        VCReset,
    }

    public enum VicBAType
    {
        None,
        Badline,
        Sprite0,
        Sprite01,
        Sprite012,
        Sprite12,
        Sprite123,
        Sprite23,
        Sprite234,
        Sprite34,
        Sprite345,
        Sprite45,
        Sprite456,
        Sprite56,
        Sprite567,
        Sprite67,
        Sprite7
    }

    public enum VicFetchType
    {
        None,
        Graphics,
        Color,
        Idle,
        Refresh,
        Sprite,
        Pointer
    }

    public enum VicRowType
    {
        None,
        ScreenVisible,
        ScreenBlank,
        ResetVCBase
    }

    sealed public class VicTiming
    {
        public int ColumnCount;
        public int DelayColumn;
        public int RasterAdvanceColumn;
        public int RasterCount;
        public int RasterWidth;
    }
    sealed public partial class Vic
    {
        int frequency;
        VicColumnState[] pipelineColumns;
        VicRowType[] pipelineRows;
        int rasterCount;
        int rasterWidth;
    }
}
