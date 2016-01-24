namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
	public sealed partial class Vic
	{
	    private const int BaResetCounter = 7;
	    private const int PipelineUpdateVc = 1;
	    private const int PipelineChkSprCrunch = 2;
	    private const int PipelineUpdateMcBase = 4;
	    private const int PipelineChkBrdL1 = 8;
	    private const int PipelineChkBrdL0 = 16;
	    private const int PipelineChkSprDma = 32;
	    private const int PipelineChkBrdR0 = 64;
	    private const int PipelineChkSprExp = 128;
	    private const int PipelineChkBrdR1 = 256;
	    private const int PipelineChkSprDisp = 512;
	    private const int PipelineUpdateRc = 1024;
	    private const int PipelineHBlankL = 0x10000000;
	    private const int PipelineHBlankR = 0x20000000;
	    private const int PipelineHoldX = 0x40000000;
	    private const int RasterIrqLine0Cycle = 1;
	    private const int RasterIrqLineXCycle = 0;

		private int _parseaddr;
		private int _parsecycleBAsprite0;
		private int _parsecycleBAsprite1;
		private int _parsecycleBAsprite2;
		private int _parsecycleFetchSpriteIndex;
		private int _parsefetch;
		private int _parsefetchType;
		private int _parseba;
		private int _parseact;

		private void ParseCycle()
		{
			_parseaddr = 0x3FFF;
			_parsefetch = _fetchPipeline[_cycleIndex];
			_parseba = _baPipeline[_cycleIndex];
			_parseact = _actPipeline[_cycleIndex];

			// apply X location
			_rasterX = _rasterXPipeline[_cycleIndex];
			_rasterXHold = (_parseact & PipelineHoldX) != 0;

			// perform fetch
			_parsefetchType = _parsefetch & 0xFF00;
			switch (_parsefetchType)
			{
			    case 0x100:
			        // fetch R
			        _refreshCounter = (_refreshCounter - 1) & 0xFF;
			        _parseaddr = 0x3F00 | _refreshCounter;
			        ReadMemory(_parseaddr);
			        break;
			    case 0x200:
			        if (!_idle)
			        {
			            if (_badline)
			            {
			                _parseaddr = _pointerVm | _vc;
			                _dataC = ReadMemory(_parseaddr);
			                _dataC |= (ReadColorRam(_parseaddr) & 0xF) << 8;
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
			        _srColorSync |= 0x01 << (7 - _xScroll);
                    break;
			    case 0x300:
			        // fetch G
			        if (_idle)
			            _parseaddr = 0x3FFF;
			        else
			        {
			            if (_bitmapMode)
			                _parseaddr = _rc | (_vc << 3) | ((_pointerCb & 0x4) << 11);
			            else
			                _parseaddr = _rc | ((_dataC & 0xFF) << 3) | (_pointerCb << 11);
			        }
			        if (_extraColorMode)
			            _parseaddr &= 0x39FF;
			        _dataG = ReadMemory(_parseaddr);
			        _sr |= _dataG << (7 - _xScroll);
			        _srSync |= 0xAA << (7 - _xScroll);
                    if (!_idle)
                    {
                        _bufferG[_vmli] = _dataG;
			            _vmli = (_vmli + 1) & 0x3F;
			            _vc = (_vc + 1) & 0x3FF;
			        }
			        break;
			    case 0x400:
			        // fetch I
			        _parseaddr = _extraColorMode ? 0x39FF : 0x3FFF;
			        _dataG = ReadMemory(_parseaddr);
			        break;
			    case 0x500:
			        // fetch none
			        break;
			    default:
			        _parsecycleFetchSpriteIndex = _parsefetch & 0x7;
			        if ((_parsefetch & 0xF0) == 0) // sprite rule 5
			        {
			            // fetch P
			            _parseaddr = 0x3F8 | _pointerVm | _parsecycleFetchSpriteIndex;
			            _sprites[_parsecycleFetchSpriteIndex].Pointer = ReadMemory(_parseaddr);
			            _sprites[_parsecycleFetchSpriteIndex].ShiftEnable = false;
			        }
			        else
			        {
			            // fetch S
			            var spr = _sprites[_parsecycleFetchSpriteIndex];
			            if (spr.Dma)
			            {
			                _parseaddr = spr.Mc | (spr.Pointer << 6);
			                spr.Sr |= ReadMemory(_parseaddr) << ((0x30 - (_parsefetch & 0x30)) >> 1);
			                spr.Mc++;
			                spr.Loaded |= 0x800000;
			            }
			        }
			        break;
			}

			// perform BA flag manipulation
			switch (_parseba)
			{
			    case 0x0000:
			        _pinBa = true;
			        break;
			    case 0x1000:
			        _pinBa = !_badline;
			        break;
			    default:
			        _parsecycleBAsprite0 = _parseba & 0x000F;
			        _parsecycleBAsprite1 = (_parseba & 0x00F0) >> 4;
			        _parsecycleBAsprite2 = (_parseba & 0x0F00) >> 8;
			        if ((_parsecycleBAsprite0 < 8 && _sprites[_parsecycleBAsprite0].Dma) ||
			            (_parsecycleBAsprite1 < 8 && _sprites[_parsecycleBAsprite1].Dma) ||
			            (_parsecycleBAsprite2 < 8 && _sprites[_parsecycleBAsprite2].Dma))
			            _pinBa = false;
			        else
			            _pinBa = true;
			        break;
			}

			// perform actions
			_borderCheckLEnable = (_parseact & (PipelineChkBrdL0 | PipelineChkBrdL1)) != 0;
			_borderCheckREnable = (_parseact & (PipelineChkBrdR0 | PipelineChkBrdR1)) != 0;
			_hblankCheckEnableL = (_parseact & PipelineHBlankL) != 0;
			_hblankCheckEnableR = (_parseact & PipelineHBlankR) != 0;

			foreach (var spr in _sprites)
			{
				if (!spr.YExpand)
					spr.YCrunch = true;
			}

			if ((_parseact & PipelineChkSprExp) != 0)
			{
				foreach (var spr in _sprites)
				{
					if (spr.YExpand)
						spr.YCrunch ^= true;
				}
			}

			if ((_parseact & PipelineChkSprDma) != 0)
			{
				foreach (var spr in _sprites)
				{
					if (spr.Enable && spr.Y == (_rasterLine & 0xFF) && !spr.Dma)
					{
						spr.Dma = true;
						spr.Mcbase = 0;
						spr.YCrunch = !spr.YExpand;
					}
				}
			}

			if ((_parseact & PipelineChkSprDisp) != 0)
			{
				foreach (var spr in _sprites)
				{
					spr.Mc = spr.Mcbase;
					if (spr.Dma && spr.Y == (_rasterLine & 0xFF))
					{
						spr.Display = true;
					}
					else if (!spr.Dma)
					{
						spr.Display = false;
					}
				}
			}

			if ((_parseact & PipelineChkSprCrunch) != 0)
			{
				// not sure if anything has to go here,
				// some sources say yes, some say no...
			}

			if ((_parseact & PipelineUpdateMcBase) != 0)
			{
				foreach (var spr in _sprites)
				{
					if (spr.YCrunch)
					{
						spr.Mcbase = spr.Mc;
						if (spr.Mcbase == 63)
						{
							if (!spr.YCrunch)
							{
							}
							spr.Dma = false;
						}
					}
                }
			}

			if ((_parseact & PipelineUpdateRc) != 0) // VC/RC rule 5
			{
				if (_rc == 7)
				{
					_idle = true;
					_vcbase = _vc;
				}
				if (!_idle)
					_rc = (_rc + 1) & 0x7;
			}

			if ((_parseact & PipelineUpdateVc) != 0) // VC/RC rule 2
			{
				_vc = _vcbase;
			    _srColorIndexLatch = 0;
				_vmli = 0;
				if (_badline)
					_rc = 0;
			}

			_cycleIndex++;
		}
	}
}
