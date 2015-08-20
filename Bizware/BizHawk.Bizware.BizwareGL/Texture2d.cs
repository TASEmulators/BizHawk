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
			return string.Format("GL Tex: {0}x{1}", Width, Height);
		}

		public void LoadFrom(BitmapBuffer buffer)
		{
		}

		public void SetMinFilter(TextureMinFilter minFilter)
		{
			Owner.TexParameter2d(this,TextureParameterName.TextureMinFilter, (int)minFilter);
		}

		public void SetMagFilter(TextureMagFilter magFilter)
		{
			Owner.TexParameter2d(this, TextureParameterName.TextureMagFilter, (int)magFilter);
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
		public object Opaque { get; private set; }
		
		//note.. this was a lame idea. convenient, but weird. lets just change this back to ints.
		public float Width { get; private set; }
		public float Height { get; private set; }

		public int IntWidth { get { return (int)Width; } }
		public int IntHeight { get { return (int)Height; } }
		public Rectangle Rectangle { get { return new Rectangle(0, 0, IntWidth, IntHeight); } }
		public Size Size { get { return new Size(IntWidth, IntHeight); } }

		/// <summary>
		/// opengl sucks, man. seriously, screw this (textures from render targets are upside down)
		/// (couldnt we fix it up in the matrices somewhere?)
		/// </summary>
		public bool IsUpsideDown;
	}
}