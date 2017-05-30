using BizHawk.Common.BizInvoke;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Waterbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Consoles.SNK
{
	[CoreAttributes("NeoPop", "Thomas Klausner", true, false, "0.9.44.1", 
		"https://mednafen.github.io/releases/", false)]
	public class NeoGeo : IEmulator, IVideoProvider, ISoundProvider
	{
		private PeRunner _exe;
		private LibNeoGeo _neopop;

		[CoreConstructor("NGP")]
		public NeoGeo(CoreComm comm, byte[] rom)
		{
			ServiceProvider = new BasicServiceProvider(this);
			CoreComm = comm;

			_exe = new PeRunner(new PeRunnerOptions
			{
				Path = comm.CoreFileProvider.DllPath(),
				Filename = "ngp.wbx",
				SbrkHeapSizeKB = 16 * 1024,
				SealedHeapSizeKB = 16 * 1024,
				InvisibleHeapSizeKB = 16 * 1024,
				PlainHeapSizeKB = 16 * 1024,
				MmapHeapSizeKB = 16 * 1024
			});

			_neopop = BizInvoker.GetInvoker<LibNeoGeo>(_exe, _exe);

			if (!_neopop.LoadSystem(rom, rom.Length, 1))
			{
				throw new InvalidOperationException("Core rejected the rom");
			}

			_exe.Seal();
		}

		public unsafe void FrameAdvance(IController controller, bool render, bool rendersound = true)
		{
			if (controller.IsPressed("Power"))
				_neopop.HardReset();

			fixed (int* vp = _videoBuffer)
			fixed (short* sp = _soundBuffer)
			{
				var spec = new LibNeoGeo.EmulateSpec
				{
					Pixels = (IntPtr)vp,
					SoundBuff = (IntPtr)sp,
					SoundBufMaxSize = _soundBuffer.Length / 2,
					Buttons = 0,
					SkipRendering = render ? 0 : 1
				};

				_neopop.FrameAdvance(spec);
				_numSamples = spec.SoundBufSize;

				Frame++;

				/*IsLagFrame = spec.Lagged;
				if (IsLagFrame)
					LagCount++;*/
			}
		}

		private bool _disposed = false;

		public void Dispose()
		{
			if (!_disposed)
			{
				_exe.Dispose();
				_exe = null;
				_disposed = true;
			}
		}

		public int Frame { get; private set; }

		public void ResetCounters()
		{
			Frame = 0;
		}

		public IEmulatorServiceProvider ServiceProvider { get; private set; }
		public string SystemId { get { return "NGP"; } }
		public bool DeterministicEmulation { get { return true; } }
		public CoreComm CoreComm { get; }

		public ControllerDefinition ControllerDefinition => NullController.Instance.Definition;

		#region IVideoProvider

		private int[] _videoBuffer = new int[160 * 152];

		public int[] GetVideoBuffer()
		{
			return _videoBuffer;
		}

		public int VirtualWidth => 160;
		public int VirtualHeight => 152;
		public int BufferWidth => 160;
		public int BufferHeight => 152;
		public int VsyncNumerator { get; private set; } = 6144000;
		public int VsyncDenominator { get; private set; } = 515 * 198;
		public int BackgroundColor => unchecked((int)0xff000000);

		#endregion

		#region ISoundProvider

		private short[] _soundBuffer = new short[16384];
		private int _numSamples;

		public void SetSyncMode(SyncSoundMode mode)
		{
			if (mode == SyncSoundMode.Async)
			{
				throw new NotSupportedException("Async mode is not supported.");
			}
		}

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			samples = _soundBuffer;
			nsamp = _numSamples;
		}

		public void GetSamplesAsync(short[] samples)
		{
			throw new InvalidOperationException("Async mode is not supported.");
		}

		public void DiscardSamples()
		{
		}

		public bool CanProvideAsync => false;

		public SyncSoundMode SyncMode => SyncSoundMode.Sync;

		#endregion
	}
}
