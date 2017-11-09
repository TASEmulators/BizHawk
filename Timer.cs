using BizHawk.Common;
using BizHawk.Common.NumberExtensions;
using System;

namespace BizHawk.Emulation.Cores.Nintendo.GBHawk
{
	// Timer Emulation
	public class Timer
	{
		public GBHawk Core { get; set; }

		public ushort divider_reg;
		public byte timer_reload;
		public byte timer;
		public byte timer_old;
		public byte timer_control;
		public byte pending_reload;
		public byte write_ignore;
		public bool old_state;
		public bool state;
		public bool reload_block;
		public bool TMA_coincidence;	
		
		public byte ReadReg(int addr)
		{
			byte ret = 0;

			switch (addr)
			{
				case 0xFF04: ret = (byte)(divider_reg >> 8);		break; // DIV register
				case 0xFF05: ret = timer;							break; // TIMA (Timer Counter)
				case 0xFF06: ret = timer_reload;					break; // TMA (Timer Modulo)
				case 0xFF07: ret = timer_control;					break; // TAC (Timer Control)
			}

			return ret;
		}

		public void WriteReg(int addr, byte value)
		{
			switch (addr)
			{
				// DIV register
				case 0xFF04:
					divider_reg = 0;
					break;

				// TIMA (Timer Counter)
				case 0xFF05:
					if (write_ignore == 0)
					{
						timer_old = timer;
						timer = value;
						reload_block = true;
					}
					break;

				// TMA (Timer Modulo)
				case 0xFF06:
					timer_reload = value;
					if (TMA_coincidence)
					{
						timer = timer_reload;
						timer_old = timer;
					}
					break;

				// TAC (Timer Control)
				case 0xFF07:
					timer_control = (byte)((timer_control & 0xf8) | (value & 0x7)); // only bottom 3 bits function
					break;
			}
		}

		public void tick_1()
		{
			if (write_ignore > 0)
			{
				write_ignore--;
				if (write_ignore==0)
				{
					TMA_coincidence = false;
				}
			}

			if (pending_reload > 0)
			{
				pending_reload--;
				if (pending_reload == 0 && !reload_block)
				{
					timer = timer_reload;
					timer_old = timer;
					write_ignore = 4;
					TMA_coincidence = true;

					// set interrupts
					if (Core.REG_FFFF.Bit(2)) { Core.cpu.FlagI = true; }
					Core.REG_FF0F |= 0x04;
				}
			}
		}

		public void tick_2()
		{
			divider_reg++;

			// pick a bit to test based on the current value of timer control
			switch (timer_control & 3)
			{
				case 0:
					state = divider_reg.Bit(9);
					break;
				case 1:
					state = divider_reg.Bit(3);
					break;
				case 2:
					state = divider_reg.Bit(5);
					break;
				case 3:
					state = divider_reg.Bit(7);
					break;
			}

			// And it with the state of the timer on/off bit
			state &= timer_control.Bit(2);

			// this procedure allows several glitchy timer ticks, since it only measures falling edge of the state
			// so things like turning the timer off and resetting the divider will tick the timer
			if (old_state && !state)
			{
				timer_old = timer;
				timer++;

				// if overflow, set the interrupt flag and reload the timer (4 clocks later)
				if (timer < timer_old)
				{
					pending_reload = 4;
					reload_block = false;
				}
			}

			old_state = state;
		}

		public void Reset()
		{
			divider_reg = 0;
			timer_reload = 0;
			timer = 0;
			timer_old = 0;
			timer_control = 0xF8;
			pending_reload = 0;
			write_ignore = 0;
			old_state = false;
			state = false;
			reload_block = false;
			TMA_coincidence = false;
	}

		public void SyncState(Serializer ser)
		{
			ser.Sync("divider_reg", ref divider_reg);
			ser.Sync("timer_reload", ref timer_reload);
			ser.Sync("timer", ref timer);
			ser.Sync("timer_old", ref timer_old);
			ser.Sync("timer_control", ref timer_control);
			ser.Sync("pending_reload", ref pending_reload);
			ser.Sync("write_ignore", ref write_ignore);
			ser.Sync("old_state", ref old_state);
			ser.Sync("state", ref state);
			ser.Sync("reload_block", ref reload_block);
			ser.Sync("TMA_coincidence", ref TMA_coincidence);
		}
	}
}