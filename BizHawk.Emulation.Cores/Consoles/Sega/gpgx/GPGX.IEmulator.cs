using System;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Consoles.Sega.gpgx
{
	public partial class GPGX : IEmulator
	{
		public IEmulatorServiceProvider ServiceProvider { get; private set; }

		public ISoundProvider SoundProvider { get { return null; } }

		public ISyncSoundProvider SyncSoundProvider
		{
			get { return this; }
		}

		public bool StartAsyncSound()
		{
			return false;
		}

		public void EndAsyncSound() { }

		public ControllerDefinition ControllerDefinition { get; private set; }

		public IController Controller { get; set; }

		// TODO: use render and rendersound
		public void FrameAdvance(bool render, bool rendersound = true)
		{
			if (Controller["Reset"])
				LibGPGX.gpgx_reset(false);
			if (Controller["Power"])
				LibGPGX.gpgx_reset(true);

			// do we really have to get each time?  nothing has changed
			if (!LibGPGX.gpgx_get_control(input, inputsize))
				throw new Exception("gpgx_get_control() failed!");

			ControlConverter.ScreenWidth = vwidth;
			ControlConverter.ScreenHeight = vheight;
			ControlConverter.Convert(Controller, input);

			if (!LibGPGX.gpgx_put_control(input, inputsize))
				throw new Exception("gpgx_put_control() failed!");

			IsLagFrame = true;
			Frame++;
			_drivelight = false;

			if (Tracer.Enabled)
				LibGPGX.gpgx_set_trace_callback(_tracecb);
			else
				LibGPGX.gpgx_set_trace_callback(null);

			LibGPGX.gpgx_advance();
			UpdateVideo();
			update_audio();

			if (IsLagFrame)
				LagCount++;

			if (CD != null)
				DriveLightOn = _drivelight;
		}

		public int Frame { get; private set; }

		public string SystemId
		{
			get { return "GEN"; }
		}

		public bool DeterministicEmulation
		{
			get { return true; }
		}

		public string BoardName
		{
			get { return null; }
		}

		public void ResetCounters()
		{
			Frame = 0;
			IsLagFrame = false;
			LagCount = 0;
		}

		public CoreComm CoreComm { get; private set; }

		public void Dispose()
		{
			if (!disposed)
			{
				if (AttachedCore != this)
					throw new Exception();
				if (SaveRamModified)
					_disposedSaveRam = CloneSaveRam();
				KillMemCallbacks();
				if (CD != null)
				{
					CD.Dispose();
				}
				AttachedCore = null;
				disposed = true;
			}
		}
	}
}
