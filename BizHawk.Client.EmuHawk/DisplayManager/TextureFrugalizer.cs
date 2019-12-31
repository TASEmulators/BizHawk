using System;
using System.Collections.Generic;
using BizHawk.Bizware.BizwareGL;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// Recycles a pair of temporary textures (in case double-buffering helps any) to contain a BitmapBuffer's or DisplaySurface's contents, as long as the dimensions match.
	/// When the dimensions don't match, a new one will be allocated
	/// </summary>
	public class TextureFrugalizer : IDisposable
	{
		public TextureFrugalizer(IGL gl)
		{
			GL = gl;
			ResetList();
		}

		public void Dispose()
		{
			foreach (var ct in CurrentTextures)
			{
				ct?.Dispose();
			}

			ResetList();
		}

		void ResetList()
		{
			CurrentTextures = new List<Texture2d> { null, null };
		}

		private readonly IGL GL;
		private List<Texture2d> CurrentTextures;

		public Texture2d Get(DisplaySurface ds)
		{
			using var bb = new BitmapBuffer(ds.PeekBitmap(), new BitmapLoadOptions());
			return Get(bb);
		}
		public Texture2d Get(BitmapBuffer bb)
		{
			//get the current entry
			Texture2d currentTexture = CurrentTextures[0];

			// TODO - its a bit cruddy here that we dont respect the current texture HasAlpha condition (in fact, there's no such concept)
			// we might need to deal with that in the future to fix some bugs.

			//check if its rotten and needs recreating
			if (currentTexture == null || currentTexture.IntWidth != bb.Width || currentTexture.IntHeight != bb.Height)
			{
				//needs recreating. be sure to kill the old one...
				currentTexture?.Dispose();
				//and make a new one
				currentTexture = GL.LoadTexture(bb);
			}
			else
			{
				//its good! just load in the data
				GL.LoadTextureData(currentTexture, bb);
			}

			//now shuffle the buffers
			CurrentTextures[0] = CurrentTextures[1];
			CurrentTextures[1] = currentTexture;

			//deterministic state, i guess
			currentTexture.SetFilterNearest();

			return currentTexture;
		}
	}
}