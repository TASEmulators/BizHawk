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
	public class VirtualBoyee : IEmulator, IVideoProvider
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
				NormalHeapSizeKB = 1024,
				SealedHeapSizeKB = 12 * 1024,
				InvisibleHeapSizeKB = 6 * 1024,
				SpecialHeapSizeKB = 64
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
			var scratch = new short[16384];

			fixed(int*vp = _videoBuffer)
				fixed(short*sp = scratch)
			{
				var spec = new LibVirtualBoyee.EmulateSpec
				{
					Pixels = (IntPtr)vp,
					SoundBuf = (IntPtr)sp,
					SoundBufMaxSize = 8192
				};

				_boyee.Emulate(spec);
				VirtualWidth = BufferWidth = spec.DisplayRect.W;
				VirtualWidth = BufferHeight = spec.DisplayRect.H;
				Console.WriteLine(spec.SoundBufSize);
			}

			Frame++;

			/*_core.biz_set_input_callback(InputCallbacks.Count > 0 ? _inputCallback : null);

			if (controller.IsPressed("Power"))
				_core.biz_hard_reset();
			else if (controller.IsPressed("Reset"))
				_core.biz_soft_reset();

			UpdateControls(controller);
			Frame++;
			LibSnes9x.frame_info frame = new LibSnes9x.frame_info();

			_core.biz_run(frame, _inputState);
			IsLagFrame = frame.padread == 0;
			if (IsLagFrame)
				LagCount++;
			using (_exe.EnterExit())
			{
				Blit(frame);
				Sblit(frame);
			}*/
		}

		public int Frame { get; private set; }

		public void ResetCounters()
		{
			Frame = 0;
		}

		public string SystemId { get { return "VB"; } }
		public bool DeterministicEmulation { get { return true; } }
		public CoreComm CoreComm { get; private set; }

		public ControllerDefinition ControllerDefinition => NullController.Instance.Definition;

		#region IVideoProvider

		private int[] _videoBuffer = new int[0];

		public int[] GetVideoBuffer()
		{
			throw new NotImplementedException();
		}

		public int VirtualWidth { get; private set; }
		public int VirtualHeight { get; private set; }

		public int BufferWidth { get; private set; }
		public int BufferHeight { get; private set; }

		public int VsyncNumerator { get; private set; }

		public int VsyncDenominator { get; private set; }

		public int BackgroundColor => unchecked((int)0xff000000);

		#endregion
	}
}
