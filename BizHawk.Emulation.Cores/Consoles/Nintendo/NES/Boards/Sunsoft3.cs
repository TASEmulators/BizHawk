using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Nintendo.NES
{
	//AKA mapper 67
	//this may be confusing due to general chaos with the early sunsoft mappers. see docs/sunsoft.txt
	public sealed class Sunsoft3 : NES.NESBoardBase
	{
		//configuration
		int prg_bank_mask_16k, chr_bank_mask_2k;

		//state
		bool toggle;
		ByteBuffer prg_banks_16k = new ByteBuffer(2);
		ByteBuffer chr_banks_2k = new ByteBuffer(4);
		int irq_counter;
		bool irq_enable;
		bool irq_asserted;
		int clock_counter;

		//the VS actually does have 2 KB of nametable address space
		//let's make the extra space here, instead of in the main NES to avoid confusion
		byte[] CIRAM_VS = new byte[0x800];


		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			ser.Sync("toggle", ref toggle);
			ser.Sync("prg_banks_16k", ref prg_banks_16k);
			ser.Sync("chr_banks_2k", ref chr_banks_2k);
			ser.Sync("irq_counter", ref irq_counter);
			ser.Sync("irq_enable", ref irq_enable);
			ser.Sync("irq_asserted", ref irq_asserted);
			ser.Sync("clock_counter", ref clock_counter);

			if (NES.IsVS)
			{
				ser.Sync("VS_CIRAM", ref CIRAM_VS, false);
			}

			SyncIRQ();
		}

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER067VS":
					NES._isVS = true;
					//update the state of the dip switches
					//this is only done at power on
					NES.VS_dips[0] = (byte)(NES.SyncSettings.VSDipswitches.Dip_Switch_1 ? 1 : 0);
					NES.VS_dips[1] = (byte)(NES.SyncSettings.VSDipswitches.Dip_Switch_2 ? 1 : 0);
					NES.VS_dips[2] = (byte)(NES.SyncSettings.VSDipswitches.Dip_Switch_3 ? 1 : 0);
					NES.VS_dips[3] = (byte)(NES.SyncSettings.VSDipswitches.Dip_Switch_4 ? 1 : 0);
					NES.VS_dips[4] = (byte)(NES.SyncSettings.VSDipswitches.Dip_Switch_5 ? 1 : 0);
					NES.VS_dips[5] = (byte)(NES.SyncSettings.VSDipswitches.Dip_Switch_6 ? 1 : 0);
					NES.VS_dips[6] = (byte)(NES.SyncSettings.VSDipswitches.Dip_Switch_7 ? 1 : 0);
					NES.VS_dips[7] = (byte)(NES.SyncSettings.VSDipswitches.Dip_Switch_8 ? 1 : 0);
					break;
				case "MAPPER067":
					break;
				case "SUNSOFT-3":
					AssertPrg(128); AssertChr(128);
					break;
				default:
					return false;
			}

			prg_bank_mask_16k = (Cart.prg_size / 16) - 1;
			chr_bank_mask_2k = (Cart.chr_size / 2) - 1;

			prg_banks_16k[0] = 0;
			prg_banks_16k[1] = 0xFF;
			ApplyMemoryMapMask(prg_bank_mask_16k, prg_banks_16k);

			return true;
		}

		void SetCHR(int block, byte value)
		{
			chr_banks_2k[block] = value;
			ApplyMemoryMapMask(chr_bank_mask_2k, chr_banks_2k);
		}

		void SyncIRQ()
		{
			IRQSignal = irq_asserted;
		}

		public override void WritePRG(int addr, byte value)
		{
			//Console.WriteLine("{0:X4} = {1:X2}", addr, value);
			switch (addr & 0xF800)
			{
				case 0x0800: //0x8800
					SetCHR(0, value);
					break;
				case 0x1800: //0x9800
					SetCHR(1, value);
					break;
				case 0x2800: //0xA800
					SetCHR(2, value);
					break;
				case 0x3800: //0xB800
					SetCHR(3, value);
					break;
				case 0x4800: //0xC800
					if (!toggle)
						irq_counter = (irq_counter & 0xFF) | (value << 8);
					else irq_counter = (irq_counter & 0xFF00) | (value);
					toggle ^= true;
					break;
				case 0x5800: //0xD800
					irq_enable = value.Bit(4);
					toggle = false;
					irq_asserted = false;
					SyncIRQ();
					break;
				case 0x6800: //0xE800:
					switch (value & 3)
					{
						case 0: SetMirrorType(NES.NESBoardBase.EMirrorType.Vertical); break;
						case 1: SetMirrorType(NES.NESBoardBase.EMirrorType.Horizontal); break;
						case 2: SetMirrorType(NES.NESBoardBase.EMirrorType.OneScreenA); break;
						case 3: SetMirrorType(NES.NESBoardBase.EMirrorType.OneScreenB); break;
					}
					break;
				case 0x7800: //0xF800:
					prg_banks_16k[0] = value;
					ApplyMemoryMapMask(prg_bank_mask_16k, prg_banks_16k);
					break;
			}
		}


		public override void ClockCPU()
		{
			if (!irq_enable) return;
			if (irq_counter == 0)
			{
				irq_counter = 0xFFFF;
				//Console.WriteLine("IRQ!!!");
				irq_asserted = true;
				SyncIRQ();
				irq_enable = false;
			}
			else irq_counter--;
		}

		public override byte ReadPRG(int addr)
		{
			addr = ApplyMemoryMap(14, prg_banks_16k, addr);
			return ROM[addr]; 
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				addr = ApplyMemoryMap(11, chr_banks_2k, addr);
				return base.ReadPPUChr(addr);
			}
			else
			{
				if (NES._isVS)
				{
					addr = addr - 0x2000;
					if (addr < 0x800)
					{
						return NES.CIRAM[addr];
					}
					else
					{
						return CIRAM_VS[addr - 0x800];
					}
				}
				else
					return base.ReadPPU(addr);
			}
		}

		public override void WritePPU(int addr, byte value)
		{
			if (NES._isVS)
			{
				if (addr < 0x2000)
				{
					addr = ApplyMemoryMap(11, chr_banks_2k, addr);
					if (VRAM != null)
						VRAM[addr] = value;
				}
				else
				{
					addr = addr - 0x2000;
					if (addr < 0x800)
					{
						NES.CIRAM[addr] = value;
					}
					else
					{
						CIRAM_VS[addr - 0x800] = value;
					}
				}
			}
			else
				base.WritePPU(addr, value);
		}

		/*
		public override void ClockPPU()
		{
			clock_counter++;
			if (clock_counter == 3)
			{
				clock_counter = 0;
				ClockCPU();
			}
		}*/
	}
}
