using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.N3DS
{
	public class EncoreVideoProvider : IVideoProvider
	{
		internal int BW = 400;
		internal int BH = 480;
		internal bool VideoDirty;

		protected readonly LibEncore _core;
		protected readonly IntPtr _context;
		
		public EncoreVideoProvider(LibEncore core, IntPtr context)
		{
			_core = core;
			_context = context;
		}

		// ReSharper disable ConvertToAutoPropertyWhenPossible
		public int VirtualWidth => BW;
		public int VirtualHeight => BH;
		public int BufferWidth => BW;
		public int BufferHeight => BH;
		public int VsyncNumerator => 268111856;
		public int VsyncDenominator => 4481136;
		public int BackgroundColor => 0;

		private int[] _vbuf = new int[400 * 480];

		public int[] GetVideoBuffer()
		{
			if (VideoDirty)
			{
				if (_vbuf.Length < BW * BH)
				{
					_vbuf = new int[BW * BH];
				}

				_core.Encore_ReadFrameBuffer(_context, _vbuf);
				VideoDirty = false;
			}

			return _vbuf;
		}
	}
	
	public class EncoreGLTextureProvider : EncoreVideoProvider, IGLTextureProvider
	{
		public EncoreGLTextureProvider(LibEncore core, IntPtr context)
			: base(core, context)
		{
		}

		public int GetGLTexture()
			=> _core.Encore_GetGLTexture(_context);
	}
}