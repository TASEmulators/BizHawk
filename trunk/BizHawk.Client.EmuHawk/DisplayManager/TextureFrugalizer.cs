using System;
using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Client.Common;

using BizHawk.Bizware.BizwareGL;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// Recycles a temporary texture to contain a BitmapBuffer's or DisplaySurface's contents, as long as the dimensions match.
	/// When the dimensions dont match, a new one will be allocated
	/// </summary>
	class TextureFrugalizer : IDisposable
	{
		public TextureFrugalizer(IGL gl)
		{
			GL = gl;
		}

		public void Dispose()
		{
			if (CurrentTexture != null)
			{
				CurrentTexture.Dispose();
				CurrentTexture = null;
			}
		}

		IGL GL;
		Texture2d CurrentTexture;

		public Texture2d Get(DisplaySurface ds)
		{
			using (var bb = new BitmapBuffer(ds.PeekBitmap(), new BitmapLoadOptions()))
			{
				return Get(bb);
			}
		}
		public Texture2d Get(BitmapBuffer bb)
		{
			if (CurrentTexture == null || CurrentTexture.IntWidth != bb.Width || CurrentTexture.IntHeight != bb.Height)
			{
				if (CurrentTexture != null)
					CurrentTexture.Dispose();
				CurrentTexture = GL.LoadTexture(bb);
			}
			else
			{
				GL.LoadTextureData(CurrentTexture, bb);
			}

			return CurrentTexture;
		}
	}
}