
using BizHawk.Common.NumberExtensions;
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
		public abstract byte ReadPort(ushort port);

		/// <summary>
		/// Writes a byte of data to a specified port address
		/// </summary>
		public abstract void WritePort(ushort port, byte value);

		/// <summary>
		/// Returns a single port device enum based on the port address
		/// (for IN operations)
		/// https://web.archive.org/web/20090808085929/http://www.cepece.info/amstrad/docs/iopord.html
		/// http://www.cpcwiki.eu/index.php/I/O_Port_Summary
		/// </summary>
		protected virtual List<PortDevice> DecodeINPort(ushort port)
		{
			var devs = new List<PortDevice>();

			if (!port.Bit(15) && port.Bit(14))
				devs.Add(PortDevice.GateArray);

			if (!port.Bit(15))
				devs.Add(PortDevice.PAL);

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
		/// Returns a list of port device enums based on the port address
		/// (for OUT operations)
		/// https://web.archive.org/web/20090808085929/http://www.cepece.info/amstrad/docs/iopord.html
		/// http://www.cpcwiki.eu/index.php/I/O_Port_Summary
		/// </summary>
		protected virtual List<PortDevice> DecodeOUTPort(ushort port)
		{
			var devs = new List<PortDevice>();

			if (!port.Bit(15) && port.Bit(14))
				devs.Add(PortDevice.GateArray);

			if (!port.Bit(15))
				devs.Add(PortDevice.PAL);

			if (!port.Bit(15))
				devs.Add(PortDevice.PAL);

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
			PAL,
			CRCT,
			ROMSelect,
			Printer,
			PPI,
			Expansion
		}
	}
}
