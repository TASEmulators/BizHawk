using System.IO;

using BizHawk.Common;
using BizHawk.Emulation.Common;


namespace BizHawk.Emulation.Cores.Sega.MasterSystem
{
	public sealed partial class SMS : IStatable
	{
		public bool BinarySaveStatesPreferred
		{
			get { return false; }
		}

		public void SaveStateBinary(BinaryWriter bw)
		{
			SyncState(Serializer.CreateBinaryWriter(bw));
		}

		public void LoadStateBinary(BinaryReader br)
		{
			SyncState(Serializer.CreateBinaryReader(br));
		}

		public void SaveStateText(TextWriter tw)
		{
			SyncState(Serializer.CreateTextWriter(tw));
		}

		public void LoadStateText(TextReader tr)
		{
			SyncState(Serializer.CreateTextReader(tr));
		}

		public byte[] SaveStateBinary()
		{
			if (_stateBuffer == null)
			{
				var stream = new MemoryStream();
				var writer = new BinaryWriter(stream);
				SaveStateBinary(writer);
				_stateBuffer = stream.ToArray();
				writer.Close();
				return _stateBuffer;
			}
			else
			{
				var stream = new MemoryStream(_stateBuffer);
				var writer = new BinaryWriter(stream);
				SaveStateBinary(writer);
				writer.Close();
				return _stateBuffer;
			}
		}

		private byte[] _stateBuffer;

		private void SyncState(Serializer ser)
		{
			ser.BeginSection("SMS");
			Cpu.SyncState(ser);
			Vdp.SyncState(ser);
			PSG.SyncState(ser);
			ser.Sync("RAM", ref SystemRam, false);
			ser.Sync("RomBank0", ref RomBank0);
			ser.Sync("RomBank1", ref RomBank1);
			ser.Sync("RomBank2", ref RomBank2);
			ser.Sync("RomBank3", ref RomBank3);
			ser.Sync("Port01", ref Port01);
			ser.Sync("Port02", ref Port02);
			ser.Sync("Port3E", ref Port3E);
			ser.Sync("Port3F", ref Port3F);

			if (SaveRAM != null)
			{
				ser.Sync("SaveRAM", ref SaveRAM, false);
				ser.Sync("SaveRamBank", ref SaveRamBank);
			}

			if (ExtRam != null)
			{
				ser.Sync("ExtRAM", ref ExtRam, true);
			}

			if (HasYM2413)
			{
				YM2413.SyncState(ser);
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
