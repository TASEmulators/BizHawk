using BizHawk.Bizware.Graphics;
using BizHawk.Emulation.Common;
using Silk.NET.OpenGL;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// Provides a way for a core to use OpenGL
	/// </summary>
	public class OpenGLProvider : IOpenGLProvider
	{
		public bool SupportsGLVersion(int major, int minor)
			=> OpenGLVersion.SupportsVersion(major, minor);

		public object RequestGLContext(int major, int minor, bool coreProfile)
		{
			var ret = new SDL2OpenGLContext(major, minor, coreProfile);
			ret.SetVsync(false);
			return ret;
		}

		public void ReleaseGLContext(object context)
			=> ((SDL2OpenGLContext)context).Dispose();

		public void ActivateGLContext(object context)
			=> ((SDL2OpenGLContext)context).MakeContextCurrent();

		public void DeactivateGLContext()
			=> SDL2OpenGLContext.MakeNoneCurrent();

		public IntPtr GetGLProcAddress(string proc)
			=> SDL2OpenGLContext.GetGLProcAddress(proc);

		public IFboWithTexture GetOpenGLFBOWithTexture(int width, int height, bool depth)
		{
			return new FboWithTexture(new OpenGLFBOWithTexture(width, height, depth));
		}

		public void ReadFBO(IFboWithTexture fbo, int width, int height, Span<int> dest)
		{
			using var gl = GL.GetApi(GetGLProcAddress);
			fbo.Bind();
			gl.ReadPixels(0, 0, (uint)width, (uint)height, PixelFormat.Bgra, PixelType.UnsignedByte, dest);

			// flip the image vertically
			Span<int> tempRow = stackalloc int[width];
			for (int i = 0; i < height / 2; i++)
			{
				var row1 = dest.Slice(i * width, width);
				var row2 = dest.Slice((height - i - 1) * width, width);
				row1.CopyTo(tempRow);
				row2.CopyTo(row1);
				tempRow.CopyTo(row2);
			}
		}
	}

	internal class FboWithTexture : IFboWithTexture
	{
		private readonly IFBOWithTexture _ifboWithTextureImplementation;

		public FboWithTexture(IFBOWithTexture renderTarget) => _ifboWithTextureImplementation = renderTarget;

		public uint FBO => _ifboWithTextureImplementation.FBO;
		public uint TextureId => _ifboWithTextureImplementation.TexID;

		public void Bind() => _ifboWithTextureImplementation.Bind();
		public void Dispose() => _ifboWithTextureImplementation.Dispose();
	}
}
