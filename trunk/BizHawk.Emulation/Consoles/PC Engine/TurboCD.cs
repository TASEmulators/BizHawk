using System;

namespace BizHawk.Emulation.Consoles.TurboGrafx
{
	public partial class PCEngine
	{
		private byte[] CdIoPorts = new byte[16];
		private ScsiCD scsi = new ScsiCD();

		private void WriteCD(int addr, byte value)
		{
			Console.WriteLine("Write to reg[{0:X4}] {1:X2}", addr & 0x1FFF, value);
			switch (addr & 0x1FFF)
			{
				case 0x1800: // SCSI Drive Control Line
					CdIoPorts[0] = value;
					//    Console.WriteLine("Write to CDC Status [0] {0:X2}", value);

					scsi.SEL = true;
					scsi.Think();
					scsi.SEL = false;
					scsi.Think();

					// this probably does some things
					// possibly clear irq line or trigger or who knows
					break;

				case 0x1801: // CDC Command
					CdIoPorts[1] = value;
					scsi.DB = value;
					scsi.Think();
					//                    Console.WriteLine("Write to CDC Command [1] {0:X2}", value);
					break;

				case 0x1802: // ACK and Interrupt Control
					CdIoPorts[2] = value;
					scsi.ACK = ((value & 0x80) != 0);
					scsi.Think();
					RefreshIRQ2();
					break;

				case 0x1804: // CD Reset Command
					CdIoPorts[4] = value;
					scsi.RST = ((value & 0x02) != 0);
					scsi.Think();
					if (scsi.RST)
					{
						CdIoPorts[3] &= 0x8F; // Clear interrupt control bits
						RefreshIRQ2();
					}
					break;

				case 0x1807: // BRAM Unlock
					if (BramEnabled && (value & 0x80) != 0)
					{
						Console.WriteLine("UNLOCK BRAM!");
						BramLocked = false;
					}
					break;

				case 0x180B: // ADPCM DMA Control
					CdIoPorts[0x0B] = value;
					//                  Console.WriteLine("Write to ADPCM DMA Control [B]");
					// TODO... there is DMA to be done 
					break;

				case 0x180D: // ADPCM Address Control
					CdIoPorts[0x0D] = value;
					//                Console.WriteLine("Write to ADPCM Address Control [D]");
					break;

				case 0x180E: // ADPCM Playback Rate
					CdIoPorts[0x0E] = value;
					//              Console.WriteLine("Write to ADPCM Address Control [E]");
					break;

				case 0x180F: // Audio Fade Timer
					CdIoPorts[0x0F] = value;
					//                    Console.WriteLine("Write to CD Audio fade timer [F]");
					// TODO: hook this up to audio system);
					break;

				default:
					Console.WriteLine("unknown write to {0:X4}:{1:X2}", addr, value);
					break;
			}
		}

		public byte ReadCD(int addr)
		{
			byte returnValue = 0;
			switch (addr & 0x1FFF)
			{
				case 0x1800: //  SCSI Drive Control Line
					scsi.Think();
					if (scsi.IO) returnValue |= 0x08;
					if (scsi.CD) returnValue |= 0x10;
					if (scsi.MSG) returnValue |= 0x20;
					if (scsi.REQ) returnValue |= 0x40;
					if (scsi.BSY) returnValue |= 0x80;
					//if (returnValue != 0) returnValue = 0x40;
					Console.WriteLine("Read SCSI Drive Control Line [0]: {0:X2}   btw, pc={1:X4} ", returnValue, this.Cpu.PC);
					return returnValue;

				case 0x1802: // ADPCM / CD Control
					Console.WriteLine("Read 1802 {0:X2}", CdIoPorts[2]);
					return CdIoPorts[2];

				case 0x1803: // BRAM Lock
					if (BramEnabled)
					{
						Console.WriteLine("LOCKED BRAM! (read 1803)");
						BramLocked = true;
					}
					return CdIoPorts[3];

				case 0x1804: // CD Reset
					Console.WriteLine("Read 1804 {0:X2}", CdIoPorts[4]);
					return CdIoPorts[4];

				case 0x180F: // Audio Fade Timer
					Console.WriteLine("Read 180F {0:X2}", CdIoPorts[0xF]);
					return CdIoPorts[0x0F];

				// These are some retarded version check
				case 0x18C1: return 0xAA;
				case 0x18C2: return 0x55;
				case 0x18C3: return 0x00;
				case 0x18C5: return 0xAA;
				case 0x18C6: return 0x55;
				case 0x18C7: return 0x03;

				default:
					Console.WriteLine("unknown read to {0:X4}", addr);
					return 0xFF;
			}
		}

		private void RefreshIRQ2()
		{
			int mask = CdIoPorts[2] & CdIoPorts[3] & 0x7C;
			Cpu.IRQ2Assert = (mask != 0);
		}
	}
}
