using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Computers.AmstradCPC
{
	/// <summary>
	/// CPC6128
	/// * Port *
	/// </summary>
	public partial class CPC6128 : CPCBase
	{
		/// <summary>
		/// Reads a byte of data from a specified port address
		/// </summary>
		public override byte ReadPort(ushort port)
		{
			int result = 0xff;

			if (DecodeINPort(port) == PortDevice.GateArray)
			{
				GateArray.ReadPort(port, ref result);
			}
			else if (DecodeINPort(port) == PortDevice.CRCT)
			{
				CRTC.ReadPort(port, ref result);
			}
			else if (DecodeINPort(port) == PortDevice.ROMSelect)
			{

			}
			else if (DecodeINPort(port) == PortDevice.Printer)
			{

			}
			else if (DecodeINPort(port) == PortDevice.PPI)
			{
				PPI.ReadPort(port, ref result);
			}
			else if (DecodeINPort(port) == PortDevice.Expansion)
			{
				if (!port.Bit(7))
				{
					// FDC
					if (port.Bit(8) && !port.Bit(0))
					{
						// FDC status register
						UPDDiskDevice.ReadStatus(ref result);
					}
					if (port.Bit(8) && port.Bit(0))
					{
						// FDC data register
						UPDDiskDevice.ReadData(ref result);
					}
				}
			}

			return (byte)result;
		}

		/// <summary>
		/// Writes a byte of data to a specified port address
		/// Because of the port decoding, multiple devices can be written to
		/// </summary>
		public override void WritePort(ushort port, byte value)
		{
			var devs = DecodeOUTPort(port);

			foreach (var d in devs)
			{
				if (d == PortDevice.GateArray)
				{
					GateArray.WritePort(port, value);
				}
				else if (d == PortDevice.PAL)
				{
					PAL.WritePort(port, value);					
				}
				else if (d == PortDevice.CRCT)
				{
					CRTC.WritePort(port, value);
				}
				else if (d == PortDevice.ROMSelect)
				{
					UpperROMPosition = value;
				}
				else if (d == PortDevice.Printer)
				{

				}
				else if (d == PortDevice.PPI)
				{
					PPI.WritePort(port, value);
				}
				else if (d == PortDevice.Expansion)
				{
					if (!port.Bit(7))
					{
						// FDC
						if (port.Bit(8) && !port.Bit(0) || port.Bit(8) && port.Bit(0))
						{
							// FDC data register
							UPDDiskDevice.WriteData(value);
						}
						if ((!port.Bit(8) && !port.Bit(0)) || (!port.Bit(8) && port.Bit(0)))
						{
							// FDC motor
							UPDDiskDevice.Motor(value);
						}
					}
				}
			}

			return;
		}
	}
}
