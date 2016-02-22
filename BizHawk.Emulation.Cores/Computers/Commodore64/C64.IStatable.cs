using System.IO;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64
{
	public sealed partial class C64 : IStatable
	{
		public bool BinarySaveStatesPreferred { get { return false; } }

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
		    using (var ms = new MemoryStream())
		    {
                var bw = new BinaryWriter(ms);
                SaveStateBinary(bw);
                bw.Flush();
                return ms.ToArray();
            }
        }

		private void SyncState(Serializer ser)
		{
			ser.BeginSection("core");
			SaveState.SyncObject(ser, this);
			ser.EndSection();
		}
	}
}
