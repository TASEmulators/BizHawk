using BizHawk.Emulation.Common;
using System;

namespace BizHawk.Emulation.Cores.Computers.MSX
{
	public partial class MSX : IEmulator, ISoundProvider, IVideoProvider
	{
		public IEmulatorServiceProvider ServiceProvider { get; }

		public ControllerDefinition ControllerDefinition
		{
			get
			{
				return GGController;
			}
		}

		// not savestated variables
		int s_L, s_R;

		public bool FrameAdvance(IController controller, bool render, bool rendersound)
		{
			_controller = controller;

			_frame++;
			
			if (Tracer.Enabled)
			{
				tracecb = MakeTrace;
			}
			else
			{
				tracecb = null;
			}

			LibMSX.MSX_settracecallback(MSX_Pntr, tracecb);

			LibMSX.MSX_frame_advance(MSX_Pntr, 0, 0, true, true);

			return true;
		}

		public int Frame => _frame;

		public string SystemId => "MSX";

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
			if (MSX_Pntr != IntPtr.Zero)
			{
				LibMSX.MSX_destroy(MSX_Pntr);
				MSX_Pntr = IntPtr.Zero;
			}

			if (blip_L != null)
			{
				blip_L.Dispose();
				blip_L = null;
			}

			if (blip_R != null)
			{
				blip_R.Dispose();
				blip_R = null;
			}
		}

		#region Audio

		public BlipBuffer blip_L = new BlipBuffer(4096);
		public BlipBuffer blip_R = new BlipBuffer(4096);
		const int blipbuffsize = 4096;

		public uint sampleclock;
		public int old_s_L = 0;
		public int old_s_R = 0;

		public bool CanProvideAsync { get { return false; } }

		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode != SyncSoundMode.Sync)
			{
				throw new NotSupportedException("Only sync mode is supported");
			}
		}

		public void GetSamplesAsync(short[] samples)
		{
			throw new NotSupportedException("Async not supported");
		}

		public SyncSoundMode SyncMode
		{
			get { return SyncSoundMode.Sync; }
		}

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			blip_L.EndFrame(sampleclock);
			blip_R.EndFrame(sampleclock);

			nsamp = Math.Max(Math.Max(blip_L.SamplesAvailable(), blip_R.SamplesAvailable()), 1);
			samples = new short[nsamp * 2];

			blip_L.ReadSamplesLeft(samples, nsamp);
			blip_R.ReadSamplesRight(samples, nsamp);

			sampleclock = 0;
		}

		public void DiscardSamples()
		{
			blip_L.Clear();
			blip_R.Clear();
			sampleclock = 0;
		}

		#endregion

		#region Video
		public int _frameHz = 60;

		public int[] _vidbuffer = new int[160 * 144];

		public int[] GetVideoBuffer()
		{
			LibMSX.MSX_get_video(MSX_Pntr, _vidbuffer);
			return _vidbuffer;
		}

		public int VirtualWidth => 160;
		public int VirtualHeight => 144;
		public int BufferWidth => 160;
		public int BufferHeight => 144;
		public int BackgroundColor => unchecked((int)0xFF000000);
		public int VsyncNumerator => _frameHz;
		public int VsyncDenominator => 1;

		#endregion
	}
}
