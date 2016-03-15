using BizHawk.Common;
using BizHawk.Emulation.Cores.Computers.Commodore64.Media;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.Cassette
{
    public class TapeDrive : CassettePortDevice
    {
        [SaveState.SaveWithName("Tape")]
        private Tape _tape;

        public override void ExecutePhase2()
        {
            if (_tape != null && !ReadMotor()) _tape.ExecuteCycle();
        }

        public override void HardReset()
        {
            if (_tape != null) _tape.Rewind();
        }

        public override bool ReadDataInputBuffer()
        {
            return _tape == null || _tape.Read();
        }

        public override bool ReadSenseBuffer()
        {
            return _tape == null;
        }

        public override void SyncState(Serializer ser)
        {
            SaveState.SyncObject(ser, this);
        }

        public void Insert(Tape tape)
        {
            _tape = tape;
        }

        public void RemoveMedia()
        {
            _tape = null;
        }
    }
}
