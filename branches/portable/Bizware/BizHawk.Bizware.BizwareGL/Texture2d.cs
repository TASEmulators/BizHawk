using System;
using System.Drawing;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

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
		
		//note.. this was a lame idea. convenient, but weird. lets just change this back to ints.
		public float Width { get; private set; }
		public float Height { get; private set; }

		public int IntWidth { get { return (int)Width; } }
		public int IntHeight { get { return (int)Height; } }
		public Size Size { get { return new Size(IntWidth, IntHeight); } }
	}
}