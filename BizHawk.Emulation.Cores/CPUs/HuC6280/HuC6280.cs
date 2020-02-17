using System;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Components.H6280
{
	public sealed partial class HuC6280
	{
		public HuC6280(IMemoryCallbackSystem callbacks)
		{
			Reset();
			MemoryCallbacks = callbacks;
		}

		public void Reset()
		{
			A = 0;
			X = 0;
			Y = 0;
			//P = 0x14; // Set I and B
			P = 0x04; // Set I
			S = 0;
			PC = 0;
			PendingCycles = 0;
			TotalExecutedCycles = 0;
			LagIFlag = true;
			LowSpeed = true;
		}

		public void ResetPC()
		{
			PC = ReadWord(ResetVector);
		}

		// ==== CPU State ====

		public byte A;
		public byte X;
		public byte Y;
		public byte P;
		public ushort PC;
		public byte S;
		public byte[] MPR = new byte[8];

		public bool LagIFlag;
		public bool IRQ1Assert;
		public bool IRQ2Assert;
		public bool TimerAssert;
		public byte IRQControlByte, IRQNextControlByte;

		public long TotalExecutedCycles;
		public int PendingCycles;
		public bool LowSpeed;

		private bool InBlockTransfer = false;
		private ushort btFrom;
		private ushort btTo;
		private ushort btLen;
		private int btAlternator;

		// -- Timer Support --

		public int TimerTickCounter;
		public byte TimerReloadValue;
		public byte TimerValue;
		public bool TimerEnabled;

		public void SyncState(Serializer ser)
		{
			ser.BeginSection(nameof(HuC6280));
			ser.Sync(nameof(A), ref A);
			ser.Sync(nameof(X), ref X);
			ser.Sync(nameof(Y), ref Y);
			ser.Sync(nameof(P), ref P);
			ser.Sync(nameof(PC), ref PC);
			ser.Sync(nameof(S), ref S);
			ser.Sync(nameof(MPR), ref MPR, false);
			ser.Sync(nameof(LagIFlag), ref LagIFlag);
			ser.Sync(nameof(IRQ1Assert), ref IRQ1Assert);
			ser.Sync(nameof(IRQ2Assert), ref IRQ2Assert);
			ser.Sync(nameof(TimerAssert), ref TimerAssert);
			ser.Sync(nameof(IRQControlByte), ref IRQControlByte);
			ser.Sync(nameof(IRQNextControlByte), ref IRQNextControlByte);
			ser.Sync("ExecutedCycles", ref TotalExecutedCycles);
			ser.Sync(nameof(PendingCycles), ref PendingCycles);
			ser.Sync(nameof(LowSpeed), ref LowSpeed);
			ser.Sync(nameof(TimerTickCounter), ref TimerTickCounter);
			ser.Sync(nameof(TimerReloadValue), ref TimerReloadValue);
			ser.Sync(nameof(TimerValue), ref TimerValue);
			ser.Sync(nameof(TimerEnabled), ref TimerEnabled);
			ser.Sync(nameof(InBlockTransfer), ref InBlockTransfer);
			ser.Sync("BTFrom", ref btFrom);
			ser.Sync("BTTo", ref btTo);
			ser.Sync("BTLen", ref btLen);
			ser.Sync("BTAlternator", ref btAlternator);
			ser.EndSection();
		}

		// ==== Interrupts ====

		private const ushort ResetVector = 0xFFFE;
		private const ushort NMIVector = 0xFFFC;
		private const ushort TimerVector = 0xFFFA;
		private const ushort IRQ1Vector = 0xFFF8;
		private const ushort IRQ2Vector = 0xFFF6;

		private const byte IRQ2Selector = 0x01;
		private const byte IRQ1Selector = 0x02;
		private const byte TimerSelector = 0x04;

		public void WriteIrqControl(byte value)
		{
			// There is a single-instruction delay before writes to the IRQ Control Byte take effect.
			value &= 7;
			IRQNextControlByte = value;
		}

		public void WriteIrqStatus()
		{
			TimerAssert = false;
		}

		public byte ReadIrqStatus()
		{
			byte status = 0;
			if (IRQ2Assert) status |= 1;
			if (IRQ1Assert) status |= 2;
			if (TimerAssert) status |= 4;
			return status;
		}

		public void WriteTimer(byte value)
		{
			value &= 0x7F;
			TimerReloadValue = value;
		}

		public void WriteTimerEnable(byte value)
		{
			if (TimerEnabled == false && (value & 1) == 1)
			{
				TimerValue = TimerReloadValue; // timer value is reset when toggled from off to on
				TimerTickCounter = 0;
			}
			TimerEnabled = (value & 1) == 1;
		}

		public byte ReadTimerValue()
		{
			if (TimerTickCounter + 5 > 1024)
			{
				// There exists a slight delay between when the timer counter is decremented and when 
				// the interrupt fires; games can detect it, so we hack it this way.
				return (byte)((TimerValue - 1) & 0x7F);
			}
			return TimerValue;
		}

		// ==== Flags ====

		/// <summary>Carry Flag</summary>
		private bool FlagC
		{
			get => (P & 0x01) != 0;
			set => P = (byte)((P & ~0x01) | (value ? 0x01 : 0x00));
		}

		/// <summary>Zero Flag</summary>
		private bool FlagZ
		{
			get => (P & 0x02) != 0;
			set => P = (byte)((P & ~0x02) | (value ? 0x02 : 0x00));
		}

		/// <summary>Interrupt Disable Flag</summary>
		private bool FlagI
		{
			get => (P & 0x04) != 0;
			set => P = (byte)((P & ~0x04) | (value ? 0x04 : 0x00));
		}

		/// <summary>Decimal Mode Flag</summary>
		private bool FlagD
		{
			get => (P & 0x08) != 0;
			set => P = (byte)((P & ~0x08) | (value ? 0x08 : 0x00));
		}

		/// <summary>Break Flag</summary>
		private bool FlagB
		{
			get => (P & 0x10) != 0;
			set => P = (byte)((P & ~0x10) | (value ? 0x10 : 0x00));
		}

		/// <summary>T... Flag</summary>
		private bool FlagT
		{
			get => (P & 0x20) != 0;
			set => P = (byte)((P & ~0x20) | (value ? 0x20 : 0x00));
		}

		/// <summary>Overflow Flag</summary>
		private bool FlagV
		{
			get => (P & 0x40) != 0;
			set => P = (byte)((P & ~0x40) | (value ? 0x40 : 0x00));
		}

		/// <summary>Negative Flag</summary>
		private bool FlagN
		{
			get => (P & 0x80) != 0;
			set => P = (byte)((P & ~0x80) | (value ? 0x80 : 0x00));
		}

		// ==== Memory ====

		public Func<int, byte> ReadMemory21;
		public Action<int, byte> WriteMemory21;
		public Action<int, byte> WriteVDC;
		public Action<int> ThinkAction = delegate { };

		public IMemoryCallbackSystem MemoryCallbacks;

		public byte ReadMemory(ushort address)
		{
			byte page = MPR[address >> 13];
			var result = ReadMemory21((page << 13) | (address & 0x1FFF));
			uint flags = (uint)(MemoryCallbackFlags.AccessRead);
			MemoryCallbacks.CallMemoryCallbacks(address, result, flags, "System Bus");
			return result;
		}

		public void WriteMemory(ushort address, byte value)
		{
			byte page = MPR[address >> 13];
			WriteMemory21((page << 13) | (address & 0x1FFF), value);
			uint flags = (uint)(MemoryCallbackFlags.AccessWrite);
			MemoryCallbacks.CallMemoryCallbacks(address, value, flags, "System Bus");
		}

		private ushort ReadWord(ushort address)
		{
			byte l = ReadMemory(address);
			byte h = ReadMemory(++address);
			return (ushort)((h << 8) | l);
		}

		private void WriteWord(ushort address, ushort value)
		{
			byte l = (byte)(value & 0xFF);
			byte h = (byte)(value >> 8);
			WriteMemory(address, l);
			WriteMemory(++address, h);
		}

		private ushort ReadWordPageWrap(ushort address)
		{
			ushort highAddress = (ushort)((address & 0xFF00) + ((address + 1) & 0xFF));
			return (ushort)(ReadMemory(address) | (ReadMemory(highAddress) << 8));
		}

		public string TraceHeader => "HuC6280: PC, machine code, mnemonic, operands, registers (A, X, Y, P, SP, Cy), flags (NVTBDIZC)";

		public TraceInfo State()
		{
			int notused;

			return new TraceInfo
			{
				Disassembly = $"{MPR[PC >> 13]:X2}:{PC:X4}:  {ReadMemory(PC):X2}  {Disassemble(PC, out notused)} ".PadRight(30),
				RegisterInfo = string.Join(" ",
					$"A:{A:X2}",
					$"X:{X:X2}",
					$"Y:{Y:X2}",
					$"P:{P:X2}",
					$"SP:{S:X2}",
					$"Cy:{TotalExecutedCycles}",
					string.Concat(
						FlagN ? "N" : "n",
						FlagV ? "V" : "v",
						FlagT ? "T" : "t",
						FlagB ? "B" : "b",
						FlagD ? "D" : "d",
						FlagI ? "I" : "i",
						FlagZ ? "Z" : "z",
						FlagC ? "C" : "c"))
			};
		}

		private static readonly byte[] TableNZ = 
		{ 
			0x02, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
			0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80,
			0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80,
			0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80,
			0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80,
			0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80,
			0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80,
			0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80,
			0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80, 0x80
		};
	}
}