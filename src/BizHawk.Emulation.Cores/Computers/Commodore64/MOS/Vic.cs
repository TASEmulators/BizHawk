using System.Collections.Generic;
using System.Linq;

using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
	public sealed partial class Vic
	{
		/*
			Commodore VIC-II 6567/6569/6572 core.

			Many thanks to:
			- Christian Bauer for the VIC-II document.
				http://www.zimmers.net/cbmpics/cbm/c64/vic-ii.txt
			- VICE team for the addendum to the above document.
				http://vice-emu.sourceforge.net/plain/VIC-Addendum.txt
			- Whoever scanned the CSG 6567 preliminary datasheet.
				http://www.classiccmp.org/cini/pdf/Commodore/ds_6567.pdf
			- Michael Huth for die shots of the 6569R3 chip (to get ideas how to implement)
				http://mail.lipsia.de/~enigma/neu/6581.html
		*/

		public Func<int, int> ReadColorRam;
		public Func<int, int> ReadMemory;

		public bool ReadAec() { return _pinAec; }
		public bool ReadBa() { return _pinBa; }
		public bool ReadIrq() { return (_irqBuffer & 1) == 0; }

		private readonly int _cyclesPerSec;
		private readonly int[] _rasterXPipeline;
		private readonly int[] _fetchPipeline;
		private readonly int[] _baPipeline;
		private readonly int[] _actPipeline;
		private readonly int _totalCycles;
		private readonly int _totalLines;
		private int _irqBuffer;

		private int _hblankStartCheckXRaster;
		private int _hblankEndCheckXRaster;

		private readonly int _pixelRatioNum;
		private readonly int _pixelRatioDen;

		public Vic(int newCycles, int newLines, IList<int[]> newPipeline, int newCyclesPerSec, int hblankStart, int hblankEnd, int vblankStart, int vblankEnd, C64.BorderType borderType, int pixelRatioNum, int pixelRatioDen)
		{
			Util.DebugWriteLine("C64 VIC timings:");
			Util.DebugWriteLine("RX   FTCH BA   ACT");
			for (var i = 0; i < newPipeline[0].Length; i++)
			{
				Util.DebugWriteLine("{0:x4} {1:x4} {2:x4} {3:x8}", newPipeline[0][i], newPipeline[1][i], newPipeline[2][i], newPipeline[3][i]);
			}

			_pixelRatioNum = pixelRatioNum;
			_pixelRatioDen = pixelRatioDen;

			_rasterXPipeline = newPipeline[0];
			_fetchPipeline = newPipeline[1];
			_baPipeline = newPipeline[2];
			_actPipeline = newPipeline[3];
			_totalCycles = newCycles;
			_totalLines = newLines;
			_cyclesPerSec = newCyclesPerSec;

			ConfigureBlanking(newLines, hblankStart, hblankEnd, vblankStart, vblankEnd, borderType);

			_sprites = new Sprite[8];
			for (var i = 0; i < 8; i++)
				_sprites[i] = new Sprite(i);

			_sprite0 = _sprites[0];
			_sprite1 = _sprites[1];
			_sprite2 = _sprites[2];
			_sprite3 = _sprites[3];
			_sprite4 = _sprites[4];
			_sprite5 = _sprites[5];
			_sprite6 = _sprites[6];
			_sprite7 = _sprites[7];
			_bufferC = new int[40];
			_pixBuffer = new int[PixBufferSize];
			_pixBorderBuffer = new int[PixBorderBufferSize];
		}

		private void ConfigureBlanking(int lines, int hblankStart, int hblankEnd, int vblankStart, int vblankEnd,
			C64.BorderType borderType)
		{
			var newHblankStart = hblankStart;
			var newHblankEnd = hblankEnd;
			var newVblankStart = vblankStart;
			var newVblankEnd = vblankEnd;
			var hBorderSize = 16; // must be a multiple of 4
			var vBorderSize = hBorderSize * _pixelRatioNum / _pixelRatioDen; // to keep top and bottom in proportion
			var maxWidth = _rasterXPipeline.Max();

			switch (borderType)
			{
				
				case C64.BorderType.Full:
					newHblankStart = -1;
					newHblankEnd = -1;
					_hblank = false;
					newVblankStart = -1;
					newVblankEnd = -1;
					_vblank = false;
					break;                    
				case C64.BorderType.Normal:
					newHblankStart = hblankStart;
					newHblankEnd = hblankEnd;
					newVblankStart = vblankStart;
					newVblankEnd = vblankEnd;
					_vblank = true;
					_hblank = true;
					break;
				case C64.BorderType.SmallProportional:
					_vblank = true;
					_hblank = true;
					newHblankStart = 0x158 + PixBufferSize + hBorderSize;
					newHblankEnd = 0x018 + PixBufferSize - hBorderSize;
					newVblankStart = 0xFA + vBorderSize;
					newVblankEnd = 0x32 - vBorderSize;
					break;
				case C64.BorderType.SmallFixed:
					_vblank = true;
					_hblank = true;
					newHblankStart = 0x158 + PixBufferSize + hBorderSize;
					newHblankEnd = 0x018 + PixBufferSize - hBorderSize;
					newVblankStart = 0xFA + hBorderSize;
					newVblankEnd = 0x32 - hBorderSize;
					break;
				case C64.BorderType.None:
					newHblankStart = 0x158 + PixBufferSize;
					newHblankEnd = 0x018 + PixBufferSize;
					newVblankStart = 0xFA;
					newVblankEnd = 0x32;
					_vblank = true;
					_hblank = true;
					break;
			}

			// wrap values
			if (_hblank)
			{
				newHblankStart = WrapValue(0, maxWidth, newHblankStart);
				newHblankEnd = WrapValue(0, maxWidth, newHblankEnd);
			}
			if (_vblank)
			{
				newVblankStart = WrapValue(0, lines, newVblankStart);
				newVblankEnd = WrapValue(0, lines, newVblankEnd);
			}

			// calculate output dimensions
			_hblankStartCheckXRaster = newHblankStart & 0xFFC;
			_hblankEndCheckXRaster = newHblankEnd & 0xFFC;
			_vblankStart = newVblankStart;
			_vblankEnd = newVblankEnd;
			_bufWidth = TimingBuilder_ScreenWidth(_rasterXPipeline, newHblankStart, newHblankEnd);
			_bufHeight = TimingBuilder_ScreenHeight(newVblankStart, newVblankEnd, lines);
			_buf = new int[_bufWidth * _bufHeight];
			_bufLength = _buf.Length;
			VirtualWidth = _bufWidth * _pixelRatioNum / _pixelRatioDen;
			VirtualHeight = _bufHeight;
		}

		private int WrapValue(int min, int max, int val)
		{
			if (min == max)
			{
				return min;
			}

			var width = Math.Abs(min - max);
			while (val > max)
			{
				val -= width;
			}

			while (val < min)
			{
				val += width;
			}

			return val;
		}

		public int CyclesPerFrame => _totalCycles * _totalLines;

		public int CyclesPerSecond => _cyclesPerSec;

		public void ExecutePhase1()
		{
			// phi1

			// advance cycle and optionally raster line
			_cycle++;
			if (_cycle > _totalCycles)
			{
				// border check
				if (_rasterLine == _borderB)
				{
					_borderOnVertical = true;
				}

				if (_rasterLine == _borderT && _displayEnable)
				{
					_borderOnVertical = false;
				}

				// vblank check
				if (_rasterLine == _vblankStart)
					_vblank = true;
				if (_rasterLine == _vblankEnd)
					_vblank = false;

				// reset to beginning of rasterline
				_cycleIndex = 0;
				_cycle = 1;
				_rasterLine++;

				if (_rasterLine == _totalLines)
				{
					// reset to rasterline 0
					_rasterLine = 0;
					_vcbase = 0;
					_vc = 0;
					_refreshCounter = 0xFF;
				}
			}

			// bg collision clear
			if (_spriteBackgroundCollisionClearPending)
			{
				foreach (var spr in _sprites)
				{
					spr.CollideData = false;
				}
				_spriteBackgroundCollisionClearPending = false;
			}

			// sprite collision clear
			if (_spriteSpriteCollisionClearPending)
			{
				foreach (var spr in _sprites)
				{
					spr.CollideSprite = false;
				}
				_spriteSpriteCollisionClearPending = false;
			}
			
			// start of rasterline
			if ((_cycle == RasterIrqLineXCycle && _rasterLine > 0) || (_cycle == RasterIrqLine0Cycle && _rasterLine == 0))
			{
				if (_rasterLine == BadLineDisableRaster)
					_badlineEnable = false;

				// raster compares are done here
				if (_rasterLine == _rasterInterruptLine)
				{
					_intRaster = true;
				}
			}

			// render
			ParseCycle();
			UpdateBa();
			UpdatePins();
			Render();
		}

		public void ExecutePhase2()
		{
			// phi2

			// check top and bottom border
			if (_rasterLine == _borderB)
			{
				_borderOnVertical = true;
			}
			if (_displayEnable && _rasterLine == _borderT)
			{
				_borderOnVertical = false;
			}

			// display enable compare
			if (_rasterLine == BadLineEnableRaster)
			{
				_badlineEnable |= _displayEnable;
			}

			// badline compare
			_vcEnable = !_idle;
			if (_badlineEnable)
			{
				if ((_rasterLine & 0x7) == _yScroll)
				{
					_badline = true;

					// go into display state on a badline
					_idle = false;
				}
				else
				{
					_badline = false;
				}
			}
			else
			{
				_badline = false;
			}

			// render
			ParseCycle();
			UpdatePins();
			Render();
		}
		
		private void UpdateBa()
		{
			if (_ba)
				_baCount = BaResetCounter;
			else if (_baCount >= 0)
				_baCount--;
		}

		private void UpdateBorder()
		{
			_borderL = _columnSelect ? BorderLeft40 : BorderLeft38;
			_borderR = _columnSelect ? BorderRight40 : BorderRight38;
			_borderT = _rowSelect ? BorderTop25 : BorderTop24;
			_borderB = _rowSelect ? BorderBottom25 : BorderBottom24;
		}

		private void UpdatePins()
		{
			// IRQ is treated as a delay line

			var intIrq = (_enableIntRaster && _intRaster) ? 0x0002 : 0x0000;
			var sdIrq = (_enableIntSpriteDataCollision & _intSpriteDataCollision) ? 0x0001 : 0x0000;
			var ssIrq = (_enableIntSpriteCollision & _intSpriteCollision) ? 0x0001 : 0x0000;
			var lpIrq = (_enableIntLightPen & _intLightPen) ? 0x0001 : 0x0000;

			_irqBuffer >>= 1;
			_irqBuffer |= intIrq | sdIrq | ssIrq | lpIrq;
			_pinAec = _ba || _baCount >= 0;
			_pinBa = _ba;
		}

		private void UpdateVideoMode()
		{
			if (!_extraColorMode && !_bitmapMode && !_multicolorMode)
			{
				_videoMode = VideoMode000;
				return;
			}

			if (!_extraColorMode && !_bitmapMode && _multicolorMode)
			{
				_videoMode = VideoMode001;
				return;
			}

			if (!_extraColorMode && _bitmapMode && !_multicolorMode)
			{
				_videoMode = VideoMode010;
				return;
			}

			if (!_extraColorMode && _bitmapMode && _multicolorMode)
			{
				_videoMode = VideoMode011;
				return;
			}

			if (_extraColorMode && !_bitmapMode && !_multicolorMode)
			{
				_videoMode = VideoMode100;
				return;
			}

			_videoMode = VideoModeInvalid;
		}
	}
}
