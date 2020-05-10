//public domain assumed from cyotek.com

namespace Cyotek.Drawing.BitmapFont
{
  public struct Kerning
  {
    #region  Public Constructors

    public Kerning(char firstCharacter, char secondCharacter, int amount)
      : this()
    {
      this.FirstCharacter = firstCharacter;
      this.SecondCharacter = secondCharacter;
      this.Amount = amount;
    }

    #endregion  Public Constructors

    #region  Public Methods

    public override string ToString()
    {
      return string.Format("{0} to {1} = {2}", this.FirstCharacter, this.SecondCharacter, this.Amount);
    }

    #endregion  Public Methods

    #region  Public Properties

    public char FirstCharacter { get; set; }

    public char SecondCharacter { get; set; }

    public int Amount { get; set; }

    #endregion  Public Properties
  }
}
