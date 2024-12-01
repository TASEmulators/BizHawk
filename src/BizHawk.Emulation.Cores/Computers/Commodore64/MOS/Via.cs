using BizHawk.Common;

namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
	public sealed partial class Via
	{
		private const int PCR_INT_CONTROL_NEGATIVE_EDGE = 0x00;
		private const int PCR_INT_CONTROL_POSITIVE_EDGE = 0x01;
		private const int PCR_CONTROL_INPUT_NEGATIVE_ACTIVE_EDGE = 0x00;
		private const int PCR_CONTROL_INDEPENDENT_INTERRUPT_INPUT_NEGATIVE_EDGE = 0x02;
		private const int PCR_CONTROL_INPUT_POSITIVE_ACTIVE_EDGE = 0x04;
		private const int PCR_CONTROL_INDEPENDENT_INTERRUPT_INPUT_POSITIVE_EDGE = 0x06;
		private const int PCR_CONTROL_HANDSHAKE_OUTPUT = 0x08;
		private const int PCR_CONTROL_PULSE_OUTPUT = 0x0A;
		private const int PCR_CONTROL_LOW_OUTPUT = 0x0C;
		private const int PCR_CONTROL_HIGH_OUTPUT = 0x0E;
		private const int ACR_SR_CONTROL_DISABLED = 0x00;
		private const int ACR_SR_CONTROL_SHIFT_IN_T2_ONCE = 0x04;
		private const int ACR_SR_CONTROL_SHIFT_IN_PHI2 = 0x08;
		private const int ACR_SR_CONTROL_SHIFT_IN_CLOCK = 0x0C;
		private const int ACR_SR_CONTROL_SHIFT_OUT_T2 = 0x10;
		private const int ACR_SR_CONTROL_SHIFT_OUT_T2_ONCE = 0x14;
		private const int ACR_SR_CONTROL_SHIFT_OUT_PHI2 = 0x18;
		private const int ACR_SR_CONTROL_SHIFT_OUT_CLOCK = 0x1C;
		private const int ACR_T2_CONTROL_TIMED = 0x00;
		private const int ACR_T2_CONTROL_COUNT_ON_PB6 = 0x20;
		private const int ACR_T1_CONTROL_INTERRUPT_ON_LOAD = 0x00;
		private const int ACR_T1_CONTROL_CONTINUOUS_INTERRUPTS = 0x40;
		private const int ACR_T1_CONTROL_INTERRUPT_ON_LOAD_AND_ONESHOT_PB7 = 0x80;
		private const int ACR_T1_CONTROL_CONTINUOUS_INTERRUPTS_AND_OUTPUT_ON_PB7 = 0xC0;
		private const int ACR_T1_CONTROL_INTERRUPT_ON_LOAD_AND_PULSE_PB7 = 0x80;

		private int _pra;
		private int _ddra;
		private int _prb;
		private int _ddrb;
		private int _t1C;
		private int _t1L;
		private int _t2C;
		private int _t2L;
		private int _sr;
		private int _acr;
		private int _pcr;
		private int _ifr;
		private int _ier;
		private readonly IPort _port;

		private int _paLatch;
		private int _pbLatch;

		private int _pcrCa1IntControl;
		private int _pcrCa2Control;
		private int _pcrCb1IntControl;
		private int _pcrCb2Control;
		private bool _acrPaLatchEnable;
		private bool _acrPbLatchEnable;
		private int _acrSrControl;
		private int _acrT1Control;
		private int _acrT2Control;
		private int _srCount;

		private bool _ca1L;
		private bool _ca2L;
		private bool _cb1L;
		private bool _cb2L;
		private bool _pb6L;

		private bool _resetCa2NextClock;
		private bool _resetCb2NextClock;
		private bool _resetPb7NextClock;
		private bool _setPb7NextClock;

		private bool _handshakeCa2NextClock;
		private bool _handshakeCb2NextClock;

		public bool Ca1;
		public bool Ca2;
		public bool Cb1;
		public bool Cb2;
		private bool _pb6;

		private int _interruptNextClock;
		private bool _t1CLoaded;
		private bool _t2CLoaded;
		private int _t1Delayed;
		private int _t2Delayed;

		public Via()
		{
			_port = new DisconnectedPort();
		}

		public Via(Func<int> readPrA, Func<int> readPrB)
		{
			_port = new DriverPort(readPrA, readPrB);
		}

		public Via(Func<bool> readClock, Func<bool> readData, Func<bool> readAtn, int driveNumber)
		{
			_port = new IecPort(readClock, readData, readAtn, driveNumber);
			_ca1L = true;
		}

		public bool Irq => (_ifr & 0x80) == 0;

		public void HardReset()
		{
			_pra = 0;
			_prb = 0;
			_ddra = 0;
			_ddrb = 0;
			_t1C = 0xFFFF;
			_t1L = 0xFFFF;
			_t2C = 0xFFFF;
			_t2L = 0xFFFF;
			_sr = 0xFF;
			_acr = 0;
			_pcr = 0;
			_ifr = 0;
			_ier = 0;
			_paLatch = 0;
			_pbLatch = 0;
			_pcrCa1IntControl = 0;
			_pcrCa2Control = 0;
			_pcrCb1IntControl = 0;
			_pcrCb2Control = 0;
			_acrPaLatchEnable = false;
			_acrPbLatchEnable = false;
			_acrSrControl = 0;
			_acrT1Control = 0;
			_acrT2Control = 0;
			_ca1L = true;
			_cb1L = true;
			Ca1 = true;
			Ca2 = true;
			Cb1 = true;
			Cb2 = true;
			_srCount = 0;

			_pb6L = true;
			_pb6 = true;
			_resetCa2NextClock = false;
			_resetCb2NextClock = false;
			_handshakeCa2NextClock = false;
			_handshakeCb2NextClock = false;
			_interruptNextClock = 0;
			_t1CLoaded = false;
			_t2CLoaded = false;
			_resetPb7NextClock = false;
			_setPb7NextClock = false;
		}

		public void ExecutePhase()
		{
			var shiftIn = false;

			// TODO: use this or delete
			////var shiftOut = false;

			// Process delayed interrupts
			_ifr |= _interruptNextClock;
			_interruptNextClock = 0;

			// Process 'pulse' and 'handshake' outputs on PB7, CA2 and CB2
			if (_resetCa2NextClock)
			{
				Ca2 = true;
				_resetCa2NextClock = false;
			}
			else if (_handshakeCa2NextClock)
			{
				Ca2 = false;
				_resetCa2NextClock = _pcrCa2Control == PCR_CONTROL_PULSE_OUTPUT;
				_handshakeCa2NextClock = false;
			}

			if (_resetCb2NextClock)
			{
				Cb2 = true;
				_resetCb2NextClock = false;
			}
			else if (_handshakeCb2NextClock)
			{
				Cb2 = false;
				_resetCb2NextClock = _pcrCb2Control == PCR_CONTROL_PULSE_OUTPUT;
				_handshakeCb2NextClock = false;
			}
			
			if (_resetPb7NextClock)
			{
				_prb &= 0x7F;
				_resetPb7NextClock = false;
			}
			else if (_setPb7NextClock)
			{
				_prb |= 0x80;
				_setPb7NextClock = false;
			}			

			// Count timers
			if (_t1Delayed > 0)
			{
				_t1Delayed--;
			}
			else
			{
				_t1C--;
				if (_t1C == 0)
				{
					switch (_acrT1Control)
					{
						case ACR_T1_CONTROL_CONTINUOUS_INTERRUPTS_AND_OUTPUT_ON_PB7:
							_prb ^= 0x80;
							break;
						case ACR_T1_CONTROL_INTERRUPT_ON_LOAD_AND_PULSE_PB7:
							_prb |= 0x80;
							break;
					}
				}
				else if (_t1C < 0)
				{
					if (_t1CLoaded)
					{
						_interruptNextClock |= 0x40;
						_t1CLoaded = false;
					}

					switch (_acrT1Control)
					{
						case ACR_T1_CONTROL_CONTINUOUS_INTERRUPTS:
						case ACR_T1_CONTROL_CONTINUOUS_INTERRUPTS_AND_OUTPUT_ON_PB7:
							_t1C = _t1L;
							_t1CLoaded = true;
							break;
					}

					_t1C &= 0xFFFF;
				}
			}

			if (_t2Delayed > 0)
			{
				_t2Delayed--;
			}
			else
			{
				switch (_acrT2Control)
				{
					case ACR_T2_CONTROL_TIMED:
						_t2C--;
						if (_t2C < 0)
						{
							if (_t2CLoaded)
							{
								_interruptNextClock |= 0x20;
								_t2CLoaded = false;
							}
							_t2C = _t2L;
						}
						break;
					case ACR_T2_CONTROL_COUNT_ON_PB6:
						_pb6L = _pb6;
						_pb6 = (_port.ReadExternalPrb() & 0x40) != 0;
						if (!_pb6 && _pb6L)
						{
							_t2C--;
							if (_t2C == 0)
								_ifr |= 0x20;
							_t2C &= 0xFFFF;
						}
						break;
				}
			}

			// Process CA2
			switch (_pcrCa2Control)
			{
				case PCR_CONTROL_INPUT_NEGATIVE_ACTIVE_EDGE:
				case PCR_CONTROL_INDEPENDENT_INTERRUPT_INPUT_NEGATIVE_EDGE:
					if (_ca2L && !Ca2)
						_ifr |= 0x01;
					break;
				case PCR_CONTROL_INPUT_POSITIVE_ACTIVE_EDGE:
				case PCR_CONTROL_INDEPENDENT_INTERRUPT_INPUT_POSITIVE_EDGE:
					if (!_ca2L && Ca2)
						_ifr |= 0x01;
					break;
				case PCR_CONTROL_HANDSHAKE_OUTPUT:
					if (_ca1L && !Ca1)
					{
						Ca2 = true;
						_ifr |= 0x01;
					}
					break;
				case PCR_CONTROL_PULSE_OUTPUT:
					break;
				case PCR_CONTROL_LOW_OUTPUT:
					Ca2 = false;
					break;
				case PCR_CONTROL_HIGH_OUTPUT:
					Ca2 = true;
					break;
			}

			// Process CB2
			switch (_pcrCb2Control)
			{
				case PCR_CONTROL_INPUT_NEGATIVE_ACTIVE_EDGE:
				case PCR_CONTROL_INDEPENDENT_INTERRUPT_INPUT_NEGATIVE_EDGE:
					if (_cb2L && !Cb2)
						_ifr |= 0x08;
					break;
				case PCR_CONTROL_INPUT_POSITIVE_ACTIVE_EDGE:
				case PCR_CONTROL_INDEPENDENT_INTERRUPT_INPUT_POSITIVE_EDGE:
					if (!_cb2L && Cb2)
						_ifr |= 0x08;
					break;
				case PCR_CONTROL_HANDSHAKE_OUTPUT:
					if (_cb1L && !Cb1)
					{
						Cb2 = true;
						_ifr |= 0x08;
					}
					break;
				case PCR_CONTROL_PULSE_OUTPUT:
					break;
				case PCR_CONTROL_LOW_OUTPUT:
					Cb2 = false;
					break;
				case PCR_CONTROL_HIGH_OUTPUT:
					Cb2 = true;
					break;
			}

			// interrupt generation

			if (_acrSrControl == ACR_SR_CONTROL_DISABLED)
			{
				_ifr &= 0xFB;
				_srCount = 0;
			}

			/*
				As long as the CA1 interrupt flag is set, the data on the peripheral pins can change
				without affecting the data in the latches. This input latching can be used with any of the CA2
				input or output modes.
				It is important to note that on the PA port, the processor always reads the data on the
				peripheral pins (as reflected in the latches). For output pins, the processor still reads the
				latches. This may or may not reflect the data currently in the ORA. Proper system operation
				requires careful planning on the part of the system designer if input latching is combined
				with output pins on the peripheral ports.
			*/

			if ((_pcrCa1IntControl is PCR_INT_CONTROL_POSITIVE_EDGE && Ca1 && !_ca1L)
				|| (_pcrCa1IntControl is PCR_INT_CONTROL_NEGATIVE_EDGE && !Ca1 && _ca1L))
			{
				if (_acrPaLatchEnable && (_ifr & 0x02) == 0)
					_paLatch = _port.ReadExternalPra();
				_ifr |= 0x02;
			}

			/*
                Input latching on the PB port is controlled in the same manner as that described for the PA port.
                However, with the peripheral B port the input latch will store either the voltage on the pin or the contents
                of the Output Register (ORB) depending on whether the pin is programmed to act as an input or an
                output. As with the PA port, the processor always reads the input latches.
            */

			if ((_pcrCb1IntControl is PCR_INT_CONTROL_POSITIVE_EDGE && Cb1 && !_cb1L)
				|| (_pcrCb1IntControl is PCR_INT_CONTROL_NEGATIVE_EDGE && !Cb1 && _cb1L))
			{
				if (_acrPbLatchEnable && (_ifr & 0x10) == 0)
					_pbLatch = _port.ReadPrb(_prb, _ddrb);
				if (_acrSrControl == ACR_SR_CONTROL_DISABLED)
					shiftIn = true;
				_ifr |= 0x10;
			}

			if (shiftIn)
			{
				_sr <<= 1;
				_sr |= Cb2 ? 1 : 0;
			}

			if ((_ifr & _ier & 0x7F) != 0)
				_ifr |= 0x80;
			else
				_ifr &= 0x7F;

			_ca1L = Ca1;
			_ca2L = Ca2;
			_cb1L = Cb1;
			_cb2L = Cb2;
		}

		public void SyncState(Serializer ser)
		{
			ser.Sync("PortOutputA", ref _pra);
			ser.Sync("PortDirectionA", ref _ddra);
			ser.Sync("PortOutputB", ref _prb);
			ser.Sync("PortDirectionB", ref _ddrb);
			ser.Sync("Timer1Counter", ref _t1C);
			ser.Sync("Timer1Latch", ref _t1L);
			ser.Sync("Timer2Counter", ref _t2C);
			ser.Sync("Timer2Latch", ref _t2L);
			ser.Sync("ShiftRegister", ref _sr);
			ser.Sync("AuxiliaryControlRegister", ref _acr);
			ser.Sync("PeripheralControlRegister", ref _pcr);
			ser.Sync("InterruptFlagRegister", ref _ifr);
			ser.Sync("InterruptEnableRegister", ref _ier);

			ser.BeginSection("Port");
			_port.SyncState(ser);
			ser.EndSection();

			ser.Sync("PortLatchA", ref _paLatch);
			ser.Sync("PortLatchB", ref _pbLatch);
			ser.Sync("CA1InterruptControl", ref _pcrCa1IntControl);
			ser.Sync("CA2Control", ref _pcrCa2Control);
			ser.Sync("CB1InterruptControl", ref _pcrCb1IntControl);
			ser.Sync("CB2Control", ref _pcrCb2Control);
			ser.Sync("PortLatchEnableA", ref _acrPaLatchEnable);
			ser.Sync("PortLatchEnableB", ref _acrPbLatchEnable);
			ser.Sync("ShiftRegisterControl", ref _acrSrControl);
			ser.Sync("Timer1Control", ref _acrT1Control);
			ser.Sync("Timer2Control", ref _acrT2Control);
			ser.Sync("PreviousCA1", ref _ca1L);
			ser.Sync("PreviousCA2", ref _ca2L);
			ser.Sync("PreviousCB1", ref _cb1L);
			ser.Sync("PreviousCB2", ref _cb2L);
			ser.Sync("PreviousPB6", ref _pb6L);
			ser.Sync("ResetCa2NextClock", ref _resetCa2NextClock);
			ser.Sync("ResetCb2NextClock", ref _resetCb2NextClock);
			ser.Sync("HandshakeCa2NextClock", ref _handshakeCa2NextClock);
			ser.Sync("HandshakeCb2NextClock", ref _handshakeCb2NextClock);
			ser.Sync("CA1", ref Ca1);
			ser.Sync("CA2", ref Ca2);
			ser.Sync("CB1", ref Cb1);
			ser.Sync("CB2", ref Cb2);
			ser.Sync("PB6", ref _pb6);
			ser.Sync("InterruptNextClock", ref _interruptNextClock);
			ser.Sync("T1Loaded", ref _t1CLoaded);
			ser.Sync("T2Loaded", ref _t2CLoaded);
			ser.Sync("T1Delayed", ref _t1Delayed);
			ser.Sync("T2Delayed", ref _t2Delayed);
			ser.Sync("ResetPb7NextClock", ref _resetPb7NextClock);
			ser.Sync("SetPb7NextClock", ref _setPb7NextClock);
			ser.Sync("ShiftRegisterCount", ref _srCount);
		}
	}
}
