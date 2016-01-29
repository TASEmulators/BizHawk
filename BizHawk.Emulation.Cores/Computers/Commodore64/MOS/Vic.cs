using System;
using System.Drawing;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
	public sealed partial class Vic
	{
		public Func<int, int> ReadColorRam;
		public Func<int, int> ReadMemory;

		public bool ReadAec() { return _pinAec; }
		public bool ReadBa() { return _pinBa; }
		public bool ReadIrq() { return _pinIrq; }

		[SaveState.DoNotSave] private readonly int _cyclesPerSec;
		private int _irqShift;
        [SaveState.DoNotSave] private readonly int[] _rasterXPipeline;
        [SaveState.DoNotSave] private readonly int[] _fetchPipeline;
        [SaveState.DoNotSave] private readonly int[] _baPipeline;
        [SaveState.DoNotSave] private readonly int[] _actPipeline;
        [SaveState.DoNotSave] private readonly int _totalCycles;
		[SaveState.DoNotSave] private readonly int _totalLines;

		public Vic(int newCycles, int newLines, int[][] newPipeline, int newCyclesPerSec, int hblankStart, int hblankEnd, int vblankStart, int vblankEnd)
		{
            _hblankStart = hblankStart;
            _hblankEnd = hblankEnd;
            _vblankStart = vblankStart;
            _vblankEnd = vblankEnd;

		    _rasterXPipeline = newPipeline[0];
            _fetchPipeline = newPipeline[1];
            _baPipeline = newPipeline[2];
            _actPipeline = newPipeline[3];

            _totalCycles = newCycles;
            _totalLines = newLines;
            _cyclesPerSec = newCyclesPerSec;

            _bufWidth = TimingBuilder_ScreenWidth(_rasterXPipeline, hblankStart, hblankEnd);
            _bufHeight = TimingBuilder_ScreenHeight(vblankStart, vblankEnd, newLines);

            _buf = new int[_bufWidth * _bufHeight];
            _bufLength = _buf.Length;

            _sprites = new Sprite[8];
            for (var i = 0; i < 8; i++)
                _sprites[i] = new Sprite();
		    _sprite0 = _sprites[0];
            _sprite1 = _sprites[1];
            _sprite2 = _sprites[2];
            _sprite3 = _sprites[3];
            _sprite4 = _sprites[4];
            _sprite5 = _sprites[5];
            _sprite6 = _sprites[6];
            _sprite7 = _sprites[7];

            _bufferC = new int[40];
            _bufferG = new int[40];
        }

        public int CyclesPerFrame
		{
			get
			{
				return _totalCycles * _totalLines;
			}
		}

		public int CyclesPerSecond
		{
			get
			{
				return _cyclesPerSec;
			}
		}

		public void ExecutePhase1()
		{
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
                _rasterInterruptTriggered = false;

                if (_rasterLine == 0x0F8)
                    _badlineEnable = false;
            }

            // rasterline IRQ compare
		    if (!_rasterInterruptTriggered && _rasterLine == _rasterInterruptLine)
		    {
		        _rasterInterruptTriggered = true;
                _intRaster = true;
            }

            // display enable compare
            if (_rasterLine == 0x030)
                _badlineEnable |= _displayEnable;

            // badline compare
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

            ParseCycle();
            _pinAec = false;
            Render();
		}

        public void ExecutePhase2()
		{
            ParseCycle();
            _extraColorModeBuffer = _extraColorMode;

            // advance cycle and optionally raster line
            _cycle++;
            if (_cycle == _totalCycles)
            {
                // border check
                if (_rasterLine == _borderB)
                    _borderOnVertical = true;
                if (_rasterLine == _borderT && _displayEnable)
                    _borderOnVertical = false;

                // vblank check
                if (_rasterLine == _vblankStart)
                    _vblank = true;
                if (_rasterLine == _vblankEnd)
                    _vblank = false;

                // reset to beginning of rasterline
                _cycleIndex = 0;
                _cycle = 0;
                _rasterLine++;

                if (_rasterLine == _totalLines)
                {
                    // reset to rasterline 0
                    _rasterLine = 0;
                    _vcbase = 0;
                    _vc = 0;
                    _badlineEnable = false;
                    _refreshCounter = 0xFF;
                }
            }

            // if the BA counter is nonzero, allow CPU bus access
            UpdateBa();
            _pinAec = _pinBa || _baCount > 0;
            Render();

            // must always come last
            UpdatePins();
        }

        private void UpdateBa()
		{
			if (_pinBa)
				_baCount = BaResetCounter;
			else if (_baCount > 0)
				_baCount--;
		}

		private void UpdateBorder()
		{
			_borderL = _columnSelect ? 0x018 : 0x01F;
			_borderR = _columnSelect ? 0x158 : 0x14F;
			_borderT = _rowSelect ? 0x033 : 0x037;
			_borderB = _rowSelect ? 0x0FB : 0x0F7;
		}

		private void UpdatePins()
		{
			var irqTemp = !(
				(_enableIntRaster & _intRaster) |
				(_enableIntSpriteDataCollision & _intSpriteDataCollision) |
				(_enableIntSpriteCollision & _intSpriteCollision) |
				(_enableIntLightPen & _intLightPen));

			_irqShift <<= 1;
			_irqShift |= irqTemp ? 0x1 : 0x0;

            // if delaying IRQ, use higher bitmask
			_pinIrq = (_irqShift & 0x1) != 0;
		}

		private void UpdateVideoMode()
		{
			if (!_extraColorMode && !_bitmapMode && !_multicolorMode)
			{
				_videoMode = 0;
				return;
			}
		    if (!_extraColorMode && !_bitmapMode && _multicolorMode)
		    {
		        _videoMode = 1;
		        return;
		    }
		    if (!_extraColorMode && _bitmapMode && !_multicolorMode)
		    {
		        _videoMode = 2;
		        return;
		    }
		    if (!_extraColorMode && _bitmapMode && _multicolorMode)
		    {
		        _videoMode = 3;
		        return;
		    }
		    if (_extraColorMode && !_bitmapMode && !_multicolorMode)
		    {
		        _videoMode = 4;
		        return;
		    }
		    _videoMode = -1;
		}
	}
}
