using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;


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
		public bool ReadIrq() { return _pinIrq; }

		[SaveState.DoNotSave] private readonly int _cyclesPerSec;
        [SaveState.DoNotSave] private readonly int[] _rasterXPipeline;
        [SaveState.DoNotSave] private readonly int[] _fetchPipeline;
        [SaveState.DoNotSave] private readonly int[] _baPipeline;
        [SaveState.DoNotSave] private readonly int[] _actPipeline;
        [SaveState.DoNotSave] private readonly int _totalCycles;
		[SaveState.DoNotSave] private readonly int _totalLines;

	    private int _cyclesExecuted;
	    [SaveState.DoNotSave] private int _hblankStartCheckXRaster;
        [SaveState.DoNotSave] private int _hblankEndCheckXRaster;

	    [SaveState.DoNotSave] private int _pixelRatioNum;
	    [SaveState.DoNotSave] private int _pixelRatioDen;

        public Vic(int newCycles, int newLines, IList<int[]> newPipeline, int newCyclesPerSec, int hblankStart, int hblankEnd, int vblankStart, int vblankEnd, C64.BorderType borderType, int pixelRatioNum, int pixelRatioDen)
		{
            Debug.WriteLine("C64 VIC timings:");
            Debug.WriteLine("RX   FTCH BA   ACT");
		    for (var i = 0; i < newPipeline[0].Length; i++)
		    {
		        Debug.WriteLine("{0:x4} {1:x4} {2:x4} {3:x8}", newPipeline[0][i], newPipeline[1][i], newPipeline[2][i], newPipeline[3][i]);
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

	    private void ConfigureBlanking(int lines, int hblankStart, int hblankEnd, int vblankStart, int vblankEnd,
	        C64.BorderType borderType)
	    {
	        var newHblankStart = hblankStart;
	        var newHblankEnd = hblankEnd;
	        var newVblankStart = vblankStart;
	        var newVblankEnd = vblankEnd;
	        var hBorderSize = 16; // must be a multiple of 4
	        var vBorderSize = hBorderSize*_pixelRatioNum/_pixelRatioDen; // to keep top and bottom in proportion
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
            }

            // wrap values
            newHblankStart = WrapValue(0, maxWidth, newHblankStart);
            newHblankEnd = WrapValue(0, maxWidth, newHblankEnd);
	        newVblankStart = WrapValue(0, lines, newVblankStart);
            newVblankEnd = WrapValue(0, lines, newVblankEnd);

            // calculate output dimensions
	        _hblankStartCheckXRaster = newHblankStart & 0xFFC;
            _hblankEndCheckXRaster = newHblankEnd & 0xFFC;
            _vblankStart = newVblankStart;
	        _vblankEnd = newVblankEnd;
            _bufWidth = TimingBuilder_ScreenWidth(_rasterXPipeline, newHblankStart, newHblankEnd);
            _bufHeight = TimingBuilder_ScreenHeight(newVblankStart, newVblankEnd, lines);
            _buf = new int[_bufWidth * _bufHeight];
            _bufLength = _buf.Length;
	        VirtualWidth = _bufWidth*_pixelRatioNum/_pixelRatioDen;
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

        public void ExecutePhase()
		{
            // phi1

            // advance cycle and optionally raster line
            _cycle++;
            if (_cycle > _totalCycles)
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
                _cycle = 1;
                _rasterLine++;

                if (_rasterLine == _totalLines)
                {
                    // reset to rasterline 0
                    _rasterLine = 0;
                    _vcbase = 0;
                    _vc = 0;
                    _badlineEnable = false;
                    _refreshCounter = 0xFF;
                    _cyclesExecuted = 0;
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

            // phi2

            // start of rasterline
            if ((_cycle == RasterIrqLineXCycle && _rasterLine > 0) || (_cycle == RasterIrqLine0Cycle && _rasterLine == 0))
            {
                _rasterInterruptTriggered = false;

                if (_rasterLine == LastDmaLine)
                    _badlineEnable = false;
            }

            // rasterline IRQ compare
            if (_rasterLine != _rasterInterruptLine)
            {
                _rasterInterruptTriggered = false;
            }
            else
            {
                if (!_rasterInterruptTriggered)
                {
                    _rasterInterruptTriggered = true;
                    _intRaster = true;
                }
            }

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
            if (_rasterLine == FirstDmaLine)
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
            else
            {
                _badline = false;
            }

            // render
            ParseCycle();
            Render();
            ParseCycle();
            Render();
            _extraColorModeBuffer = _extraColorMode;

            // if the BA counter is nonzero, allow CPU bus access
            if (_pinBa)
                _baCount = BaResetCounter;
            else if (_baCount > 0)
                _baCount--;
            _pinAec = _pinBa || _baCount > 0;

            // must always come last
            UpdatePins();

            _cyclesExecuted++;
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
			var irqTemp = !(
				(_enableIntRaster & _intRaster) |
				(_enableIntSpriteDataCollision & _intSpriteDataCollision) |
				(_enableIntSpriteCollision & _intSpriteCollision) |
				(_enableIntLightPen & _intLightPen));

			_pinIrq = irqTemp;
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
