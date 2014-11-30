using System.IO;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64
{
	sealed public partial class C64 : IEmulator
	{
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

		void SyncState(Serializer ser)
		{
			ser.BeginSection("core");
			board.SyncState(ser);
			ser.EndSection();
		}
	}
}
