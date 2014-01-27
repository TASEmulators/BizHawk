using System;

namespace BizHawk.Bizware.BizwareGL
{
	/// <summary>
	/// A full-scale 2D texture, with mip levels and everything.
	/// In OpenGL tradition, this encapsulates the sampler state, as well, which is equal parts annoying and convenient
	/// </summary>
	public class Texture2d : IDisposable
	{
		//not sure if I need this idea.
		//public class Maker
		//{
		//  public Maker(Texture2d tex)
		//  {
		//    MyTexture = tex;
		//  }
		//  public void SetWidth(int width)
		//  {
		//    MyTexture.Width = width;
		//  }
		//  public void SetHeight(int width)
		//  {
		//    MyTexture.Height = height;
		//  }

		//  Texture2d MyTexture;
		//}

		public void Dispose()
		{
			Owner.FreeTexture(Id);
			Id = Owner.GetEmptyHandle();
		}

		public Texture2d(IGL owner, IntPtr id, int width, int height)
		{
			Owner = owner;
			Id = id;
			Width = width;
			Height = height;
		}

		public void LoadFrom(BitmapBuffer buffer)
		{
		}

		public void SetMinFilter(TextureMinFilter minFilter)
		{
			Owner.BindTexture2d(this);
			Owner.TexParameter2d(TextureParameterName.TextureMinFilter, (int)minFilter);
		}

		public void SetMagFilter(TextureMagFilter magFilter)
		{
			Owner.BindTexture2d(this);
			Owner.TexParameter2d(TextureParameterName.TextureMagFilter, (int)magFilter);
		}

		public void SetFilterLinear()
		{
			SetMinFilter(TextureMinFilter.Linear);
			SetMagFilter(TextureMagFilter.Linear);
		}

		public void SetFilterNearest()
		{
			SetMinFilter(TextureMinFilter.Nearest);
			SetMagFilter(TextureMagFilter.Nearest);
		}

		public IGL Owner { get; private set; }
		public IntPtr Id { get; private set; }
		
		//note.. it is commonly helpful to have these as floats, since we're more often using them for rendering than for raster logic
		public float Width { get; private set; }
		public float Height { get; private set; }

		public int IntWidth { get { return (int)Width; } }
		public int IntHeight { get { return (int)Height; } }
	}
}