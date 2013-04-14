using System.IO;

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
			board.SyncState(ser);
			ser.BeginSection("core");
			ser.Sync("cyclesPerFrame", ref cyclesPerFrame);
			ser.Sync("loadPrg", ref loadPrg);
			ser.EndSection();
		}
	}
}
