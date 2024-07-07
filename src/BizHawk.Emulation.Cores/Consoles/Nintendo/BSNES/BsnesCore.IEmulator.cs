using System.Runtime.InteropServices;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.BSNES
{
	public partial class BsnesCore : IEmulator
	{
		public IEmulatorServiceProvider ServiceProvider { get; }

		public ControllerDefinition ControllerDefinition => _controllers.Definition;

		private short[] _audioBuffer = Array.Empty<short>();

		public bool FrameAdvance(IController controller, bool render, bool renderSound)
		{
			using (Api.EnterExit())
			{
				FrameAdvancePre(controller, render, renderSound);

				bool resetSignal = controller.IsPressed("Reset");
				if (resetSignal)
				{
					Api.core.snes_reset();
				}

				bool powerSignal = controller.IsPressed("Power");
				if (powerSignal)
				{
					Api.core.snes_power();
				}

				IsLagFrame = true;
				// run the core for one frame
				Api.core.snes_run(false);
				AdvanceRtc();
				FrameAdvancePost();

				return true;
			}
		}

		internal void FrameAdvancePre(IController controller, bool render, bool renderSound)
		{
			_controller = controller;

			Api.core.snes_set_hooks_enabled(MemoryCallbacks.HasReads, MemoryCallbacks.HasWrites, MemoryCallbacks.HasExecutes);
			Api.core.snes_set_trace_enabled(_tracer.IsEnabled());
			Api.core.snes_set_video_enabled(render);
			Api.core.snes_set_audio_enabled(renderSound);
		}

		internal void FrameAdvancePost()
		{
			int numSamples = UpdateAudioBuffer();
			_soundProvider.PutSamples(_audioBuffer, numSamples / 2);
			Frame++;

			if (IsLagFrame)
			{
				LagCount++;
			}
		}

		private int UpdateAudioBuffer()
		{
			var rawAudioBuffer = Api.core.snes_get_audiobuffer_and_size(out var size);
			if (size == 0) return 0;
			if (size > _audioBuffer.Length)
				_audioBuffer = new short[size];
			Marshal.Copy(rawAudioBuffer, _audioBuffer, 0, size);

			return size;
		}

		public int Frame { get; private set; }

		public string SystemId { get; }

		public bool DeterministicEmulation { get; }

		public void ResetCounters()
		{
			Frame = 0;
			LagCount = 0;
			IsLagFrame = false;
		}

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			Api.Dispose();
			_currentMsuTrack?.Dispose();

			_disposed = true;
		}
	}
}
