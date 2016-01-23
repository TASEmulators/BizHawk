using System;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.Cassette
{
    public abstract class CassettePortDevice
    {
        public Func<bool> ReadDataOutput;
        public Func<bool> ReadMotor;

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
