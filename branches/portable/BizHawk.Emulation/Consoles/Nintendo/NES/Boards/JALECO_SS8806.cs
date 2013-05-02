namespace BizHawk.Emulation.Consoles.Nintendo
{
	class JALECO_SS8806 : NES.NESBoardBase
	{
		//http://wiki.nesdev.com/w/index.php/INES_Mapper_018

		ByteBuffer prg_banks_8k = new ByteBuffer(4);
		ByteBuffer chr_banks_1k = new ByteBuffer(8);
		int chr_bank_mask_1k, prg_bank_mask_8k;
		int ppuclock;
		int irqclock;
		int irqreload;
		int irqcountwidth;
		bool irqcountpaused;


		public override bool Configure(NES.EDetectionOrigin origin)
		{
			//analyze board type
			switch (Cart.board_type)
			{
				case "MAPPER018":
				case "JALECO-JF-24": //TODO: there will be many boards to list here
					break;
				default:
					return false;
			}

			chr_bank_mask_1k = Cart.chr_size / 1 - 1;
			prg_bank_mask_8k = Cart.prg_size / 8 - 1;
			prg_banks_8k[3] = 0xFF;

			// i have no idea what power-on defaults are supposed to be used
			ppuclock = 0;
			irqclock = 0xffff;
			irqreload = 0xffff;
			irqcountwidth = 16;
			irqcountpaused = true;

			SetMirrorType(EMirrorType.Horizontal);
			return true;
		}

		public override void SyncState(Serializer ser)
		{
			ser.Sync("prg_banks_8k", ref prg_banks_8k);
			ser.Sync("chr_banks_1k", ref chr_banks_1k);
			ser.Sync("ppuclock", ref ppuclock);
			ser.Sync("irqclock", ref irqclock);
			ser.Sync("irqreload", ref irqreload);
			ser.Sync("irqcountwidth", ref irqcountwidth);
			ser.Sync("irqcountpaused", ref irqcountpaused);
			base.SyncState(ser);
		}

		public override void Dispose()
		{
			prg_banks_8k.Dispose();
			chr_banks_1k.Dispose();
			base.Dispose();
		}

		public override void WritePRG(int addr, byte value)
		{
			addr += 0x8000; //temporary

			addr &= 0xF003;
			switch (addr)
			{
				case 0x8000:
					prg_banks_8k[0] &= 0xf0;
					prg_banks_8k[0] |= (byte)(value & 0x0F);
					break;
				case 0x8001:
					prg_banks_8k[0] &= 0x0f;
					prg_banks_8k[0] |= (byte)((value & 0x0F) << 4);
					break;
				case 0x8002:
					prg_banks_8k[1] &= 0xf0;
					prg_banks_8k[1] |= (byte)(value & 0x0F);
					break;
				case 0x8003:
					prg_banks_8k[1] &= 0x0f;
					prg_banks_8k[1] |= (byte)((value & 0x0F) << 4);
					break;
				case 0x9000:
					prg_banks_8k[2] &= 0xf0;
					prg_banks_8k[2] |= (byte)(value & 0x0F);
					break;
				case 0x9001:
					prg_banks_8k[2] &= 0x0f;
					prg_banks_8k[2] |= (byte)((value & 0x0F) << 4);
					break;
				case 0xA000:
					chr_banks_1k[0] &= 0xf0;
					chr_banks_1k[0] |= (byte)(value & 0x0F);
					break;
				case 0xA001:
					chr_banks_1k[0] &= 0x0f;
					chr_banks_1k[0] |= (byte)((value & 0x0F) << 4);
					break;
				case 0xA002:
					chr_banks_1k[1] &= 0xf0;
					chr_banks_1k[1] |= (byte)(value & 0x0F);
					break;
				case 0xA003:
					chr_banks_1k[1] &= 0x0f;
					chr_banks_1k[1] |= (byte)((value & 0x0F) << 4);
					break;
				case 0xB000:
					chr_banks_1k[2] &= 0xf0;
					chr_banks_1k[2] |= (byte)(value & 0x0F);
					break;
				case 0xB001:
					chr_banks_1k[2] &= 0x0f;
					chr_banks_1k[2] |= (byte)((value & 0x0F) << 4);
					break;
				case 0xB002:
					chr_banks_1k[3] &= 0xf0;
					chr_banks_1k[3] |= (byte)(value & 0x0F);
					break;
				case 0xB003:
					chr_banks_1k[3] &= 0x0f;
					chr_banks_1k[3] |= (byte)((value & 0x0F) << 4);
					break;
				case 0xC000:
					chr_banks_1k[4] &= 0xf0;
					chr_banks_1k[4] |= (byte)(value & 0x0F);
					break;
				case 0xC001:
					chr_banks_1k[4] &= 0x0f;
					chr_banks_1k[4] |= (byte)((value & 0x0F) << 4);
					break;
				case 0xC002:
					chr_banks_1k[5] &= 0xf0;
					chr_banks_1k[5] |= (byte)(value & 0x0F);
					break;
				case 0xC003:
					chr_banks_1k[5] &= 0x0f;
					chr_banks_1k[5] |= (byte)((value & 0x0F) << 4);
					break;
				case 0xD000:
					chr_banks_1k[6] &= 0xf0;
					chr_banks_1k[6] |= (byte)(value & 0x0F);
					break;
				case 0xD001:
					chr_banks_1k[6] &= 0x0f;
					chr_banks_1k[6] |= (byte)((value & 0x0F) << 4);
					break;
				case 0xD002:
					chr_banks_1k[7] &= 0xf0;
					chr_banks_1k[7] |= (byte)(value & 0x0F);
					break;
				case 0xD003:
					chr_banks_1k[7] &= 0x0f;
					chr_banks_1k[7] |= (byte)((value & 0x0F) << 4);
					break;
				case 0xE000:
					irqreload &= 0xfff0;
					irqreload |= value & 0xf;
					break;
				case 0xE001:
					irqreload &= 0xff0f;
					irqreload |= (value & 0xf) << 4;
					break;
				case 0xE002:
					irqreload &= 0xf0ff;
					irqreload |= (value & 0xf) << 8;
					break;
				case 0xE003:
					irqreload &= 0x0fff;
					irqreload |= (value & 0xf) << 12;
					break;
				case 0xF002:
					switch (value & 0x03)
					{
						case 0:
							SetMirrorType(EMirrorType.Horizontal);
							break;
						case 1:
							SetMirrorType(EMirrorType.Vertical);
							break;
						case 2:
							SetMirrorType(EMirrorType.OneScreenA);
							break;
						case 3:
							SetMirrorType(EMirrorType.OneScreenB);
							break;
					}
					break;
				case 0xF000:
					// ack irq and reset
					IRQSignal = false;
					irqclock = irqreload;
					break;
				case 0xF001:
					// ack irq and set values
					IRQSignal = false;
					irqcountpaused = (value & 1) == 0;
					if ((value & 8) == 8)
						irqcountwidth = 4;
					else if ((value & 4) == 4)
						irqcountwidth = 8;
					else if ((value & 2) == 2)
						irqcountwidth = 12;
					else
						irqcountwidth = 16;
					break;
					
				case 0xF003:
				// sound chip µPD7756C
					break;
			}
			
			
		}

		public override void ClockCPU()
		{
			//ppuclock++;
			//if (ppuclock == 3)
			//{
				//ppuclock = 0;
				if (!irqcountpaused)
				{
					int newclock = irqclock - 1;
					if (squeeze(newclock) > squeeze(irqclock))
					{
						IRQSignal = true;
						irqclock = irqreload;
					}
					else
						irqclock = newclock;
				}
			//}
		}

		/// <summary>
		/// emulate underflow for the appropriate number of bits
		/// </summary>
		uint squeeze(int input)
		{
			unchecked
			{
				uint uinput = (uint)input;
				uinput <<= (32 - irqcountwidth);
				uinput >>= (32 - irqcountwidth);
				return uinput;
			}
		}

		public override byte ReadPRG(int addr)
		{
			int bank_8k = addr >> 13;
			bank_8k = prg_banks_8k[bank_8k];
			bank_8k &= prg_bank_mask_8k;
			return ROM[(bank_8k * 0x2000) + (addr & 0x1FFF)];
		}

		private int MapCHR(int addr)
		{
			int bank_1k = addr >> 10;
			bank_1k = chr_banks_1k[bank_1k];
			bank_1k &= chr_bank_mask_1k;
			addr = (bank_1k << 10) | (addr & 0x3FF);
			return addr;
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				addr = MapCHR(addr);
				if (VROM != null)
					return VROM[addr];
				else return VRAM[addr];
			}
			else return base.ReadPPU(addr);
		}
	}
}
