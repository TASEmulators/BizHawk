namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
	public sealed partial class Vic
	{
		public int Peek(int addr)
		{
			return ReadRegister(addr & 0x3F);
		}

		public void Poke(int addr, int val)
		{
			WriteRegister(addr & 0x3F, val);
		}

		public int Read(int addr)
		{
			addr &= 0x3F;

			switch (addr)
			{
				case 0x1E:
                    _spriteSpriteCollisionClearPending = true;
                    return ReadRegister(addr);
                case 0x1F:
			        _spriteBackgroundCollisionClearPending = true;
					return ReadRegister(addr);
				default:
					return ReadRegister(addr);
			}
		}

		private int ReadRegister(int addr)
		{
			switch (addr)
			{
				case 0x00:
				case 0x02:
				case 0x04:
				case 0x06:
				case 0x08:
				case 0x0A:
				case 0x0C:
				case 0x0E:
					return _sprites[addr >> 1].X & 0xFF;
				case 0x01:
				case 0x03:
				case 0x05:
				case 0x07:
				case 0x09:
				case 0x0B:
				case 0x0D:
				case 0x0F:
					return _sprites[addr >> 1].Y & 0xFF;
				case 0x10:
					return ((_sprite0.X >> 8) & 0x01) |
					         ((_sprite1.X >> 7) & 0x02) |
					         ((_sprite2.X >> 6) & 0x04) |
					         ((_sprite3.X >> 5) & 0x08) |
					         ((_sprite4.X >> 4) & 0x10) |
					         ((_sprite5.X >> 3) & 0x20) |
					         ((_sprite6.X >> 2) & 0x40) |
					         ((_sprite7.X >> 1) & 0x80);
				case 0x11:
					return (_yScroll & 0x7) |
					         (_rowSelect ? 0x08 : 0x00) |
					         (_displayEnable ? 0x10 : 0x00) |
					         (_bitmapMode ? 0x20 : 0x00) |
					         (_extraColorMode ? 0x40 : 0x00) |
					         ((_rasterLine & 0x100) >> 1);
				case 0x12:
					return _rasterLine & 0xFF;
				case 0x13:
					return _lightPenX & 0xFF;
				case 0x14:
					return _lightPenY & 0xFF;
				case 0x15:
					return (_sprite0.Enable ? 0x01 : 0x00) |
					         (_sprite1.Enable ? 0x02 : 0x00) |
					         (_sprite2.Enable ? 0x04 : 0x00) |
					         (_sprite3.Enable ? 0x08 : 0x00) |
					         (_sprite4.Enable ? 0x10 : 0x00) |
					         (_sprite5.Enable ? 0x20 : 0x00) |
					         (_sprite6.Enable ? 0x40 : 0x00) |
					         (_sprite7.Enable ? 0x80 : 0x00);
				case 0x16:
					return 0xC0 | (_xScroll & 0x7) |
					          (_columnSelect ? 0x08 : 0x00) |
					          (_multicolorMode ? 0x10 : 0x00);
				case 0x17:
					return (_sprite0.YExpand ? 0x01 : 0x00) |
					         (_sprite1.YExpand ? 0x02 : 0x00) |
					         (_sprite2.YExpand ? 0x04 : 0x00) |
					         (_sprite3.YExpand ? 0x08 : 0x00) |
					         (_sprite4.YExpand ? 0x10 : 0x00) |
					         (_sprite5.YExpand ? 0x20 : 0x00) |
					         (_sprite6.YExpand ? 0x40 : 0x00) |
					         (_sprite7.YExpand ? 0x80 : 0x00);
				case 0x18:
					return 0x01 | ((_pointerVm & 0x3C00) >> 6) |
					          ((_pointerCb & 0x7) << 1);
				case 0x19:
					return 0x70 | (_intRaster ? 0x01 : 0x00) |
					          (_intSpriteDataCollision ? 0x02 : 0x00) |
					          (_intSpriteCollision ? 0x04 : 0x00) |
					          (_intLightPen ? 0x08 : 0x00) |
					          (_pinIrq ? 0x00 : 0x80);
				case 0x1A:
					return 0xF0 | (_enableIntRaster ? 0x01 : 0x00) |
					          (_enableIntSpriteDataCollision ? 0x02 : 0x00) |
					          (_enableIntSpriteCollision ? 0x04 : 0x00) |
					          (_enableIntLightPen ? 0x08 : 0x00);
				case 0x1B:
					return (_sprite0.Priority ? 0x01 : 0x00) |
					         (_sprite1.Priority ? 0x02 : 0x00) |
					         (_sprite2.Priority ? 0x04 : 0x00) |
					         (_sprite3.Priority ? 0x08 : 0x00) |
					         (_sprite4.Priority ? 0x10 : 0x00) |
					         (_sprite5.Priority ? 0x20 : 0x00) |
					         (_sprite6.Priority ? 0x40 : 0x00) |
					         (_sprite7.Priority ? 0x80 : 0x00);
				case 0x1C:
					return (_sprite0.Multicolor ? 0x01 : 0x00) |
					         (_sprite1.Multicolor ? 0x02 : 0x00) |
					         (_sprite2.Multicolor ? 0x04 : 0x00) |
					         (_sprite3.Multicolor ? 0x08 : 0x00) |
					         (_sprite4.Multicolor ? 0x10 : 0x00) |
					         (_sprite5.Multicolor ? 0x20 : 0x00) |
					         (_sprite6.Multicolor ? 0x40 : 0x00) |
					         (_sprite7.Multicolor ? 0x80 : 0x00);
				case 0x1D:
					return (_sprite0.XExpand ? 0x01 : 0x00) |
					         (_sprite1.XExpand ? 0x02 : 0x00) |
					         (_sprite2.XExpand ? 0x04 : 0x00) |
					         (_sprite3.XExpand ? 0x08 : 0x00) |
					         (_sprite4.XExpand ? 0x10 : 0x00) |
					         (_sprite5.XExpand ? 0x20 : 0x00) |
					         (_sprite6.XExpand ? 0x40 : 0x00) |
					         (_sprite7.XExpand ? 0x80 : 0x00);
				case 0x1E:
					return (_sprite0.CollideSprite ? 0x01 : 0x00) |
					         (_sprite1.CollideSprite ? 0x02 : 0x00) |
					         (_sprite2.CollideSprite ? 0x04 : 0x00) |
					         (_sprite3.CollideSprite ? 0x08 : 0x00) |
					         (_sprite4.CollideSprite ? 0x10 : 0x00) |
					         (_sprite5.CollideSprite ? 0x20 : 0x00) |
					         (_sprite6.CollideSprite ? 0x40 : 0x00) |
					         (_sprite7.CollideSprite ? 0x80 : 0x00);
				case 0x1F:
					return (_sprite0.CollideData ? 0x01 : 0x00) |
					         (_sprite1.CollideData ? 0x02 : 0x00) |
					         (_sprite2.CollideData ? 0x04 : 0x00) |
					         (_sprite3.CollideData ? 0x08 : 0x00) |
					         (_sprite4.CollideData ? 0x10 : 0x00) |
					         (_sprite5.CollideData ? 0x20 : 0x00) |
					         (_sprite6.CollideData ? 0x40 : 0x00) |
					         (_sprite7.CollideData ? 0x80 : 0x00);
				case 0x20:
					return 0xF0 | _borderColor & 0x0F;
				case 0x21:
                    return 0xF0 | _backgroundColor0 & 0x0F;
				case 0x22:
                    return 0xF0 | _backgroundColor1 & 0x0F;
				case 0x23:
                    return 0xF0 | _backgroundColor2 & 0x0F;
				case 0x24:
                    return 0xF0 | _backgroundColor3 & 0x0F;
				case 0x25:
                    return 0xF0 | _spriteMulticolor0 & 0x0F;
				case 0x26:
                    return 0xF0 | _spriteMulticolor1 & 0x0F;
				case 0x27:
				case 0x28:
				case 0x29:
				case 0x2A:
				case 0x2B:
				case 0x2C:
				case 0x2D:
				case 0x2E:
					return 0xF0 | _sprites[addr - 0x27].Color & 0xF;
                default:
			        return 0xFF;
			}
		}

		public void Write(int addr, int val)
		{
			addr &= 0x3F;
			switch (addr)
			{
                case 0x17:
                    // vic-ii addendum rule 7
			        foreach (var spr in _sprites)
			        {
			            if ((val & 1) == 0 && !spr.YCrunch)
			            {
			                if (_parseIsSprCrunch)
			                {
			                    spr.Mc = (0x2a & spr.Mcbase & spr.Mc) | (0x15 & (spr.Mcbase | spr.Mc));
			                }
			                spr.YCrunch = true;
			            }
			        }
                    WriteRegister(addr, val);
                    break;
				case 0x19:
					// interrupts are cleared by writing a 1
					if ((val & 0x01) != 0)
						_intRaster = false;
					if ((val & 0x02) != 0)
						_intSpriteDataCollision = false;
					if ((val & 0x04) != 0)
						_intSpriteCollision = false;
					if ((val & 0x08) != 0)
						_intLightPen = false;
					UpdatePins();
					break;
				case 0x1E:
				case 0x1F:
					// can't write to these
					break;
				case 0x2F:
				case 0x30:
				case 0x31:
				case 0x32:
				case 0x33:
				case 0x34:
				case 0x35:
				case 0x36:
				case 0x37:
				case 0x38:
				case 0x39:
				case 0x3A:
				case 0x3B:
				case 0x3C:
				case 0x3D:
				case 0x3E:
				case 0x3F:
					// not connected
					break;
				default:
					WriteRegister(addr, val);
					break;
			}
		}

		private void WriteRegister(int addr, int val)
		{
			switch (addr)
			{
				case 0x00:
				case 0x02:
				case 0x04:
				case 0x06:
				case 0x08:
				case 0x0A:
				case 0x0C:
				case 0x0E:
					_sprites[addr >> 1].X &= 0x100;
					_sprites[addr >> 1].X |= val;
					break;
				case 0x01:
				case 0x03:
				case 0x05:
				case 0x07:
				case 0x09:
				case 0x0B:
				case 0x0D:
				case 0x0F:
					_sprites[addr >> 1].Y = val;
					break;
				case 0x10:
					_sprite0.X = (_sprite0.X & 0xFF) | ((val & 0x01) << 8);
					_sprite1.X = (_sprite1.X & 0xFF) | ((val & 0x02) << 7);
					_sprite2.X = (_sprite2.X & 0xFF) | ((val & 0x04) << 6);
					_sprite3.X = (_sprite3.X & 0xFF) | ((val & 0x08) << 5);
					_sprite4.X = (_sprite4.X & 0xFF) | ((val & 0x10) << 4);
					_sprite5.X = (_sprite5.X & 0xFF) | ((val & 0x20) << 3);
					_sprite6.X = (_sprite6.X & 0xFF) | ((val & 0x40) << 2);
					_sprite7.X = (_sprite7.X & 0xFF) | ((val & 0x80) << 1);
					break;
				case 0x11:
					_yScroll = val & 0x07;
					_rowSelect = (val & 0x08) != 0;
					_displayEnable = (val & 0x10) != 0;
					_bitmapMode = (val & 0x20) != 0;
					_extraColorMode = (val & 0x40) != 0;
					_rasterInterruptLine &= 0xFF;
					_rasterInterruptLine |= (val & 0x80) << 1;
					UpdateBorder();
					UpdateVideoMode();
					break;
				case 0x12:
					_rasterInterruptLine &= 0x100;
					_rasterInterruptLine |= val;
					break;
				case 0x13:
					_lightPenX = val;
					break;
				case 0x14:
					_lightPenY = val;
					break;
				case 0x15:
					_sprite0.Enable = (val & 0x01) != 0;
					_sprite1.Enable = (val & 0x02) != 0;
					_sprite2.Enable = (val & 0x04) != 0;
					_sprite3.Enable = (val & 0x08) != 0;
					_sprite4.Enable = (val & 0x10) != 0;
					_sprite5.Enable = (val & 0x20) != 0;
					_sprite6.Enable = (val & 0x40) != 0;
					_sprite7.Enable = (val & 0x80) != 0;
					break;
				case 0x16:
					_xScroll = val & 0x07;
					_columnSelect = (val & 0x08) != 0;
					_multicolorMode = (val & 0x10) != 0;
					UpdateBorder();
					UpdateVideoMode();
					break;
				case 0x17:
					_sprite0.YExpand = (val & 0x01) != 0;
					_sprite1.YExpand = (val & 0x02) != 0;
					_sprite2.YExpand = (val & 0x04) != 0;
					_sprite3.YExpand = (val & 0x08) != 0;
					_sprite4.YExpand = (val & 0x10) != 0;
					_sprite5.YExpand = (val & 0x20) != 0;
					_sprite6.YExpand = (val & 0x40) != 0;
					_sprite7.YExpand = (val & 0x80) != 0;
					break;
				case 0x18:
					_pointerVm = (val << 6) & 0x3C00;
					_pointerCb = (val >> 1) & 0x7;
					break;
				case 0x19:
					_intRaster = (val & 0x01) != 0;
					_intSpriteDataCollision = (val & 0x02) != 0;
					_intSpriteCollision = (val & 0x04) != 0;
					_intLightPen = (val & 0x08) != 0;
					UpdatePins();
					break;
				case 0x1A:
					_enableIntRaster = (val & 0x01) != 0;
					_enableIntSpriteDataCollision = (val & 0x02) != 0;
					_enableIntSpriteCollision = (val & 0x04) != 0;
					_enableIntLightPen = (val & 0x08) != 0;
					UpdatePins();
					break;
				case 0x1B:
					_sprite0.Priority = (val & 0x01) != 0;
					_sprite1.Priority = (val & 0x02) != 0;
					_sprite2.Priority = (val & 0x04) != 0;
					_sprite3.Priority = (val & 0x08) != 0;
					_sprite4.Priority = (val & 0x10) != 0;
					_sprite5.Priority = (val & 0x20) != 0;
					_sprite6.Priority = (val & 0x40) != 0;
					_sprite7.Priority = (val & 0x80) != 0;
					break;
				case 0x1C:
					_sprite0.Multicolor = (val & 0x01) != 0;
					_sprite1.Multicolor = (val & 0x02) != 0;
					_sprite2.Multicolor = (val & 0x04) != 0;
					_sprite3.Multicolor = (val & 0x08) != 0;
					_sprite4.Multicolor = (val & 0x10) != 0;
					_sprite5.Multicolor = (val & 0x20) != 0;
					_sprite6.Multicolor = (val & 0x40) != 0;
					_sprite7.Multicolor = (val & 0x80) != 0;
					break;
				case 0x1D:
					_sprite0.XExpand = (val & 0x01) != 0;
					_sprite1.XExpand = (val & 0x02) != 0;
					_sprite2.XExpand = (val & 0x04) != 0;
					_sprite3.XExpand = (val & 0x08) != 0;
					_sprite4.XExpand = (val & 0x10) != 0;
					_sprite5.XExpand = (val & 0x20) != 0;
					_sprite6.XExpand = (val & 0x40) != 0;
					_sprite7.XExpand = (val & 0x80) != 0;
					break;
				case 0x1E:
					_sprite0.CollideSprite = (val & 0x01) != 0;
					_sprite1.CollideSprite = (val & 0x02) != 0;
					_sprite2.CollideSprite = (val & 0x04) != 0;
					_sprite3.CollideSprite = (val & 0x08) != 0;
					_sprite4.CollideSprite = (val & 0x10) != 0;
					_sprite5.CollideSprite = (val & 0x20) != 0;
					_sprite6.CollideSprite = (val & 0x40) != 0;
					_sprite7.CollideSprite = (val & 0x80) != 0;
					break;
				case 0x1F:
					_sprite0.CollideData = (val & 0x01) != 0;
					_sprite1.CollideData = (val & 0x02) != 0;
					_sprite2.CollideData = (val & 0x04) != 0;
					_sprite3.CollideData = (val & 0x08) != 0;
					_sprite4.CollideData = (val & 0x10) != 0;
					_sprite5.CollideData = (val & 0x20) != 0;
					_sprite6.CollideData = (val & 0x40) != 0;
					_sprite7.CollideData = (val & 0x80) != 0;
					break;
				case 0x20:
					_borderColor = val & 0xF;
					break;
				case 0x21:
					_backgroundColor0 = val & 0xF;
					break;
				case 0x22:
					_backgroundColor1 = val & 0xF;
					break;
				case 0x23:
					_backgroundColor2 = val & 0xF;
					break;
				case 0x24:
					_backgroundColor3 = val & 0xF;
					break;
				case 0x25:
					_spriteMulticolor0 = val & 0xF;
					break;
				case 0x26:
					_spriteMulticolor1 = val & 0xF;
					break;
				case 0x27:
				case 0x28:
				case 0x29:
				case 0x2A:
				case 0x2B:
				case 0x2C:
				case 0x2D:
				case 0x2E:
					_sprites[addr - 0x27].Color = val & 0xF;
					break;
			}
		}
	}
}
