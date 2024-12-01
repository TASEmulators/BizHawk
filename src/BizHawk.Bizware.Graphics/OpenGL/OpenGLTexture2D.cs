using Silk.NET.OpenGL;

namespace BizHawk.Bizware.Graphics
{
	internal class OpenGLTexture2D : ITexture2D
	{
		private readonly GL GL;
		public readonly uint TexID;

		public int Width { get; }
		public int Height { get; }
		public bool IsUpsideDown { get; }

		public OpenGLTexture2D(GL gl, int width, int height)
		{
			GL = gl;
			TexID = GL.GenTexture();

			GL.BindTexture(TextureTarget.Texture2D, TexID);
			unsafe
			{
				GL.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba8, (uint)width, (uint)height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, null);
			}

			// sensible defaults
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToEdge);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToEdge);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);

			Width = width;
			Height = height;
			IsUpsideDown = false;
		}

		public OpenGLTexture2D(GL gl, uint texID, int width, int height)
		{
			GL = gl;
			TexID = texID;
			Width = width;
			Height = height;
			IsUpsideDown = true;
		}

		public virtual void Dispose()
			=> GL.DeleteTexture(TexID);

		public unsafe BitmapBuffer Resolve()
		{
			GL.BindTexture(TextureTarget.Texture2D, TexID);

			var pixels = new int[Width * Height];
			fixed (int* p = pixels)
			{
				GL.GetTexImage(TextureTarget.Texture2D, 0, PixelFormat.Bgra, PixelType.UnsignedByte, p);
			}

			return new(Width, Height, pixels);
		}

		public unsafe void LoadFrom(BitmapBuffer buffer)
		{
			if (buffer.Width != Width || buffer.Height != Height)
			{
				throw new InvalidOperationException("BitmapBuffer dimensions do not match texture dimensions");
			}

			var bmpData = buffer.LockBits();
			try
			{
				GL.BindTexture(TextureTarget.Texture2D, TexID);
				GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, (uint)buffer.Width, (uint)buffer.Height, PixelFormat.Bgra, PixelType.UnsignedByte, bmpData.Scan0.ToPointer());
			}
			finally
			{
				buffer.UnlockBits(bmpData);
			}
		}

		public void SetFilterLinear()
		{
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
		}

		public void SetFilterNearest()
		{
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
		}

		public override string ToString()
			=> $"OpenGL Texture2D: {Width}x{Height}";
	}
}
