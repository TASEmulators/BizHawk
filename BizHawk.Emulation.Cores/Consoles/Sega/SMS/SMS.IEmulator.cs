using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Sega.MasterSystem
{
	public sealed partial class SMS : IEmulator
	{
		public IEmulatorServiceProvider ServiceProvider { get; private set; }

		public ISoundProvider SoundProvider
		{
			get { return ActiveSoundProvider; }
		}

		public ISyncSoundProvider SyncSoundProvider
		{
			get { return new FakeSyncSound(ActiveSoundProvider, 735); }
		}

		public bool StartAsyncSound()
		{
			return true;
		}

		public void EndAsyncSound() { }

		public ControllerDefinition ControllerDefinition
		{
			get
			{
				if (IsGameGear)
				{
					return GGController;
				}

				return SmsController;
			}
		}

		public IController Controller { get; set; }

		public void FrameAdvance(bool render, bool rendersound)
		{
			_lagged = true;
			Frame++;
			PSG.BeginFrame(Cpu.TotalExecutedCycles);
			Cpu.Debug = Tracer.Enabled;
			if (!IsGameGear)
			{
				PSG.StereoPanning = Settings.ForceStereoSeparation ? ForceStereoByte : (byte)0xFF;
			}

			if (Cpu.Debug && Cpu.Logger == null) // TODO, lets not do this on each frame. But lets refactor CoreComm/CoreComm first
			{
				Cpu.Logger = (s) => Tracer.Put(s);
			}

			if (IsGameGear == false)
			{
				Cpu.NonMaskableInterrupt = Controller["Pause"];
			}

			if (IsGame3D && Settings.Fix3D)
			{
				Vdp.ExecFrame((Frame & 1) == 0);
			}
			else
			{
				Vdp.ExecFrame(render);
			}

			PSG.EndFrame(Cpu.TotalExecutedCycles);
			if (_lagged)
			{
				_lagCount++;
				_isLag = true;
			}
			else
			{
				_isLag = false;
			}
		}

		public string SystemId { get { return "SMS"; } }

		public bool DeterministicEmulation { get { return true; } }

		public string BoardName { get { return null; } }

		public void ResetCounters()
		{
			Frame = 0;
			_lagCount = 0;
			_isLag = false;
		}

		public CoreComm CoreComm { get; private set; }

		public void Dispose() { }
	}
}
