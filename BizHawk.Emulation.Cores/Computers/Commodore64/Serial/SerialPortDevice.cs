using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.Serial
{
    public abstract class SerialPortDevice
    {
        [SaveState.DoNotSave]
        public Func<bool> ReadMasterAtn = () => true;
        [SaveState.DoNotSave]
        public Func<bool> ReadMasterClk = () => true;
        [SaveState.DoNotSave]
        public Func<bool> ReadMasterData = () => true;

        public virtual void ExecutePhase()
        {
        }

        public virtual void ExecuteDeferred(int cycles)
        {
        }

        public virtual void HardReset()
        {
        }

        public virtual bool ReadDeviceClk()
        {
            return true;
        }

        public virtual bool ReadDeviceData()
        {
            return true;
        }

        public virtual bool ReadDeviceLight()
        {
            return false;
        }

        public virtual void SyncState(Serializer ser)
        {
            SaveState.SyncObject(ser, this);
        }
    }
}
