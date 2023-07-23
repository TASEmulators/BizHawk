using System;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo._3DS
{
	public class CitraVideoProvider : IVideoProvider
	{
		internal int Width = 400;
		internal int Height = 480;
		internal bool VideoDirty;

		protected readonly LibCitra _core;
		protected readonly IntPtr _context;
		
		public CitraVideoProvider(LibCitra core, IntPtr context)
		{
			_core = core;
			_context = context;
		}

		// ReSharper disable ConvertToAutoPropertyWhenPossible
		public int VirtualWidth => Width;
		public int VirtualHeight => Height;
		public int BufferWidth => Width;
		public int BufferHeight => Height;
		public int VsyncNumerator => 268111856;
		public int VsyncDenominator => 4481136;
		public int BackgroundColor => 0;

		private int[] _vbuf = new int[400 * 480];

		public int[] GetVideoBuffer()
		{
			if (VideoDirty)
			{
				if (_vbuf.Length < Width * Height)
				{
					_vbuf = new int[Width * Height];
				}

				_core.Citra_ReadFrameBuffer(_context, _vbuf);
				VideoDirty = false;
			}

			return _vbuf;
		}
	}
	
	public class CitraGLTextureProvider : CitraVideoProvider, IGLTextureProvider
	{
		public CitraGLTextureProvider(LibCitra core, IntPtr context)
			: base(core, context)
		{
		}

		public int GetGLTexture()
			=> _core.Citra_GetGLTexture(_context);
	}
}