using System;
using System.Drawing;

namespace BizHawk.MultiClient.tools
{
    class LuaText
    {
        public int X;
        public int Y;
        public Color Color;
        public Color Outline;
        public String Message;

        public LuaText()
        {
            X = 0;
            Y = 0;
            Color = Color.White;
            Outline = Color.Black;
            Message = "";
        }
    }
}
