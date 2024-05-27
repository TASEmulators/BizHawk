//http://www.angelcode.com/products/bmfont/
//http://cyotek.com/blog/angelcode-bitmap-font-parsing-using-csharp

using System;
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
			for (var i=0; i<FontInfo.Pages.Length; i++)
			{
				TexturePages.Add(owner.LoadTexture(textures[i]));
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

		public SizeF Measure(string str)
		{
			float x = 0;
			float y = FontInfo.LineHeight;
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
					y += FontInfo.LineHeight;
					continue;
				}

				var bfc = FontInfo[c];
				x += bfc.XAdvance;
			}

			return new(Math.Max(x, ox), y);
		}

		public void RenderString(IGuiRenderer renderer, float x, float y, string str)
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
					y += FontInfo.LineHeight;
					continue;
				}

				var bfc = FontInfo[c];

				// calculate texcoords (we shouldve already had this cached, but im speedcoding now)
				var tex = TexturePages[bfc.TexturePage];
				var w = (float)tex.Width;
				var h = (float)tex.Height;
				var bounds = new Rectangle(bfc.X, bfc.Y, bfc.Width, bfc.Height);
				var u0 = bounds.Left / w;
				var v0 = bounds.Top / h;
				var u1 = bounds.Right / w;
				var v1 = bounds.Bottom / h;

				var gx = x + bfc.XOffset;
				var gy = y + bfc.YOffset;
				renderer.DrawSubrect(tex, gx, gy, bfc.Width, bfc.Height, u0, v0, u1, v1);

				x += bfc.XAdvance;
			}
		}

		public IGL Owner { get; }

		private readonly BitmapFont FontInfo;
		private List<ITexture2D> TexturePages = new();
	}
}