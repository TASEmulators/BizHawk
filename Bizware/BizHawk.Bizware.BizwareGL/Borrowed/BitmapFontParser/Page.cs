//public domain assumed from cyotek.com

using System.IO;

namespace Cyotek.Drawing.BitmapFont
{
  public struct Page
  {
    #region  Public Constructors

    public Page(int id, string fileName)
      : this()
    {
      this.FileName = fileName;
      this.Id = id;
    }

    #endregion  Public Constructors

    #region  Public Methods

    public override string ToString()
    {
      return string.Format("{0} ({1})", this.Id, Path.GetFileName(this.FileName));
    }

    #endregion  Public Methods

    #region  Public Properties

    public string FileName { get; set; }

    public int Id { get; set; }

    #endregion  Public Properties
  }
}
