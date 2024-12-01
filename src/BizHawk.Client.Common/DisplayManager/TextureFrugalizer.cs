using System.Collections.Generic;

using BizHawk.Bizware.Graphics;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// Recycles a pair of temporary textures (in case double-buffering helps any) to contain a BitmapBuffer's contents, as long as the dimensions match.
	/// When the dimensions don't match, a new one will be allocated
	/// </summary>
	public class TextureFrugalizer : IDisposable
	{
		public TextureFrugalizer(IGL gl)
		{
			_gl = gl;
			ResetList();
		}

		public void Dispose()
		{
			foreach (var ct in _currentTextures)
			{
				ct?.Dispose();
			}

			ResetList();
		}

		private void ResetList()
		{
			_currentTextures = new List<ITexture2D> { null, null };
		}

		private readonly IGL _gl;
		private List<ITexture2D> _currentTextures;

		public ITexture2D Get(BitmapBuffer bb)
		{
			//get the current entry
			var currentTexture = _currentTextures[0];

			// TODO - its a bit cruddy here that we don't respect the current texture HasAlpha condition (in fact, there's no such concept)
			// we might need to deal with that in the future to fix some bugs.

			//check if its rotten and needs recreating
			if (currentTexture == null || currentTexture.Width != bb.Width || currentTexture.Height != bb.Height)
			{
				//needs recreating. be sure to kill the old one...
				currentTexture?.Dispose();
				//and make a new one
				currentTexture = _gl.LoadTexture(bb);
			}
			else
			{
				//its good! just load in the data
				currentTexture.LoadFrom(bb);
			}

			//now shuffle the buffers
			_currentTextures[0] = _currentTextures[1];
			_currentTextures[1] = currentTexture;

			//deterministic state, i guess
			currentTexture.SetFilterNearest();

			return currentTexture;
		}
	}
}