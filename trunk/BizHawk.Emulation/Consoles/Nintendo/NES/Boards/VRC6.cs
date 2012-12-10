using System;
using System.IO;
using System.Diagnostics;

namespace BizHawk.Emulation.Consoles.Nintendo
{
	//mapper 24 + 26
	//If you change any of the IRQ logic here, be sure to change it in VRC 2/3/4/7 as well.
	public class VRC6 : NES.NESBoardBase
	{
		//configuration
		int prg_bank_mask_8k, chr_bank_mask_1k;
		bool newer_variant;

		//Sound.VRC6 VRC6Sound = new Sound.VRC6();
		Sound.VRC6Alt VRC6Sound;

		//state
		int prg_bank_16k, prg_bank_8k;
		ByteBuffer prg_banks_8k = new ByteBuffer(4);
		ByteBuffer chr_banks_1k = new ByteBuffer(8);
		bool irq_mode;
		bool irq_enabled, irq_pending, irq_autoen;
		byte irq_reload;
		byte irq_counter;
		int irq_prescaler;

		public override void Dispose()
		{
			base.Dispose();
			prg_banks_8k.Dispose();
			chr_banks_1k.Dispose();
			if (VRC6Sound != null)
				VRC6Sound.Dispose();
		}

		public override void SyncState(Serializer ser)
		{
			base.SyncState(ser);
			VRC6Sound.SyncState(ser);
			ser.Sync("prg_bank_16k", ref prg_bank_16k);
			ser.Sync("prg_bank_8k", ref prg_bank_8k);
			ser.Sync("chr_banks_1k", ref chr_banks_1k);
			ser.Sync("irq_mode", ref irq_mode);
			ser.Sync("irq_enabled", ref irq_enabled);
			ser.Sync("irq_pending", ref irq_pending);
			ser.Sync("irq_autoen", ref irq_autoen);
			ser.Sync("irq_reload", ref irq_reload);
			ser.Sync("irq_counter", ref irq_counter);
			ser.Sync("irq_prescaler", ref irq_prescaler);

			SyncPRG();
			SyncIRQ();
		}

		void SyncPRG()
		{
			prg_banks_8k[0] = (byte)(prg_bank_16k * 2);
			prg_banks_8k[1] = (byte)(prg_bank_16k * 2 + 1);
			prg_banks_8k[2] = (byte)(prg_bank_8k);
			prg_banks_8k[3] = 0xFF;
		}

		void SyncIRQ()
		{
			IRQSignal = (irq_pending && irq_enabled);
		}

		public override bool Configure(NES.EDetectionOrigin origin)
		{
			switch (Cart.board_type)
			{
				case "MAPPER024":
					newer_variant = false;
					break;
				case "MAPPER026":
					newer_variant = true;
					break;
				case "KONAMI-VRC-6":
					if (Cart.pcb == "351951")
						newer_variant = false;
					else if (Cart.pcb == "351949A")
						newer_variant = true;
					else throw new Exception("Unknown PCB type for VRC6");
					break;
				default:
					return false;
			}
			AssertPrg(256); AssertChr(128, 256); AssertVram(0); AssertWram(0, 8);

			prg_bank_mask_8k = Cart.prg_size / 8 - 1;
			chr_bank_mask_1k = Cart.chr_size - 1;

			prg_bank_16k = 0;
			prg_bank_8k = 0;
			SyncPRG();
			SetMirrorType(EMirrorType.Vertical);

			VRC6Sound = new Sound.VRC6Alt((uint)NES.cpuclockrate);

			return true;
		}
		public override byte ReadPRG(int addr)
		{
			int bank_8k = addr >> 13;
			int ofs = addr & ((1 << 13) - 1);
			bank_8k = prg_banks_8k[bank_8k];
			bank_8k &= prg_bank_mask_8k;
			addr = (bank_8k << 13) | ofs;
			return ROM[addr];
		}

		public override byte ReadPPU(int addr)
		{
			if (addr < 0x2000)
			{
				int bank_1k = addr >> 10;
				int ofs = addr & ((1 << 10) - 1);
				bank_1k = chr_banks_1k[bank_1k];
				bank_1k &= chr_bank_mask_1k;
				addr = (bank_1k << 10) | ofs;
				return VROM[addr];
			}
			else return base.ReadPPU(addr);
		}

		public override void WritePRG(int addr, byte value)
		{
			if (newer_variant)
			{
				addr = (addr & 0xFFFC) | ((addr >> 1) & 1) | ((addr << 1) & 2);
			}
			switch (addr)
			{
				case 0x0000: //$8000
				case 0x0001:
				case 0x0002:
				case 0x0003:
					prg_bank_16k = value;
					SyncPRG();
					break;
				
				case 0x1000: //$9000
					VRC6Sound.Write9000(value);
					break;
				case 0x1001: //$9001
					VRC6Sound.Write9001(value);
					break;
				case 0x1002: //$9002
					VRC6Sound.Write9002(value);
					break;

				case 0x2000: //$A000
					VRC6Sound.WriteA000(value);
					break;
				case 0x2001: //$A001
					VRC6Sound.WriteA001(value);
					break;
				case 0x2002: //$A002
					VRC6Sound.WriteA002(value);
					break;

				case 0x3000: //$B000
					VRC6Sound.WriteB000(value);
					break;
				case 0x3001: //$B001
					VRC6Sound.WriteB001(value);
					break;
				case 0x3002: //$B002
					VRC6Sound.WriteB002(value);
					break;
				
				case 0x3003: //$B003
					switch ((value>>2) & 3)
					{
						case 0: SetMirrorType(NES.NESBoardBase.EMirrorType.Vertical); break;
						case 1: SetMirrorType(NES.NESBoardBase.EMirrorType.Horizontal); break;
						case 2: SetMirrorType(NES.NESBoardBase.EMirrorType.OneScreenA); break;
						case 3: SetMirrorType(NES.NESBoardBase.EMirrorType.OneScreenB); break;
					}
					break;

				case 0x4000: //$C000
				case 0x4001:
				case 0x4002:
				case 0x4003:
					prg_bank_8k = value;
					SyncPRG();
					break;

				case 0x5000: //$D000
				case 0x5001: //$D001
				case 0x5002: //$D002
				case 0x5003: //$D003
					chr_banks_1k[addr - 0x5000] = value;
					break;

				case 0x6000: //$E000
				case 0x6001: //$E001
				case 0x6002: //$E002
				case 0x6003: //$E003
					chr_banks_1k[4+ addr - 0x6000] = value;
					break;

				case 0x7000: //$F000 (reload)
					irq_reload = value;
					break;
				case 0x7001: //$F001 (control)
					irq_mode = value.Bit(2);
					irq_autoen = value.Bit(0);

					if (value.Bit(1))
					{
						//enabled
						irq_enabled = true;
						irq_counter = irq_reload;
						irq_prescaler = 341;
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
				
				case 0x7002: //$F002 (ack)
					irq_pending = false;
					irq_enabled = irq_autoen;
					SyncIRQ();
					break;
			}
		}

		void ClockIRQ()
		{
			if (irq_counter == 0xFF)
			{
				irq_pending = true;
				irq_counter = irq_reload;
				SyncIRQ();
			}
			else
				irq_counter++;
		}

		public override void ClockCPU()
		{
			VRC6Sound.Clock();

			if (!irq_enabled) return;

			if (irq_mode)
			{
				ClockIRQ();
			}
			else
			{
				irq_prescaler -= 3;
				if (irq_prescaler <= 0)
				{
					irq_prescaler += 341;
					ClockIRQ();
				}
			}
		}

		public override void ApplyCustomAudio(short[] samples)
		{
			/*
			short[] fmsamples = new short[samples.Length];
			VRC6Sound.GetSamples(fmsamples);
			int len = samples.Length;
			for (int i = 0; i < len; i++)
			{
				samples[i] = (short)((samples[i] >> 1) + (fmsamples[i] >> 1));
			}
			*/
			VRC6Sound.ApplyCustomAudio(samples);
		}

	}
}