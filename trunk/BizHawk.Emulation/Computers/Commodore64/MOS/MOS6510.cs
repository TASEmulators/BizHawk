using BizHawk.Emulation.CPUs.M6502;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Emulation.Computers.Commodore64.MOS
{
	// an extension of the 6502 processor

	public class MOS6510
	{
		// ------------------------------------

		private MOS6502X cpu;
		private bool freezeCpu;
		private bool pinCassetteButton; // note: these are only
		private bool pinCassetteMotor; // latches!
		private bool pinCassetteOutput;
		private bool pinCharen;
		private bool pinLoram;
		private bool pinHiram;
		private bool pinNMILast;
		private byte portDir;
		private bool unusedPin0;
		private bool unusedPin1;
		private uint unusedPinTTL0;
		private uint unusedPinTTL1;
		private uint unusedPinTTLCycles;

		public Func<int, byte> PeekMemory;
		public Action<int, byte> PokeMemory;
		public Func<bool> ReadAEC;
		public Func<bool> ReadCassetteButton;
		public Func<bool> ReadIRQ;
		public Func<bool> ReadNMI;
		public Func<bool> ReadRDY;
		public Func<ushort, byte> ReadMemory;
		public Action<bool> WriteCassetteLevel;
		public Action<bool> WriteCassetteMotor;
		public Action<ushort, byte> WriteMemory;

		// ------------------------------------

		public MOS6510()
		{
			cpu = new MOS6502X();

			// configure cpu r/w
			cpu.DummyReadMemory = Read;
			cpu.ReadMemory = Read;
			cpu.WriteMemory = Write;

			// configure data port defaults
			portDir = 0x00;
			SetPortData(0x17);

			// todo: verify this value (I only know that unconnected bits fade after a number of cycles)
			unusedPinTTLCycles = 40;
			unusedPinTTL0 = 0;
			unusedPinTTL1 = 0;

			// NMI is high on startup (todo: verify)
			pinNMILast = true;
		}

		public void HardReset()
		{
			cpu.Reset();
			cpu.FlagI = true;
			cpu.BCD_Enabled = true;
			cpu.PC = (ushort)(ReadMemory(0xFFFC) | (ReadMemory(0xFFFD) << 8));
		}

		// ------------------------------------

		public void ExecutePhase1()
		{
		}

		public void ExecutePhase2()
		{
			if (ReadAEC() && !freezeCpu)
			{
				// the 6502 core expects active high
				// so we reverse the polarity here
				bool thisNMI = ReadNMI();
				if (!thisNMI && pinNMILast)
					cpu.NMI = true;
				else
					cpu.NMI = false;
				pinNMILast = thisNMI;

				cpu.IRQ = !ReadIRQ();
				cpu.ExecuteOne();
			}

			// unfreeze cpu if BA is high
			if (ReadRDY()) freezeCpu = false;

			// process unused pin TTL
			if (unusedPinTTL0 == 0)
				unusedPin0 = false;
			else
				unusedPinTTL0--;

			if (unusedPinTTL1 == 0)
				unusedPin1 = false;
			else
				unusedPinTTL1--;
		}

		// ------------------------------------

		public byte Peek(int addr)
		{
			if (addr == 0x0000)
				return PortDirection;
			else if (addr == 0x0001)
				return PortData;
			else
				return PeekMemory(addr);
		}

		public void Poke(int addr, byte val)
		{
			if (addr == 0x0000)
				SetPortDir(val);
			else if (addr == 0x0001)
				SetPortData(val);
			else
				PokeMemory(addr, val);
		}

		public byte Read(ushort addr)
		{
			// cpu freezes after first read when RDY is low
			if (!ReadRDY())
				freezeCpu = true;

			if (addr == 0x0000)
				return PortDirection;
			else if (addr == 0x0001)
				return PortData;
			else
				return ReadMemory(addr);
		}

		public void Write(ushort addr, byte val)
		{
			if (addr == 0x0000)
				PortDirection = val;
			else if (addr == 0x0001)
				PortData = val;
			WriteMemory(addr, val);
		}

		// ------------------------------------

		public bool CassetteButton
		{
			get { return pinCassetteButton; }
			set { pinCassetteButton = value; }
		}

		public bool CassetteMotor
		{
			get { return pinCassetteMotor; }
		}

		public bool CassetteOutputLevel
		{
			get { return pinCassetteOutput; }
		}

		public bool Charen
		{
			get { return pinCharen; }
		}

		public bool HiRam
		{
			get { return pinHiram; }
		}

		public bool LoRam
		{
			get { return pinLoram; }
		}

		public byte PortData
		{
			get
			{
				byte result = 0x00;

				result |= pinLoram ? (byte)0x01 : (byte)0x00;
				result |= pinHiram ? (byte)0x02 : (byte)0x00;
				result |= pinCharen ? (byte)0x04 : (byte)0x00;
				result |= pinCassetteOutput ? (byte)0x08 : (byte)0x00;
				result |= pinCassetteButton ? (byte)0x10 : (byte)0x00;
				result |= pinCassetteMotor ? (byte)0x20 : (byte)0x00;
				result |= unusedPin0 ? (byte)0x40 : (byte)0x00;
				result |= unusedPin1 ? (byte)0x80 : (byte)0x00;
				return result;
			}
			set
			{
				byte val = Port.CPUWrite(PortData, value, portDir);
				SetPortData(val);
			}
		}

		public byte PortDirection
		{
			get { return portDir; }
			set
			{
				SetPortDir(value);
			}
		}

		public byte ReadPortData()
		{
			return PortData;
		}

		private void SetPortData(byte val)
		{
			pinCassetteOutput = ((val & 0x08) != 0);
			pinCassetteButton = ((val & 0x10) != 0);
			pinCassetteMotor = ((val & 0x20) != 0);

			pinLoram = ((val & 0x01) != 0) || ((portDir & 0x01) == 0);
			pinHiram = ((val & 0x02) != 0) || ((portDir & 0x02) == 0);
			pinCharen = ((val & 0x04) != 0) || ((portDir & 0x04) == 0);

			unusedPin0 = ((val & 0x40) != 0);
			unusedPin1 = ((val & 0x80) != 0);
			unusedPinTTL0 = unusedPinTTLCycles;
			unusedPinTTL1 = unusedPinTTLCycles;
		}

		private void SetPortDir(byte val)
		{
			portDir = val;
			SetPortData(PortData);
		}

		public void SyncState(Serializer ser)
		{
			cpu.SyncState(ser);
			ser.Sync("freezeCpu", ref freezeCpu);
			ser.Sync("pinCassetteButton", ref pinCassetteButton);
			ser.Sync("pinCassetteMotor", ref pinCassetteMotor);
			ser.Sync("pinCassetteOutput", ref pinCassetteOutput);
			ser.Sync("pinCharen", ref pinCharen);
			ser.Sync("pinLoram", ref pinLoram);
			ser.Sync("pinHiram", ref pinHiram);
			ser.Sync("pinNMILast", ref pinNMILast);
			ser.Sync("portDir", ref portDir);
			ser.Sync("unusedPin0", ref unusedPin0);
			ser.Sync("unusedPin1", ref unusedPin1);
			ser.Sync("unusedPinTTL0", ref unusedPinTTL0);
			ser.Sync("unusedPinTTL1", ref unusedPinTTL1);
			ser.Sync("unusedPinTTLCycles", ref unusedPinTTLCycles);
		}

		public void WritePortData(byte data)
		{
			PortData = data;
		}

		// ------------------------------------
	}
}
