namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
	public sealed partial class Via
	{
		public int Peek(int addr)
		{
			return ReadRegister(addr & 0xF);
		}

		public void Poke(int addr, int val)
		{
			WriteRegister(addr & 0xF, val);
		}

		public int Read(int addr)
		{
			addr &= 0xF;
			switch (addr)
			{
				case 0x0:
					_ifr &= ~IRQ_CB1;
					if ((_pcr & PCR_CB2_ACK) == 0)
					{
						_ifr &= ~IRQ_CB2;
					}
					_cb2Handshake = true;
					_cb2Pulse = false;
					break;
				case 0x1:
					_ifr &= ~IRQ_CA1;
					if ((_pcr & PCR_CA2_ACK) == 0)
					{
						_ifr &= ~IRQ_CA2;
					}
					_ca2Handshake = true;
					_ca2Pulse = false;
					break;
				case 0x4:
					_ifr &= ~IRQ_T1;
					break;
				case 0x8:
					_ifr &= ~IRQ_T2;
					break;
				case 0xA:
					_srAccessed = true;
					_ifr &= ~IRQ_SR;
					break;
			}

			return ReadRegister(addr);
		}

		private int ReadRegister(int addr)
		{
			switch (addr)
			{
				case 0x0:
					return (PrB & DdrB) | (_irb & ~DdrB);
				case 0x1:
				case 0xF:
					return _ira;
				case 0x2:
					return _ddrb;
				case 0x3:
					return _ddra;
				case 0x4:
					return _t1C & 0xFF;
				case 0x5:
					return (_t1C >> 8) & 0xFF;
				case 0x6:
					return _t1L & 0xFF;
				case 0x7:
					return (_t1L >> 8) & 0xFF;
				case 0x8:
					return _t2C & 0xFF;
				case 0x9:
					return (_t2C >> 8) & 0xFF;
				case 0xA:
					return _sr;
				case 0xB:
					return _acr;
				case 0xC:
					return _pcr;
				case 0xD:
					return (_ifr & 0x7F) | (_irq ? 0x80 : 0x00);
				case 0xE:
					return _ier;
			}

			return 0xFF;
		}

		public void Write(int addr, int val)
		{
			addr &= 0xF;
			switch (addr)
			{
				case 0x0:
					_ifr &= ~IRQ_CB1;
					if ((_pcr & PCR_CB2_ACK) == 0)
					{
						_ifr &= ~IRQ_CB2;
					}
					WriteRegister(addr, val);
					break;
				case 0x1:
					_ifr &= ~IRQ_CA1;
					if ((_pcr & PCR_CA2_ACK) == 0)
					{
						_ifr &= ~IRQ_CA2;
					}
					WriteRegister(addr, val);
					break;
				case 0x4:
				case 0x6:
					_t1L = (_t1L & 0xFF00) | (val & 0xFF);
					break;
				case 0x5:
					_t1L = (_t1L & 0xFF) | ((val & 0xFF) << 8);
					_t1C = _t1L;
					_ifr &= ~IRQ_T1;
					_t1Reload = false;
					_t1Out = false;
					_t1IrqAllowed = true;
					break;
				case 0x7:
					_t1L = (_t1L & 0xFF) | ((val & 0xFF) << 8);
					_ifr &= ~IRQ_T1;
					break;
				case 0x8:
					_t2L = (_t2L & 0xFF00) | (val & 0xFF);
					break;
				case 0x9:
					_t2L = (_t2L & 0xFF) | ((val & 0xFF) << 8);
					_ifr &= ~IRQ_T2;
					_t2IrqAllowed = true;
					break;
				case 0xA:
					_srAccessed = true;
					_srWritten = true;
					WriteRegister(addr, val);
					break;
				case 0xD:
					_ifr &= ~(val & 0x7F);
					break;
				case 0xE:
					if ((val & IRQ_BIT) != 0)
					{
						_ier |= val & IRQ_MASK;
					}
					else
					{
						_ier &= ~(val & IRQ_MASK);
					}
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
				case 0x0:
					_orb = val & 0xFF;
					break;
				case 0x1:
				case 0xF:
					_ora = val & 0xFF;
					break;
				case 0x2:
					_ddrb = val & 0xFF;
					break;
				case 0x3:
					_ddra = val & 0xFF;
					break;
				case 0x4:
					_t1C = (_t1C & 0xFF00) | (val & 0xFF);
					break;
				case 0x5:
					_t1C = (_t1C & 0xFF) | ((val & 0xFF) << 8);
					break;
				case 0x6:
					_t1L = (_t1L & 0xFF00) | (val & 0xFF);
					break;
				case 0x7:
					_t1L = (_t1L & 0xFF) | ((val & 0xFF) << 8);
					break;
				case 0x8:
					_t2C = (_t2C & 0xFF00) | (val & 0xFF);
					break;
				case 0x9:
					_t2C = (_t2C & 0xFF) | ((val & 0xFF) << 8);
					break;
				case 0xA:
					_sr = val & 0xFF;
					break;
				case 0xB:
					_acr = val & 0xFF;
					break;
				case 0xC:
					_pcr = val & 0xFF;
					break;
				case 0xD:
					_ifr = val & 0x7F;
					break;
				case 0xE:
					_ier = val & 0xFF;
					break;
			}
		}

		public int DdrA => _ddra;

		public int DdrB => _ddrb | (_acr & ACR_T1_PB7_OUT);

		public int PrA => _ora;

		public int PrB => (_acr & ACR_T1_PB7_OUT) != 0
			? (_orb & 0x7F) | (_t1Out ? 0x80 : 0x00)
			: _orb;
	}
}
