using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.SNES
{
	public partial class BsnesCore : IEmulator
	{
		public IEmulatorServiceProvider ServiceProvider { get; }

		public ControllerDefinition ControllerDefinition => _controllers.Definition;

		public bool FrameAdvance(IController controller, bool render, bool renderSound)
		{
			_controller = controller;

			/* if the input poll callback is called, it will set this to false
			 * this has to be done before we save the per-frame state in deterministic
			 * mode, because in there, the core actually advances, and might advance
			 * through the point in time where IsLagFrame gets set to false.  makes sense?
			 */
			IsLagFrame = true;

			bool resetSignal = controller.IsPressed("Reset");
			if (resetSignal)
			{
				Api._core.snes_reset();
			}

			bool powerSignal = controller.IsPressed("Power");
			if (powerSignal)
			{
				Api._core.snes_power();
			}

			var enables = new BsnesApi.LayerEnables
			{
				BG1_Prio0 = _settings.ShowBG1_0,
				BG1_Prio1 = _settings.ShowBG1_1,
				BG2_Prio0 = _settings.ShowBG2_0,
				BG2_Prio1 = _settings.ShowBG2_1,
				BG3_Prio0 = _settings.ShowBG3_0,
				BG3_Prio1 = _settings.ShowBG3_1,
				BG4_Prio0 = _settings.ShowBG4_0,
				BG4_Prio1 = _settings.ShowBG4_1,
				Obj_Prio0 = _settings.ShowOBJ_0,
				Obj_Prio1 = _settings.ShowOBJ_1,
				Obj_Prio2 = _settings.ShowOBJ_2,
				Obj_Prio3 = _settings.ShowOBJ_3
			};
			// TODO: I really don't think stuff like this should be set every single frame (only on change)
			Api._core.snes_set_layer_enables(enables);
			Api._core.snes_set_trace_enabled(_tracer.Enabled);
			Api._core.snes_set_video_enabled(render);
			Api._core.snes_set_audio_enabled(renderSound);

			// run the core for one frame
			Frame++;
			Api._core.snes_run();

			// once upon a time we forwarded messages from bsnes here, by checking for queued text messages, but I don't think it's needed any longer
			if (IsLagFrame)
			{
				LagCount++;
			}

			return true;
		}

		public int Frame { get; private set; }

		public string SystemId { get; }
		public bool DeterministicEmulation => true;

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
			_resampler.Dispose();

			_disposed = true;
		}
	}
}
