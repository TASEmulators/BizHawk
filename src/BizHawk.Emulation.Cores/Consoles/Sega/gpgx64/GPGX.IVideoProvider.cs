using BizHawk.Emulation.Common;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Consoles.Sega.gpgx
{
	public partial class GPGX : IVideoProvider
	{
		public int[] GetVideoBuffer() => _vidBuff;

		public int VirtualWidth { get; private set; }

		public int VirtualHeight { get; private set; }

		public int BufferWidth => _vwidth;

		public int BufferHeight => _vheight;

		public int BackgroundColor => unchecked((int)0xff000000);

		public int VsyncNumerator { get; }

		public int VsyncDenominator { get; }

		private int[] _vidBuff = [ ];
		private int _vwidth;
		private int _vheight;

		private void SetVirtualDimensions()
		{
			var widthHasOverscan = (_syncSettings.Overscan & LibGPGX.InitSettings.OverscanType.Horizontal) != 0;
			var heightHasOverscan = (_syncSettings.Overscan & LibGPGX.InitSettings.OverscanType.Vertical) != 0;
			var isPal = Region == DisplayType.PAL;
			bool isVdpPal = _syncSettings.ForceVDP switch
			{
				LibGPGX.ForceVDP.NTSC => false,
				LibGPGX.ForceVDP.PAL => true,
				_ => isPal
			};
			double videoSampleRate = isVdpPal ? 14750000.0 : 135000000.0 / 11.0;
			int clockRate = isPal ? 53203424 : 53693175;

			if (SystemId == VSystemID.Raw.GEN)
			{
				VirtualWidth = 320;
				VirtualHeight = 224;
				VirtualWidth += widthHasOverscan ? 28 : 0;
				VirtualHeight += heightHasOverscan ? (isPal ? 48 : 0) + 16 : 0;

				VirtualWidth = (int)(VirtualWidth * 4.0 * videoSampleRate / clockRate);
			}
			else
			{
				VirtualWidth = 256;
				VirtualHeight = 192;

				if (SystemId == VSystemID.Raw.GG && !_syncSettings.GGExtra)
				{
					VirtualWidth += widthHasOverscan ? 28 : -96;
					VirtualHeight += heightHasOverscan ? (isPal ? 96 : 48) : -48;
				}
				else
				{
					VirtualWidth += widthHasOverscan ? 28 : 0;
					VirtualHeight += heightHasOverscan ? (isPal ? 96 : 48) : 0;
				}

				VirtualWidth = (int)(VirtualWidth * 5.0 * videoSampleRate / clockRate);
			}
		}

		private void UpdateVideoInitial()
		{
			// hack: you should call update_video() here, but that gives you 256x192 on frame 0
			// and we know that genesis games will almost always be 320x224 immediately afterwards

			// we set more proper width/height fields in VirtualWidth/VirtualHeight
			// so we'll just use them for the buffer width/height
			// if that's wrong, it'll be fixed next frame

			_vwidth = VirtualWidth;
			_vheight = VirtualHeight;
			_vidBuff = new int[_vwidth * _vheight];
			for (var i = 0; i < _vidBuff.Length; i++)
			{
				_vidBuff[i] = unchecked((int)0xff000000);
			}
		}

		private unsafe void UpdateVideo()
		{
			if (Frame == 0)
			{
				UpdateVideoInitial();
				return;
			}

			using (_elf.EnterExit())
			{
				Core.gpgx_get_video(out var gpwidth, out var gpheight, out var gppitch, out var src);

				_vwidth = gpwidth;
				_vheight = gpheight;

				if (_settings.PadScreen320 && _vwidth < 320)
					_vwidth = 320;

				var xpad = (_vwidth - gpwidth) / 2;
				var xpad2 = _vwidth - gpwidth - xpad;

				if (_vidBuff.Length < _vwidth * _vheight)
				{
					_vidBuff = new int[_vwidth * _vheight];
				}

				var rinc = (gppitch / 4) - gpwidth;
				fixed (int* pdst_ = _vidBuff)
				{
					var pdst = pdst_;
					var psrc = (int*)src;

					for (var j = 0; j < gpheight; j++)
					{
						for (var i = 0; i < xpad; i++)
						{
							*pdst++ = unchecked((int)0xff000000);
						}

						for (var i = 0; i < gpwidth; i++)
						{
							*pdst++ = *psrc++;
						}

						for (var i = 0; i < xpad2; i++)
						{
							*pdst++ = unchecked((int)0xff000000);
						}

						psrc += rinc;
					}
				}
			}
		}
	}
}
