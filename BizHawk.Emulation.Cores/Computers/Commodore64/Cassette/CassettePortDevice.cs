using System;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.Cassette
{
    public abstract class CassettePortDevice
    {
        [SaveState.DoNotSave]
        public Func<bool> ReadDataOutput;
        [SaveState.DoNotSave]
        public Func<bool> ReadMotor;

        public virtual void ExecutePhase2()
        {
        }

        public virtual void HardReset()
        {
        }

        public virtual bool ReadDataInputBuffer()
        {
            return true;
        }

        public virtual bool ReadSenseBuffer()
        {
            return true;
        }

        public virtual void SyncState(Serializer ser)
        {
            SaveState.SyncObject(ser, this);
        }
    }
}
