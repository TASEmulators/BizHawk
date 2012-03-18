using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
//using Font = SlimDX.Direct3D9.Font;
using System.Drawing;
using SlimDX;
using SlimDX.Direct3D9;

namespace BizHawk.MultiClient.tools
{
    class LuaText
    {
        public int x;
        public int y;
//        public Font font;
        public Color color;
        public Color outline;
        public String message;
        //private Device device;

        public LuaText()
        {
            x = 0;
            y = 0;
//            font = new Font(device, 16, 0, FontWeight.Bold, 1, false, CharacterSet.Default, Precision.Default, FontQuality.Default, PitchAndFamily.Default | PitchAndFamily.DontCare, "Arial"); 
            color = Color.White;
            outline = Color.Black;
            message = "";
        }
    }
}
