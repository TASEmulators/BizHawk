using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Intellivision
{
	public sealed partial class Intellivision : IEmulator, IBoardInfo
	{
		public IEmulatorServiceProvider ServiceProvider { get; }

		public ControllerDefinition ControllerDefinition => _controllerDeck.Definition;

		public bool FrameAdvance(IController controller, bool render, bool renderSound)
		{
			if (_tracer.Enabled)
			{
				_cpu.TraceCallback = s => _tracer.Put(s);
			}
			else
			{
				_cpu.TraceCallback = null;
			}

			_frame++;
			_sticRow = -1;

			// read the controller state here for now
			GetControllerState(controller);

			// this timer tracks cycles stolen by the STIC during the visible part of the frame, quite a large number of them actually
			int delayCycles = 700; 
			int delayTimer = -1;

			_cpu.PendingCycles = 14934 - 3791 + _cpu.GetPendingCycles();
			_stic.Sr1 = true;
			_isLag = true;

			bool activeDisplay = _stic.active_display;

			// also at the start of every frame the color stack is reset
			_stic.ColorSP = 0x0028;

			while (_cpu.GetPendingCycles() > 0)
			{
				int cycles = _cpu.Execute();
				_psg.generate_sound(cycles);

				if (delayCycles >= 0 && activeDisplay)
				{
					delayCycles += cycles;
				}

				if (delayTimer > 0 && activeDisplay)
				{
					delayTimer -= cycles;
					if (delayTimer <= 0)
					{
						_stic.ToggleSr2();
						delayCycles = 0;
					}
				}

				if (delayCycles >= 750 && activeDisplay)
				{
					delayCycles = -1;
					delayTimer = 110;
					_stic.ToggleSr2();
					if (_sticRow >= 0)
					{
						_stic.in_vb_2 = true;
						_stic.Background(_sticRow);
						_stic.in_vb_2 = false;
					}

					_sticRow++;
				}

				Connect();
			}

			// set up VBlank variables
			_stic.in_vb_1 = true;
			_stic.in_vb_2 = true;

			if (_stic.active_display)
			{
				_stic.Mobs();
			}

			_stic.active_display = false;
			_stic.Sr1 = false;

			_cpu.PendingCycles = 3000 + _cpu.GetPendingCycles();

			while (_cpu.GetPendingCycles() > 0)
			{
				int cycles = _cpu.Execute();
				_psg.generate_sound(cycles);
				Connect();
			}

			// vblank phase 2
			_cpu.PendingCycles = 791 + _cpu.GetPendingCycles();
			_stic.in_vb_1 = false;

			while (_cpu.GetPendingCycles() > 0)
			{
				int cycles = _cpu.Execute();
				_psg.generate_sound(cycles);
				Connect();
			}

			_stic.in_vb_2 = false;

			if (_isLag)
			{
				_lagCount++;
			}

			if (controller.IsPressed("Power"))
			{
				HardReset();
			}

			if (controller.IsPressed("Reset"))
			{
				SoftReset();
			}

			return true;
		}

		public int Frame => _frame;

		public string SystemId => "INTV";

		public bool DeterministicEmulation => true;

		public void ResetCounters()
		{
			_frame = 0;
			_lagCount = 0;
		}

		public void Dispose()
		{
		}

		// IBoardInfo
		public string BoardName => _cart.BoardName;
	}
}
