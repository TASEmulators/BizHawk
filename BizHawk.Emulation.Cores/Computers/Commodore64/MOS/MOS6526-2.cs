using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BizHawk.Common;

#pragma warning disable 0169
#pragma warning disable 0414
#pragma warning disable 0649

// thanks to these fine folks for their research on this buggy as hell chip:
// Simon White (s_a_white@email.com)
// Antti S. Lankila "alankila"
// Andreas Boose (viceteam@t-online.de)
// Alexander Bluhm (mam96ehy@studserv.uni-leipzig.de)

namespace BizHawk.Emulation.Cores.Computers.Commodore64.MOS
{
	sealed public partial class MOS6526_2
	{
		sealed private class CiaTimer
		{
			public const int TIMER_CR_START = 0x01;
			public const int TIMER_STEP = 0x04;
			public const int TIMER_CR_ONESHOT = 0x08;
			public const int TIMER_CR_FLOAD = 0x10;
			public const int TIMER_PHI2IN = 0x20;
			public const int TIMER_MASK = TIMER_CR_START | TIMER_CR_ONESHOT | TIMER_CR_FLOAD | TIMER_PHI2IN;
			public const int TIMER_COUNT2 = 0x100;
			public const int TIMER_COUNT3 = 0x200;
			public const int TIMER_ONESHOT0 = 0x800;
			public const int TIMER_ONESHOT = 0x80000;
			public const int TIMER_LOAD1 = 0x1000;
			public const int TIMER_LOAD = 0x100000;
			public const int TIMER_OUT = 0x40000000 << 1;

			const int WANTED = TIMER_CR_START | TIMER_PHI2IN | TIMER_COUNT2 | TIMER_COUNT3;
			const int UNWANTED = TIMER_OUT | TIMER_CR_FLOAD | TIMER_LOAD1 | TIMER_LOAD;
			const int UNWANTED1 = TIMER_CR_START | TIMER_PHI2IN;
			const int UNWANTED2 = TIMER_CR_START | TIMER_STEP;

			int adj;
			bool toggle;
			public int state;
			int lastControlValue;
			int timer;
			int latch;
			bool pbToggle;
			int ciaEventPauseTime;
			bool phi1tod;
			bool phi1run;
			int cycleSkippingEvent = -1;
			int nextTick = 0;

			Action serialPort;
			Action underFlow;

			public CiaTimer(Action serialPortCallback, Action underFlowCallback)
			{
				this.serialPort = serialPortCallback;
				this.underFlow = underFlowCallback;
			}

			public void clock()
			{
				if (timer != 0 && (state & TIMER_COUNT3) != 0)
				{
					timer--;
				}

				adj = state & (TIMER_CR_START | TIMER_CR_ONESHOT | TIMER_PHI2IN);
				if ((state & (TIMER_CR_START | TIMER_PHI2IN)) == (TIMER_CR_START | TIMER_PHI2IN))
				{
					adj |= TIMER_COUNT2;
				}
				if ((state & TIMER_COUNT2) != 0 || (state & (TIMER_STEP | TIMER_CR_START)) == (TIMER_STEP | TIMER_CR_START))
				{
					adj |= TIMER_COUNT3;
				}
				adj |= (state & (TIMER_CR_FLOAD | TIMER_CR_ONESHOT | TIMER_LOAD1 | TIMER_ONESHOT0)) << 8;
				state = adj;

				if (timer == 0 && (state & TIMER_COUNT3) != 0)
				{
					state |= TIMER_LOAD | TIMER_OUT;

					if ((state & (TIMER_ONESHOT | TIMER_ONESHOT0)) != 0)
					{
						state &= ~(TIMER_CR_START | TIMER_COUNT2);
					}

					toggle = (lastControlValue & 0x06) == 0x06;
					pbToggle = toggle && !pbToggle;
					serialPort();
					underFlow();
				}

				if ((state & TIMER_LOAD) != 0)
				{
					timer = latch;
					state &= ~TIMER_COUNT3;
				}
			}

			public bool getPbToggle()
			{
				return pbToggle;
			}

			public int getTimer()
			{
				return timer;
			}

			public void reschedule()
			{
				if ((state & UNWANTED) != 0)
				{
					return;
				}

				if ((state & TIMER_COUNT3) != 0)
				{
					if ((timer & 0xFFFF) > 2 && (state & UNWANTED) == WANTED)
					{
						ciaEventPauseTime = 1;
						cycleSkippingEvent = (timer - 1) & 0xFFFF;
						return;
					}
					nextTick = 1;
					return;
				}
				else
				{
					if ((state & UNWANTED1) == UNWANTED1 || (state & UNWANTED2) == UNWANTED2)
					{
						nextTick = 1;
						return;
					}

					ciaEventPauseTime = -1;
					return;
				}
			}

			public void reset()
			{
				timer = 0xFFFF;
				latch = 0xFFFF;
				pbToggle = false;
				state = 0;
				ciaEventPauseTime = 0;
				cycleSkippingEvent = -1;
			}

			public void setControlRegister(int cr)
			{
				state &= ~TIMER_MASK;
				state |= cr & TIMER_MASK ^ TIMER_PHI2IN;
				lastControlValue = cr;
			}

			public void setLatchLow(int low)
			{
				latch = ((latch & 0xFF00) | (low & 0xFF));
				if ((state & TIMER_LOAD) != 0)
				{
					timer = ((timer & 0xFF00) | (low & 0xFF));
				}
			}

			public void setLatchHigh(int high)
			{
				latch = ((latch & 0xFF) | ((high & 0xFF) << 8));
				if ((state & TIMER_LOAD) != 0 || (state & TIMER_CR_START) == 0)
				{
					timer = latch;
				}
			}

			public void setPbToggle(bool st)
			{
				pbToggle = st;
			}

			public void SyncState(Serializer ser)
			{
				SaveState.SyncObject(ser, this);
			}
		}

		const int INT_NONE = 0x00;
		const int INT_UNDERFLOW_A = 0x01;
		const int INT_UNDERFLOW_B = 0x02;
		const int INT_ALARM = 0x04;
		const int INT_SP = 0x08;
		const int INT_FLAG = 0x10;

		int pra;
		int prb;
		int ddra;
		int ddrb;
		int ta;
		int tb;
		int tod_ten;
		int tod_sec;
		int tod_min;
		int tod_hr;
		int sdr;
		int icr;
		int idr;
		int cra;
		int crb;

		int sdr_out;
		bool sdr_buffered;
		int sdr_count;
		bool tod_latched;
		bool tod_stopped;
		int[] tod_clock = new int[4];
		int[] tod_alarm = new int[4];
		int[] tod_latch = new int[4];
		int tod_cycles = -1;
		int tod_period = -1;
		bool postTimerBEvent;
		int idr_old;
		bool int_delayed;

		int bcd_internal;

		CiaTimer a;
		CiaTimer b;
		LatchedPort portA;
		LatchedPort portB;

		public MOS6526_2(Region region)
		{
			a = new CiaTimer(serialPortA, underFlowA);
			b = new CiaTimer(serialPortB, underFlowB);
			portA = new LatchedPort();
			portB = new LatchedPort();
			switch (region)
			{
				case Region.NTSC:
					tod_period = 14318181 / 140;
					break;
				case Region.PAL:
					tod_period = 17734472 / 180;
					break;
			}
		}

		int bcdToByte(int input)
		{
			return 10 * ((input & 0xF0) >> 4) + (input & 0x0F);
		}

		int byteToBcd(int input)
		{
			return (((input / 10) << 4) + (input % 10)) & 0xFF;
		}

		int int_clear()
		{
			int_delayed = false;
			idr_old = idr;
			idr = 0;
			return idr_old;
		}

		void int_clearEnabled(int i)
		{
			icr &= ~i;
		}

		void int_reset()
		{
			int_delayed = false;
		}

		void int_set(int i)
		{
			idr |= i;
			if ((icr & idr) != 0 && (idr & 0x80) == 0)
			{
				int_delayed = true;
			}
		}

		void int_setEnabled(int i)
		{
			icr |= i & ~0x80;
		}

		void proc_a()
		{
			if (int_delayed)
			{
				int_delayed = false;
				idr |= 0x80;
			}
			if (postTimerBEvent)
			{
				postTimerBEvent = false;
				b.state |= CiaTimer.TIMER_STEP;
			}
			a.clock();
		}

		void proc_b()
		{
			b.clock();
		}

		void reset()
		{
			a.reset();
			b.reset();
			sdr_out = 0;
			sdr_count = 0;
			sdr_buffered = false;
			icr = 0;
			idr = 0;
			idr_old = 0;
			int_reset();
			portA.Latch = 0xFF;
			portB.Latch = 0xFF;
			portA.Direction = 0xFF;
			portB.Direction = 0xFF;
			PortAMask = 0xFF;
			PortBMask = 0xFF;
		}

		void serialPortA()
		{
			if ((cra & 0x40) != 0)
			{
				if (sdr_count != 0)
				{
					if (--sdr_count == 0)
					{
						triggerInterruptSP();
					}
				}
				if (sdr_count == 0 && sdr_buffered)
				{
					sdr_out = sdr;
					sdr_buffered = false;
					sdr_count = 16;
				}
			}
		}

		void serialPortB()
		{
			// nop
		}

		void tod()
		{
			tod_cycles += tod_period;
			if (!tod_stopped)
			{
				int todpos = 0;
				int t = bcdToByte(tod_clock[todpos]) + 1;
				tod_clock[todpos++] = byteToBcd(t % 10);
				if (t >= 10)
				{
					t = bcdToByte(tod_clock[todpos]) + 1;
					tod_clock[todpos++] = byteToBcd(t % 60);
					if (t >= 60)
					{
						t = bcdToByte(tod_clock[todpos]) + 1;
						tod_clock[todpos++] = byteToBcd(t % 60);
						if (t >= 60)
						{
							int pm = tod_clock[todpos] & 0x80;
							t = bcdToByte(tod_clock[todpos] & 0x1F);
							if (t == 0x11)
							{
								pm ^= 0x80;
							}
							if (t == 0x12)
							{
								t = 1;
							}
							else if (++t == 10)
							{
								t = 0x10;
							}
							t &= 0x1F;
							tod_clock[todpos] = t | pm;
						}
					}
				}
				if (tod_clock[0] == tod_alarm[0] &&
					tod_clock[1] == tod_alarm[1] &&
					tod_clock[2] == tod_alarm[2] &&
					tod_clock[3] == tod_alarm[3])
				{
					triggerInterruptAlarm();
				}
			}
		}

		void triggerInterruptAlarm()
		{
			int_set(INT_ALARM);
		}

		void triggerInterruptSP()
		{
			int_set(INT_SP);
		}

		void triggerInterruptUnderFlowA()
		{
			int_set(INT_UNDERFLOW_A);
		}

		void triggerInterruptUnderFlowB()
		{
			int_set(INT_UNDERFLOW_B);
		}

		void underFlowA()
		{
			triggerInterruptUnderFlowA();
			if ((crb & 0x41) == 0x41)
			{
				if ((b.state & CiaTimer.TIMER_CR_START) != 0)
				{
					postTimerBEvent = true;
				}
			}
		}

		void underFlowB()
		{
			triggerInterruptUnderFlowB();
		}

		public void SyncState(Serializer ser)
		{
			SaveState.SyncObject(ser, this);
		}
	}
}
