using BizHawk.Emulation.Common;
using System;

namespace BizHawk.Emulation.Cores.Intellivision
{
	public sealed partial class Intellivision : IEmulator
	{
		public IEmulatorServiceProvider ServiceProvider { get; private set; }

		[FeatureNotImplemented]
		public ISoundProvider SoundProvider
		{
			get { return NullSound.SilenceProvider; }
		}

		[FeatureNotImplemented]
		public ISyncSoundProvider SyncSoundProvider
		{
			get { return new FakeSyncSound(NullSound.SilenceProvider, 735); }
		}

		public bool StartAsyncSound()
		{
			return true;
		}

		public void EndAsyncSound()
		{

		}

		public ControllerDefinition ControllerDefinition
		{
			get { return IntellivisionController; }
		}

		public IController Controller { get; set; }

		public void FrameAdvance(bool render, bool rendersound)
		{
			if (Tracer.Enabled)
				_cpu.TraceCallback = (s) => Tracer.Put(s);
			else
				_cpu.TraceCallback = null;

			Frame++;
			// read the controller state here for now
			get_controller_state();
			//_stic.Mobs();
			_cpu.AddPendingCycles(3791);
			_stic.Sr1 = true;

			while (_cpu.GetPendingCycles() > 0)
			{
				int cycles = _cpu.Execute();
				Connect();
			}

			_cpu.AddPendingCycles(14934 - 3791 - _cpu.GetPendingCycles());
			_stic.Sr1 = false;
			_stic.Background();
			_stic.Mobs();

			while (_cpu.GetPendingCycles() > 0)
			{
				int cycles = _cpu.Execute();
				Connect();
			}

		}

		public int Frame { get; private set; }

		public string SystemId
		{
			get { return "INTV"; }
		}

		public bool DeterministicEmulation { get { return true; } }

		[FeatureNotImplemented]
		public string BoardName { get { return null; } }

		public void ResetCounters()
		{
			Frame = 0;
		}

		public CoreComm CoreComm { get; private set; }

		public void Dispose()
		{

		}

		public void get_controller_state()
		{
			ushort result = 0;
			// player 1
			for (int i = 0; i < 31; i++)
			{
				if (Controller.IsPressed(IntellivisionController.BoolButtons[i]))
				{
					result |= HandControllerButtons[i];
				}
			}
			
			_psg.Register[14] = (ushort)(0xFF - result);
			result = 0;

			// player 2
			for (int i = 31; i < 62; i++)
			{
				if (Controller.IsPressed(IntellivisionController.BoolButtons[i]))
				{
					result |= HandControllerButtons[i-31];
				}
			}

			_psg.Register[15] = (ushort)(0xFF - result);
		}

		static byte[] HandControllerButtons = new byte[] {
			0x60, //OUTPUT_ACTION_BUTTON_BOTTOM_LEFT
			0xC0, //OUTPUT_ACTION_BUTTON_BOTTOM_RIGHT
			0xA0, //OUTPUT_ACTION_BUTTON_TOP
			0x48, //OUTPUT_KEYPAD_ZERO
			0x81, //OUTPUT_KEYPAD_ONE
			0x41, //OUTPUT_KEYPAD_TWO
			0x21, //OUTPUT_KEYPAD_THREE
			0x82, //OUTPUT_KEYPAD_FOUR
			0x42, //OUTPUT_KEYPAD_FIVE
			0x22, //OUTPUT_KEYPAD_SIX
			0x84, //OUTPUT_KEYPAD_SEVEN
			0x44, //OUTPUT_KEYPAD_EIGHT
			0x24, //OUTPUT_KEYPAD_NINE
			0x28, //OUTPUT_KEYPAD_ENTER
			0x88, //OUTPUT_KEYPAD_CLEAR
			
			0x04, //OUTPUT_DISC_NORTH
			0x14, //OUTPUT_DISC_NORTH_NORTH_EAST
			0x16, //OUTPUT_DISC_NORTH_EAST
			0x06, //OUTPUT_DISC_EAST_NORTH_EAST
			0x02, //OUTPUT_DISC_EAST
			0x12, //OUTPUT_DISC_EAST_SOUTH_EAST
			0x13, //OUTPUT_DISC_SOUTH_EAST
			0x03, //OUTPUT_DISC_SOUTH_SOUTH_EAST
			0x01, //OUTPUT_DISC_SOUTH
			0x11, //OUTPUT_DISC_SOUTH_SOUTH_WEST
			0x19, //OUTPUT_DISC_SOUTH_WEST
			0x09, //OUTPUT_DISC_WEST_SOUTH_WEST
			0x08, //OUTPUT_DISC_WEST
			0x18, //OUTPUT_DISC_WEST_NORTH_WEST
			0x1C, //OUTPUT_DISC_NORTH_WEST
			0x0C  //OUTPUT_DISC_NORTH_NORTH_WEST		
			};
		}
	}
