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
		private int[] _bufferC;
		private int[] _bufferG;
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
		private bool _hblank;
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
		private readonly Sprite _sprite0;
		private readonly Sprite _sprite1;
		private readonly Sprite _sprite2;
		private readonly Sprite _sprite3;
		private readonly Sprite _sprite4;
		private readonly Sprite _sprite5;
		private readonly Sprite _sprite6;
		private readonly Sprite _sprite7;
		private readonly Sprite[] _sprites;
		private int _sr;
		private bool _vblank;
		private int _vblankEnd;
		private int _vblankStart;
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
			{
				_sprites[i].HardReset();
			}

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
			ser.Sync("_cyclesExecuted", ref _cyclesExecuted);
			ser.Sync("_parseIsSprCrunch", ref _parseIsSprCrunch);
			ser.Sync("_srSync", ref _srSync);
			ser.Sync("_srColorSync", ref _srColorSync);
			ser.Sync("_srColorIndexLatch", ref _srColorIndexLatch);
			ser.Sync("_videoMode", ref _videoMode);
			ser.Sync("_borderOnShiftReg", ref _borderOnShiftReg);
			ser.Sync("_backgroundColor0", ref _backgroundColor0);
			ser.Sync("_backgroundColor1", ref _backgroundColor1);
			ser.Sync("_backgroundColor2", ref _backgroundColor2);
			ser.Sync("_backgroundColor3", ref _backgroundColor3);
			ser.Sync("_baCount", ref _baCount);
			ser.Sync("_badline", ref _badline);
			ser.Sync("_badlineEnable", ref _badlineEnable);
			ser.Sync("_bitmapMode", ref _bitmapMode);
			ser.Sync("_borderB", ref _borderB);
			ser.Sync("_borderCheckLEnable", ref _borderCheckLEnable);
			ser.Sync("_borderCheckREnable", ref _borderCheckREnable);
			ser.Sync("_borderColor", ref _borderColor);
			ser.Sync("_borderL", ref _borderL);
			ser.Sync("_borderOnMain", ref _borderOnMain);
			ser.Sync("_borderOnVertical", ref _borderOnVertical);
			ser.Sync("_borderR", ref _borderR);
			ser.Sync("_borderT", ref _borderT);
			ser.Sync("_bufferC", ref _bufferC, useNull: false);
			ser.Sync("_bufferG", ref _bufferG, useNull: false);
			ser.Sync("_cycle", ref _cycle);
			ser.Sync("_cycleIndex", ref _cycleIndex);
			ser.Sync("_columnSelect", ref _columnSelect);
			ser.Sync("_dataC", ref _dataC);
			ser.Sync("_dataG", ref _dataG);
			ser.Sync("_displayEnable", ref _displayEnable);
			ser.Sync("_displayC", ref _displayC);
			ser.Sync("_enableIntLightPen", ref _enableIntLightPen);
			ser.Sync("_enableIntRaster", ref _enableIntRaster);
			ser.Sync("_enableIntSpriteCollision", ref _enableIntSpriteCollision);
			ser.Sync("_enableIntSpriteDataCollision", ref _enableIntSpriteDataCollision);
			ser.Sync("_extraColorMode", ref _extraColorMode);
			ser.Sync("_extraColorModeBuffer", ref _extraColorModeBuffer);
			ser.Sync("_idle", ref _idle);
			ser.Sync("_intLightPen", ref _intLightPen);
			ser.Sync("_intRaster", ref _intRaster);
			ser.Sync("_intSpriteCollision", ref _intSpriteCollision);
			ser.Sync("_intSpriteDataCollision", ref _intSpriteDataCollision);
			ser.Sync("_lightPenX", ref _lightPenX);
			ser.Sync("_lightPenY", ref _lightPenY);
			ser.Sync("_multicolorMode", ref _multicolorMode);
			ser.Sync("_pinAec", ref _pinAec);
			ser.Sync("_pinBa", ref _pinBa);
			ser.Sync("_pinIrq", ref _pinIrq);
			ser.Sync("_pointerCb", ref _pointerCb);
			ser.Sync("_pointerVm", ref _pointerVm);
			ser.Sync("_rasterInterruptLine", ref _rasterInterruptLine);
			ser.Sync("_rasterInterruptTriggered", ref _rasterInterruptTriggered);
			ser.Sync("_rasterLine", ref _rasterLine);
			ser.Sync("_rasterX", ref _rasterX);
			ser.Sync("_rasterXHold", ref _rasterXHold);
			ser.Sync("_rc", ref _rc);
			ser.Sync("_refreshCounter", ref _refreshCounter);
			ser.Sync("_renderEnabled", ref _renderEnabled);
			ser.Sync("_rowSelect", ref _rowSelect);
			ser.Sync("_spriteBackgroundCollisionClearPending", ref _spriteBackgroundCollisionClearPending);
			ser.Sync("_spriteSpriteCollisionClearPending", ref _spriteSpriteCollisionClearPending);
			ser.Sync("_spriteMulticolor0", ref _spriteMulticolor0);
			ser.Sync("_spriteMulticolor1", ref _spriteMulticolor1);

			for (int i = 0; i < _sprites.Length; i++)
			{
				ser.BeginSection("Sprite" + i);
				_sprites[i].SyncState(ser);
				ser.EndSection();
			}

			ser.Sync("_sr", ref _sr);
			ser.Sync("_vc", ref _vc);
			ser.Sync("_vcbase", ref _vcbase);
			ser.Sync("_vmli", ref _vmli);
			ser.Sync("_xScroll", ref _xScroll);
			ser.Sync("_yScroll", ref _yScroll);
			ser.Sync("_bufOffset", ref _bufOffset);
			ser.Sync("_pixBuffer", ref _pixBuffer, useNull: false);
			ser.Sync("_pixBufferIndex", ref _pixBufferIndex);
			ser.Sync("_pixBorderBuffer", ref _pixBorderBuffer, useNull: false);
			ser.Sync("_pixBufferBorderIndex", ref _pixBufferBorderIndex);

			if (ser.IsReader)
			{
				UpdateBorder();
				UpdatePins();
				UpdateVideoMode();
			}
		}
	}
}
