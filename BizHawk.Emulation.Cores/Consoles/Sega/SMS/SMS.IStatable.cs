using System.IO;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Sega.MasterSystem
{
	public partial class SMS : IStatable
	{
		public void SaveStateText(TextWriter writer)
		{
			SyncState(new Serializer(writer));
		}

		public void LoadStateText(TextReader reader)
		{
			SyncState(new Serializer(reader));
		}

		public void SaveStateBinary(BinaryWriter bw)
		{
			SyncState(new Serializer(bw));
		}

		public void LoadStateBinary(BinaryReader br)
		{
			SyncState(new Serializer(br));
		}

		public byte[] SaveStateBinary()
		{
			using var ms = new MemoryStream();
			using var bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			return ms.ToArray();
		}

		private void SyncState(Serializer ser)
		{
			byte[] core = null;
			if (ser.IsWriter)
			{
				using var ms = new MemoryStream();
				ms.Close();
				core = ms.ToArray();
			}
			Cpu.SyncState(ser);

			ser.BeginSection(nameof(SMS));
			Vdp.SyncState(ser);
			PSG.SyncState(ser);
			ser.Sync("RAM", ref SystemRam, false);
			ser.Sync(nameof(RomBank0), ref RomBank0);
			ser.Sync(nameof(RomBank1), ref RomBank1);
			ser.Sync(nameof(RomBank2), ref RomBank2);
			ser.Sync(nameof(RomBank3), ref RomBank3);
			ser.Sync(nameof(Bios_bank), ref Bios_bank);
			ser.Sync(nameof(Port01), ref Port01);
			ser.Sync(nameof(Port02), ref Port02);
			ser.Sync(nameof(Port03), ref Port03);
			ser.Sync(nameof(Port04), ref Port04);
			ser.Sync(nameof(Port05), ref Port05);
			ser.Sync(nameof(Port3E), ref Port3E);
			ser.Sync(nameof(Port3F), ref Port3F);
			ser.Sync(nameof(Controller1SelectHigh), ref Controller1SelectHigh);
			ser.Sync(nameof(Controller2SelectHigh), ref Controller2SelectHigh);
			ser.Sync(nameof(LatchLightPhaser), ref LatchLightPhaser);
			ser.Sync(nameof(start_pressed), ref start_pressed);
			ser.Sync(nameof(cntr_rd_0), ref cntr_rd_0);
			ser.Sync(nameof(cntr_rd_1), ref cntr_rd_1);
			ser.Sync(nameof(cntr_rd_2), ref cntr_rd_2);
			ser.Sync(nameof(p3_write), ref p3_write);
			ser.Sync(nameof(p4_read), ref p4_read);
			ser.Sync(nameof(stand_alone), ref stand_alone);
			ser.Sync(nameof(disablePSG), ref disablePSG);
			ser.Sync(nameof(sampleclock), ref sampleclock);
			ser.Sync(nameof(old_s_L), ref old_s_L);
			ser.Sync(nameof(old_s_R), ref old_s_R);

			if (SaveRAM != null)
			{
				ser.Sync(nameof(SaveRAM), ref SaveRAM, false);
			}

			ser.Sync(nameof(SaveRamBank), ref SaveRamBank);

			if (ExtRam != null)
			{
				ser.Sync("ExtRAM", ref ExtRam, true);
			}

			if (HasYM2413)
			{
				YM2413.SyncState(ser);
			}
			
			if (EEPROM != null)
			{
				EEPROM.SyncState(ser);
			}

			ser.Sync("Frame", ref _frame);
			ser.Sync("LagCount", ref _lagCount);
			ser.Sync("IsLag", ref _isLag);

			ser.EndSection();

			if (ser.IsReader)
			{
				SyncAllByteArrayDomains();
			}
		}
	}
}
