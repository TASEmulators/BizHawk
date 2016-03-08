namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
	public sealed partial class Vic
	{
	    [SaveState.DoNotSave] private int _borderPixel;
	    [SaveState.DoNotSave] private int _bufferPixel;
	    [SaveState.DoNotSave] private int _ecmPixel;
		[SaveState.DoNotSave] private int _pixel;
		[SaveState.DoNotSave] private int _pixelCounter;
		[SaveState.DoNotSave] private int _pixelData;
		[SaveState.DoNotSave] private int _pixelOwner;
		[SaveState.DoNotSave] private int _sprData;
		[SaveState.DoNotSave] private int _sprIndex;
		[SaveState.DoNotSave] private int _sprPixel;
	    private int _srSync;
	    private int _srColorSync;
	    private int _srColorIndexLatch;
		private int _videoMode;
	    private int _borderOnShiftReg;

	    [SaveState.DoNotSave] private const int VideoMode000 = 0;
        [SaveState.DoNotSave] private const int VideoMode001 = 1;
        [SaveState.DoNotSave] private const int VideoMode010 = 2;
        [SaveState.DoNotSave] private const int VideoMode011 = 3;
        [SaveState.DoNotSave] private const int VideoMode100 = 4;
	    [SaveState.DoNotSave] private const int VideoModeInvalid = -1;

	    [SaveState.DoNotSave] private const int SrMask1 = 0x20000;
		[SaveState.DoNotSave] private const int SrMask2 = SrMask1 << 1;
		[SaveState.DoNotSave] private const int SrMask3 = SrMask1 | SrMask2;
	    [SaveState.DoNotSave] private const int SrColorMask = 0x8000;
        [SaveState.DoNotSave] private const int SrSpriteMask = SrSpriteMask2;
        [SaveState.DoNotSave] private const int SrSpriteMask1 = 0x400000;
		[SaveState.DoNotSave] private const int SrSpriteMask2 = SrSpriteMask1 << 1;
		[SaveState.DoNotSave] private const int SrSpriteMask3 = SrSpriteMask1 | SrSpriteMask2;
        [SaveState.DoNotSave] private const int SrSpriteMaskMc = SrSpriteMask3;

        private void Render()
		{
            if (_rasterX == _hblankEndCheckXRaster)
                _hblank = false;
            if (_rasterX == _hblankStartCheckXRaster)
                _hblank = true;

            _renderEnabled = !_hblank && !_vblank;
			_pixelCounter = -1;
			while (_pixelCounter++ < 3)
			{

			    if ((_srColorSync & SrColorMask) != 0)
			    {
                    _displayC = _bufferC[_srColorIndexLatch];
                    _srColorIndexLatch = (_srColorIndexLatch + 1) & 0x3F;
                }

                #region PRE-RENDER BORDER

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

                #endregion

                #region CHARACTER GRAPHICS
                switch (_videoMode)
				{
					case VideoMode000:
						_pixelData = _sr & SrMask2;
						_pixel = _pixelData != 0 ? _displayC >> 8 : _backgroundColor0;
						break;
					case VideoMode001:
						if ((_displayC & 0x800) != 0)
						{
						    // multicolor 001
							if ((_srSync & SrMask2) != 0)
								_pixelData = _sr & SrMask3;

						    switch (_pixelData)
						    {
						        case 0:
						            _pixel = _backgroundColor0;
						            break;
						        case SrMask1:
						            _pixel = _backgroundColor1;
						            break;
						        case SrMask2:
						            _pixel = _backgroundColor2;
						            break;
						        default:
						            _pixel = (_displayC & 0x700) >> 8;
						            break;
						    }
						}
						else
						{
							// standard 001
							_pixelData = _sr & SrMask2;
							_pixel = _pixelData != 0 ? _displayC >> 8 : _backgroundColor0;
						}
						break;
					case VideoMode010:
						_pixelData = _sr & SrMask2;
						_pixel = _pixelData != 0 ? _displayC >> 4 : _displayC;
						break;
					case VideoMode011:
						if ((_srSync & SrMask2) != 0)
							_pixelData = _sr & SrMask3;

						switch (_pixelData)
						{
						    case 0:
						        _pixel = _backgroundColor0;
						        break;
						    case SrMask1:
						        _pixel = _displayC >> 4;
						        break;
						    case SrMask2:
						        _pixel = _displayC;
						        break;
						    default:
						        _pixel = _displayC >> 8;
						        break;
						}
						break;
					case VideoMode100:
						_pixelData = _sr & SrMask2;
						if (_pixelData != 0)
						{
							_pixel = _displayC >> 8;
						}
						else
						{
						    _ecmPixel = (_displayC & 0xC0) >> 6;
						    switch (_ecmPixel)
						    {
						        case 0:
						            _pixel = _backgroundColor0;
						            break;
						        case 1:
						            _pixel = _backgroundColor1;
						            break;
						        case 2:
						            _pixel = _backgroundColor2;
						            break;
						        default:
						            _pixel = _backgroundColor3;
						            break;
						    }
						}
				        break;
					default:
						_pixelData = 0;
						_pixel = 0;
						break;
				}
				_pixel &= 0xF;
				_sr <<= 1;
				_srSync <<= 1;
			    _srColorSync <<= 1;
                #endregion

                #region SPRITES
                // render sprites
                _pixelOwner = -1;
				_sprIndex = 0;
				foreach (var spr in _sprites)
				{
					_sprData = 0;
					_sprPixel = _pixel;

					if (spr.X == _rasterX)
					{
						spr.ShiftEnable = spr.Display;
						spr.XCrunch = !spr.XExpand;
						spr.MulticolorCrunch = false;
					}
					else
					{
						spr.XCrunch |= !spr.XExpand;
					}

					if (spr.ShiftEnable) // sprite rule 6
					{
						if (spr.Multicolor)
						{
							_sprData = spr.Sr & SrSpriteMaskMc;
							if (spr.MulticolorCrunch && spr.XCrunch && !_rasterXHold)
							{
								if (spr.Loaded == 0)
								{
									spr.ShiftEnable = false;
								}
								spr.Sr <<= 2;
								spr.Loaded >>= 2;
							}
							spr.MulticolorCrunch ^= spr.XCrunch;
						}
						else
						{
							_sprData = spr.Sr & SrSpriteMask;
							if (spr.XCrunch && !_rasterXHold)
							{
								if (spr.Loaded == 0)
								{
									spr.ShiftEnable = false;
								}
								spr.Sr <<= 1;
								spr.Loaded >>= 1;
							}
						}
						spr.XCrunch ^= spr.XExpand;

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
							            _sprPixel = spr.Color;
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
									spr.CollideSprite = true;
									_sprites[_pixelOwner].CollideSprite = true;
								}
							}

							// sprite-data collision
							if (!_borderOnVertical && (_pixelData >= SrMask2))
							{
								spr.CollideData = true;
							}

							// sprite priority logic
							if (spr.Priority)
							{
								_pixel = _pixelData >= SrMask2 ? _pixel : _sprPixel;
							}
							else
							{
								_pixel = _sprPixel;
							}
						}
					}

					_sprIndex++;
				}

                #endregion

                #region POST-RENDER BORDER

                // border doesn't work with the background buffer
			    _borderPixel = _pixBorderBuffer[_pixBufferBorderIndex];
                _pixBorderBuffer[_pixBufferBorderIndex] = _borderColor;
				#endregion

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
			}

            if (_pixBufferBorderIndex >= PixBorderBufferSize)
                _pixBufferBorderIndex = 0;
            if (_pixBufferIndex >= PixBufferSize)
				_pixBufferIndex = 0;
		}
	}
}
