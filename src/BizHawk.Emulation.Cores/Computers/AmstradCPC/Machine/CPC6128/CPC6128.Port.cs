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
			int finalResult = 0;
			int result = 0;
			bool deviceResponse = false;

			var devs = DecodeINPort(port);

			foreach (var d in devs)
			{
				if (d == PortDevice.PPI)
				{
					PPI.ReadPort(port, ref result);
					finalResult |= result;
					deviceResponse = true;
				}

				if (d == PortDevice.Expansion)
				{
					if (!port.Bit(7))
					{
						// FDC
						if (port.Bit(8) && !port.Bit(0))
						{
							// FDC status register
							UPDDiskDevice.ReadStatus(ref result);
						}
						else if (port.Bit(8) && port.Bit(0))
						{
							// FDC data register
							UPDDiskDevice.ReadData(ref result);
						}

						finalResult |= result;
						deviceResponse = true;
					}
				}

				if (d == PortDevice.GateArray && !deviceResponse)
				{
					// ACCC 4.4.2
					// The GATE ARRAY is write-only, and the RD pin is in the inactive state, which implies that a read
					// on this circuit is not considered. At best, a high impedance state available on the data bus is recovered.
					result = 0xFF;
					finalResult |= result;
				}

				if (d == PortDevice.CRCT)
				{
					/*
					// ACCC 4.4.2
					// However, the CRTCs are not connected to the Z80A's RD and WR pins, so there is no detection of the I/O direction.
					// Consequently, if a read instruction is used on a write register of the CRTC, then a data is sent to the CRTC
					// (whatever is on the data bus). "it would be risky to trust the returned value".
					CRTC.WritePort(port, CPU.Regs[CPU.DB]);
					result = CPU.Regs[CPU.DB];

					if (!deviceResponse)
						finalResult |= result;
					*/
				}				

				if (d == PortDevice.ROMSelect && !deviceResponse)
				{
					// TODO: confirm this is a write-only port
					result = 0xFF;
					finalResult |= result;
				}

				if (d == PortDevice.Printer && !deviceResponse)
				{
					// TODO: confirm this is a write-only port
					result = 0xFF;
					finalResult |= result;
				}

				if (d == PortDevice.PAL && !deviceResponse)
				{
					// TODO: confirm this is a write-only port
					result = 0xFF;
					finalResult |= result;
				}
			}

			return (byte)finalResult;
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
