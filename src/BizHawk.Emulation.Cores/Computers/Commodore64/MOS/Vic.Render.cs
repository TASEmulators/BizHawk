namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
	public sealed partial class Vic
	{
		private int _borderPixel;
		private int _bufferPixel;
		private int _pixel;
		private int _pixelCounter;
		private int _pixelOwner;
		private Sprite _spr;
		private int _sprData;
		private int _sprIndex;
		private int _sprPixel;
		private int _srColor0;
		private int _srColor1;
		private int _srColor2;
		private int _srColor3;
		private int _srData1;
		private int _srColorEnable;
		private int _videoMode;
		private int _borderOnShiftReg;

		private const int VideoMode000 = 0;
		private const int VideoMode001 = 1;
		private const int VideoMode010 = 2;
		private const int VideoMode011 = 3;
		private const int VideoMode100 = 4;
		private const int VideoModeInvalid = -1;

		private const int SrMask1 = 0x40000;
		private const int SrSpriteMask = SrSpriteMask2;
		private const int SrSpriteMask1 = 0x400000;
		private const int SrSpriteMask2 = SrSpriteMask1 << 1;
		private const int SrSpriteMask3 = SrSpriteMask1 | SrSpriteMask2;
		private const int SrSpriteMaskMc = SrSpriteMask3;

		private void Render()
		{
			void PreRenderBorder()
			{
				// check left border
				if (_borderCheckLEnable && (_rasterX == _borderL))
				{
					if (_rasterLine == _borderB)
						_borderOnVertical = true;
					if (_cycle == _totalCycles && _rasterLine == _borderT && _displayEnable)
						_borderOnVertical = false;
					if (!_borderOnVertical)
						_borderOnMain = false;
				}

				// check right border
				if (_borderCheckREnable && (_rasterX == _borderR))
					_borderOnMain = true;
			}

			void PostRenderBorder()
			{
				// border doesn't work with the background buffer
				_borderPixel = _pixBorderBuffer[_pixBufferBorderIndex];
				_pixBorderBuffer[_pixBufferBorderIndex] = _borderColor;
			}

			if (_rasterX == _hblankEndCheckXRaster)
				_hblank = false;
			if (_rasterX == _hblankStartCheckXRaster)
				_hblank = true;

			_renderEnabled = !_hblank && !_vblank;
			_pixelCounter = 4;
			while (--_pixelCounter >= 0)
			{
				PreRenderBorder();

				// render graphics
				if ((_srColorEnable & SrMask1) != 0)
				{
					_pixel = ((_srColor0 & SrMask1) >> 18) |
						((_srColor1 & SrMask1) >> 17) |
						((_srColor2 & SrMask1) >> 16) |
						((_srColor3 & SrMask1) >> 15);
				}
				else
				{
					switch (((_srColor0 & SrMask1) >> 18) | ((_srColor1 & SrMask1) >> 17))
					{
						case 1:
							_pixel = _idle ? 0 : _backgroundColor1;
							break;
						case 2:
							_pixel = _idle ? 0 : _backgroundColor2;
							break;
						case 3:
							_pixel = _idle ? 0 : _backgroundColor3;
							break;
						default:
							_pixel = _backgroundColor0;
							break;
					}
				}

				// render sprites
				_pixelOwner = -1;
				for (_sprIndex = 0; _sprIndex < 8; _sprIndex++)
				{
					_spr = _sprites[_sprIndex];
					_sprData = 0;
					_sprPixel = _pixel;

					if (_spr.X == _rasterX)
					{
						_spr.ShiftEnable = _spr.Display;
						_spr.XCrunch = !_spr.XExpand;
						_spr.MulticolorCrunch = false;
					}
					else
					{
						_spr.XCrunch |= !_spr.XExpand;
					}

					if (_spr.ShiftEnable) // sprite rule 6
					{
						if (_spr.Multicolor)
						{
							_sprData = _spr.Sr & SrSpriteMaskMc;
							if (_spr.MulticolorCrunch && _spr.XCrunch && !_rasterXHold)
							{
								if (_spr.Loaded == 0)
								{
									_spr.ShiftEnable = false;
								}
								_spr.Sr <<= 2;
								_spr.Loaded >>= 2;
							}
							_spr.MulticolorCrunch ^= _spr.XCrunch;
						}
						else
						{
							_sprData = _spr.Sr & SrSpriteMask;
							if (_spr.XCrunch && !_rasterXHold)
							{
								if (_spr.Loaded == 0)
								{
									_spr.ShiftEnable = false;
								}
								_spr.Sr <<= 1;
								_spr.Loaded >>= 1;
							}
						}
						_spr.XCrunch ^= _spr.XExpand;

						if (_sprData != 0)
						{
							// sprite-sprite collision
							if (_pixelOwner < 0)
							{
								switch (_sprData)
								{
									case SrSpriteMask1:
										_sprPixel = _spriteMulticolor0;
										break;
									case SrSpriteMask2:
										_sprPixel = _spr.Color;
										break;
									case SrSpriteMask3:
										_sprPixel = _spriteMulticolor1;
										break;
								}
								_pixelOwner = _sprIndex;
							}
							else
							{
								if (!_borderOnVertical)
								{
									_spr.CollideSprite = true;
									_sprites[_pixelOwner].CollideSprite = true;
									_intSpriteCollision = true;
								}
							}

							// sprite-data collision
							if (!_borderOnVertical && (_srData1 & SrMask1) != 0)
							{
								_spr.CollideData = true;
								_intSpriteDataCollision = true;
							}

							// sprite priority logic
							if (_spr.Priority)
							{
								_pixel = (_srData1 & SrMask1) != 0 ? _pixel : _sprPixel;
							}
							else
							{
								_pixel = _sprPixel;
							}
						}
					}
				}

				PostRenderBorder();

				// plot pixel if within viewing area
				if (_renderEnabled)
				{
					_bufferPixel = (_borderOnShiftReg & 0x80000) != 0 ? _borderPixel : _pixBuffer[_pixBufferIndex];
					_buf[_bufOffset] = Palette[_bufferPixel];
					_bufOffset++;
					if (_bufOffset == _bufLength)
						_bufOffset = 0;
				}

				_borderOnShiftReg <<= 1;
				_borderOnShiftReg |= (_borderOnVertical || _borderOnMain) ? 1 : 0;
				_pixBuffer[_pixBufferIndex] = _pixel;
				_pixBufferIndex++;
				_pixBufferBorderIndex++;

				if (!_rasterXHold)
					_rasterX++;
				
				_srColor0 <<= 1;
				_srColor1 <<= 1;
				_srColor2 <<= 1;
				_srColor3 <<= 1;
				_srData1 <<= 1;
				_srColorEnable <<= 1;
			}

			if (_pixBufferBorderIndex >= PixBorderBufferSize)
			{
				_pixBufferBorderIndex = 0;
			}

			if (_pixBufferIndex >= PixBufferSize)
			{
				_pixBufferIndex = 0;
			}
		}
	}
}
