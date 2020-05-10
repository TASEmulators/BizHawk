using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
	public sealed partial class Vic
	{
		private int _backgroundColor0;
		private int _backgroundColor1;
		private int _backgroundColor2;
		private int _backgroundColor3;
		private bool _ba;
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
		private int _cycle;
		private int _cycleIndex;
		private bool _columnSelect;
		private int _dataC;
		private int _dataG;
		private bool _displayEnable;
		private bool _enableIntLightPen;
		private bool _enableIntRaster;
		private bool _enableIntSpriteCollision;
		private bool _enableIntSpriteDataCollision;
		private bool _extraColorMode;
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
		private int _pointerCb;
		private int _pointerVm;
		private int _rasterInterruptLine;
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
		private bool _vblank;
		private int _vblankEnd;
		private int _vblankStart;
		private int _vc;
		private int _vcbase;
		private bool _vcEnable;
		private int _vmli;
		private int _xScroll;
		private int _yScroll;

		public void HardReset()
		{
			_backgroundColor0 = 0;
			_backgroundColor1 = 0;
			_backgroundColor2 = 0;
			_backgroundColor3 = 0;
			_ba = true;
			_baCount = BaResetCounter;
			_badline = false;
			_badlineEnable = false;
			_bitmapMode = false;
			_borderCheckLEnable = false;
			_borderCheckREnable = false;
			_borderColor = 0;
			_borderOnMain = true;
			_borderOnVertical = true;
			_bufOffset = 0;
			_columnSelect = false;
			_cycle = 0;
			_cycleIndex = 0;
			_dataC = 0;
			_dataG = 0;
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
			_irqBuffer = 0;
			_lightPenX = 0;
			_lightPenY = 0;
			_multicolorMode = false;
			_pinAec = true;
			_pinBa = true;
			_pointerCb = 0;
			_pointerVm = 0;
			_rasterInterruptLine = 0;
			_rasterLine = 0;
			_rasterX = 0;
			_rasterXHold = false;
			_rc = 7;
			_refreshCounter = 0xFF;
			_rowSelect = false;
			_spriteBackgroundCollisionClearPending = false;
			_spriteSpriteCollisionClearPending = false;
			_spriteMulticolor0 = 0;
			_spriteMulticolor1 = 0;
			_vc = 0;
			_vcbase = 0;
			_vcEnable = false;
			_vmli = 0;
			_xScroll = 0;
			_yScroll = 0;

			// reset sprites
			for (var i = 0; i < 8; i++)
			{
				_sprites[i].HardReset();
			}

			// clear C buffer
			for (var i = 0; i < 40; i++)
			{
				_bufferC[i] = 0;
			}

			_pixBufferIndex = 0;
			_pixBufferBorderIndex = 0;
			UpdateBorder();
		}

		public void SyncState(Serializer ser)
		{
			ser.Sync(nameof(_ba), ref _ba);
			ser.Sync(nameof(_backgroundColor0), ref _backgroundColor0);
			ser.Sync(nameof(_backgroundColor1), ref _backgroundColor1);
			ser.Sync(nameof(_backgroundColor2), ref _backgroundColor2);
			ser.Sync(nameof(_backgroundColor3), ref _backgroundColor3);
			ser.Sync(nameof(_baCount), ref _baCount);
			ser.Sync(nameof(_badline), ref _badline);
			ser.Sync(nameof(_badlineEnable), ref _badlineEnable);
			ser.Sync(nameof(_bitmapMode), ref _bitmapMode);
			ser.Sync(nameof(_borderB), ref _borderB);
			ser.Sync(nameof(_borderCheckLEnable), ref _borderCheckLEnable);
			ser.Sync(nameof(_borderCheckREnable), ref _borderCheckREnable);
			ser.Sync(nameof(_borderColor), ref _borderColor);
			ser.Sync(nameof(_borderL), ref _borderL);
			ser.Sync(nameof(_borderOnMain), ref _borderOnMain);
			ser.Sync(nameof(_borderOnShiftReg), ref _borderOnShiftReg);
			ser.Sync(nameof(_borderOnVertical), ref _borderOnVertical);
			ser.Sync(nameof(_borderR), ref _borderR);
			ser.Sync(nameof(_borderT), ref _borderT);
			ser.Sync(nameof(_bufferC), ref _bufferC, useNull: false);
			ser.Sync(nameof(_bufOffset), ref _bufOffset);
			ser.Sync(nameof(_cycle), ref _cycle);
			ser.Sync(nameof(_cycleIndex), ref _cycleIndex);
			ser.Sync(nameof(_columnSelect), ref _columnSelect);
			ser.Sync(nameof(_dataC), ref _dataC);
			ser.Sync(nameof(_dataG), ref _dataG);
			ser.Sync(nameof(_displayEnable), ref _displayEnable);
			ser.Sync(nameof(_enableIntLightPen), ref _enableIntLightPen);
			ser.Sync(nameof(_enableIntRaster), ref _enableIntRaster);
			ser.Sync(nameof(_enableIntSpriteCollision), ref _enableIntSpriteCollision);
			ser.Sync(nameof(_enableIntSpriteDataCollision), ref _enableIntSpriteDataCollision);
			ser.Sync(nameof(_extraColorMode), ref _extraColorMode);
			ser.Sync(nameof(_idle), ref _idle);
			ser.Sync(nameof(_intLightPen), ref _intLightPen);
			ser.Sync(nameof(_intRaster), ref _intRaster);
			ser.Sync(nameof(_intSpriteCollision), ref _intSpriteCollision);
			ser.Sync(nameof(_intSpriteDataCollision), ref _intSpriteDataCollision);
			ser.Sync(nameof(_irqBuffer), ref _irqBuffer);
			ser.Sync(nameof(_lightPenX), ref _lightPenX);
			ser.Sync(nameof(_lightPenY), ref _lightPenY);
			ser.Sync(nameof(_multicolorMode), ref _multicolorMode);
			ser.Sync(nameof(_pinAec), ref _pinAec);
			ser.Sync(nameof(_pinBa), ref _pinBa);
			ser.Sync(nameof(_parseIsSprCrunch), ref _parseIsSprCrunch);
			ser.Sync(nameof(_pixBorderBuffer), ref _pixBorderBuffer, useNull: false);
			ser.Sync(nameof(_pixBufferBorderIndex), ref _pixBufferBorderIndex);
			ser.Sync(nameof(_pixBuffer), ref _pixBuffer, useNull: false);
			ser.Sync(nameof(_pixBufferIndex), ref _pixBufferIndex);
			ser.Sync(nameof(_pointerCb), ref _pointerCb);
			ser.Sync(nameof(_pointerVm), ref _pointerVm);
			ser.Sync(nameof(_rasterInterruptLine), ref _rasterInterruptLine);
			ser.Sync(nameof(_rasterLine), ref _rasterLine);
			ser.Sync(nameof(_rasterX), ref _rasterX);
			ser.Sync(nameof(_rasterXHold), ref _rasterXHold);
			ser.Sync(nameof(_rc), ref _rc);
			ser.Sync(nameof(_refreshCounter), ref _refreshCounter);
			ser.Sync(nameof(_renderEnabled), ref _renderEnabled);
			ser.Sync(nameof(_rowSelect), ref _rowSelect);
			ser.Sync(nameof(_spriteBackgroundCollisionClearPending), ref _spriteBackgroundCollisionClearPending);
			ser.Sync(nameof(_spriteSpriteCollisionClearPending), ref _spriteSpriteCollisionClearPending);
			ser.Sync(nameof(_spriteMulticolor0), ref _spriteMulticolor0);
			ser.Sync(nameof(_spriteMulticolor1), ref _spriteMulticolor1);

			foreach (var sprite in _sprites)
			{
				ser.BeginSection($"Sprite{sprite.Index}");
				sprite.SyncState(ser);
				ser.EndSection();
			}

			ser.Sync(nameof(_vc), ref _vc);
			ser.Sync(nameof(_vcbase), ref _vcbase);
			ser.Sync(nameof(_vcEnable), ref _vcEnable);
			ser.Sync(nameof(_videoMode), ref _videoMode);
			ser.Sync(nameof(_vmli), ref _vmli);
			ser.Sync(nameof(_xScroll), ref _xScroll);
			ser.Sync(nameof(_yScroll), ref _yScroll);
			
			if (ser.IsReader)
			{
				UpdateBorder();
				UpdateVideoMode();
			}
		}
	}
}
