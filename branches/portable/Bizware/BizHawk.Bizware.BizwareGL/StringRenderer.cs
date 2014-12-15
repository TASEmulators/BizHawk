//http://www.angelcode.com/products/bmfont/
//http://cyotek.com/blog/angelcode-bitmap-font-parsing-using-csharp

using System;
using sd=System.Drawing;
using System.Collections.Generic;
using System.IO;

namespace BizHawk.Bizware.BizwareGL
{
	public class StringRenderer : IDisposable
	{
		public StringRenderer(IGL owner, Stream xml, params Stream[] textures)
		{
			Owner = owner;
			FontInfo = Cyotek.Drawing.BitmapFont.BitmapFontLoader.LoadFontFromXmlFile(xml);
			
			//load textures
			for(int i=0;i<FontInfo.Pages.Length;i++)
			{
				TexturePages.Add(owner.LoadTexture(textures[i]));
			}
		}

		public void Dispose()
		{
			foreach (var tex in TexturePages)
				tex.Dispose();
			TexturePages = null;
		}

		public sd.SizeF Measure(string str)
		{
			float x = 0;
			int len = str.Length;
			for (int i = 0; i < len; i++)
			{
				Cyotek.Drawing.BitmapFont.Character c;
				if (!FontInfo.Characters.TryGetValue(str[i], out c))
					c = FontInfo.Characters[unchecked((char)-1)];

				x += c.XAdvance;
			}

			return new sd.SizeF(x, FontInfo.LineHeight);
		}

		public void RenderString(IGuiRenderer renderer, float x, float y, string str)
		{
			int len = str.Length;
			for (int i = 0; i < len; i++)
			{
				Cyotek.Drawing.BitmapFont.Character c;
				if (!FontInfo.Characters.TryGetValue(str[i], out c))
					c = FontInfo.Characters[unchecked((char)-1)];
				
				//calculate texcoords (we shouldve already had this cached, but im speedcoding now)
				Texture2d tex = TexturePages[c.TexturePage];
				float w = tex.Width;
				float h = tex.Height;
				float u0 = c.Bounds.Left / w;
				float v0 = c.Bounds.Top / h;
				float u1 = c.Bounds.Right / w;
				float v1 = c.Bounds.Bottom / h;

				float gx = x + c.Offset.X;
				float gy = y + c.Offset.Y;
				renderer.DrawSubrect(tex, gx, gy, c.Bounds.Width, c.Bounds.Height, u0, v0, u1, v1);

				x += c.XAdvance;
			}
		}

		public IGL Owner { get; private set; }

		Cyotek.Drawing.BitmapFont.BitmapFont FontInfo;
		List<Texture2d> TexturePages = new List<Texture2d>();

	}
}