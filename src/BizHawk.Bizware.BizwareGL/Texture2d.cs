using System;
using System.Drawing;

namespace BizHawk.Bizware.BizwareGL
{
	/// <summary>
	/// A full-scale 2D texture, with mip levels and everything.
	/// In OpenGL tradition, this encapsulates the sampler state, as well, which is equal parts annoying and convenient
	/// </summary>
	public class Texture2d : IDisposable
	{
		/// <summary>
		/// resolves the texture into a new BitmapBuffer
		/// </summary>
		public BitmapBuffer Resolve()
		{
			return Owner.ResolveTexture2d(this);
		}

		public void Dispose()
		{
			Owner.FreeTexture(this);
			Opaque = null;
		}

		public Texture2d(IGL owner, object opaque, int width, int height)
		{
			Owner = owner;
			Opaque = opaque;
			Width = width;
			Height = height;
		}

		public override string ToString()
		{
			return $"GL Tex: {Width}x{Height}";
		}

		public void LoadFrom(BitmapBuffer buffer)
		{
		}

		public void SetMinFilter(TextureMinFilter minFilter) => Owner.SetMinFilter(this, minFilter);

		public void SetMagFilter(TextureMagFilter magFilter) => Owner.SetMagFilter(this, magFilter);

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

		public IGL Owner { get; }
		public object Opaque { get; private set; }
		
		// note.. this was a lame idea. convenient, but weird. lets just change this back to ints.
		public float Width { get; }
		public float Height { get; }

		public int IntWidth => (int)Width;
		public int IntHeight => (int)Height;
		public Rectangle Rectangle => new Rectangle(0, 0, IntWidth, IntHeight);
		public Size Size => new Size(IntWidth, IntHeight);

		/// <summary>
		/// opengl sucks, man. seriously, screw this (textures from render targets are upside down)
		/// (couldn't we fix it up in the matrices somewhere?)
		/// </summary>
		public bool IsUpsideDown;
	}
}