//public domain assumed from cyotek.com

using System.Drawing;

namespace Cyotek.Drawing.BitmapFont
{
  public struct Character
  {


    public override string ToString()
    {
      return this.Char.ToString();
    }





    public int Channel { get; set; }

    public Rectangle Bounds { get; set; }

    public Point Offset { get; set; }

    public char Char { get; set; }

    public int TexturePage { get; set; }

    public int XAdvance { get; set; }


  }
}
