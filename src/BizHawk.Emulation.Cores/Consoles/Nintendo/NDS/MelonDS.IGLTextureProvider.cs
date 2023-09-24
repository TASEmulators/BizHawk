using System;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.NDS
{
	public class MelonDSGLTextureProvider : IGLTextureProvider
	{
		private readonly IVideoProvider _vp;
		private readonly LibMelonDS _core;
		private readonly Action _activateGLContextCallback;
		private readonly int[] _vbuf = new int[256 * 16 * 384 * 16];

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
			if (VideoDirty)
			{
				_activateGLContextCallback();
				_core.ReadFrameBuffer(_vbuf);
				VideoDirty = false;
			}

			return _vbuf;
		}

		public int VirtualWidth => 256;
		public int VirtualHeight => 384;
		public int BufferWidth => _vp.BufferWidth;
		public int BufferHeight => _vp.BufferHeight;
		public int VsyncNumerator => _vp.VsyncNumerator;
		public int VsyncDenominator => _vp.VsyncDenominator;
		public int BackgroundColor => _vp.BackgroundColor;
	}
}
