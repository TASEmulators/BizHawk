namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
	public sealed partial class Vic
	{
		private const int BaResetCounter = 3;
		private const int PipelineUpdateVc = 0x00000001; // vc/rc rule 2
		private const int PipelineSpriteCrunch = 0x00000002;
		private const int PipelineUpdateMcBase = 0x00000004;
		private const int PipelineBorderLeft1 = 0x00000008;
		private const int PipelineBorderLeft0 = 0x00000010;
		private const int PipelineSpriteDma = 0x00000020; // sprite rule 3
		private const int PipelineBorderRight0 = 0x00000040;
		private const int PipelineSpriteExpansion = 0x00000080; // sprite rule 2
		private const int PipelineBorderRight1 = 0x00000100;
		private const int PipelineSpriteDisplay = 0x00000200; // sprite rule 4
		private const int PipelineUpdateRc = 0x00000400; // vc/rc rule 5
		private const int PipelineHoldX = 0x40000000;
		private const int RasterIrqLine0Cycle = 2;
		private const int RasterIrqLineXCycle = 1;
		private const int FetchTypeSprite = 0x0000;
		private const int FetchTypeRefresh = 0x0100;
		private const int FetchTypeColor = 0x0200;
		private const int FetchTypeGraphics = 0x0300;
		private const int FetchTypeIdle = 0x0400;
		private const int FetchTypeNone = 0x0500;
		private const int BaTypeNone = 0x0888;
		private const int BaTypeCharacter = 0x1000;
		private const int BaTypeMaskSprite0 = 0x000F;
		private const int BaTypeMaskSprite1 = 0x00F0;
		private const int BaTypeMaskSprite2 = 0x0F00;
		private const int AddressMask = 0x3FFF;
		private const int AddressMaskEc = 0x39FF;
		private const int AddressMaskRefresh = 0x3F00;

		private int _parseAddr;
		private int _parseCycleBaSprite0;
		private int _parseCycleBaSprite1;
		private int _parseCycleBaSprite2;
		private int _parseCycleFetchSpriteIndex;
		private int _parseFetch;
		private int _parseFetchType;
		private int _parseBa;
		private int _parseAct;
		private bool _parseIsSprCrunch;
		private int _parsePixelData;
		private int _parsePixelDataIndex;
		private int _parseSrData0;
		private int _parseSrData1;
		private int _parseSrShift;
		private bool _parseSrColorEnable;

		private void ParseCycle()
		{
			// initialization
			_parseAddr = AddressMask;
			_parseFetch = _fetchPipeline[_cycleIndex];
			_parseBa = _baPipeline[_cycleIndex];
			_parseAct = _actPipeline[_cycleIndex];

			// apply X location
			_rasterX = _rasterXPipeline[_cycleIndex];
			_rasterXHold = (_parseAct & PipelineHoldX) != 0;

			// perform fetch
			_parseFetchType = _parseFetch & 0xFF00;
			switch (_parseFetchType)
			{
				case FetchTypeColor:
					// fetch C
					if (!_idle)
					{
						if (_badline)
						{
							_parseAddr = _pointerVm | _vc;
							_dataC = _baCount >= 0 ? 0xFF : ReadMemory(_parseAddr);
							_dataC |= (ReadColorRam(_parseAddr) & 0xF) << 8;
							_bufferC[_vmli] = _dataC;
						}
						else
						{
							_dataC = _bufferC[_vmli];
						}
					}
					else
					{
						_dataC = 0;
						_bufferC[_vmli] = _dataC;
					}
					break;
				case FetchTypeGraphics:
					// fetch G
					if (!_idle)
					{
						if (_bitmapMode)
							_parseAddr = _rc | (_vc << 3) | ((_pointerCb & 0x4) << 11);
						else
							_parseAddr = _rc | ((_dataC & 0xFF) << 3) | (_pointerCb << 11);
					}

					if (_extraColorMode)
						_parseAddr &= AddressMaskEc;
					_dataG = ReadMemory(_parseAddr);

					if (!_idle && _vcEnable)
					{
						_vmli = (_vmli + 1) & 0x3F;
						_vc = (_vc + 1) & 0x3FF;
					}

					// graphics data shift register
					_parseSrShift = 7 - _xScroll;
					_srData1 &= ~(0xFF << _parseSrShift);
					_srColorEnable &= ~(0xFF << _parseSrShift);

					if (_multicolorMode && (_bitmapMode || (_dataC & 0x800) != 0))
					{
						_parseSrData0 = (_dataG & 0x55) | ((_dataG & 0x55) << 1);
						_parseSrData1 = (_dataG & 0xAA) | ((_dataG & 0xAA) >> 1);
					}
					else
					{
						_parseSrData0 = _parseSrData1 = _dataG;
					}
					_srData1 |= _parseSrData1 << _parseSrShift;

					if (_bitmapMode && !_multicolorMode)
						_parseSrData1 ^= 0xFF;

					// graphics color shift register
					_srColor0 &= ~(0xFF << _parseSrShift);
					_srColor1 &= ~(0xFF << _parseSrShift);
					_srColor2 &= ~(0xFF << _parseSrShift);
					_srColor3 &= ~(0xFF << _parseSrShift);
					for (_parsePixelDataIndex = 7; _parsePixelDataIndex >= 0; _parsePixelDataIndex--)
					{
						_parsePixelData = ((_parseSrData0 & 0x80) >> 7) | ((_parseSrData1 & 0x80) >> 6);
						switch (_videoMode)
						{
							case VideoMode000:
							case VideoMode001:
								if (_parsePixelData == 3)
								{
									_pixel = _idle ? 0 : (_multicolorMode ? _dataC & 0x700 : _dataC) >> 8;
									_parseSrColorEnable = true;
								}
								else
								{
									_pixel = _parsePixelData;
									_parseSrColorEnable = false;
								}
								break;
							case VideoMode010:
							case VideoMode011:
								_parseSrColorEnable = _parsePixelData != 0;
								switch (_parsePixelData)
								{
									case 0:
										_pixel = 0;
										break;
									case 1:
										_pixel = _idle ? 0 : _dataC >> 4;
										break;
									case 2:
										_pixel = _idle ? 0 : _dataC;
										break;
									default:
										_pixel = _idle ? 0 : _dataC >> 8;
										break;
								}
								break;
							case VideoMode100:
								if (_parsePixelData != 0)
								{
									_pixel = _idle ? 0 : _dataC >> 8;
									_parseSrColorEnable = true;
								}
								else
								{
									_pixel = (_dataC & 0xC0) >> 6;
									_parseSrColorEnable = false;
								}
								break;
							default:
								_parsePixelData = 0;
								_pixel = 0;
								_parseSrColorEnable = true;
								break;
						}

						_parseSrData0 <<= 1;
						_parseSrData1 <<= 1;
						_srColor0 |= (_pixel & 1) << (_parseSrShift + _parsePixelDataIndex);
						_srColor1 |= ((_pixel >> 1) & 1) << (_parseSrShift + _parsePixelDataIndex);
						_srColor2 |= ((_pixel >> 2) & 1) << (_parseSrShift + _parsePixelDataIndex);
						_srColor3 |= ((_pixel >> 3) & 1) << (_parseSrShift + _parsePixelDataIndex);
						_srColorEnable |= (_parseSrColorEnable ? 1 : 0) << (_parseSrShift + _parsePixelDataIndex);
					}
					
					break;
				case FetchTypeNone:
					// fetch none
					break;
				case FetchTypeRefresh:
					// fetch R
					_refreshCounter = (_refreshCounter - 1) & 0xFF;
					_parseAddr = AddressMaskRefresh | _refreshCounter;
					ReadMemory(_parseAddr);
					break;
				case FetchTypeIdle:
					// fetch I
					ReadMemory(AddressMask);
					break;
				default:
					_parseCycleFetchSpriteIndex = _parseFetch & 0x7;
					if ((_parseFetch & 0xF0) == 0) // sprite rule 5
					{
						// fetch P
						_parseAddr = 0x3F8 | _pointerVm | _parseCycleFetchSpriteIndex;
						_sprites[_parseCycleFetchSpriteIndex].Pointer = ReadMemory(_parseAddr);
						_sprites[_parseCycleFetchSpriteIndex].ShiftEnable = false;
					}
					else
					{
						// fetch S
						var spr = _sprites[_parseCycleFetchSpriteIndex];
						if (spr.Dma)
						{
							_parseAddr = spr.Mc | (spr.Pointer << 6);
							spr.Sr |= ReadMemory(_parseAddr) << ((0x30 - (_parseFetch & 0x30)) >> 1);
							spr.Mc = (spr.Mc + 1) & 0x3F;
							spr.Loaded |= 0x800000;
						}
						else if ((_parseFetch & 0xF0) == 0x20)
						{
							ReadMemory(AddressMask);
						}
					}
					break;
			}

			// perform actions
			_borderCheckLEnable = (_parseAct & (PipelineBorderLeft0 | PipelineBorderLeft1)) != 0;
			_borderCheckREnable = (_parseAct & (PipelineBorderRight0 | PipelineBorderRight1)) != 0;

			foreach (var spr in _sprites) // sprite rule 1
			{
				if (!spr.YExpand)
				{
					spr.YCrunch = true;
				}
			}

			if ((_parseAct & PipelineUpdateMcBase) != 0) // VIC addendum sprite rule 7
			{
				foreach (var spr in _sprites)
				{
					if (spr.YCrunch)
					{
						spr.Mcbase = spr.Mc;
						if (spr.Mcbase == 63)
						{
							spr.Dma = false;
						}
					}
				}
			}

			if ((_parseAct & PipelineSpriteDma) != 0) // sprite rule 3
			{
				foreach (var spr in _sprites)
				{
					if (spr.Enable && spr.Y == (_rasterLine & 0xFF) && !spr.Dma)
					{
						spr.Dma = true;
						spr.Mcbase = 0;
						spr.YCrunch = spr.YExpand;
					}
				}
			}

			if ((_parseAct & PipelineSpriteExpansion) != 0) // sprite rule 2
			{
				foreach (var spr in _sprites)
				{
					if (spr.Dma && spr.YExpand)
					{
						spr.YCrunch ^= true;
					}
				}
			}

			if ((_parseAct & PipelineSpriteDisplay) != 0) // VIC addendum on sprite rule 4
			{
				foreach (var spr in _sprites)
				{
					spr.Mc = spr.Mcbase;
					if (spr.Dma)
					{
						if (spr.Enable && spr.Y == (_rasterLine & 0xFF))
						{
							spr.Display = true;
						}
					}
					else
					{
						spr.Display = false;
					}
				}
			}

			_parseIsSprCrunch = (_parseAct & PipelineSpriteCrunch) != 0; // VIC addendum sprite rule 7

			if ((_parseAct & PipelineUpdateVc) != 0) // VC/RC rule 2
			{
				_vc = _vcbase;
				_vmli = 0;
				if (_badline)
				{
					_rc = 0;
				}
			}

			if ((_parseAct & PipelineUpdateRc) != 0) // VC/RC rule 5
			{
				if (_rc == 7)
				{
					_idle = true;
					_vcbase = _vc;
				}

				if (!_idle || _badline)
				{
					_rc = (_rc + 1) & 0x7;
					_idle = false;
				}
			}

			// perform BA flag manipulation
			switch (_parseBa)
			{
				case BaTypeNone:
					_ba = true;
					break;
				case BaTypeCharacter:
					_ba = !_badline;
					break;
				default:
					_parseCycleBaSprite0 = _parseBa & BaTypeMaskSprite0;
					_parseCycleBaSprite1 = (_parseBa & BaTypeMaskSprite1) >> 4;
					_parseCycleBaSprite2 = (_parseBa & BaTypeMaskSprite2) >> 8;
					_ba = !((_parseCycleBaSprite0 < 8 && _sprites[_parseCycleBaSprite0].Dma) ||
						(_parseCycleBaSprite1 < 8 && _sprites[_parseCycleBaSprite1].Dma) ||
						(_parseCycleBaSprite2 < 8 && _sprites[_parseCycleBaSprite2].Dma));
					break;
			}

			_cycleIndex++;
		}
	}
}
