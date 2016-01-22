using BizHawk.Common;
using BizHawk.Emulation.Cores.Computers.Commodore64.Media;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.Cassette
{
    public class TapeDrive : CassettePortDevice
    {
        private Tape _tape;

        public override void HardReset()
        {
            if (_tape != null) _tape.Rewind();
        }

        public override bool ReadDataInputBuffer()
        {
            return _tape == null || ReadMotor() || _tape.Read();
        }

        public override bool ReadSenseBuffer()
        {
            return _tape == null; // Just assume that "play" is constantly pressed as long as a tape is inserted
        }

        public override void SyncState(Serializer ser)
        {
            SaveState.SyncObject(ser, this);
        }

        public void Insert(Tape tape)
        {
            _tape = tape;
        }
    }
}
