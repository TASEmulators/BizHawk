using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Computers.SinclairSpectrum
{
	/// <summary>
	/// ZXHawk: Core Class
	/// * IEmulator *
	/// </summary>
	public partial class ZXSpectrum : IEmulator
	{
		public IEmulatorServiceProvider ServiceProvider { get; }

		public ControllerDefinition ControllerDefinition { get; set; }

		public bool FrameAdvance(IController controller, bool render, bool renderSound)
		{
			_controller = controller;

			bool ren = render;
			bool renSound = renderSound;

			if (DeterministicEmulation)
			{
				ren = true;
				renSound = true;
			}

			_isLag = true;

			if (_tracer.Enabled)
			{
				_cpu.TraceCallback = s => _tracer.Put(s);
			}
			else
			{
				_cpu.TraceCallback = null;
			}

			_machine.ExecuteFrame(ren, renSound);

			if (_isLag)
			{
				_lagCount++;
			}

			return true;
		}

		public int Frame => _machine?.FrameCount ?? 0;

		public string SystemId => "ZXSpectrum";

		public bool DeterministicEmulation { get; }

		public void ResetCounters()
		{
			_machine.FrameCount = 0;
			_lagCount = 0;
			_isLag = false;
		}

		public CoreComm CoreComm { get; }

		public void Dispose()
		{
			if (_machine != null)
			{
				_machine = null;
			}
		}
	}
}
