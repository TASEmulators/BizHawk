using System;
using System.Drawing;
using System.Collections.Generic;

using BizHawk.Bizware.BizwareGL;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// Recycles a pair of temporary render targets, as long as the dimensions match.
	/// When the dimensions don't match, a new one will be allocated
	/// </summary>
	public class RenderTargetFrugalizer : IDisposable
	{
		public RenderTargetFrugalizer(IGL gl)
		{
			_gl = gl;
			ResetList();
		}

		public void Dispose()
		{
			foreach (var ct in _currentRenderTargets)
			{
				ct?.Dispose();
			}

			ResetList();
		}

		private void ResetList()
		{
			_currentRenderTargets = new List<RenderTarget> { null, null };
		}

		private readonly IGL _gl;
		private List<RenderTarget> _currentRenderTargets;

		public RenderTarget Get(Size dimensions)
		{
			return Get(dimensions.Width, dimensions.Height);
		}

		public RenderTarget Get(int width, int height)
		{
			//get the current entry
			RenderTarget currentRenderTarget = _currentRenderTargets[0];

			//check if its rotten and needs recreating
			if (currentRenderTarget == null
				|| currentRenderTarget.Texture2d.IntWidth != width
				|| currentRenderTarget.Texture2d.IntHeight != height)
			{
				// needs recreating. be sure to kill the old one...
				currentRenderTarget?.Dispose();
				// and make a new one
				currentRenderTarget = _gl.CreateRenderTarget(width, height);
			}
			else
			{
				// its good! nothing more to do
			}

			// now shuffle the buffers
			_currentRenderTargets[0] = _currentRenderTargets[1];
			_currentRenderTargets[1] = currentRenderTarget;

			return currentRenderTarget;
		}
	}
}