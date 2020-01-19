using System;
using System.IO;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.MSX
{
	public partial class MSX : IStatable
	{
		public bool BinarySaveStatesPreferred => true;

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
			ser.BeginSection("MSX");

			if (SaveRAM != null)
			{
				ser.Sync(nameof(SaveRAM), ref SaveRAM, false);
			}

			ser.Sync(nameof(SaveRamBank), ref SaveRamBank);

			ser.Sync("Frame", ref _frame);
			ser.Sync("LagCount", ref _lagCount);
			ser.Sync("IsLag", ref _isLag);

			ser.EndSection();

			if (ser.IsReader)
			{
				ser.Sync(nameof(MSX_core), ref MSX_core, false);
				LibMSX.MSX_load_state(MSX_Pntr, MSX_core);
				Console.WriteLine("here1");
			}
			else
			{
				LibMSX.MSX_save_state(MSX_Pntr, MSX_core);
				ser.Sync(nameof(MSX_core), ref MSX_core, false);
				Console.WriteLine("here2");
			}
		}
	}
}
