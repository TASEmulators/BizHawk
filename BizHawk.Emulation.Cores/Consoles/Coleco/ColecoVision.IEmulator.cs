using BizHawk.Emulation.Common;
using BizHawk.Common.NumberExtensions;

namespace BizHawk.Emulation.Cores.ColecoVision
{
	public partial class ColecoVision : IEmulator
	{
		public IEmulatorServiceProvider ServiceProvider { get; }

		public ControllerDefinition ControllerDefinition => ControllerDeck.Definition;

		public void FrameAdvance(IController controller, bool render, bool renderSound)
		{
			_controller = controller;

			// NOTE: Need to research differences between reset and power cycle
			if (_controller.IsPressed("Power"))
			{
				HardReset();
			}

			if (_controller.IsPressed("Reset"))
			{
				SoftReset();
			}

			_frame++;
			_isLag = true;

			if (_tracer.Enabled)
			{
				_cpu.TraceCallback = s => _tracer.Put(s);
			}
			else
			{
				_cpu.TraceCallback = null;
			}
			byte tempRet1 = ControllerDeck.ReadPort1(controller, true, true);
			byte tempRet2 = ControllerDeck.ReadPort2(controller, true, true);

			bool intPending = (!tempRet1.Bit(4)) | (!tempRet2.Bit(4));

			for (int scanLine = 0; scanLine < 262; scanLine++)
			{
				_vdp.RenderScanline(scanLine);

				if (scanLine == 192)
				{
					_vdp.InterruptPending = true;

					if (_vdp.EnableInterrupts)
						_cpu.NonMaskableInterrupt = true;
				}

				for (int i = 0; i < 228; i++)
				{
					PSG.generate_sound(1);
					if (use_SGM) { SGM_sound.generate_sound(1); }			
					_cpu.ExecuteOne();

					// pick out sound samples from the sound devies twice per scanline
					if ((i==76) || (i==152))
					{
						PSG.Sample();
						if (use_SGM) { SGM_sound.Sample(); }
					}
				}

				_cpu.FlagI = false;
				if (intPending && scanLine == 50)
				{
					if (_vdp.EnableInterrupts)
					{
						_cpu.FlagI = true;
						intPending = false;
					}
				}
			}

			if (_isLag)
			{
				_lagCount++;
			}
		}

		public bool use_SGM = false;
		public bool is_MC = false;
		public int MC_bank = 0;
		public bool enable_SGM_high = false;
		public bool enable_SGM_low = false;
		public byte port_0x53, port_0x7F;


		public int Frame => _frame;

		public string SystemId => "Coleco";

		public bool DeterministicEmulation => true;

		public void ResetCounters()
		{
			_frame = 0;
			_lagCount = 0;
			_isLag = false;
		}

		public CoreComm CoreComm { get; }

		public void Dispose()
		{
		}
	}
}
