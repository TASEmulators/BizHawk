using System;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.SNES9X
{
	[CoreAttributes("Snes9x", "FIXME", true, false, "5e0319ab3ef9611250efb18255186d0dc0d7e125", "https://github.com/snes9xgit/snes9x", true)]
	[ServiceNotApplicable(typeof(IDriveLight))]
	public class Snes9x : IEmulator, IVideoProvider, ISoundProvider
	{
		[CoreConstructor("SNES")]
		public Snes9x(CoreComm comm, byte[] rom)
		{
			if (!LibSnes9x.debug_init(rom, rom.Length))
				throw new Exception();

			ServiceProvider = new BasicServiceProvider(this);
			CoreComm = comm;
		}

		#region controller

		public ControllerDefinition ControllerDefinition
		{
			get { return NullController.Instance.Definition; }
		}

		#endregion

		public void Dispose()
		{
		}

		public IEmulatorServiceProvider ServiceProvider { get; private set; }

		public void FrameAdvance(IController controller, bool render, bool rendersound = true)
		{
			Frame++;

			LibSnes9x.debug_advance(_vbuff);
		}

		public int Frame { get; private set; }

		public void ResetCounters()
		{
			Frame = 0;
		}

		public string SystemId { get { return "SNES"; } }
		public bool DeterministicEmulation { get { return true; } }
		public CoreComm CoreComm { get; private set; }

		#region IVideoProvider

		private int[] _vbuff = new int[512 * 480];
		public int[] GetVideoBuffer() { return _vbuff; }
		public int VirtualWidth
		{ get { return (int)(BufferWidth * 1.146); ; } }
		public int VirtualHeight { get { return BufferHeight; } }
		public int BufferWidth { get { return 256; } }
		public int BufferHeight { get { return 224; } }
		public int BackgroundColor { get { return unchecked((int)0xff000000); } }

		public int VsyncNumerator
		{
			[FeatureNotImplemented]
			get
			{
				return NullVideo.DefaultVsyncNum;
			}
		}

		public int VsyncDenominator
		{
			[FeatureNotImplemented]
			get
			{
				return NullVideo.DefaultVsyncDen;
			}
		}

		#endregion

		#region ISoundProvider

		private short[] _sbuff = new short[2048];

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			samples = _sbuff;
			nsamp = 735;
		}

		public void DiscardSamples()
		{
			// Nothing to do
		}

		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode == SyncSoundMode.Async)
			{
				throw new NotSupportedException("Async mode is not supported.");
			}
		}

		public bool CanProvideAsync
		{
			get { return false; }
		}

		public SyncSoundMode SyncMode
		{
			get { return SyncSoundMode.Sync; }
		}

		public void GetSamplesAsync(short[] samples)
		{
			throw new InvalidOperationException("Async mode is not supported.");
		}

		#endregion
	}
}
