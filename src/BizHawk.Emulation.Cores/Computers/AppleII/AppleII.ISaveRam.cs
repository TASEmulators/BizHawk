using System.IO;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.AppleII
{
	public partial class AppleII : ISaveRam
	{
		private byte[][] _diskDeltas;

		private void InitSaveRam()
		{
			_diskDeltas = new byte[DiskCount][];
		}

		public bool SaveRamModified => true;

		public byte[] CloneSaveRam()
		{
			using var ms = new MemoryStream();
			using var bw = new BinaryWriter(ms);

			SaveDelta();
			bw.Write(DiskCount);
			for (var i = 0; i < DiskCount; i++)
			{
				bw.WriteByteBuffer(_diskDeltas[i]);
			}

			return ms.ToArray();
		}

		public void StoreSaveRam(byte[] data)
		{
			using var ms = new MemoryStream(data, false);
			using var br = new BinaryReader(ms);

			var ndisks = br.ReadInt32();

			if (ndisks != DiskCount)
			{
				throw new InvalidOperationException("Disk count mismatch!");
			}

			for (var i = 0; i < DiskCount; i++)
			{
				_diskDeltas[i] = br.ReadByteBuffer(returnNull: true);
			}

			LoadDelta(true);
		}

		private void SaveDelta()
		{
			_machine.DiskIIController.Drive1.DeltaUpdate((current, original) =>
			{
				_diskDeltas[CurrentDisk] = DeltaSerializer.GetDelta<byte>(original, current).ToArray();
			});
		}

		private void LoadDelta(bool maybeDifferent)
		{
			_machine.DiskIIController.Drive1.DeltaUpdate((current, original) =>
			{
				if (_diskDeltas[CurrentDisk] is not null)
				{
					DeltaSerializer.ApplyDelta<byte>(original, current, _diskDeltas[CurrentDisk]);
				}
				else if (maybeDifferent)
				{
					original.AsSpan().CopyTo(current);
				}
			});
		}
	}
}
