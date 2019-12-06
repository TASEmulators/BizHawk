using System.IO;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64
{
	public sealed partial class C64 : IStatable
	{
		public bool BinarySaveStatesPreferred => true;

		public void LoadStateBinary(BinaryReader br)
		{
			SyncState(new Serializer(br));
		}

		public void LoadStateText(TextReader reader)
		{
			SyncState(new Serializer(reader));
		}

		public void SaveStateBinary(BinaryWriter bw)
		{
			SyncState(new Serializer(bw));
		}

		public void SaveStateText(TextWriter writer)
		{
			SyncState(new Serializer(writer));
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
			ser.BeginSection("core");
			ser.Sync(nameof(_frameCycles), ref _frameCycles);
			ser.Sync(nameof(Frame), ref _frame);
			ser.Sync(nameof(IsLagFrame), ref _isLagFrame);
			ser.Sync(nameof(LagCount), ref _lagCount);
			ser.Sync(nameof(CurrentDisk), ref _currentDisk);
			ser.Sync("PreviousDiskPressed", ref _prevPressed);
			ser.Sync("NextDiskPressed", ref _nextPressed);
			ser.BeginSection("Board");
			_board.SyncState(ser);
			ser.EndSection();
			ser.EndSection();
		}
	}
}
