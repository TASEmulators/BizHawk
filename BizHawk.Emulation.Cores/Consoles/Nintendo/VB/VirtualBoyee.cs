using BizHawk.Common.BizInvoke;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Waterbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.VB
{
	[CoreAttributes("Virtual Boyee", "???", true, false, "0.9.44.1", 
		"https://mednafen.github.io/releases/", false)]
	public class VirtualBoyee : IEmulator, IVideoProvider, ISoundProvider
	{
		private PeRunner _exe;
		private LibVirtualBoyee _boyee;

		[CoreConstructor("VB")]
		public VirtualBoyee(CoreComm comm, byte[] rom)
		{
			ServiceProvider = new BasicServiceProvider(this);
			CoreComm = comm;

			_exe = new PeRunner(new PeRunnerOptions
			{
				Path = comm.CoreFileProvider.DllPath(),
				Filename = "vb.wbx",
				NormalHeapSizeKB = 4 * 1024,
				SealedHeapSizeKB = 12 * 1024,
				InvisibleHeapSizeKB = 6 * 1024,
				SpecialHeapSizeKB = 64,
				MmapHeapSizeKB = 16 * 1024
			});

			_boyee = BizInvoker.GetInvoker<LibVirtualBoyee>(_exe, _exe);

			if (!_boyee.Load(rom, rom.Length))
			{
				throw new InvalidOperationException("Core rejected the rom");
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

		public IEmulatorServiceProvider ServiceProvider { get; private set; }

		public unsafe void FrameAdvance(IController controller, bool render, bool rendersound = true)
		{
			if (controller.IsPressed("Power"))
				_boyee.HardReset();

			fixed (int* vp = _videoBuffer)
			fixed (short* sp = _soundBuffer)
			{
				var spec = new LibVirtualBoyee.EmulateSpec
				{
					Pixels = (IntPtr)vp,
					SoundBuf = (IntPtr)sp,
					SoundBufMaxSize = _soundBuffer.Length / 2,
					Buttons = GetButtons(controller)
				};

				_boyee.Emulate(spec);
				BufferWidth = spec.DisplayRect.W;
				BufferHeight = spec.DisplayRect.H;
				_numSamples = spec.SoundBufSize;
			}

			Frame++;

			/*_core.biz_set_input_callback(InputCallbacks.Count > 0 ? _inputCallback : null);

			UpdateControls(controller);
			Frame++;
			LibSnes9x.frame_info frame = new LibSnes9x.frame_info();

			_core.biz_run(frame, _inputState);
			IsLagFrame = frame.padread == 0;
			if (IsLagFrame)
				LagCount++;*/
		}

		public int Frame { get; private set; }

		public void ResetCounters()
		{
			Frame = 0;
		}

		public string SystemId { get { return "VB"; } }
		public bool DeterministicEmulation { get { return true; } }
		public CoreComm CoreComm { get; private set; }

		#region Controller

		private LibVirtualBoyee.Buttons GetButtons(IController c)
		{
			var ret = 0;
			var val = 1;
			foreach (var s in CoreButtons)
			{
				if (c.IsPressed(s))
					ret |= val;
				val <<= 1;
			}
			return (LibVirtualBoyee.Buttons)ret;
		}

		private static readonly string[] CoreButtons =
		{
			"A", "B", "R", "L",
			"Up_R", "Right_R",
			"Right", "Left", "Down", "Up",
			"Start", "Select", "Left_R", "Down_R"
		};

		private static readonly ControllerDefinition VirtualBoyController = new ControllerDefinition
		{
			Name = "VirtualBoy Controller",
			BoolButtons = CoreButtons.Concat(new[] { "Power" }).ToList()
		};

		public ControllerDefinition ControllerDefinition => VirtualBoyController;

		#endregion

		#region IVideoProvider

		private int[] _videoBuffer = new int[1024 * 1024];

		public int[] GetVideoBuffer()
		{
			return _videoBuffer;
		}

		public int VirtualWidth => BufferWidth;
		public int VirtualHeight => BufferWidth;

		public int BufferWidth { get; private set; } = 384;
		public int BufferHeight { get; private set; } = 224;

		public int VsyncNumerator { get; private set; } = 20000000;

		public int VsyncDenominator { get; private set; } = 397824;

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
