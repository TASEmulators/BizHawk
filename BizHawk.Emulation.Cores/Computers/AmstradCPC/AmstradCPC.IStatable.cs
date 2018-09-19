using System.IO;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
    /// <summary>
    /// CPCHawk: Core Class
    /// * IStatable *
    /// </summary>
    public partial class AmstradCPC : IStatable
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

            if (ser.IsWriter)
            {
                ser.SyncEnum("_machineType", ref _machineType);

                _cpu.SyncState(ser);
                ser.BeginSection("AmstradCPC");
                _machine.SyncState(ser);
                ser.Sync("Frame", ref _machine.FrameCount);
                ser.Sync("LagCount", ref _lagCount);
                ser.Sync("IsLag", ref _isLag);
                ser.EndSection();
            }

            if (ser.IsReader)
            {
                var tmpM = _machineType;
                ser.SyncEnum("_machineType", ref _machineType);
                if (tmpM != _machineType && _machineType.ToString() != "72")
                {
                    string msg = "SAVESTATE FAILED TO LOAD!!\n\n";
                    msg += "Current Configuration: " + tmpM.ToString();
                    msg += "\n";
                    msg += "Saved Configuration:    " + _machineType.ToString();
                    msg += "\n\n";
                    msg += "If you wish to load this SaveState ensure that you have the correct machine configuration selected, reboot the core, then try again.";
                    CoreComm.ShowMessage(msg);
                    _machineType = tmpM;
                }
                else
                {
                    _cpu.SyncState(ser);
                    ser.BeginSection("AmstradCPC");
                    _machine.SyncState(ser);
                    ser.Sync("Frame", ref _machine.FrameCount);
                    ser.Sync("LagCount", ref _lagCount);
                    ser.Sync("IsLag", ref _isLag);
                    ser.EndSection();

                    SyncAllByteArrayDomains();
                }
            }
        }
    }
}
