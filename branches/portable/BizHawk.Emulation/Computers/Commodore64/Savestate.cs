using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64
{
	public partial class C64 : IEmulator
	{
		public void ClearSaveRam()
		{
		}

		public void LoadStateBinary(BinaryReader br)
		{
			SyncState(new Serializer(br));
		}

		public void LoadStateText(TextReader reader)
		{
			SyncState(new Serializer(reader));
		}

		public byte[] ReadSaveRam()
		{
			return null;
		}

		// TODO: when disk support is finished, set this flag according to if any writes to disk were done
		public bool SaveRamModified
		{
			get
			{
				return false;
			}
			set
			{
			}
		}

		public void SaveStateBinary(BinaryWriter bw)
		{
			SyncState(new Serializer(bw));
		}

		public void SaveStateText(TextWriter writer)
		{
			SyncState(new Serializer(writer));
		}

		public void StoreSaveRam(byte[] data)
		{
		}

		void SyncState(Serializer ser)
		{
			// global stuffs
			ser.BeginSection("GAME");
			ser.Sync("Lag", ref _lagcount);
			ser.Sync("Frame", ref _frame);
			ser.Sync("IsLag", ref _islag);
			ser.EndSection();

			// cpu creates its own section..
			cpu.SyncState(ser);

			ser.BeginSection("MEM");
			mem.SyncState(ser);
			ser.EndSection();

			ser.BeginSection("VIC");
			vic.SyncState(ser);
			ser.EndSection();

			ser.BeginSection("SID");
			sid.SyncState(ser);
			ser.EndSection();

			ser.BeginSection("CIA0");
			cia0.SyncState(ser);
			ser.EndSection();

			ser.BeginSection("CIA1");
			cia1.SyncState(ser);
			ser.EndSection();

			// TODO: drive
		}
	}
}
