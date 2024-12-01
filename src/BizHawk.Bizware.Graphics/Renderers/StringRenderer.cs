// https://www.angelcode.com/products/bmfont/
// https://devblog.cyotek.com/post/angelcode-bitmap-font-parsing-using-csharp

using System.Collections.Generic;
using System.Drawing;
using System.IO;

using Cyotek.Drawing.BitmapFont;

namespace BizHawk.Bizware.Graphics
{
	public class StringRenderer : IDisposable
	{
		public StringRenderer(IGL owner, Stream xml, params Stream[] textures)
		{
			Owner = owner;
			FontInfo = new();
			FontInfo.LoadXml(xml);
			
			// load textures
			for (var i = 0; i < FontInfo.Pages.Length; i++)
			{
				TexturePages.Add(owner.LoadTexture(textures[i]));
			}

			// precalc texcoords
			foreach (var bfc in FontInfo)
			{
				var tex = TexturePages[bfc.TexturePage];
				var w = (float)tex.Width;
				var h = (float)tex.Height;
				var bounds = new Rectangle(bfc.X, bfc.Y, bfc.Width, bfc.Height);
				var u0 = bounds.Left / w;
				var v0 = bounds.Top / h;
				var u1 = bounds.Right / w;
				var v1 = bounds.Bottom / h;
				CharTexCoords.Add(bfc.Char, new(u0, v0, u1, v1));
			}
		}

		public void Dispose()
		{
			foreach (var tex in TexturePages)
			{
				tex.Dispose();
			}

			TexturePages = null;
		}

		public SizeF Measure(string str, float scale)
		{
			float x = 0;
			float y = FontInfo.LineHeight * scale;
			var ox = x;
			var len = str.Length;

			for (var i = 0; i < len; i++)
			{
				var c = str[i];

				if (c == '\r')
				{
					if (i != len - 1 && str[i + 1] == '\n')
					{
						i++;
					}
				}

				if (c == '\r')
				{
					c = '\n';
				}

				if (c == '\n')
				{
					if (x > ox)
					{
						ox = x;
					}

					x = 0;
					y += FontInfo.LineHeight * scale;
					continue;
				}

				var bfc = FontInfo[c];
				x += bfc.XAdvance * scale;
			}

			return new(Math.Max(x, ox), y);
		}

		public void RenderString(IGuiRenderer renderer, float x, float y, string str, float scale)
		{
			if (Owner != renderer.Owner)
			{
				throw new InvalidOperationException("Owner mismatch!");
			}

			var ox = x;
			var len = str.Length;

			for (var i = 0; i < len; i++)
			{
				var c = str[i];

				if (c == '\r')
				{
					if (i != len - 1 && str[i + 1] == '\n')
					{
						i++;
					}
				}

				if (c == '\r')
				{
					c = '\n';
				}

				if (c == '\n')
				{
					x = ox;
					y += FontInfo.LineHeight * scale;
					continue;
				}

				var bfc = FontInfo[c];
				var tex = TexturePages[bfc.TexturePage];
				var gx = x + bfc.XOffset * scale;
				var gy = y + bfc.YOffset * scale;
				var charTexCoords = CharTexCoords[bfc.Char];
				renderer.DrawSubrect(tex, gx, gy, bfc.Width * scale, bfc.Height * scale,
					charTexCoords.U0, charTexCoords.V0, charTexCoords.U1, charTexCoords.V1);

				x += bfc.XAdvance * scale;
			}
		}

		public IGL Owner { get; }

		private readonly BitmapFont FontInfo;
		private List<ITexture2D> TexturePages = [ ];
		private readonly Dictionary<char, TexCoords> CharTexCoords = [ ];

		/// <remarks>TODO can this be a struct? it's only 16o and only used here, in the above dict</remarks>
		private sealed record class TexCoords(float U0, float V0, float U1, float V1);
	}
}
