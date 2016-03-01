using System;
using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.Cassette
{
    public sealed class CassettePort
    {
        [SaveState.DoNotSave]
        public Func<bool> ReadDataOutput = () => true;
        [SaveState.DoNotSave]
        public Func<bool> ReadMotor = () => true;

        [SaveState.SaveWithName("Device")]
        private CassettePortDevice _device;
        [SaveState.SaveWithName("Connected")]
        private bool _connected;

        public void HardReset()
        {
            if (_connected)
            {
                _device.HardReset();
            }
        }

        public void ExecutePhase()
        {
            if (_connected)
            {
                _device.ExecutePhase2();
            }
        }

        public bool ReadDataInputBuffer()
        {
            return !_connected || _device.ReadDataInputBuffer();
        }

        public bool ReadSenseBuffer()
        {
            return !_connected || _device.ReadSenseBuffer();
        }

        public void SyncState(Serializer ser)
        {
            SaveState.SyncObject(ser, this);
        }

        public void Connect(CassettePortDevice device)
        {
            _connected = device != null;
            _device = device;
            if (_device == null)
            {
                return;
            }

            _device.ReadDataOutput = () => ReadDataOutput();
            _device.ReadMotor = () => ReadMotor();
        }
    }
}
