using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Sony.PSP
{
	public partial class PPSSPP : IVideoProvider
	{
		static int BW = 480;
		static int BH = 270;
		internal bool VideoDirty;

		protected readonly IntPtr _context;
		
		public PPSSPP(LibPPSSPP core, IntPtr context)
		{
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

		private int[] _vbuf = new int[BW * BH];

		public int[] GetVideoBuffer()
		{
			/*
			if (VideoDirty)
			{
				if (_vbuf.Length < BW * BH)
				{
					_vbuf = new int[BW * BH];
				}

				_core.PPSSPP_ReadFrameBuffer(_context, _vbuf);
				VideoDirty = false;
			}
			*/

			_libPPSSPP.GetVideo(_vbuf);
			return _vbuf;
		}
	}
}