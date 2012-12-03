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
			chips.SyncState(ser);
			ser.BeginSection("core");
			ser.Sync("cyclesPerFrame", ref cyclesPerFrame);
			ser.Sync("loadPrg", ref loadPrg);
			for (uint i = 0; i < 2; i++)
				for (uint j = 0; j < 5; j++)
					ser.Sync("joystickPressed" + i.ToString() + j.ToString(), ref joystickPressed[i, j]);
			for (uint i = 0; i < 8; i++)
				for (uint j = 0; j < 8; j++)
					ser.Sync("keyboardPressed" + i.ToString() + j.ToString(), ref keyboardPressed[i, j]);
			ser.EndSection();
		}
	}
}
