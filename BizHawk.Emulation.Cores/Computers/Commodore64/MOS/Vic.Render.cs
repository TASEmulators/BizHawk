namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
	public sealed partial class Vic
	{
	    private int _ecmPixel;
		private int _pixel;
		private int _pixelCounter;
		private int _pixelData;
		private int _pixelOwner;
		private int _sprData;
		private int _sprIndex;
		private int _sprPixel;
	    private int _srSync;
	    private int _srColorSync;
	    private int _srColorIndexLatch;
		private int _videoMode;

		private void Render()
		{
			if (_hblankCheckEnableL)
			{
				if (_rasterX == _hblankEnd)
					_hblank = false;
			}
			else if (_hblankCheckEnableR)
			{
				if (_rasterX == _hblankStart)
					_hblank = true;
			}

			_renderEnabled = !_hblank && !_vblank;
			_pixelCounter = -1;
			while (_pixelCounter++ < 3)
			{

			    if ((_srColorSync & _srColorMask) != 0)
			    {
                    _displayC = _bufferC[_srColorIndexLatch];
                    _srColorIndexLatch = (_srColorIndexLatch + 1) & 0x3F;
                }

                #region PRE-RENDER BORDER
                if (_borderCheckLEnable && (_rasterX == _borderL))
				{
					if (_rasterLine == _borderB)
						_borderOnVertical = true;
					if (_rasterLine == _borderT && _displayEnable)
						_borderOnVertical = false;
					if (!_borderOnVertical)
						_borderOnMain = false;
				}
				#endregion

				#region CHARACTER GRAPHICS
				switch (_videoMode)
				{
					case 0:
						_pixelData = _sr & _srMask2;
						_pixel = _pixelData != 0 ? _displayC >> 8 : _backgroundColor0;
						break;
					case 1:
						if ((_displayC & 0x800) != 0)
						{
							// multicolor 001
							if ((_srSync & _srMask2) != 0)
								_pixelData = _sr & _srMask3;

							if (_pixelData == 0)
								_pixel = _backgroundColor0;
							else if (_pixelData == _srMask1)
								_pixel = _backgroundColor1;
							else if (_pixelData == _srMask2)
								_pixel = _backgroundColor2;
							else
								_pixel = (_displayC & 0x700) >> 8;
						}
						else
						{
							// standard 001
							_pixelData = _sr & _srMask2;
							_pixel = _pixelData != 0 ? _displayC >> 8 : _backgroundColor0;
						}
						break;
					case 2:
						_pixelData = _sr & _srMask2;
						_pixel = _pixelData != 0 ? _displayC >> 4 : _displayC;
						break;
					case 3:
						if ((_srSync & _srMask2) != 0)
							_pixelData = _sr & _srMask3;

						if (_pixelData == 0)
							_pixel = _backgroundColor0;
						else if (_pixelData == _srMask1)
							_pixel = _displayC >> 4;
						else if (_pixelData == _srMask2)
							_pixel = _displayC;
						else
							_pixel = _displayC >> 8;
						break;
					case 4:
						_pixelData = _sr & _srMask2;
						if (_pixelData != 0)
						{
							_pixel = _displayC >> 8;
						}
						else
						{
						    _ecmPixel = _displayC & 0xC0;
						    switch (_ecmPixel)
						    {
						        case 0x00:
						            _pixel = _backgroundColor0;
						            break;
						        case 0x40:
						            _pixel = _backgroundColor1;
						            break;
						        case 0x80:
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
							_sprData = spr.Sr & _srSpriteMaskMc;
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
							_sprData = spr.Sr & _srSpriteMask;
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
								if (_sprData == _srSpriteMask1)
									_sprPixel = _spriteMulticolor0;
								else if (_sprData == _srSpriteMask2)
									_sprPixel = spr.Color;
								else if (_sprData == _srSpriteMask3)
									_sprPixel = _spriteMulticolor1;
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
							if (!_borderOnVertical && (_pixelData >= _srMask2))
							{
								spr.CollideData = true;
							}

							// sprite priority logic
							if (spr.Priority)
							{
								_pixel = _pixelData >= _srMask2 ? _pixel : _sprPixel;
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
				if (_borderCheckREnable && (_rasterX == _borderR))
					_borderOnMain = true;

				// border doesn't work with the background buffer
				if (_borderOnMain || _borderOnVertical)
					_pixel = _borderColor;
				#endregion

				// plot pixel if within viewing area
				if (_renderEnabled)
				{
					_buf[_bufOffset] = Palette[_pixBuffer[_pixBufferIndex]];
					_bufOffset++;
					if (_bufOffset == _bufLength)
						_bufOffset = 0;
				}

				_pixBuffer[_pixBufferIndex] = _pixel;
				_pixBufferIndex++;

				if (!_rasterXHold)
					_rasterX++;
			}

			if (_pixBufferIndex >= PixBufferSize)
				_pixBufferIndex = 0;
		}
	}
}
