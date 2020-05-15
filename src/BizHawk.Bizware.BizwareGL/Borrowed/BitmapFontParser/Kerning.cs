//public domain assumed from cyotek.com

namespace Cyotek.Drawing.BitmapFont
{
  public struct Kerning
  {
    public Kerning(char firstCharacter, char secondCharacter, int amount)
      : this()
    {
      this.FirstCharacter = firstCharacter;
      this.SecondCharacter = secondCharacter;
      this.Amount = amount;
    }

    public override string ToString()
    {
      return string.Format("{0} to {1} = {2}", this.FirstCharacter, this.SecondCharacter, this.Amount);
    }

    public char FirstCharacter { get; set; }

    public char SecondCharacter { get; set; }

    public int Amount { get; set; }
  }
}
