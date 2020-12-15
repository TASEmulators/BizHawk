using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	//mapper 73 AKA salamander
	//different IRQ logic than other VRC
	internal sealed class VRC3 : NesBoardBase
	{
		//configuration
		private int prg_bank_mask_16k;

		//state
		private int[] prg_banks_16k = new int[2];
		private bool irq_mode;
		private bool irq_enabled, irq_pending, irq_autoen;
		private ushort irq_reload;
		private ushort irq_counter;
		private int irq_cycles;

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync(nameof(prg_banks_16k), ref prg_banks_16k, false);
			ser.Sync(nameof(irq_mode), ref irq_mode);
			ser.Sync(nameof(irq_enabled), ref irq_enabled);
			ser.Sync(nameof(irq_pending), ref irq_pending);
			ser.Sync(nameof(irq_autoen), ref irq_autoen);
			ser.Sync(nameof(irq_reload), ref irq_reload);
			ser.Sync(nameof(irq_counter), ref irq_counter);
			ser.Sync(nameof(irq_cycles), ref irq_cycles);
			SyncIRQ();
		}

		private void SyncIRQ()
		{
			IrqSignal = (irq_pending && irq_enabled);
		}

		public override bool Configure(EDetectionOrigin origin)
		{
			switch (Cart.BoardType)
			{
				case "MAPPER073":
					break;
				case "KONAMI-VRC-3":
					AssertPrg(128,512); AssertChr(0,128); AssertVram(0,8); AssertWram(0,8);
					break;
				default:
					return false;
			}

			prg_bank_mask_16k = Cart.PrgSize / 16 - 1;

			SetMirrorType(EMirrorType.Vertical);

			prg_banks_16k[1] = (byte)(0xFF & prg_bank_mask_16k);

			return true;
		}
		public override byte ReadPrg(int addr)
		{
			int bank_16k = addr >> 14;
			int ofs = addr & ((1 << 14) - 1);
			bank_16k = prg_banks_16k[bank_16k];
			addr = (bank_16k << 14) | ofs;
			return Rom[addr];
		}

		private void WriteIrqReload(int bit, byte value)
		{
			int mask = 0xF << bit;
			irq_reload = (ushort)((irq_reload & ~mask) | (value << bit));
		}
		public override void WritePrg(int addr, byte value)
		{
			switch (addr)
			{
				case 0x0000: WriteIrqReload(0, value); break;
				case 0x1000: WriteIrqReload(4, value); break;
				case 0x2000: WriteIrqReload(8, value); break;
				case 0x3000: WriteIrqReload(12, value); break;

				case 0x4000:
					irq_mode = value.Bit(2);
					irq_autoen = value.Bit(0);

					if (value.Bit(1))
					{
						//enabled
						irq_enabled = true;
						if (irq_mode)
						{
							//8bit..
							irq_counter &= 0xFF00;
							irq_counter |= (ushort)(irq_reload & 0xFF);
						}
						else
						{
							irq_counter = irq_reload;
						}
						irq_cycles = 3;
					}
					else
					{
						//disabled
						irq_enabled = false;
					}

					//acknowledge
					irq_pending = false;

					SyncIRQ();

					break;

				case 0x5000:
					irq_pending = false;
					irq_enabled = irq_autoen;
					SyncIRQ();
					break;

				case 0x7000:
					prg_banks_16k[0] = value & 0xF;
					prg_banks_16k[0] &= prg_bank_mask_16k;
					break;

			}
		}

		public override void ClockCpu()
		{
			if (!irq_enabled) return;
			if (irq_mode)
			{
				//8 bit mode
				ushort temp = irq_counter;
				temp &= 0xFF;
				irq_counter &= 0xFF00;
				if (temp == 0xFF)
				{
					irq_pending = true;
					irq_counter = irq_reload;
					irq_counter |= (ushort)(irq_reload & 0xFF);
					SyncIRQ();
				}
				else
				{
					temp++;
					irq_counter |= temp;
				}
			}
			else
			{
				//16 bit mode
				if (irq_counter == 0xFFFF)
				{
					irq_pending = true;
					irq_counter = irq_reload;
					SyncIRQ();
				}
				else
					irq_counter++;
			}
		}
	}
}
