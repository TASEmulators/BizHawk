using BizHawk.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.Components
{
	/// <summary>
	/// YM2149F variant
	/// this implementation is quite incomplete
	/// http://wiki.nesdev.com/w/index.php/Sunsoft_5B_audio
	/// </summary>
	public class Sunsoft5BAudio
	{
		private class Pulse
		{
			private readonly Action<int> SendDiff;
			// regs
			private int Period;
			private bool Disable;
			private int Volume;
			// state
			private int clock;
			private int sequence;
			private int output;

			public void SyncState(Serializer ser)
			{
				ser.Sync(nameof(Period), ref Period);
				ser.Sync(nameof(Disable), ref Disable);
				ser.Sync(nameof(Volume), ref Volume);
				ser.Sync(nameof(clock), ref clock);
				ser.Sync(nameof(sequence), ref sequence);
				ser.Sync(nameof(output), ref output);
			}

			public Pulse(Action<int> SendDiff)
			{
				this.SendDiff = SendDiff;
			}

			private void CalcOut()
			{
				int newout = 1;
				if (!Disable)
				{
					newout = sequence;
				}

				newout *= Volume;
				if (newout != output)
				{
					SendDiff(newout - output);
					output = newout;
				}
			}

			public void Clock()
			{
				clock--;
				if (clock < 0)
				{
					clock = Period * 16;
					sequence ^= 1;
					CalcOut();
				}
			}
			public void WritePeriodLow(byte val)
			{
				Period &= 0xf00;
				Period |= val;
			}
			public void WritePeriodHigh(byte val)
			{
				Period &= 0x0ff;
				Period |= val << 8 & 0xf00;
			}
			public void SetToneDisable(bool disable)
			{
				Disable = disable;
				CalcOut();
			}
			public void SetVolume(byte val)
			{
				Volume = val & 15;
				CalcOut();
			}
		}

		private int RegNum;
		private readonly Pulse[] pulse = new Pulse[3];

		public void RegSelect(byte val)
		{
			RegNum = val & 15;
		}
		public void RegWrite(byte val)
		{
			switch (RegNum)
			{
				case 0: pulse[0].WritePeriodLow(val); break;
				case 1: pulse[0].WritePeriodHigh(val); break;
				case 2: pulse[1].WritePeriodLow(val); break;
				case 3: pulse[1].WritePeriodHigh(val); break;
				case 4: pulse[2].WritePeriodLow(val); break;
				case 5: pulse[2].WritePeriodHigh(val); break;
				case 6: break; // noise period
				case 7: // also noise disable
					pulse[0].SetToneDisable(val.Bit(0));
					pulse[1].SetToneDisable(val.Bit(1));
					pulse[2].SetToneDisable(val.Bit(2));
					break;
				case 8: pulse[0].SetVolume(val); break; // also envelope enable
				case 9: pulse[1].SetVolume(val); break; // also envelope enable
				case 10: pulse[2].SetVolume(val); break; // also envelope enable
				case 11: break; // envelope low
				case 12: break; // envelope high
				case 13: break; // envelope params
				// ports 14 and 15 are not hooked up on Sunsoft 5B
			}
		}

		public void SyncState(Serializer ser)
		{
			ser.BeginSection(nameof(Sunsoft5BAudio));
			ser.Sync(nameof(RegNum), ref RegNum);
			for (int i = 0; i < pulse.Length; i++)
			{
				ser.BeginSection("Pulse" + i);
				pulse[i].SyncState(ser);
				ser.EndSection();
			}
			ser.EndSection();
		}

		private readonly Action<int> enqueuer;
		private void PulseAddDiff(int val)
		{
			enqueuer(val * 250);
		}

		public Sunsoft5BAudio(Action<int> enqueuer)
		{
			this.enqueuer = enqueuer;
			for (int i = 0; i < pulse.Length; i++)
			{
				pulse[i] = new Pulse(PulseAddDiff);
			}
		}

		public void Clock()
		{
			foreach (Pulse p in pulse)
			{
				p.Clock();
			}
		}
	}
}
