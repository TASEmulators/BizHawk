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

			if (_tracer.IsEnabled())
			{
				_cpu.TraceCallback = s => _tracer.Put(s);
			}
			else
			{
				_cpu.TraceCallback = null;
			}

			// Fast tape loading (loader acceleration): while a load is in progress - i.e. the tape has been
			// auto-started - and instant loading is enabled on a non-deterministic core, run several whole
			// machine-frames per host frame. The actual loader executes (just faster in wall-clock), so this
			// covers every loading scheme (standard ROM, custom/turbo like Speedlock, CSW/WAV) and the loading
			// screen and inter-block pauses appear naturally. FrameCount advances once per machine-frame, so it
			// still matches an accurate load. Disabled when deterministic (TAS/movie recording).
			int steps = 1;
			if (!DeterministicEmulation
				&& SyncSettings.TapeLoadSpeed == TapeLoadSpeed.Instant
				&& _machine.TapeDevice is { TapeIsPlaying: true })
			{
				// user-configurable machine-frames per host frame (clamped to the settings-slider range 5..50)
				steps = SyncSettings.TapeLoadTurboMultiplier;
				if (steps < 5) steps = 5;
				else if (steps > 50) steps = 50;
			}

			for (int i = 0; i < steps; i++)
			{
				bool last = i == steps - 1;
				_machine.ExecuteFrame(ren && last, renSound && last);

				// discard the intermediate frames' audio so the mixer only returns the final frame's samples
				// (otherwise the skipped frames' samples would pile up and the sound buffers would overflow)
				if (!last)
				{
					SoundMixer.DiscardSamples();
				}
			}

			if (_isLag)
			{
				_lagCount++;
			}

			return true;
		}

		public int Frame => _machine?.FrameCount ?? 0;

		public string SystemId => VSystemID.Raw.ZXSpectrum;

		public bool DeterministicEmulation { get; }

		public void ResetCounters()
		{
			_machine.FrameCount = 0;
			_lagCount = 0;
			_isLag = false;
		}

		public void Dispose()
		{
			_machine = null;
		}
	}
}
