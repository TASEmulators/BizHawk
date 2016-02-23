using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
	public sealed partial class Vic
	{
		private int _backgroundColor0;
		private int _backgroundColor1;
		private int _backgroundColor2;
		private int _backgroundColor3;
		private int _baCount;
		private bool _badline;
		private bool _badlineEnable;
	    private bool _bitmapMode;
		private int _borderB;
		private bool _borderCheckLEnable;
		private bool _borderCheckREnable;
		private int _borderColor;
		private int _borderL;
		private bool _borderOnMain;
		private bool _borderOnVertical;
		private int _borderR;
		private int _borderT;
		private readonly int[] _bufferC;
		private readonly int[] _bufferG;
		private int _cycle;
		private int _cycleIndex;
		private bool _columnSelect;
		private int _dataC;
		private int _dataG;
		private bool _displayEnable;
		private int _displayC;
	    private bool _enableIntLightPen;
		private bool _enableIntRaster;
		private bool _enableIntSpriteCollision;
		private bool _enableIntSpriteDataCollision;
		private bool _extraColorMode;
        private bool _extraColorModeBuffer;
        [SaveState.DoNotSave] private bool _hblank;
		private bool _idle;
		private bool _intLightPen;
		private bool _intRaster;
		private bool _intSpriteCollision;
		private bool _intSpriteDataCollision;
	    private int _lightPenX;
		private int _lightPenY;
		private bool _multicolorMode;
		private bool _pinAec = true;
		private bool _pinBa = true;
		private bool _pinIrq = true;
		private int _pointerCb;
		private int _pointerVm;
		private int _rasterInterruptLine;
	    private bool _rasterInterruptTriggered;
		private int _rasterLine;
		private int _rasterX;
		private bool _rasterXHold;
		private int _rc;
		private int _refreshCounter;
		private bool _renderEnabled;
		private bool _rowSelect;
        private bool _spriteBackgroundCollisionClearPending;
        private bool _spriteSpriteCollisionClearPending;
        private int _spriteMulticolor0;
	    private int _spriteMulticolor1;
	    [SaveState.DoNotSave] private readonly Sprite _sprite0;
        [SaveState.DoNotSave] private readonly Sprite _sprite1;
        [SaveState.DoNotSave] private readonly Sprite _sprite2;
        [SaveState.DoNotSave] private readonly Sprite _sprite3;
        [SaveState.DoNotSave] private readonly Sprite _sprite4;
        [SaveState.DoNotSave] private readonly Sprite _sprite5;
        [SaveState.DoNotSave] private readonly Sprite _sprite6;
        [SaveState.DoNotSave] private readonly Sprite _sprite7;
        private readonly Sprite[] _sprites;
		private int _sr;
		[SaveState.DoNotSave] private bool _vblank;
		[SaveState.DoNotSave] private int _vblankEnd;
        [SaveState.DoNotSave] private int _vblankStart;
		private int _vc;
		private int _vcbase;
		private int _vmli;
		private int _xScroll;
		private int _yScroll;

		public void HardReset()
		{
			_pinAec = true;
			_pinBa = true;
			_pinIrq = true;

			_bufOffset = 0;

			_backgroundColor0 = 0;
			_backgroundColor1 = 0;
			_backgroundColor2 = 0;
			_backgroundColor3 = 0;
			_baCount = BaResetCounter;
			_badline = false;
			_badlineEnable = false;
			_bitmapMode = false;
			_borderCheckLEnable = false;
			_borderCheckREnable = false;
			_borderColor = 0;
			_borderOnMain = true;
			_borderOnVertical = true;
			_columnSelect = false;
			_displayEnable = false;
			_enableIntLightPen = false;
			_enableIntRaster = false;
			_enableIntSpriteCollision = false;
			_enableIntSpriteDataCollision = false;
			_extraColorMode = false;
			_idle = true;
			_intLightPen = false;
			_intRaster = false;
			_intSpriteCollision = false;
			_intSpriteDataCollision = false;
			_lightPenX = 0;
			_lightPenY = 0;
			_multicolorMode = false;
			_pointerCb = 0;
			_pointerVm = 0;
			_rasterInterruptLine = 0;
			_rasterLine = 0;
			_rasterX = 0;
			_rc = 7;
			_refreshCounter = 0xFF;
			_rowSelect = false;
            _spriteBackgroundCollisionClearPending = false;
            _spriteSpriteCollisionClearPending = false;
            _spriteMulticolor0 = 0;
			_spriteMulticolor1 = 0;
			_sr = 0;
			_vc = 0;
			_vcbase = 0;
			_vmli = 0;
			_xScroll = 0;
			_yScroll = 0;
		    _cycle = 0;

			// reset sprites
			for (var i = 0; i < 8; i++)
				_sprites[i].HardReset();

			// clear C buffer
			for (var i = 0; i < 40; i++)
			{
				_bufferC[i] = 0;
				_bufferG[i] = 0;
			}

			_pixBuffer = new int[PixBufferSize];
            _pixBorderBuffer = new int[PixBorderBufferSize];
		    _pixBufferIndex = 0;
            _pixBufferBorderIndex = 0;
            UpdateBorder();
		}

		public void SyncState(Serializer ser)
		{
			SaveState.SyncObject(ser, this);
		    if (ser.IsReader)
		    {
                UpdateBorder();
                UpdatePins();
                UpdateVideoMode();
            }
        }
	}
}
