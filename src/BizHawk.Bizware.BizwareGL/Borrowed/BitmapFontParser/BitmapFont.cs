using System.Collections;
using System.Collections.Generic;
using System.Drawing;

// public domain assumed from cyotek.com
namespace Cyotek.Drawing.BitmapFont
{
	public class BitmapFont : IEnumerable<Character>
	{
		public IEnumerator<Character> GetEnumerator()
		{
			foreach (KeyValuePair<char, Character> pair in Characters)
			{
				yield return pair.Value;
			}
		}

		public int AlphaChannel { get; set; }

		public int BaseHeight { get; set; }

		public int BlueChannel { get; set; }

		public bool Bold { get; set; }

		public IDictionary<char, Character> Characters { get; set; }

		public string Charset { get; set; }

		public string FamilyName { get; set; }

		public int FontSize { get; set; }

		public int GreenChannel { get; set; }

		public bool Italic { get; set; }

		public IDictionary<Kerning, int> Kernings { get; set; }

		public int LineHeight { get; set; }

		public int OutlineSize { get; set; }

		public bool Packed { get; set; }

		public Padding Padding { get; set; }

		public Page[] Pages { get; set; }

		public int RedChannel { get; set; }

		public bool Smoothed { get; set; }

		public Point Spacing { get; set; }

		public int StretchedHeight { get; set; }

		public int SuperSampling { get; set; }

		public Size TextureSize { get; set; }

		public Character this[char character] => Characters[character];

		public bool Unicode { get; set; }

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}
}
