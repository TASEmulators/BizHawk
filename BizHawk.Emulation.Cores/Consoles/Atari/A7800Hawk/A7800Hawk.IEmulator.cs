using BizHawk.Common.NumberExtensions;
using BizHawk.Emulation.Common;
using System;

namespace BizHawk.Emulation.Cores.Atari.A7800Hawk
{
	public partial class A7800Hawk : IEmulator 
	{
		public IEmulatorServiceProvider ServiceProvider { get; }

		public ControllerDefinition ControllerDefinition => _controllerDeck.Definition;

		//Maria related variables
		public int cycle;
		public int cpu_cycle;
		public bool cpu_is_haltable;
		public bool cpu_is_halted;
		public bool cpu_halt_pending;
		public bool cpu_resume_pending;

		// input state of controllers and console
		public byte p1_state;
		public byte p2_state;
		public byte p1_fire;
		public byte p2_fire;
		public byte p1_fire_2x;
		public byte p2_fire_2x;
		public byte con_state;
		public bool left_toggle;
		public bool right_toggle;
		public bool left_was_pressed;
		public bool right_was_pressed;
		public bool p1_is_2button;
		public bool p2_is_2button;
		public bool p1_is_lightgun;
		public bool p2_is_lightgun;
		public float p1_lightgun_x;
		public float p1_lightgun_y;
		public float p2_lightgun_x;
		public float p2_lightgun_y;
		public int lg_counting_down;
		public int lg_counting_down_2;
		public int lg_counting_down_3;
		public bool lg_trigger_hit;
		public bool lg_do_once = true;

		// there are 4 maria cycles in a CPU cycle (fast access, both NTSC and PAL)
		// if the 6532 or TIA are accessed (PC goes to one of those addresses) the next access will be slower by 1/2 a CPU cycle
		// i.e. it will take 6 Maria cycles instead of 4
		public bool slow_access = false;
		public int slow_countdown;

		public void FrameAdvance(IController controller, bool render, bool rendersound)
		{
			Console.WriteLine("-----------------------FRAME-----------------------");

			if (_tracer.Enabled)
			{
				cpu.TraceCallback = s => _tracer.Put(s);
			}
			else
			{
				cpu.TraceCallback = null;
			}

			_frame++;

			if (controller.IsPressed("Power"))
			{
				// it seems that theMachine.Reset() doesn't clear ram, etc
				// this should leave hsram intact but clear most other things
				HardReset();
			}

			_islag = true;

			GetControllerState(controller);
			GetConsoleState(controller);

			//reset lightgun detection
			lg_do_once = true;
			lg_trigger_hit = false;

			maria.RunFrame();

			if (_islag)
			{
				_lagcount++;
			}
		}

		public void RunCPUCycle()
		{
			if (slow_countdown==0)
			{
				cpu_cycle++;
			}
			else
			{
				slow_countdown--;
			}

			if (lg_counting_down > 0)
			{
				lg_counting_down--;
				if (lg_counting_down==0)
				{
					lg_trigger_hit = true;
					lg_counting_down_2 = 454;
					lg_counting_down_3 = 8;
				}
			}

			if (lg_counting_down_2 > 0)
			{
				lg_counting_down_2--;
				if (lg_counting_down_2 == 0 && lg_counting_down_3 > 0)
				{
					lg_counting_down_3--;
					lg_counting_down_2 = 454;
					lg_trigger_hit = true;
				}

				if (lg_counting_down_2 == 424)
				{
					lg_trigger_hit = false;
				}
			}


			tia._hsyncCnt++;
			tia._hsyncCnt %= 454;
			// do the audio sampling
			if (tia._hsyncCnt == 113 || tia._hsyncCnt == 340)
			{
				tia.Execute(0);
			}

			// tick the m6532 timer, which is still active although not recommended to use
			// also it runs off of the cpu cycle timer
			if (cpu_cycle== 4)
			{
				m6532.Timer.Tick();
			}

			if (cpu_cycle <= (2 + (slow_access ? 1 : 0)))
			{
				cpu_is_haltable = true;
			}
			else
			{
				cpu_is_haltable = false;
			}

			// the time a cpu cycle takes depends on the status of the address bus
			// any address in range of the TIA or m6532 takes an extra cycle to complete
			if (cpu_cycle == (4 + (slow_access ? 2 : 0)))
			{
				if (!cpu_is_halted)
				{
					cpu.ExecuteOne();

					// we need to stall the next cpu cycle from starting if the current one is a slow access
					if (slow_access)
					{
						slow_access = false;
						slow_countdown = 2;
					}
				}
				else
				{
					// we still want to keep track of CPU time even if it is halted, so increment the counter here
					// The basic 6502 has no halt state, this feature is specific to SALLY
					cpu.TotalExecutedCycles++;
				}

				cpu_cycle = 0;

				if (cpu_halt_pending)
				{
					cpu_halt_pending = false;
					cpu_is_halted = true;
				}

				if (cpu_resume_pending)
				{
					cpu_resume_pending = false;
					cpu_is_halted = false;
				}
			}
		}

		public void GetControllerState(IController controller)
		{
			InputCallbacks.Call();

			p1_state = _controllerDeck.ReadPort1(controller);
			p2_state = _controllerDeck.ReadPort2(controller);
			p1_fire = _controllerDeck.ReadFire1(controller);
			p2_fire = _controllerDeck.ReadFire2(controller);
			p1_fire_2x = _controllerDeck.ReadFire1_2x(controller);
			p2_fire_2x = _controllerDeck.ReadFire2_2x(controller);
			p1_is_2button = _controllerDeck.Is_2_button1(controller);
			p2_is_2button = _controllerDeck.Is_2_button2(controller);
			p1_is_lightgun = _controllerDeck.Is_LightGun1(controller, out p1_lightgun_x, out p1_lightgun_y);
			p2_is_lightgun = _controllerDeck.Is_LightGun2(controller, out p2_lightgun_x, out p2_lightgun_y);
		}

		public void GetConsoleState(IController controller)
		{
			byte result = 0;

			if (controller.IsPressed("Right Difficulty"))
			{
				if (!right_was_pressed)
				{
					right_toggle = !right_toggle;
				}
				right_was_pressed = true;
				result |= (byte)((right_toggle ? 1 : 0) << 7);
			}
			else
			{
				right_was_pressed = false;
				result |= (byte)((right_toggle ? 1 : 0) << 7);
			}

			if (controller.IsPressed("Left Difficulty"))
			{
				if (!left_was_pressed)
				{
					left_toggle = !left_toggle;
				}
				left_was_pressed = true;
				result |= (byte)((left_toggle ? 1 : 0) << 6);
			}
			else
			{
				left_was_pressed = false;
				result |= (byte)((left_toggle ? 1 : 0) << 6);
			}

			if (!controller.IsPressed("Pause"))
			{
				result |= (1 << 3);
			}
			if (!controller.IsPressed("Select"))
			{
				result |= (1 << 1);
			}
			if (!controller.IsPressed("Reset"))
			{
				result |= 1;
			}

			con_state = result;
		}

		public byte getLightGunState(int p_x)
		{
			float x = p_x == 1 ? p1_lightgun_x : p2_lightgun_x;
			float y = p_x == 1 ? p1_lightgun_y : p2_lightgun_y;

			if ((maria.scanline - 20) == y-4)
			{
				if (maria.cycle >= (133 + x) && lg_do_once)
				{
					// return true 61 cycles into the future
					lg_counting_down = 64 - (maria.cycle - (int)(133 + x));
					lg_do_once = false;
				}
			}

			if (lg_trigger_hit)
			{
				return 0x0;
			}
			else
			{
				return 0x80;
			}
		}

		public int Frame => _frame;

		public string SystemId => "A7800"; 

		public bool DeterministicEmulation { get; set; }

		public void ResetCounters()
		{
			_frame = 0;
			_lagcount = 0;
			_islag = false;
		}

		public CoreComm CoreComm { get; }

		public void Dispose()
		{
			maria = null;
			tia = null;
			m6532 = null;
		}
	}
}
