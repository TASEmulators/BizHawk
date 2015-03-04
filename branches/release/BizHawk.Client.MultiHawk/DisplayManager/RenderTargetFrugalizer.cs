using System;
using System.Collections.Generic;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Client.Common;

using BizHawk.Bizware.BizwareGL;

namespace BizHawk.Client.MultiHawk
{
	/// <summary>
	/// Recycles a pair of temporary render targets, as long as the dimensions match.
	/// When the dimensions dont match, a new one will be allocated
	/// </summary>
	class RenderTargetFrugalizer : IDisposable
	{
		public RenderTargetFrugalizer(IGL gl)
		{
			GL = gl;
			ResetList();
		}

		public void Dispose()
		{
			foreach (var ct in CurrentRenderTargets)
				if (ct != null)
					ct.Dispose();
			ResetList();
		}

		void ResetList()
		{
			CurrentRenderTargets = new List<RenderTarget>();
			CurrentRenderTargets.Add(null);
			CurrentRenderTargets.Add(null);
		}

		IGL GL;
		List<RenderTarget> CurrentRenderTargets;

		public RenderTarget Get(System.Drawing.Size dimensions) { return Get(dimensions.Width, dimensions.Height); }
		public RenderTarget Get(int width, int height)
		{
			//get the current entry
			RenderTarget CurrentRenderTarget = CurrentRenderTargets[0];

			//check if its rotten and needs recreating
			if (CurrentRenderTarget == null || CurrentRenderTarget.Texture2d.IntWidth != width || CurrentRenderTarget.Texture2d.IntHeight != height)
			{
				//needs recreating. be sure to kill the old one...
				if (CurrentRenderTarget != null)
					CurrentRenderTarget.Dispose();
				//and make a new one
				CurrentRenderTarget = GL.CreateRenderTarget(width, height);
			}
			else
			{
				//its good! nothing more to do
			}

			//now shuffle the buffers
			CurrentRenderTargets[0] = CurrentRenderTargets[1];
			CurrentRenderTargets[1] = CurrentRenderTarget;

			return CurrentRenderTarget;
		}
	}
}