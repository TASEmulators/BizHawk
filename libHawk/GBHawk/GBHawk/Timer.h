#include <iostream>
#include <cstdint>
#include <iomanip>
#include <string>

using namespace std;

namespace GBHawk
{
	class Timer
	{
	public:

		Timer()
		{

		};

		bool* FlagI = nullptr;
		uint8_t* REG_FFFF = nullptr;
		uint8_t* REG_FF0F = nullptr;

		bool old_state;
		bool state;
		bool reload_block;
		bool TMA_coincidence;
		
		uint8_t timer_reload;
		uint8_t timer;
		uint8_t timer_old;
		uint8_t timer_control;
		uint8_t pending_reload;
		uint8_t write_ignore;
		
		uint32_t divider_reg;

		uint8_t ReadReg(uint32_t addr)
		{
			uint8_t ret = 0;

			switch (addr)
			{
			case 0xFF04: ret = (uint8_t)(divider_reg >> 8);		break; // DIV register
			case 0xFF05: ret = timer;							break; // TIMA (Timer Counter)
			case 0xFF06: ret = timer_reload;					break; // TMA (Timer Modulo)
			case 0xFF07: ret = timer_control;					break; // TAC (Timer Control)
			}

			return ret;
		}

		void WriteReg(int addr, uint8_t value)
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
				timer_control = (uint8_t)((timer_control & 0xf8) | (value & 0x7)); // only bottom 3 bits function
				break;
			}
		}

		void tick_1()
		{
			if (write_ignore > 0)
			{
				write_ignore--;
				if (write_ignore == 0)
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
					if ((REG_FFFF[0] & 0x4) > 0) { FlagI[0] = true; }
					REG_FF0F[0] |= 0x04;
				}
			}
		}

		void tick_2()
		{
			divider_reg++;

			// pick a bit to test based on the current value of timer control
			switch (timer_control & 3)
			{
			case 0:
				state = (divider_reg & 0x200) > 0;
				break;
			case 1:
				state = (divider_reg & 0x8) > 0;
				break;
			case 2:
				state = (divider_reg & 0x20) > 0;
				break;
			case 3:
				state = (divider_reg & 0x80) > 0;
				break;
			}

			// And it with the state of the timer on/off bit
			state &= (timer_control & 4) > 0;

			// this procedure allows several glitchy timer ticks, since it only measures falling edge of the state
			// so things like turning the timer off and resetting the divider will tick the timer
			if (old_state && !state)
			{
				timer_old = timer;
				timer++;

				// if overflow happens, set the interrupt flag and reload the timer (if applicable)
				if (timer < timer_old)
				{
					if ((timer_control & 4) > 0)
					{
						pending_reload = 4;
						reload_block = false;
					}
					else
					{
						//TODO: Check if timer still gets reloaded if TAC diabled causes overflow
						if ((REG_FFFF[0] & 0x4) > 0) { FlagI[0] = true; }
						REG_FF0F[0] |= 0x04;
					}
				}
			}

			old_state = state;
		}

		void Reset()
		{
			divider_reg = 8; // probably always 8 but not confirmed for GB as far as I know
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

		#pragma region State Save / Load

		uint8_t* SaveState(uint8_t* saver)
		{

			*saver = (uint8_t)(old_state ? 1 : 0); saver++;
			*saver = (uint8_t)(state ? 1 : 0); saver++;
			*saver = (uint8_t)(reload_block ? 1 : 0); saver++;
			*saver = (uint8_t)(TMA_coincidence ? 1 : 0); saver++;

			*saver = timer_reload; saver++;
			*saver = timer; saver++;
			*saver = timer_old; saver++;
			*saver = timer_control; saver++;
			*saver = pending_reload; saver++;
			*saver = write_ignore; saver++;

			*saver = (uint8_t)(divider_reg & 0xFF); saver++; *saver = (uint8_t)((divider_reg >> 8) & 0xFF); saver++;
			*saver = (uint8_t)((divider_reg >> 16) & 0xFF); saver++; *saver = (uint8_t)((divider_reg >> 24) & 0xFF); saver++;

			return saver;
		}

		uint8_t* LoadState(uint8_t* loader)
		{
			old_state = *loader == 1; loader++;
			state = *loader == 1; loader++;
			reload_block = *loader == 1; loader++;
			TMA_coincidence = *loader == 1; loader++;

			timer_reload = *loader; loader++;
			timer = *loader; loader++;
			timer_old = *loader; loader++;
			timer_control = *loader; loader++;
			pending_reload = *loader; loader++;
			write_ignore  = *loader; loader++;

			divider_reg  = *loader; loader++; divider_reg |= (*loader << 8); loader++;
			divider_reg |= (*loader << 16); loader++; divider_reg |= (*loader << 24); loader++;

			return loader;
		}

		#pragma endregion
	};
}