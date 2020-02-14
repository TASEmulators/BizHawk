using System.IO;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Intellivision
{
	public partial class Intellivision : IStatable
	{
		public void SaveStateBinary(BinaryWriter bw)
		{
			SyncState(Serializer.CreateBinaryWriter(bw));
		}

		public void LoadStateBinary(BinaryReader br)
		{
			SyncState(Serializer.CreateBinaryReader(br));
			SetupMemoryDomains(); // resync the memory domains
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
			int version = 1;
			ser.BeginSection(nameof(Intellivision));
			ser.Sync(nameof(version), ref version);
			ser.Sync("Frame", ref _frame);
			ser.Sync("stic_row", ref _sticRow);

			ser.Sync(nameof(ScratchpadRam), ref ScratchpadRam, false);
			ser.Sync(nameof(SystemRam), ref SystemRam, false);
			ser.Sync(nameof(ExecutiveRom), ref ExecutiveRom, false);
			ser.Sync(nameof(GraphicsRom), ref GraphicsRom, false);
			ser.Sync(nameof(GraphicsRam), ref GraphicsRam, false);
			ser.Sync("islag", ref _isLag);
			ser.Sync("lagcount", ref _lagCount);

			_cpu.SyncState(ser);
			_stic.SyncState(ser);
			_psg.SyncState(ser);
			_cart.SyncState(ser);
			_controllerDeck.SyncState(ser);

			ser.EndSection();
		}
	}
}
