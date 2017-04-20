using System.IO;

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.SNES
{
	public partial class LibsnesCore : IEmulator
	{
		public IEmulatorServiceProvider ServiceProvider { get; }

		public ControllerDefinition ControllerDefinition => _controllerDeck.Definition;

		public IController Controller { get; set; }

		public void FrameAdvance(bool render, bool rendersound)
		{
			/* if the input poll callback is called, it will set this to false
			 * this has to be done before we save the per-frame state in deterministic
			 * mode, because in there, the core actually advances, and might advance
			 * through the point in time where IsLagFrame gets set to false.  makes sense?
			 */

			IsLagFrame = true;

			if (!_nocallbacks && _tracer.Enabled)
			{
				Api.QUERY_set_trace_callback(_tracecb);
			}
			else
			{
				Api.QUERY_set_trace_callback(null);
			}

			// for deterministic emulation, save the state we're going to use before frame advance
			// don't do this during nocallbacks though, since it's already been done
			if (!_nocallbacks && DeterministicEmulation)
			{
				var ms = new MemoryStream();
				var bw = new BinaryWriter(ms);
				bw.Write(CoreSaveState());
				bw.Write(false); // not framezero
				var ssc = new SaveController();
				ssc.CopyFrom(Controller);
				ssc.Serialize(bw);
				bw.Close();
				_savestatebuff = ms.ToArray();
			}

			// speedup when sound rendering is not needed
			Api.QUERY_set_audio_sample(rendersound ? _soundcb : null);

			bool resetSignal = Controller.IsPressed("Reset");
			if (resetSignal)
			{
				Api.CMD_reset();
			}

			bool powerSignal = Controller.IsPressed("Power");
			if (powerSignal)
			{
				Api.CMD_power();
			}

			var enables = new LibsnesApi.LayerEnables
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

			Api.SetLayerEnables(ref enables);

			RefreshMemoryCallbacks(false);

			// apparently this is one frame?
			_timeFrameCounter++;
			Api.CMD_run();

			// once upon a time we forwarded messages frmo bsnes here, by checking for queued text messages, but I don't think it's needed any longer
			if (IsLagFrame)
			{
				LagCount++;
			}
		}

		public int Frame
		{
			get { return _timeFrameCounter; }
			private set { _timeFrameCounter = value; }
		}

		public string SystemId { get; }

		// adelikat: Nasty hack to force new business logic.  Compatibility (and Accuracy when fully supported) will ALWAYS be in deterministic mode,
		// a consequence is a permanent performance hit to the compatibility core
		// Perormance will NEVER be in deterministic mode (and the client side logic will prohibit movie recording on it)
		// feos: Nasty hack to a nasty hack. Allow user disable it with a strong warning.
		public bool DeterministicEmulation =>
			_settings.ForceDeterminism
			&& (CurrentProfile == "Compatibility" || CurrentProfile == "Accuracy");

		public string BoardName { get; }

		public void ResetCounters()
		{
			_timeFrameCounter = 0;
			LagCount = 0;
			IsLagFrame = false;
		}

		public CoreComm CoreComm { get; }

		public void Dispose()
		{
			if (_disposed)
			{
				return;
			}

			_disposed = true;

			Api.CMD_unload_cartridge();
			Api.CMD_term();

			_resampler.Dispose();
			Api.Dispose();

			_currCdl?.Unpin();
		}
	}
}
