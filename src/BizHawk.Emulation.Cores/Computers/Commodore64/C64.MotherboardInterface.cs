namespace BizHawk.Emulation.Cores.Computers.Commodore64
{
	public sealed partial class Motherboard
	{
		private int _lastReadVicAddress = 0x3FFF;
		private int _lastReadVicData = 0xFF;
		private int _vicBank = 0xC000;

		private bool CassPort_ReadDataOutput()
		{
			return (Cpu.PortData & 0x08) != 0;
		}

		private bool CassPort_ReadMotor()
		{
			return (Cpu.PortData & 0x20) != 0;
		}

		private int Cia1_ReadPortA()
		{
			// the low bits are actually the VIC memory address.
			return (SerPort_ReadDataOut() && Serial.ReadDeviceData() ? 0x80 : 0x00) |
				   (SerPort_ReadClockOut() && Serial.ReadDeviceClock() ? 0x40 : 0x00) |
					0x3F;
		}

		private int Cia1_ReadPortB()
		{
			// Ordinarily these are connected to the userport.
			return 0x00;
		}

		private int Cpu_ReadPort()
		{
			var data = 0x1F;
			if (!Cassette.ReadSenseBuffer())
			{
				data &= 0xEF;
			}

			return data;
		}

		private void Cpu_WriteMemoryPort(int addr)
		{
			Pla.WriteMemory(addr, ReadOpenBus());
		}

		private bool Glue_ReadIRQ()
		{
			return Cia0.ReadIrq() && Vic.ReadIrq() && CartPort.ReadIrq();
		}

		private bool Glue_ReadNMI()
		{
			return !_restorePressed && Cia1.ReadIrq() && CartPort.ReadNmi();
		}

		private bool[] Input_ReadJoysticks()
		{
			return _joystickPressed;
		}

		private bool[] Input_ReadKeyboard()
		{
			return _keyboardPressed;
		}

		private bool Pla_ReadCharen()
		{
			return (Cpu.PortData & 0x04) != 0;
		}

		private int Pla_ReadCia0(int addr)
		{
			if (addr == 0xDC00 || addr == 0xDC01)
			{
				InputRead = true;
			}
			return Cia0.Read(addr);
		}

		private int Pla_ReadColorRam(int addr)
		{
			var result = ReadOpenBus();
			result &= 0xF0;
			result |= ColorRam.Read(addr);
			return result;
		}

		private bool Pla_ReadHiRam()
		{
			return (Cpu.PortData & 0x02) != 0;
		}

		private bool Pla_ReadLoRam()
		{
			return (Cpu.PortData & 0x01) != 0;
		}

		private int Pla_ReadExpansion0(int addr)
		{
			return CartPort.IsConnected ? CartPort.ReadLoExp(addr) : _lastReadVicData;
		}

		private int Pla_ReadExpansion1(int addr)
		{
			return CartPort.IsConnected ? CartPort.ReadHiExp(addr) : _lastReadVicData;
		}

		private bool SerPort_ReadAtnOut()
		{
			// inverted PA3 (input NOT pulled up)
			return !((Cia1.DdrA & 0x08) != 0 && (Cia1.PrA & 0x08) != 0);
		}

		private bool SerPort_ReadClockOut()
		{
			// inverted PA4 (input NOT pulled up)
			return !((Cia1.DdrA & 0x10) != 0 && (Cia1.PrA & 0x10) != 0);
		}

		private bool SerPort_ReadDataOut()
		{
			// inverted PA5 (input NOT pulled up)
			return !((Cia1.DdrA & 0x20) != 0 && (Cia1.PrA & 0x20) != 0);
		}

		private int Sid_ReadPotX()
		{
			return 255;
		}

		private int Sid_ReadPotY()
		{
			return 255;
		}

		private int Vic_ReadMemory(int addr)
		{
			// the system sees (cia1.PortAData & 0x3) but we use a shortcut
			_lastReadVicAddress = addr | _vicBank;
			_lastReadVicData = Pla.VicRead(_lastReadVicAddress);
			return _lastReadVicData;
		}

		private int ReadOpenBus()
		{
			return _lastReadVicData;
		}
	}
}
