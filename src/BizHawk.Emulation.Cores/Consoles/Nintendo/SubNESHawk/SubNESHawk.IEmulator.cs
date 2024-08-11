using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Nintendo.NES;

namespace BizHawk.Emulation.Cores.Nintendo.SubNESHawk
{
	public partial class SubNESHawk : IEmulator
	{
		public IEmulatorServiceProvider ServiceProvider { get; }

		public ControllerDefinition ControllerDefinition => _nesCore.ControllerDefinition;

		public bool FrameAdvance(IController controller, bool render, bool renderSound)
		{
			//Console.WriteLine("-----------------------FRAME-----------------------");
			if (_tracer.IsEnabled())
			{
				_nesCore.cpu.TraceCallback = s => _tracer.Put(s);
			}
			else
			{
				_nesCore.cpu.TraceCallback = null;
			}

			if (controller.IsPressed("Power"))
			{
				HardReset();
			}

			reset_frame = false;
			if (controller.IsPressed("Reset"))
			{
				reset_frame = true;
			}

			reset_cycle = controller.AxisValue("Reset Cycle");
			reset_cycle_int = (int)Math.Floor(reset_cycle);

			if (_nesCore.Board is FDS fds)
			{
				if (controller.IsPressed("FDS Eject"))
				{
					fds.Eject();
				}

				for (var i = 0; i < fds.NumSides; i++)
				{
					if (controller.IsPressed("FDS Insert " + i))
					{
						fds.InsertSide(i);
					}
				}
			}

			if (_nesCore.IsVS)
			{
				_nesCore.VS_service = (byte)(controller.IsPressed("Service Switch") ? 1 : 0);

				if (controller.IsPressed("Insert Coin P1"))
				{
					_nesCore.VS_coin_inserted |= 1;
				}
				else
				{
					_nesCore.VS_coin_inserted &= 2;
				}

				if (controller.IsPressed("Insert Coin P2"))
				{
					_nesCore.VS_coin_inserted |= 2;
				}
				else
				{
					_nesCore.VS_coin_inserted &= 1;
				}
			}

			_nesCore.lagged = true;

			DoFrame(controller);

			bool ret = pass_a_frame;

			if (pass_a_frame)
			{
				_nesCore.videoProvider.FillFrameBuffer();
				current_cycle = 0;
				_nesCore.cpu.ext_ppu_cycle = current_cycle;
			}
			
			_isLag = _nesCore.lagged;

			if (_isLag)
			{
				_lagCount++;
			}

			reset_frame = false;

			_frame++;

			return ret;
		}

		private bool stop_cur_frame;
		private bool pass_new_input;
		private bool pass_a_frame;
		private bool reset_frame;
		private int current_cycle;
		private float reset_cycle;
		private int reset_cycle_int;

		private void DoFrame(IController controller)
		{
			stop_cur_frame = false;
			while (!stop_cur_frame)
			{
				if (reset_frame && (current_cycle == reset_cycle_int))
				{
					SoftReset();
					reset_frame = false;
				}
				_nesCore.do_single_step(controller, out pass_new_input, out pass_a_frame);
				current_cycle++;
				_nesCore.cpu.ext_ppu_cycle = current_cycle;
				stop_cur_frame |= pass_a_frame;
				stop_cur_frame |= pass_new_input;
			}
		}

		public int Frame => _frame;

		public string SystemId => VSystemID.Raw.NES;

		public bool DeterministicEmulation => _nesCore.DeterministicEmulation;

		public void ResetCounters()
		{
			_frame = 0;
			_lagCount = 0;
			_isLag = false;
		}

		public void Dispose() => _nesCore.Dispose();
	}
}
