using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Sony.PSP
{
	public class PPSSPPVideoProvider : IVideoProvider
	{
		internal int BW = 400;
		internal int BH = 480;
		internal bool VideoDirty;

		protected readonly LibPPSSPP _core;
		protected readonly IntPtr _context;
		
		public PPSSPPVideoProvider(LibPPSSPP core, IntPtr context)
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

				_core.PPSSPP_ReadFrameBuffer(_context, _vbuf);
				VideoDirty = false;
			}

			return _vbuf;
		}
	}
	
}