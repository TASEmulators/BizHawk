using System.IO;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.PCEngine
{
	public sealed partial class PCEngine : IStatable
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
				writer.Flush();
				_stateBuffer = stream.ToArray();
				writer.Close();
				return _stateBuffer;
			}
			else
			{
				var stream = new MemoryStream(_stateBuffer);
				var writer = new BinaryWriter(stream);
				SaveStateBinary(writer);
				writer.Flush();
				writer.Close();
				return _stateBuffer;
			}
		}

		private byte[] _stateBuffer;

		private void SyncState(Serializer ser)
		{
			ser.BeginSection("PCEngine");
			Cpu.SyncState(ser);
			VCE.SyncState(ser);
			VDC1.SyncState(ser, 1);
			PSG.SyncState(ser);

			if (SuperGrafx)
			{
				VPC.SyncState(ser);
				VDC2.SyncState(ser, 2);
			}

			if (TurboCD)
			{
				ADPCM.SyncState(ser);
				CDAudio.SyncState(ser);
				SCSI.SyncState(ser);

				ser.Sync("CDRAM", ref CDRam, false);
				if (SuperRam != null)
				{
					ser.Sync("SuperRAM", ref SuperRam, false);
				}

				if (ArcadeCard)
				{
					ArcadeCardSyncState(ser);
				}
			}

			ser.Sync("RAM", ref Ram, false);
			ser.Sync("IOBuffer", ref IOBuffer);
			ser.Sync("CdIoPorts", ref CdIoPorts, false);
			ser.Sync("BramLocked", ref BramLocked);

			ser.Sync("Frame", ref frame);
			ser.Sync("Lag", ref _lagCount);
			ser.Sync("IsLag", ref _isLag);
			if (Cpu.ReadMemory21 == ReadMemorySF2)
			{
				ser.Sync("SF2MapperLatch", ref SF2MapperLatch);
			}

			if (PopulousRAM != null)
			{
				ser.Sync("PopulousRAM", ref PopulousRAM, false);
			}

			if (BRAM != null)
			{
				ser.Sync("BRAM", ref BRAM, false);
			}

			ser.EndSection();

			if (ser.IsReader)
			{
				SyncAllByteArrayDomains();
			}
		}
	}
}
