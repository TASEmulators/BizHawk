/*
 * NullDevice.cs
 *
 * Default memory mappable device.
 *
 * Copyright © 2003, 2004 Mike Murphy
 *
 */
using System;

namespace EMU7800.Core
{
    public sealed class NullDevice : IDevice
    {
        MachineBase M { get; set; }

        #region IDevice Members

        public void Reset()
        {
            Log("{0} reset", this);
        }

        public byte this[ushort addr]
        {
            get
            {
                LogDebug("NullDevice: Peek at ${0:x4}, PC=${1:x4}", addr, M.CPU.PC);
                return 0;
            }
            set
            {
                LogDebug("NullDevice: Poke at ${0:x4},${1:x2}, PC=${2:x4}", addr, value, M.CPU.PC);
            }
        }

        #endregion

        public override String ToString()
        {
            return "NullDevice";
        }

        #region Constructors

        private NullDevice()
        {
        }

        public NullDevice(MachineBase m)
        {
            if (m == null)
                throw new ArgumentNullException("m");
            M = m;
        }

        #endregion

        #region Helpers

        void Log(string format, params object[] args)
        {
            if (M == null || M.Logger == null)
                return;
            M.Logger.WriteLine(format, args);
        }

        [System.Diagnostics.Conditional("DEBUG")]
        void LogDebug(string format, params object[] args)
        {
            if (M == null || M.Logger == null)
                return;
            M.Logger.WriteLine(format, args);
        }

        #endregion
    }
}