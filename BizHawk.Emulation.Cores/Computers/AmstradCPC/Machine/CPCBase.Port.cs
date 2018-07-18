
using BizHawk.Common.NumberExtensions;
using System;
using System.Collections.Generic;

namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
    /// <summary>
    /// The abstract class that all emulated models will inherit from
    /// * Port Access *
    /// </summary>
    public abstract partial class CPCBase
    {
        /// <summary>
        /// Reads a byte of data from a specified port address
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public abstract byte ReadPort(ushort port);

        /// <summary>
        /// Writes a byte of data to a specified port address
        /// </summary>
        /// <param name="port"></param>
        /// <param name="value"></param>
        public abstract void WritePort(ushort port, byte value);

        /// <summary>
        /// Returns a single port device enum based on the port address
        /// (for IN operations)
        /// https://web.archive.org/web/20090808085929/http://www.cepece.info/amstrad/docs/iopord.html
        /// http://www.cpcwiki.eu/index.php/I/O_Port_Summary
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        protected virtual PortDevice DecodeINPort(ushort port)
        {
            PortDevice dev = PortDevice.Unknown;

            if (!port.Bit(15) && port.Bit(14))
                dev = PortDevice.GateArray;

            else if (!port.Bit(15))
                dev = PortDevice.RAMManagement;

            else if (!port.Bit(14))
                dev = PortDevice.CRCT;

            else if (!port.Bit(13))
                dev = PortDevice.ROMSelect;

            else if (!port.Bit(12))
                dev = PortDevice.Printer;

            else if (!port.Bit(11))
                dev = PortDevice.PPI;

            else if (!port.Bit(10))
                dev = PortDevice.Expansion;

            return dev;
        }

        /// <summary>
        /// Returns a list of port device enums based on the port address
        /// (for OUT operations)
        /// https://web.archive.org/web/20090808085929/http://www.cepece.info/amstrad/docs/iopord.html
        /// http://www.cpcwiki.eu/index.php/I/O_Port_Summary
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        protected virtual List<PortDevice> DecodeOUTPort(ushort port)
        {
            List<PortDevice> devs = new List<PortDevice>();

            if (!port.Bit(15) && port.Bit(14))
                devs.Add(PortDevice.GateArray);

            if (!port.Bit(15))
                devs.Add(PortDevice.RAMManagement);

            if (!port.Bit(14))
                devs.Add(PortDevice.CRCT);

            if (!port.Bit(13))
                devs.Add(PortDevice.ROMSelect);

            if (!port.Bit(12))
                devs.Add(PortDevice.Printer);

            if (!port.Bit(11))
                devs.Add(PortDevice.PPI);

            if (!port.Bit(10))
                devs.Add(PortDevice.Expansion);

            return devs;
        }

        /// <summary>
        /// Potential port devices
        /// </summary>
        public enum PortDevice
        {
            Unknown,
            GateArray,
            RAMManagement,
            CRCT,
            ROMSelect,
            Printer,
            PPI,
            Expansion
        }
    }
}
