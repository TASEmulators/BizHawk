using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Sony.PSP
{
	public partial class PPSSPP : IVideoProvider
	{
		static int BW = 480;
		static int BH = 270;
		internal bool VideoDirty;

		protected readonly IntPtr _context;

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
#if false
			if (VideoDirty)
			{
				if (_vbuf.Length < BW * BH)
				{
					_vbuf = new int[BW * BH];
				}

				_core.PPSSPP_ReadFrameBuffer(_context, _vbuf);
				VideoDirty = false;
			}
#endif

			_libPPSSPP.GetVideo(_vbuf);
			return _vbuf;
		}
	}
}
