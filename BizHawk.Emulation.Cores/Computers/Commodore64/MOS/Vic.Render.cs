namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
	public sealed partial class Vic
	{
		private int _bufferPixel;
		private int _gfxData;
		private bool _gfxSense;
		private int _gfxJitter;
		private bool _gfxMc;
		private int _pixel;
		private int _pixelCounter;
		private int _sprOwner;
		private Sprite _spr;
		private int _sprData;
		private int _sprIndex;
		private int _sprPixel;
		private bool _sprSense;
		private bool _sprPriority;
		private int _srData1;
		private int _videoMode;

		private const int VideoMode000 = 0;
		private const int VideoMode001 = 1;
		private const int VideoMode010 = 2;
		private const int VideoMode011 = 3;
		private const int VideoMode100 = 4;
		private const int VideoModeInvalid = -1;

		private const int SrMask0 = 0x4000000;
		private const int SrSpriteMask = SrSpriteMask2;
		private const int SrSpriteMask1 = 0x400000;
		private const int SrSpriteMask2 = SrSpriteMask1 << 1;
		private const int SrSpriteMask3 = SrSpriteMask1 | SrSpriteMask2;
		private const int SrSpriteMaskMc = SrSpriteMask3;

		private void Render()
		{
			if (_rasterX == _hblankEndCheckXRaster)
				_hblank = false;
			if (_rasterX == _hblankStartCheckXRaster)
				_hblank = true;

			_renderEnabled = !_hblank && !_vblank;
			_pixelCounter = 4;
			while (--_pixelCounter >= 0)
			{
				#region PRE-RENDER BORDER

				// check left border
				if (_rasterX == _borderL)
				{
					if (_rasterLine == _borderB)
						_borderOnVertical = true;
					if (_cycle == _totalCycles && _rasterLine == _borderT && _displayEnable)
						_borderOnVertical = false;
					if (!_borderOnVertical)
						_borderOnMain = false;
				}

				// check right border
				if (_rasterX == _borderR)
					_borderOnMain = true;

				#endregion

				// render graphics
				
				#region Graphics Sequencer

				if (_xScroll == (_rasterX & 0x7))
				{
					_displayC = _dataCPrev & 0xFFF;
				}
				_gfxMc = _multicolorMode && (_bitmapMode || (_displayC & 0x800) != 0);

				_pixel = _backgroundColor0;
				_gfxJitter = _gfxMc ? (_xScroll ^ _rasterX) & 1 : 0;
				_srData1 <<= 1;
				_gfxData = _srData1 >> (18 + _gfxJitter); // bit 1-0 has the histogram
				_gfxSense = (_gfxData & 2) != 0; // bit 1 is used for foreground data purposes too

				switch (_videoMode)
				{
					case VideoMode000:
					{
						if (_gfxSense)
							_pixel = _displayC >> 8;
						break;
					}
					case VideoMode001:
					{
						if (_gfxMc)
						{
							switch (_gfxData & 0x3)
							{
								case 0x1:
									_pixel = _backgroundColor1;
									break;
								case 0x2:
									_pixel = _backgroundColor2;
									break;
								case 0x3:
									_pixel = (_displayC >> 8) & 7;
									break;
							}
						}
						else if (_gfxSense)
						{
							_pixel = _displayC >> 8;
						}
						break;
					}
					case VideoMode010:
					{
						_pixel = (_gfxSense ? _displayC >> 4 : _displayC) & 0xF;
						break;
					}
					case VideoMode011:
					{
						switch (_gfxData & 0x3)
						{
							case 0x1:
								_pixel = (_displayC >> 4) & 0xF;
								break;
							case 0x2:
								_pixel = _displayC & 0xF;
								break;
							case 0x3:
								_pixel = (_displayC >> 8) & 0xF;
								break;
						}
						break;
					}
					case VideoMode100:
					{
						if (_gfxSense)
						{
							_pixel = (_displayC >> 8) & 0xF;
						}
						else
						{
							switch (_displayC & 0xC0)
							{
								case 0x40:
								{
									_pixel = _backgroundColor1;
									break;
								}
								case 0x80:
								{
									_pixel = _backgroundColor2;
									break;
								}
								case 0xC0:
								{
									_pixel = _backgroundColor3;
									break;
								}
							}
						}
						break;
					}
					default:
					{
						_pixel = 0;
						break;
					}
				}
				
				#endregion Graphics Sequencer
				
				// render sprites
				
				#region Sprite Sequencer + Mux Collision
				
				_sprOwner = -1;
				_sprSense = false;
				for (_sprIndex = 0; _sprIndex < 8; _sprIndex++)
				{
					_spr = _sprites[_sprIndex];
					_sprData = 0;

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
								if (_spr.Sr == 0)
								{
									_spr.ShiftEnable = false;
								}
								_spr.Sr <<= 2;
							}
							_spr.MulticolorCrunch ^= _spr.XCrunch;
						}
						else
						{
							_sprData = _spr.Sr & SrSpriteMask;
							if (_spr.XCrunch && !_rasterXHold)
							{
								if (_spr.Sr == 0)
								{
									_spr.ShiftEnable = false;
								}
								_spr.Sr <<= 1;
							}
						}
						_spr.XCrunch ^= _spr.XExpand;

						if (_sprData != 0)
						{
							// sprite-sprite collision
							if (_sprSense)
							{
								_spr.CollideSprite = true;
								_sprites[_sprOwner].CollideSprite = true;
								_intSpriteCollision = true;
							}
							else
							{
								_sprSense = true;
								_sprOwner = _sprIndex;
								_sprPriority = _spr.Priority;
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
							}

							// sprite-data collision
							if (_gfxSense)
							{
								_spr.CollideData = true;
								_intSpriteDataCollision = true;
							}
						}
					}
				}
				
				#endregion Sprite Sequencer + Mux Collision

				#region Mux Color
				
				// sprite priority logic
				if (_sprSense && (!_sprPriority || !_gfxSense))
					_pixel = _sprPixel;

				#endregion Mux Color
				
				// plot pixel if within viewing area
				if (_renderEnabled)
				{
					_bufferPixel = (_borderOnVertical || _borderOnMain) ? _borderColor : _pixel;
					_buf[_bufOffset++] = Palette[_bufferPixel & 0xF];
					if (_bufOffset == _bufLength)
						_bufOffset = 0;
				}

				if (!_rasterXHold)
					_rasterX++;
			}
		}
	}
}
