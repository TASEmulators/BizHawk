using System.IO;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Atari.Atari2600
{
	public partial class Atari2600 : IStatable
	{
		public bool BinarySaveStatesPreferred
		{
			get { return false; }
		}

		public void SaveStateText(TextWriter writer)
		{
			SyncState(Serializer.CreateTextWriter(writer));
		}

		public void LoadStateText(TextReader reader)
		{
			SyncState(Serializer.CreateTextReader(reader));
		}

		public void SaveStateBinary(BinaryWriter bw)
		{
			SyncState(Serializer.CreateBinaryWriter(bw));
		}

		public void LoadStateBinary(BinaryReader br)
		{
			SyncState(Serializer.CreateBinaryReader(br));
		}

		public byte[] SaveStateBinary()
		{
			var ms = new MemoryStream();
			var bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			return ms.ToArray();
		}

		private void SyncState(Serializer ser)
		{
			ser.BeginSection("A2600");
			Cpu.SyncState(ser);
			ser.Sync("ram", ref this.Ram, false);
			ser.Sync("Lag", ref _lagcount);
			ser.Sync("Frame", ref _frame);
			ser.Sync("IsLag", ref _islag);
			ser.Sync("frameStartPending", ref _frameStartPending);
			ser.Sync("leftDifficultySwitchPressed", ref _leftDifficultySwitchPressed);
			ser.Sync("rightDifficultySwitchPressed", ref _rightDifficultySwitchPressed);
			ser.Sync("leftDifficultySwitchHeld", ref _leftDifficultySwitchHeld);
			ser.Sync("rightDifficultySwitchHeld", ref _rightDifficultySwitchHeld);

			_tia.SyncState(ser);
			M6532.SyncState(ser);
			ser.BeginSection("Mapper");
			_mapper.SyncState(ser);
			ser.EndSection();
			ser.EndSection();

			if (ser.IsReader)
			{
				SyncAllByteArrayDomains();
			}
		}
	}
}
