using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Consoles.SuperVision
{
	public partial class ASIC
	{
		public const int R_LCD_X_SIZE = 0x00;
		public const int R_LCD_Y_SIZE = 0x01;
		public const int R_X_SCROLL = 0x02;
		public const int R_Y_SCROLL = 0x03;
		public const int R_LCD_X_Size2 = 0x04;
		public const int R_LCD_Y_Size2 = 0x05;
		public const int R_X_Scroll2 = 0x06;
		public const int R_Y_SCROLL2 = 0x07;
		public const int R_DMA_SOURCE_LOW = 0x08;
		public const int R_DMA_SOURCE_HIGH = 0x09;
		public const int R_DMA_DEST_LOW = 0x0A;
		public const int R_DMA_DEST_HIGH = 0x0B;
		public const int R_DMA_LENGTH = 0x0C;
		public const int R_DMA_CONTROL = 0x0D;
		public const int R_CH1_F_LOW = 0x10;
		public const int R_CH1_F_HI = 0x11;
		public const int R_CH1_VOL_DUTY = 0x12;
		public const int R_CH1_LENGTH = 0x13;
		public const int R_CH2_F_LOW = 0x14;
		public const int R_CH2_F_HI = 0x15;
		public const int R_CH2_VOL_DUTY = 0x16;
		public const int R_CH2_LENGTH = 0x17;
		public const int R_CH3_ADDR_LOW = 0x18;
		public const int R_CH3_ADDR_HIGH = 0x19;
		public const int R_CH3_LENGTH = 0x1A;
		public const int R_CH3_CONTROL = 0x1B;
		public const int R_CH3_TRIGGER = 0x1C;
		public const int R_CH4_FREQ_VOL = 0x28;
		public const int R_CH4_LENGTH = 0x29;
		public const int R_CH4_CONTROL = 0x2A;
		public const int R_IRQ_TIMER = 0x23;
		public const int R_RESET_IRQ_TIMER = 0x24;
		public const int R_RESET_SOUND_DMA_IRQ = 0x25;
		public const int R_SYSTEM_CONTROL = 0x26;
		public const int R_IRQ_STATUS = 0x27;

		private SuperVision _sv;

		private byte[] _regs = new byte[0x30];

		public ASIC(SuperVision sv, SuperVision.SuperVisionSyncSettings ss)
		{
			_sv = sv;
			_screen = new LCD(ss.ScreenType);
		}

		public bool FrameStart;
		private int _intTimer;
		private int _nmiTimer;
		private bool _intFlag;
		private bool _dmaInProgress;
		private int _dmaCounter;

		/// <summary>
		/// ASIC is clocked at the same rate as the CPU
		/// </summary>
		public void Clock()
		{
			CheckInterrupt();
			CheckDMA();
			VideoClock();
			AudioClock();

			if (FrameStart)
				FrameStart = false;
		}

		private int IntPrescaler => _regs[R_SYSTEM_CONTROL].Bit(4) ? 16384 : 256;

		/// <summary>
		/// Interrupt management
		/// </summary>
		private void CheckInterrupt()
		{
			_nmiTimer++;

			// The NMI occurs every 65536 clock cycles (61.04Hz) regardless of the rate that the LCD refreshes
			if (_nmiTimer == 0x10000 && _regs[R_SYSTEM_CONTROL].Bit(0))
			{
				_nmiTimer = 0;
				_sv._cpu.NMI = true;
			}

			_intTimer--;

			if (_intTimer <= 0)
			{

			}
		}

		/// <summary>
		/// DMA Control
		/// </summary>
		private void CheckDMA()
		{
			if (_regs[R_DMA_CONTROL].Bit(7))
			{
				// DMA start requested
				_dmaInProgress = true;
			}

			if (_dmaInProgress)
			{
				ushort source = (ushort) (_regs[R_DMA_SOURCE_HIGH] << 8 | _regs[R_DMA_SOURCE_LOW]);
				ushort dest = (ushort) (_regs[R_DMA_DEST_HIGH] << 8 | _regs[R_DMA_DEST_LOW]);

				_dmaCounter++;

				if (_dmaCounter == 4096)
				{
					// wrap around
					_dmaCounter = 0;
				}
			}
			
		}

		/// <summary>
		/// CPU writes a byte of data to the port latched into the ASIC address pins
		/// </summary>
		public void WritePort(ushort address, byte value)
		{
			int regIndex = address - 0x2000;

			switch (regIndex)
			{
				// LCD_X_Size
				case 0x00:
				case 0x04:
					break;

				// LCD_Y_Size
				case 0x01:
				case 0x05:
					break;

				// X_Scroll
				case 0x02:
				case 0x06:
					break;

				// Y_Scroll
				case 0x03:
				case 0x07:
					break;

				// DMA Source low
				case 0x08:
					break;

				// DMA Source high
				case 0x09:
					break;

				// DMA Destination low
				case 0x0A:
					break;

				// DMA Destination high
				case 0x0B:
					break;

				// DMA Length
				case 0x0C:
					break;

				// DMA Control
				case 0x0D:
					break;

				// CH1_Flow (right only)
				case 0x10:
					break;

				// CH1_Fhi
				case 0x11:
					break;

				// CH1_Vol_Duty
				case 0x12:
					break;

				// CH1_Length
				case 0x13:
					break;

				// CH2_Flow (right only)
				case 0x14:
					break;

				// CH2_Fhi
				case 0x15:
					break;

				// CH2_Vol_Duty
				case 0x16:
					break;

				// CH2_Length
				case 0x17:
					break;

				// CH3_Addrlow
				case 0x18:
					break;

				// CH3_Addrhi
				case 0x19:
					break;

				// CH3_Length
				case 0x1A:
					break;

				// CH3_Control
				case 0x1B:
					break;

				// CH3_Trigger
				case 0x1C:
					break;				

				// Link port DDR
				case 0x21:
					break;

				// Link port data
				case 0x22:
					break;

				// IRQ timer
				case 0x23:

					// When a value is written to this register, the timer will start decrementing until it is 00h
					// then it will stay at 00h. When the timer expires, it sets a flag which triggers an IRQ
					// This timer is clocked by a prescaler, which is reset when the timer is written to.
					// This prescaler can divide the system clock by 256 or 16384.
					// 8bits

					// reset prescaler?
					_regs[R_SYSTEM_CONTROL] = (byte)(_regs[R_SYSTEM_CONTROL] & ~(1 << 4)); // Reset bit 4

					_intTimer = value * IntPrescaler;

					// Writing 00h to the IRQ Timer register results in an instant IRQ. It does not wrap to FFh and continue counting;  it just stays at 00h and fires off an IRQ.

					break;

				// Reset IRQ timer flag
				case 0x24:
					break;

				// Reset Sound DMA IRQ flag
				case 0x25:
					break;

				// System Control
				case 0x26:

					// Bit 0:	Enable NMI (1 == enable)
					// Bit 1:	Enable IRQ (1 == enable)
					// Bit 2:	??
					// Bit 3:	Display Enable (1 == enable, 0 == disable)
					// Bit 4:	IRQ Timer Prescaler (1 == divide by 16384, 0 == divide by 256)
					// Bit 5-8:	Bank select bits for 0x8000 -> 0xBFFF
					_regs[regIndex] = value;

					// lcd displayenable
					_screen.DisplayEnable = value.Bit(3);

					// banking
					_sv.BankSelect = value >> 5;

					// writing to this register resets the LCD rendering system and makes it start rendering from the upper left corner, regardless of the bit pattern.
					_screen.ResetPosition();

					break;

				// IRQ status
				case 0x27:
					break;

				// CH4_Freq_Vol (left and right)
				case 0x28:
				case 0x2C:
					break;

				// CH4_Length
				case 0x29:
				case 0x2D:
					break;

				// CH4_Control
				case 0x2A:
				case 0x2E:
					break;


				// READONLY				
				case 0x20:		// Controller
					break;

				// UNKNOWN
				case 0x0E:
				case 0x0F:
				case 0x1D:
				case 0x1E:
				case 0x1F:
				case 0x2B:
					break;
			}
		}

		/// <summary>
		/// CPU reads a byte of data from the address latched into the ASIC address pins
		/// </summary>
		public byte ReadPort(ushort address)
		{
			byte result = 0xFF;
			int regIndex = address - 0x2000;

			switch (regIndex)
			{
				// LCD_X_Size
				case 0x00:
				case 0x04:
					break;

				// LCD_Y_Size
				case 0x01:
				case 0x05:
					break;

				// X_Scroll
				case 0x02:
				case 0x06:
					break;

				// Y_Scroll
				case 0x03:
				case 0x07:
					break;

				// DMA Source low
				case 0x08:
					break;

				// DMA Source high
				case 0x09:
					break;

				// DMA Destination low
				case 0x0A:
					break;

				// DMA Destination high
				case 0x0B:
					break;

				// DMA Length
				case 0x0C:
					break;

				// DMA Control
				case 0x0D:
					break;

				// CH1_Flow (right only)
				case 0x10:
					break;

				// CH1_Fhi
				case 0x11:
					break;

				// CH1_Vol_Duty
				case 0x12:
					break;

				// CH1_Length
				case 0x13:
					break;

				// CH2_Flow (right only)
				case 0x14:
					break;

				// CH2_Fhi
				case 0x15:
					break;

				// CH2_Vol_Duty
				case 0x16:
					break;

				// CH2_Length
				case 0x17:
					break;

				// CH3_Addrlow
				case 0x18:
				break;

				// CH3_Addrhi
				case 0x19:
					break;

				// CH3_Length
				case 0x1A:
					break;

				// CH3_Control
				case 0x1B:
					break;

				// CH3_Trigger
				case 0x1C:
					break;

				// Controller
				case 0x20:
					result = _sv.ReadControllerByte();
					break;

				// Link port DDR
				case 0x21:
					break;

				// Link port data
				case 0x22:
					break;

				// IRQ timer
				case 0x23:
					break;

				// Reset IRQ timer flag
				case 0x24:
					break;

				// Reset Sound DMA IRQ flag
				case 0x25:
					break;

				// System Control
				case 0x26:
					result = _regs[regIndex];
					break;

				// IRQ status
				case 0x27:
					break;

				// CH4_Freq_Vol (left and right)
				case 0x28:
				case 0x2C:
					break;

				// CH4_Length
				case 0x29:
				case 0x2D:
					break;

				// CH4_Control
				case 0x2A:
				case 0x2E:
					break;


				// UNKNOWN
				case 0x0E:
				case 0x0F:
				case 0x1D:
				case 0x1E:
				case 0x1F:
				case 0x2B:
					break;

			}

			return result;
		}

		public void Reset()
		{
			// Reset the ASIC
		}

		public virtual void SyncState(Serializer ser)
		{
			ser.BeginSection("ASIC");
			ser.Sync(nameof(_regs), ref _regs, false);
			ser.Sync(nameof(FrameStart), ref FrameStart);
			ser.Sync(nameof(_intTimer), ref _intTimer);
			ser.Sync(nameof(_nmiTimer), ref _nmiTimer);
			ser.Sync(nameof(_intFlag), ref _intFlag);
			ser.Sync(nameof(_dmaInProgress), ref _dmaInProgress);
			ser.Sync(nameof(_dmaCounter), ref _dmaCounter);

			ser.Sync(nameof(_field), ref _field);
			ser.Sync(nameof(_latchedVRAM), ref _latchedVRAM);
			ser.Sync(nameof(_currentVRAMPointer), ref _currentVRAMPointer);
			ser.Sync(nameof(_currY), ref _currY);
			ser.Sync(nameof(_currX), ref _currX);

			_screen.SyncState(ser);

			ser.EndSection();
		}
	}
}
