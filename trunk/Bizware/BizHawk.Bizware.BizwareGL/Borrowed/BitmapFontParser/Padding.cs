//public domain assumed from cyotek.com

namespace Cyotek.Drawing.BitmapFont
{
  public struct Padding
  {
    #region  Public Constructors

    public Padding(int left, int top, int right, int bottom)
      : this()
    {
      this.Top = top;
      this.Left = left;
      this.Right = right;
      this.Bottom = bottom;
    }

    #endregion  Public Constructors

    #region  Public Methods

    public override string ToString()
    {
      return string.Format("{0}, {1}, {2}, {3}", this.Left, this.Top, this.Right, this.Bottom);
    }

    #endregion  Public Methods

    #region  Public Properties

    public int Top { get; set; }

    public int Left { get; set; }

    public int Right { get; set; }

    public int Bottom { get; set; }

    #endregion  Public Properties
  }
}
