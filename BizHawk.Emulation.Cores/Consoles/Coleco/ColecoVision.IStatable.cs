using System.IO;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.ColecoVision
{
	public partial class ColecoVision : IStatable
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

		private void SyncState(Serializer ser)
		{
			ser.BeginSection("Coleco");
			Cpu.SyncState(ser);
			VDP.SyncState(ser);
			PSG.SyncState(ser);
			ser.Sync("RAM", ref Ram, false);
			ser.Sync("Frame", ref frame);
			ser.Sync("LagCount", ref _lagCount);
			ser.Sync("IsLag", ref _isLag);
			ser.EndSection();

			if (ser.IsReader)
			{
				SyncAllByteArrayDomains();
			}
		}

		private byte[] _stateBuffer;
	}
}
