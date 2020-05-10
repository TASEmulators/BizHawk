using System.IO;

// public domain assumed from cyotek.com
namespace Cyotek.Drawing.BitmapFont
{
	public struct Page
	{
		public Page(int id, string fileName)
		  : this()
		{
			FileName = fileName;
			Id = id;
		}

		public override string ToString()
		{
			return $"{Id} ({Path.GetFileName(FileName)})";
		}

		public string FileName { get; set; }

		public int Id { get; set; }
	}
}
