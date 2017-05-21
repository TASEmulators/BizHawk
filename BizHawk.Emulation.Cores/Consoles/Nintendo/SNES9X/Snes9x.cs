using System;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Waterbox;
using BizHawk.Common.BizInvoke;

namespace BizHawk.Emulation.Cores.Nintendo.SNES9X
{
	[CoreAttributes("Snes9x", "FIXME", true, false, "5e0319ab3ef9611250efb18255186d0dc0d7e125", "https://github.com/snes9xgit/snes9x", false)]
	[ServiceNotApplicable(typeof(IDriveLight))]
	public class Snes9x : IEmulator, IVideoProvider, ISoundProvider
	{
		private LibSnes9x _core;
		private PeRunner _exe;


		[CoreConstructor("SNES")]
		public Snes9x(CoreComm comm, byte[] rom)
		{
			ServiceProvider = new BasicServiceProvider(this);
			CoreComm = comm;

			_exe = new PeRunner(comm.CoreFileProvider.DllPath(), "snes9x.exe", 20 * 1024 * 1024, 65536, 65536);
			try
			{
				_core = BizInvoker.GetInvoker<LibSnes9x>(_exe, _exe);
				//Console.WriteLine(_exe.Resolve("biz_init"));
				//System.Diagnostics.Debugger.Break();
				if (!_core.biz_init())
				{
					throw new InvalidOperationException("Init() failed");
				}
				if (!_core.biz_load_rom(rom, rom.Length))
				{
					throw new InvalidOperationException("LoadRom() failed");
				}
			}
			catch
			{
				Dispose();
				throw;
			}
		}

		#region controller

		public ControllerDefinition ControllerDefinition
		{
			get { return NullController.Instance.Definition; }
		}

		#endregion

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

		public void FrameAdvance(IController controller, bool render, bool rendersound = true)
		{
			Frame++;
			LibSnes9x.frame_info frame = new LibSnes9x.frame_info();

			_core.biz_run(frame);
			Blit(frame);
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

		private unsafe void Blit(LibSnes9x.frame_info frame)
		{
			BufferWidth = frame.width;
			BufferHeight = frame.height;

			int vinc = frame.pitch - frame.width;

			ushort* src = (ushort*)frame.ptr;
			fixed (int* _dst = _vbuff)
			{
				byte* dst = (byte*)_dst;

				for (int j = 0; j < frame.height; j++)
				{
					for (int i = 0; i < frame.width; i++)
					{
						var c = *src++;

						*dst++ = (byte)(c << 3 & 0xf8 | c >> 2 & 7);
						*dst++ = (byte)(c >> 3 & 0xfa | c >> 9 & 3);
						*dst++ = (byte)(c >> 8 & 0xf8 | c >> 13 & 7);
						*dst++ = 0xff;
					}
					src += vinc;
				}
			}
		}

		private int[] _vbuff = new int[512 * 480];
		public int[] GetVideoBuffer() { return _vbuff; }
		public int VirtualWidth
		{ get { return (int)(BufferWidth * 1.146); ; } }
		public int VirtualHeight { get { return BufferHeight; } }
		public int BufferWidth { get; private set; } = 256;
		public int BufferHeight { get; private set; } = 224;
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
