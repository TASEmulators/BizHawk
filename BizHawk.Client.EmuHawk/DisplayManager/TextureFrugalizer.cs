using System;
using System.Collections.Generic;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Client.Common;

using BizHawk.Bizware.BizwareGL;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// Recycles a pair of temporary textures (in case double-buffering helps any) to contain a BitmapBuffer's or DisplaySurface's contents, as long as the dimensions match.
	/// When the dimensions dont match, a new one will be allocated
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
				if(ct != null)
					ct.Dispose();
			ResetList();
		}

		void ResetList()
		{
			CurrentTextures = new List<Texture2d>();
			CurrentTextures.Add(null);
			CurrentTextures.Add(null);
		}

		IGL GL;
		List<Texture2d> CurrentTextures;

		public Texture2d Get(DisplaySurface ds)
		{
			using (var bb = new BitmapBuffer(ds.PeekBitmap(), new BitmapLoadOptions()))
			{
				return Get(bb);
			}
		}
		public Texture2d Get(BitmapBuffer bb)
		{
			//get the current entry
			Texture2d CurrentTexture = CurrentTextures[0];

			//TODO - its a bit cruddy here that we dont respect the current texture HasAlpha condition (in fact, theres no such concept)
			//we might need to deal with that in the future to fix some bugs.

			//check if its rotten and needs recreating
			if (CurrentTexture == null || CurrentTexture.IntWidth != bb.Width || CurrentTexture.IntHeight != bb.Height)
			{
				//needs recreating. be sure to kill the old one...
				if (CurrentTexture != null)
					CurrentTexture.Dispose();
				//and make a new one
				CurrentTexture = GL.LoadTexture(bb);
			}
			else
			{
				//its good! just load in the data
				GL.LoadTextureData(CurrentTexture, bb);
			}

			//now shuffle the buffers
			CurrentTextures[0] = CurrentTextures[1];
			CurrentTextures[1] = CurrentTexture;

			//deterministic state, i guess
			CurrentTexture.SetFilterNearest();

			return CurrentTexture;
		}
	}
}