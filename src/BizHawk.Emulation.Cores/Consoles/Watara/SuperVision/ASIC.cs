using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Consoles.SuperVision
{
	public partial class ASIC
	{
		// register constants
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

		/// <summary>
		/// Scanline length in cpu clocks
		/// </summary>
		public int CLOCK_WIDTH => 
			((_regs[R_LCD_X_SIZE] & 0xFC)	// topmost 6 bits of the X Size register
			+ 4)                            // line latch pulse
			* 6;                            // 6 clocks per pixel

		/// <summary>
		/// Y offset modifier used with Y_Scroll to determine the VRAM pointer
		/// </summary>
		public int Y_OFFSET => _regs[R_LCD_X_SIZE] > 0xC0 ? 0x30 : 0x60;

		/// <summary>
		/// Number of scanlines in a field
		/// </summary>
		public int LINE_HEIGHT => _regs[R_LCD_Y_SIZE];

		private SuperVision _sv;
		private byte[] _regs = new byte[0x2000];

		/// <summary>
		/// The inbuilt LCD screen
		/// </summary>
		public LCD Screen;

		public ASIC(SuperVision sv, SuperVision.SuperVisionSyncSettings ss)
		{
			_sv = sv;
			Screen = new LCD(ss.ScreenType);
		}

		public bool FrameStart;
		private int _intTimer;
		private bool _intTimerEnabled;
		private bool _intTimerChanged;
		private int _nmiTimer;
		private bool _intFlag;
		private bool _dmaInProgress;
		private int _dmaCounter;
		private int _seqCounter;
		private int _byteCounter;
		private int _lineCounter;
		private int _field;
		private ushort _vramByteBuffer;
		private int _vramPointer;
		private int _vramStartAddress;

		/// <summary>
		/// ASIC is clocked at the same rate as the CPU
		/// </summary>
		public void Clock()
		{
			// According to the information presented in https://github.com/GrenderG/supervision_reveng_notes/blob/master/Supervision_Tech.txt
			// it can be surmised that the ASIC rigidly sticks to a 6-phase sequencer, which is as follows:
			// 0: CPU RDY line true / PixelCLK to LCD / Output 1/2 byte to LCD
			// 1: DMA byte transfer to VRAM (if DMA is active) / CPU RDY line false (if DMA is active)
			// 2: DMA byte transfer to VRAM (if DMA is active) / CPU RDY line false (if DMA is active)
			// 3: DMA byte transfer to VRAM (if DMA is active) / CPU RDY line false (if DMA is active)
			// 4: DMA byte transfer to VRAM (if DMA is active) / CPU RDY line false (if DMA is active)
			// 5: DMA byte transfer to VRAM (if DMA is active) / CPU RDY line false (if DMA is active)

			CheckDMA();

			// so DMA can transfer 5 bytes to VRAM every 6 clocks, the 6th clock being the 1/2 byte transfer to the LCD
			switch (_seqCounter)
			{
				case 0:					
					// there is no DMA on this cycle so CPU can run freely
					_sv._cpu.RDY = true;

					bool lineEnd = _byteCounter == CLOCK_WIDTH - 1;
					bool fieldEnd = _lineCounter == LINE_HEIGHT - 1 && lineEnd && _field == 0;
					bool frameEnd = _lineCounter == LINE_HEIGHT - 1 && lineEnd && _field == 1;

					// vram pointer
					if (fieldEnd)
					{
						// Y_Scroll offset added to the VRAM pointer at the start of the field (could be frame)
						_vramStartAddress = (_regs[R_Y_SCROLL] * Y_OFFSET) & 0x1FFF;

						if (_vramStartAddress == 0x1FE0)
							_vramStartAddress = 0;
					}

					if (_byteCounter == 0)
					{
						// new scanline
						_vramPointer = _vramStartAddress + (_regs[R_X_SCROLL] >> 2);
					}

					// ASIC reads a byte from VRAM					
					byte data = _sv.ReadVRAM((ushort) _vramPointer);
					_vramPointer++;

					// shift the last read byte in the buffer and add the new byte to the start
					_vramByteBuffer = (ushort) ((_vramByteBuffer << 8) | data);

					// get the correct byte data based on the X Scroll register lower 2 bits
					// this simulates a delay in the bits sent to the LCD
					byte b = (byte) ((_vramByteBuffer >> (_regs[R_X_SCROLL] & 0b0000_0011)) & 0xff);

					// depending on the field, a 4 bit sequence is sent to the LCD
					// Field0:	bits 0-2-4-6
					// Field1:	bits 1-3-5-7
					byte lData = _field == 0
						? (byte) ((b & 0b0000_0001) | ((b & 0b0000_0100) >> 1) | ((b & 0b0001_0000) >> 2) | ((b & 0b0100_0000) >> 3))
						: (byte) ((b & 0b0000_0010) >> 1 | ((b & 0b0000_1000) >> 2) | ((b & 0b0010_0000) >> 3) | ((b & 0b1000_0000) >> 4));					

					// send 1/2 byte to the LCD
					Screen.PixelClock(lData, _field, lineEnd, frameEnd);

					_byteCounter++;

					if (_byteCounter == CLOCK_WIDTH)
					{
						// end of scanline
						_byteCounter = 0;
						_lineCounter++;

						// setup start address
						_vramStartAddress += Y_OFFSET & 0x1FFF;
						if (_vramStartAddress == 0x1FE0)
							_vramStartAddress = 0;

						if (_lineCounter == LINE_HEIGHT)
						{
							// end of field
							_lineCounter = 0;
							_field++;

							if (_field == 2)
								_field = 0; // wraparound
						}
					}
					break;

				default:

					_sv._cpu.RDY = !_dmaInProgress;

					if (_dmaInProgress)
					{
						// perform DMA transfer
						DoDMA();
					}
					break;
			}

			_seqCounter++;			

			if (_seqCounter == 7)
				_seqCounter = 0;    // wraparound

			CheckInterrupt();
			AudioClock();

			if (FrameStart)
				FrameStart = false;

			_sv.FrameClock++;
		}

		/// <summary>
		/// The current prescaler value for the IRQ timer
		/// </summary>
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
				//_sv._cpu.NMI = true;
				_sv._cpu.SetNMI();
			}

			if (_intTimerChanged)
			{
				// IRQ timer register has just been modified
				_intTimerChanged = false;
				_intTimerEnabled = true;

				// prescaler reset
				_intTimer = 0;
			}

			if (_intTimerEnabled)
			{
				if (_regs[R_IRQ_TIMER] == 0)
				{
					if (_regs[R_SYSTEM_CONTROL].Bit(1))
					{
						// raise IRQ
						// this handles IRQ after timer countdown AND instant IRQ when timer is set to 0
						_intFlag = true;
						_intTimerEnabled = false;

						// set IRQ Timer expired bit
						_regs[R_IRQ_STATUS] = (byte) (_regs[R_IRQ_STATUS] | 2);
					}
				}
				else
				{
					// timer should be counting down clocked by the prescaler
					if (_intTimer++ == IntPrescaler)
					{
						// prescaler clock
						_intTimer = 0;

						// decrement timer
						_regs[R_IRQ_TIMER]--;
					}
				}
			}

			if (_intFlag && _regs[R_SYSTEM_CONTROL].Bit(1))
			{
				// fire IRQ		
				_sv._cpu.SetIRQ();	
				//_sv._cpu.IRQ = true;
				_intFlag = false;
			}
		}

		/// <summary>
		/// Check whether DMA needs to start
		/// </summary>
		private void CheckDMA()
		{
			if (_regs[R_DMA_CONTROL].Bit(7))
			{
				// DMA start requested
				_dmaInProgress = true;

				// Unset the DMA start bit
				_regs[R_DMA_CONTROL] = (byte) (_regs[R_DMA_CONTROL] & ~(1 << 7));
			}
		}

		/// <summary>
		/// Perform a DMA transfer
		/// </summary>
		private void DoDMA()
		{
			if (_dmaInProgress)
			{
				_dmaCounter++;

				if (_dmaCounter == 4096 || _dmaCounter == _regs[R_DMA_LENGTH] * 16)
				{
					// wraparound or length reached
					_dmaCounter = 0;
					_dmaInProgress = false;
				}
				else
				{					
					ushort source = (ushort) (_regs[R_DMA_SOURCE_HIGH] << 8 | _regs[R_DMA_SOURCE_LOW]);
					ushort dest = (ushort) (_regs[R_DMA_DEST_HIGH] << 8 | _regs[R_DMA_DEST_LOW]);

					// transfer a byte from source to dest using DMA
					_sv.WriteMemory(dest, _sv.ReadMemory(source));

					// source registers incremented
					source++;
					_regs[R_DMA_SOURCE_HIGH] = (byte) (source >> 8);
					_regs[R_DMA_SOURCE_LOW] = (byte) source;

					// destination registers incremeneted
					dest++;
					_regs[R_DMA_DEST_HIGH] = (byte) (dest >> 8);
					_regs[R_DMA_DEST_LOW] = (byte) dest;
				}
			}
		}

		/// <summary>
		/// CPU writes a byte of data to the port latched into the ASIC address pins
		/// </summary>
		public void WritePort(ushort address, byte value)
		{
			int regIndex = address - 0x2000;

			// mirror reg handling
			if (regIndex is
				0x04 or     // LCD_X_Size (mirror)
				0x05 or     // LCD_Y_Size (mirror)
				0x06 or     // X_Scroll (mirror)
				0x07 or     // Y_Scroll (mirror)
				0x2C or     // CH4_Freq_Vol (mirror)
				0x2D or     // CH4_Length (mirror)
				0x2E)       // CH4_Control (mirror)
				regIndex -= 4;

			switch (regIndex)
			{
				
				case 0x00:  // LCD_X_Size	-	Only the upper 6 bits of LCD_X_Size are usable. The lower 2 bits are ignored (the LCD size can only be changed in 4 pixel increments)
				case 0x01:  // LCD_Y_Size	-	LCD_Y_Size controls how many scanlines are shown in the field. After the requisite number of scanlines, the LCD frame latch signal is output and the frame polarity line is toggled
				case 0x02:  // X_Scroll
				case 0x03:  // Y_Scroll

				case 0x08:  // DMA Source low
				case 0x09:  // DMA Source high
				case 0x0A:  // DMA Destination low
				case 0x0B:  // DMA Destination high
				case 0x0C:  // DMA Length	-	This register selects how many bytes of data to move.  The actual number of bytes to move is (L * 16).  If the register is loaded with 0, a full 4096 bytes is moved.
				case 0x0D:  // DMA Control	-	Start DMA when written with bit7 set

				case 0x10:  // CH1_Flow (right only)
				case 0x11:  // CH1_Fhi
				case 0x12:  // CH1_Vol_Duty
				case 0x13:  // CH1_Length

				case 0x14:  // CH2_Flow (right only)
				case 0x15:  // CH2_Fhi
				case 0x16:  // CH2_Vol_Duty
				case 0x17:  // CH2_Length

				case 0x18:  // CH3_Addrlow
				case 0x19:  // CH3_Addrhi
				case 0x1A:  // CH3_Length
				case 0x1B:  // CH3_Control
				case 0x1C:  // CH3_Trigger

				case 0x28:  // CH4_Freq_Vol (left and right)
				case 0x29:  // CH4_Length
				case 0x2A:  // CH4_Control

				case 0x21:  // Link port DDR
				case 0x22:  // Link port data

					_regs[regIndex] = value;

					break;				

				// IRQ timer
				case 0x23:

					// When a value is written to this register, the timer will start decrementing until it is 00h
					// then it will stay at 00h. When the timer expires, it sets a flag which triggers an IRQ
					// This timer is clocked by a prescaler, which is reset when the timer is written to.
					// This prescaler can divide the system clock by 256 or 16384.
					// 8bits
					_regs[R_IRQ_TIMER] = value;

					_intTimerChanged = true;

					// Writing 00h to the IRQ Timer register results in an instant IRQ. It does not wrap to FFh and continue counting;  it just stays at 00h and fires off an IRQ.

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
					Screen.DisplayEnable = value.Bit(3);

					// banking
					var bank = value >> 5;
					_sv.BankSelect = (value >> 5);

					// writing to this register resets the LCD rendering system and makes it start rendering from the upper left corner, regardless of the bit pattern.
					Screen.ResetPosition();

					break;				

				// READONLY				
				case 0x20:      // Controller								
				case 0x27:      // IRQ status
				case 0x24:      // Reset IRQ timer flag
				case 0x25:      // Reset Sound DMA IRQ flag
					break;

				// UNKNOWN
				case 0x0E:
				case 0x0F:
				case 0x1D:
				case 0x1E:
				case 0x1F:
				case 0x2B:
					_regs[regIndex] = value;
					break;

				default:
					_regs[regIndex] = value;
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

			// mirror reg handling
			if (regIndex is 
				0x04 or     // LCD_X_Size (mirror)
				0x05 or     // LCD_Y_Size (mirror)
				0x06 or     // X_Scroll (mirror)
				0x07 or     // Y_Scroll (mirror)
				0x2C or     // CH4_Freq_Vol (mirror)
				0x2D or     // CH4_Length (mirror)
				0x2E)       // CH4_Control (mirror)
				regIndex -= 4;

			switch (regIndex)
			{
				case 0x00:  // LCD_X_Size
				case 0x01:  // LCD_Y_Size
				case 0x02:  // X_Scroll
				case 0x03:  // Y_Scroll

				case 0x08:  // DMA Source low
				case 0x09:  // DMA Source high
				case 0x0A:  // DMA Destination low
				case 0x0B:  // DMA Destination high
				case 0x0C:  // DMA Length
				case 0x0D:  // DMA Control

				case 0x10:  // CH1_Flow (right only)
				case 0x11:  // CH1_Fhi
				case 0x12:  // CH1_Vol_Duty
				case 0x13:  // CH1_Length

				case 0x14:  // CH2_Flow (right only)
				case 0x15:  // CH2_Fhi
				case 0x16:  // CH2_Vol_Duty
				case 0x17:  // CH2_Length

				case 0x18:  // CH3_Addrlow
				case 0x19:  // CH3_Addrhi
				case 0x1A:  // CH3_Length
				case 0x1B:  // CH3_Control
				case 0x1C:  // CH3_Trigger

				case 0x28:  // CH4_Freq_Vol (left and right)
				case 0x29:  // CH4_Length
				case 0x2A:  // CH4_Control

				case 0x21:  // Link port DDR
				case 0x22:  // Link port data

				case 0x23:  // IRQ timer
				case 0x26:  // System Control

					result = _regs[regIndex];				
				
				break;

				// Controller
				case 0x20:
					result = _sv.ReadControllerByte();
					break;

				// Reset IRQ timer flag
				case 0x24:
					// When this register is read, it resets the timer IRQ flag (clears the status reg bit too)
					break;

				// Reset Sound DMA IRQ flag
				case 0x25:
					//When this register is read, it resets the audio DMA IRQ flag (clears status reg bit too)
					break;

				// IRQ status
				case 0x27:

					// bit0:	DMA Audio System (1 == DMA audio finished)
					// bit1:	IRQ Timer expired (1 == expired)
					result = _regs[regIndex];

					break;

				// UNKNOWN
				case 0x0E:
				case 0x0F:
				case 0x1D:
				case 0x1E:
				case 0x1F:
				case 0x2B:					
					result = _regs[regIndex];
					break;

				default:
					result = _regs[regIndex];
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
			ser.Sync(nameof(_intTimerEnabled), ref _intTimerEnabled);
			ser.Sync(nameof(_intTimerChanged), ref _intTimerChanged);
			ser.Sync(nameof(_nmiTimer), ref _nmiTimer);
			ser.Sync(nameof(_intFlag), ref _intFlag);
			ser.Sync(nameof(_dmaInProgress), ref _dmaInProgress);
			ser.Sync(nameof(_dmaCounter), ref _dmaCounter);
			ser.Sync(nameof(_seqCounter), ref _seqCounter);
			ser.Sync(nameof(_byteCounter), ref _byteCounter);
			ser.Sync(nameof(_lineCounter), ref _lineCounter);
			ser.Sync(nameof(_field), ref _field);
			ser.Sync(nameof(_vramByteBuffer), ref _vramByteBuffer);
			ser.Sync(nameof(_vramPointer), ref _vramPointer);
			ser.Sync(nameof(_vramStartAddress), ref _vramStartAddress);
			Screen.SyncState(ser);

			ser.EndSection();
		}
	}
}
