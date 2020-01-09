using BizHawk.Emulation.Common;
using System;

namespace BizHawk.Emulation.Cores.Computers.MSX
{
	public partial class MSX : IEmulator, ISoundProvider
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
			_lagged = true;
			_frame++;

			if (Tracer.Enabled)
			{

			}
			else
			{

			}

			if (_lagged)
			{
				_lagCount++;
				_isLag = true;
			}
			else
			{
				_isLag = false;
			}

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

	}
}
