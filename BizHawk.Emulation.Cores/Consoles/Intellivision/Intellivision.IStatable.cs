using System.IO;

using BizHawk.Common;
using BizHawk.Emulation.Common;


namespace BizHawk.Emulation.Cores.Intellivision
{
	public partial class Intellivision : IStatable
	{
		public bool BinarySaveStatesPreferred
		{
			get { return true; }
		}

		public void SaveStateText(TextWriter writer)
		{
			SyncState(Serializer.CreateTextWriter(writer));
		}

		public void LoadStateText(TextReader reader)
		{
			SyncState(Serializer.CreateTextReader(reader));
			SetupMemoryDomains(); // resync the memory domains
		}

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
			MemoryStream ms = new MemoryStream();
			BinaryWriter bw = new BinaryWriter(ms);
			SaveStateBinary(bw);
			bw.Flush();
			return ms.ToArray();
		}

		private void SyncState(Serializer ser)
		{
			int version = 1;
			ser.BeginSection("Intellivision");
			ser.Sync("version", ref version);
			ser.Sync("Frame", ref _frame);
			ser.Sync("stic_row", ref stic_row);

			ser.Sync("ScratchpadRam", ref ScratchpadRam, false);
			ser.Sync("SystemRam", ref SystemRam, false);
			ser.Sync("ExecutiveRom", ref ExecutiveRom, false);
			ser.Sync("GraphicsRom", ref GraphicsRom, false);
			ser.Sync("GraphicsRam", ref GraphicsRam, false);
			ser.Sync("islag", ref islag);
			ser.Sync("lagcount", ref lagcount);

			_cpu.SyncState(ser);
			_stic.SyncState(ser);
			_psg.SyncState(ser);
			_cart.SyncState(ser);
			ControllerDeck.SyncState(ser);

			ser.EndSection();
		}
	}
}
