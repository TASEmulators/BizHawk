using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NDS
{
	public class MelonDSGLTextureProvider : IGLTextureProvider
	{
		private readonly IVideoProvider _vp;
		private readonly LibMelonDS _core;
		private readonly Action _activateGLContextCallback;

		internal bool VideoDirty;

		internal MelonDSGLTextureProvider(IVideoProvider vp, LibMelonDS core, Action activateGLContextCallback)
		{
			_vp = vp;
			_core = core;
			_activateGLContextCallback = activateGLContextCallback;
		}

		public int GetGLTexture()
			=> _core.GetGLTexture();

		public int[] GetVideoBuffer()
		{
			var vb = _vp.GetVideoBuffer();
			if (VideoDirty)
			{
				_activateGLContextCallback();
				_core.ReadFrameBuffer(vb);
				VideoDirty = false;
			}

			return vb;
		}

		public int VirtualWidth => _vp.BufferWidth;
		public int VirtualHeight => _vp.BufferHeight;
		public int BufferWidth => _vp.BufferWidth;
		public int BufferHeight => _vp.BufferHeight;
		public int VsyncNumerator => _vp.VsyncNumerator;
		public int VsyncDenominator => _vp.VsyncDenominator;
		public int BackgroundColor => _vp.BackgroundColor;
	}
}
