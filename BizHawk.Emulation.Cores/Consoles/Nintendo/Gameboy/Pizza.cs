using BizHawk.Common;
using BizHawk.Common.BizInvoke;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Waterbox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.Gameboy
{
	[CoreAttributes("Pizza Boy", "Davide Berra", true, false, "c7bc6ee376028b3766de8d7a02e60ab794841f45",
		"https://github.com/davideberra/emu-pizza/", false)]
	public class Pizza : IEmulator, IInputPollable, IVideoProvider, IGameboyCommon, ISoundProvider
	{
		private LibPizza _pizza;
		private PeRunner _exe;
		private bool _disposed;

		[CoreConstructor("GB")]
		public Pizza(byte[] rom, CoreComm comm)
		{
			CoreComm = comm;
			ServiceProvider = new BasicServiceProvider(this);

			_exe = new PeRunner(new PeRunnerOptions
			{
				Filename = "pizza.wbx",
				Path = comm.CoreFileProvider.DllPath(),
				SbrkHeapSizeKB = 2 * 1024,
				InvisibleHeapSizeKB = 16 * 1024,
				SealedHeapSizeKB = 16 * 1024,
				PlainHeapSizeKB = 16 * 1024,
				MmapHeapSizeKB = 32 * 1024
			});
			_pizza = BizInvoker.GetInvoker<LibPizza>(_exe, _exe);
			if (!_pizza.Init(rom, rom.Length))
			{
				throw new InvalidOperationException("Core rejected the rom!");
			}
		}

		/// <summary>
		/// the nominal length of one frame
		/// </summary>
		private const int TICKSPERFRAME = 35112;

		/// <summary>
		/// number of ticks per second (GB, CGB)
		/// </summary>
		private const int TICKSPERSECOND = 2097152;

		/// <summary>
		/// number of ticks per second (SGB)
		/// </summary>
		private const int TICKSPERSECOND_SGB = 2147727;

		private int _tickOverflow = 0;

		private static LibPizza.Buttons GetButtons(IController c)
		{
			LibPizza.Buttons b = 0;
			if (c.IsPressed("Up"))
				b |= LibPizza.Buttons.UP;
			if (c.IsPressed("Down"))
				b |= LibPizza.Buttons.DOWN;
			if (c.IsPressed("Left"))
				b |= LibPizza.Buttons.LEFT;
			if (c.IsPressed("Right"))
				b |= LibPizza.Buttons.RIGHT;
			if (c.IsPressed("A"))
				b |= LibPizza.Buttons.A;
			if (c.IsPressed("B"))
				b |= LibPizza.Buttons.B;
			if (c.IsPressed("Select"))
				b |= LibPizza.Buttons.SELECT;
			if (c.IsPressed("Start"))
				b |= LibPizza.Buttons.START;
			return b;
		}

		public unsafe void FrameAdvance(IController controller, bool render, bool rendersound = true)
		{
			fixed (int* vp = _videoBuffer)
			fixed (short* sp = _soundBuffer)
			{
				var targetClocks = TICKSPERFRAME - _tickOverflow;

				var frame = new LibPizza.FrameInfo
				{
					VideoBuffer = (IntPtr)vp,
					SoundBuffer = (IntPtr)sp,
					Clocks = targetClocks,
					Keys = GetButtons(controller)
				};

				_pizza.FrameAdvance(frame);
				_tickOverflow = frame.Clocks - targetClocks;
				_numSamples = frame.Samples;
				Frame++;
			}
		}

		public void Dispose()
		{
			if (!_disposed)
			{
				_exe.Dispose();
				_exe = null;
				_pizza = null;
				_disposed = true;
			}
		}

		public bool IsCGBMode() => _pizza.IsCGB();
		public ControllerDefinition ControllerDefinition => BizHawk.Emulation.Cores.Nintendo.Gameboy.Gameboy.GbController;
		public int Frame { get; private set; }
		public int LagCount { get; set; }
		public bool IsLagFrame { get; set; }
		public IInputCallbackSystem InputCallbacks { get; } = new InputCallbackSystem();

		public void ResetCounters()
		{
			Frame = 0;
		}

		public IEmulatorServiceProvider ServiceProvider { get; private set; }
		public string SystemId { get { return "GB"; } }
		public bool DeterministicEmulation { get; private set; }
		public CoreComm CoreComm { get; }

		#region ISoundProvider

		private short[] _soundBuffer = new short[2048];
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

		#region IVideoProvider

		private int[] _videoBuffer = new int[160 * 144];

		public int[] GetVideoBuffer()
		{
			return _videoBuffer;
		}

		public int VirtualWidth => 160;
		public int VirtualHeight => 144;
		public int BufferWidth => 160;
		public int BufferHeight => 144;
		public int VsyncNumerator { get; private set; } = TICKSPERSECOND;
		public int VsyncDenominator { get; private set; } = TICKSPERFRAME;
		public int BackgroundColor => unchecked((int)0xff000000);

		#endregion

	}
}
