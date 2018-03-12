using System.IO;

using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
    public partial class ZXSpectrum : IStatable
    {
        public bool BinarySaveStatesPreferred
        {
            get { return true; }
        }

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
            MemoryStream ms = new MemoryStream();
            BinaryWriter bw = new BinaryWriter(ms);
            SaveStateBinary(bw);
            bw.Flush();
            return ms.ToArray();
        }

        private void SyncState(Serializer ser)
        {
            byte[] core = null;
            if (ser.IsWriter)
            {
                var ms = new MemoryStream();
                ms.Close();
                core = ms.ToArray();
            }
            _cpu.SyncState(ser);

            ser.BeginSection("ZXSpectrum");
            _machine.SyncState(ser);
            ser.Sync("Frame", ref _machine.FrameCount);
            ser.Sync("LagCount", ref _lagCount);
            ser.Sync("IsLag", ref _isLag);

            ser.EndSection();

            if (ser.IsReader)
            {
                SyncAllByteArrayDomains();
            }
        }
    }
}
