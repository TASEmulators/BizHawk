using System.IO;

using BizHawk.Common;
using BizHawk.Emulation.Common;


namespace BizHawk.Emulation.Cores.Sega.MasterSystem
{
	public sealed partial class SMS : IStatable
	{
		public bool BinarySaveStatesPreferred
		{
			get { return true; }
		}

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
			MemoryStream ms = new MemoryStream();
			BinaryWriter bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			return ms.ToArray();
		}

		private void SyncState(Serializer ser)
		{
			byte[] core = null;
			if (ser.IsWriter)
			{
				var ms = new MemoryStream();
				ms.Close();
				core = ms.ToArray();
			}
			Cpu.SyncState(ser);

			ser.BeginSection("SMS");			
			Vdp.SyncState(ser);
			PSG.SyncState(ser);
			ser.Sync("RAM", ref SystemRam, false);
			ser.Sync("RomBank0", ref RomBank0);
			ser.Sync("RomBank1", ref RomBank1);
			ser.Sync("RomBank2", ref RomBank2);
			ser.Sync("RomBank3", ref RomBank3);
			ser.Sync("Bios_bank", ref Bios_bank);
			ser.Sync("Port01", ref Port01);
			ser.Sync("Port02", ref Port02);
			ser.Sync("Port05", ref Port05);
			ser.Sync("Port3E", ref Port3E);
			ser.Sync("Port3F", ref Port3F);
			ser.Sync("Controller1SelectHigh", ref Controller1SelectHigh);
			ser.Sync("ControllerSelect2High", ref Controller2SelectHigh);
			ser.Sync("LatchLightPhaser", ref LatchLightPhaser);

			if (SaveRAM != null)
			{
				ser.Sync("SaveRAM", ref SaveRAM, false);
			}

			ser.Sync("SaveRamBank", ref SaveRamBank);

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
