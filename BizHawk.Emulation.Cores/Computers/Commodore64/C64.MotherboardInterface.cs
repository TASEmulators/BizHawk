using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        /*
	    private bool Cia0_ReadCnt()
		{
			return User.ReadCounter1() && Cia0.ReadCntBuffer();
		}

	    private int Cia0_ReadPortA()
		{
			return cia0InputLatchA;
		}

	    private int Cia0_ReadPortB()
		{
			return cia0InputLatchB;
		}

	    private bool Cia0_ReadSP()
		{
			return User.ReadSerial1() && Cia0.ReadSpBuffer();
		}

	    private bool Cia1_ReadSP()
		{
			return User.ReadSerial2() && Cia1.ReadSpBuffer();
		}

	    private bool Cia1_ReadCnt()
		{
			return User.ReadCounter2() && Cia1.ReadCntBuffer();
		}
        */

        private int Cia1_ReadPortA()
		{
			// the low bits are actually the VIC memory address.
			var result = 0xFF;
			if (Serial.WriteDataIn())
				result &= 0x7F;
			if (Serial.WriteClockIn())
				result &= 0xBF;
			return result;
		}

	    private int Cpu_ReadPort()
		{
			var data = 0x1F;
			if (!Cassette.ReadSenseBuffer())
				data &= 0xEF;
			return data;
		}

	    private void Cpu_WriteMemoryPort(int addr, int val)
		{
			Pla.WriteMemory(addr, Bus);
		}

	    private bool Glue_ReadIRQ()
		{
			return Cia0.ReadIrq() && Vic.ReadIrq() && CartPort.ReadIrq();
		}

        private bool Glue_ReadNMI()
        {
            return Cia1.ReadIrq() && CartPort.ReadNmi();
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
            var result = Bus;
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

        /*
	    private bool SerPort_ReadAtnOut()
		{
			return (Cia1.PortBData & 0x08) == 0;
		}

	    private bool SerPort_ReadClockOut()
		{
			return (Cia1.PortAData & 0x10) == 0;
		}

	    private bool SerPort_ReadDataOut()
		{
			return (Cia1.PortAData & 0x20) == 0;
		}
        */

        private int Sid_ReadPotX()
		{
			return 0;
		}

	    private int Sid_ReadPotY()
		{
			return 0;
		}

	    private int Vic_ReadMemory(int addr)
		{
            // the system sees (cia1.PortAData & 0x3) but we use a shortcut
            _lastReadVicAddress = addr | _vicBank;
			_lastReadVicData = Pla.VicRead(_lastReadVicAddress);
            return _lastReadVicData;
		}
	}
}
