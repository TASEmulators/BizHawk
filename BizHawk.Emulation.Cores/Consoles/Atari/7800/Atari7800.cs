using System;
using System.IO;

using BizHawk.Emulation.Common;
using EMU7800.Core;
using EMU7800.Win;

namespace BizHawk.Emulation.Cores.Atari.Atari7800
{
	[CoreAttributes(
		"EMU7800",
		"",
		isPorted: true,
		isReleased: true,
		portedVersion: "v1.5",
		portedUrl: "http://emu7800.sourceforge.net/")]
	[ServiceNotApplicable(typeof(ISettable<,>), typeof(IDriveLight))]
	public partial class Atari7800 : IEmulator, ISaveRam, IDebuggable, IStatable, IInputPollable, IRegionable
	{
		// TODO:
		// some things don't work when you try to plug in a 2600 game
		static Atari7800()
		{
			// add alpha bits to palette tables
			for (int i = 0; i < TIATables.NTSCPalette.Length; i++)
			{
				TIATables.NTSCPalette[i] |= unchecked((int)0xff000000);
			}

			for (int i = 0; i < TIATables.PALPalette.Length; i++)
			{
				TIATables.PALPalette[i] |= unchecked((int)0xff000000);
			}

			for (int i = 0; i < MariaTables.NTSCPalette.Length; i++)
			{
				MariaTables.NTSCPalette[i] |= unchecked((int)0xff000000);
			}

			for (int i = 0; i < MariaTables.PALPalette.Length; i++)
			{
				MariaTables.PALPalette[i] |= unchecked((int)0xff000000);
			}
		}

		public Atari7800(CoreComm comm, GameInfo game, byte[] rom, string gameDbFn)
		{
			var ser = new BasicServiceProvider(this);
			ser.Register<IVideoProvider>(_avProvider);
			ser.Register<ISoundProvider>(_avProvider);
			ServiceProvider = ser;

			CoreComm = comm;
			byte[] highscoreBios = comm.CoreFileProvider.GetFirmware("A78", "Bios_HSC", false, "Some functions may not work without the high score BIOS.");
			byte[] palBios = comm.CoreFileProvider.GetFirmware("A78", "Bios_PAL", false, "The game will not run if the correct region BIOS is not available.");
			byte[] ntscBios = comm.CoreFileProvider.GetFirmware("A78", "Bios_NTSC", false, "The game will not run if the correct region BIOS is not available.");

			if (GameProgramLibrary.EMU7800DB == null)
			{
				GameProgramLibrary.EMU7800DB = new GameProgramLibrary(new StreamReader(gameDbFn));
			}

			if (rom.Length % 1024 == 128)
			{
				Console.WriteLine("Trimming 128 byte .a78 header...");
				byte[] newrom = new byte[rom.Length - 128];
				Buffer.BlockCopy(rom, 128, newrom, 0, newrom.Length);
				rom = newrom;
			}

			_gameInfo = GameProgramLibrary.EMU7800DB.TryRecognizeRom(rom);
			CoreComm.RomStatusDetails = _gameInfo.ToString();
			Console.WriteLine("Rom Determiniation from 7800DB:");
			Console.WriteLine(_gameInfo.ToString());

			_rom = rom;
			_hsbios = highscoreBios;
			_bios = _gameInfo.MachineType == MachineType.A7800PAL ? palBios : ntscBios;
			_pal = _gameInfo.MachineType == MachineType.A7800PAL || _gameInfo.MachineType == MachineType.A2600PAL;

			if (_bios == null)
			{
				throw new MissingFirmwareException("The BIOS corresponding to the region of the game you loaded is required to run Atari 7800 games.");
			}

			HardReset();
		}

		public DisplayType Region => _pal ? DisplayType.PAL : DisplayType.NTSC;

		public Atari7800Control ControlAdapter { get; private set; }

		private readonly byte[] _rom;
		private readonly byte[] _hsbios;
		private readonly byte[] _bios;
		private readonly GameProgram _gameInfo;
		private readonly byte[] _hsram = new byte[2048];
		private readonly bool _pal;

		private Cart _cart;
		private MachineBase _theMachine;
		private int _frame = 0;

		private class ConsoleLogger : ILogger
		{
			public void WriteLine(string format, params object[] args)
			{
				Console.WriteLine(format, args);
			}

			public void WriteLine(object value)
			{
				Console.WriteLine(value);
			}

			public void Write(string format, params object[] args)
			{
				Console.Write(format, args);
			}

			public void Write(object value)
			{
				Console.Write(value);
			}
		}

		private void HardReset()
		{
			_cart = Cart.Create(_rom, _gameInfo.CartType);
			ILogger logger = new ConsoleLogger();

			HSC7800 hsc7800 = null;
			if (_hsbios != null)
			{
				hsc7800 = new HSC7800(_hsbios, _hsram);
			}

			Bios7800 bios7800 = new Bios7800(_bios);
			_theMachine = MachineBase.Create(
				_gameInfo.MachineType,
				_cart,
				bios7800,
				hsc7800,
				_gameInfo.LController,
				_gameInfo.RController,
				logger);

			_theMachine.Reset();
			_theMachine.InputState.InputPollCallback = InputCallbacks.Call;

			ControlAdapter = new Atari7800Control(_theMachine);
			ControllerDefinition = ControlAdapter.ControlType;

			_avProvider.ConnectToMachine(_theMachine, _gameInfo);

			SetupMemoryDomains(hsc7800);
		}

		#region audio\video

		private MyAVProvider _avProvider = new MyAVProvider();

		private class MyAVProvider : IVideoProvider, ISoundProvider, IDisposable
		{
			// to sync exactly with audio as this emulator creates and times it, the frame rate should be exactly 60:1 or 50:1
			private int _frameHz;

			public FrameBuffer Framebuffer { get; private set; }
			public void ConnectToMachine(MachineBase m, GameProgram g)
			{
				_frameHz = m.FrameHZ;
				Framebuffer = m.CreateFrameBuffer();
				BufferWidth = Framebuffer.VisiblePitch;
				BufferHeight = Framebuffer.Scanlines;
				_vidbuffer = new int[BufferWidth * BufferHeight];

				uint newsamplerate = (uint)m.SoundSampleFrequency;
				if (newsamplerate != _samplerate)
				{
					// really shouldn't happen (after init), but if it does, we're ready
					_resampler?.Dispose();
					_resampler = new SpeexResampler((SpeexResampler.Quality)3, newsamplerate, 44100, newsamplerate, 44100, null, null);
					_samplerate = newsamplerate;
					_dcfilter = new DCFilter(256);
				}

				if (g.MachineType == MachineType.A2600PAL)
				{
					_palette = TIATables.PALPalette;
				}
				else if (g.MachineType == MachineType.A7800PAL)
				{
					_palette = MariaTables.PALPalette;
				}
				else if (g.MachineType == MachineType.A2600NTSC)
				{
					_palette = TIATables.NTSCPalette;
				}
				else
				{
					_palette = MariaTables.NTSCPalette;
				}
			}

			private uint _samplerate;
			private int[] _vidbuffer;
			private SpeexResampler _resampler;
			private DCFilter _dcfilter;
			private int[] _palette;

			public void FillFrameBuffer()
			{
				unsafe
				{
					fixed (byte* src_ = Framebuffer.VideoBuffer)
					fixed (int* dst_ = _vidbuffer)
					fixed (int* pal = _palette)
					{
						byte* src = src_;
						int* dst = dst_;
						for (int i = 0; i < _vidbuffer.Length; i++)
						{
							*dst++ = pal[*src++];
						}
					}
				}
			}

			public int[] GetVideoBuffer()
			{
				return _vidbuffer;
			}

			public int VirtualWidth => 275;
			public int VirtualHeight => BufferHeight;
			public int BufferWidth { get; private set; }
			public int BufferHeight { get; private set; }
			public int BackgroundColor => unchecked((int)0xff000000);
			public int VsyncNumerator => _frameHz;
			public int VsyncDenominator => 1;

			#region ISoundProvider

			public bool CanProvideAsync => false;

			public void GetSamplesSync(out short[] samples, out int nsamp)
			{
				int nsampin = Framebuffer.SoundBufferByteLength;
				unsafe
				{
					fixed (byte* src = Framebuffer.SoundBuffer)
					{
						for (int i = 0; i < nsampin; i++)
						{
							// the buffer values don't really get very large at all,
							// so this doesn't overflow
							short s = (short)(src[i] * 200);
							_resampler.EnqueueSample(s, s);
						}
					}
				}

				_resampler.GetSamplesSync(out samples, out nsamp);
				_dcfilter.PushThroughSamples(samples, nsamp * 2);
			}

			public SyncSoundMode SyncMode => SyncSoundMode.Sync;

			public void SetSyncMode(SyncSoundMode mode)
			{
				if (mode == SyncSoundMode.Async)
				{
					throw new NotSupportedException("Async mode is not supported.");
				}
			}

			public void GetSamplesAsync(short[] samples)
			{
				throw new InvalidOperationException("Async mode is not supported.");
			}

			public void DiscardSamples()
			{
				_resampler?.DiscardSamples();
			}

			#endregion

			public void Dispose()
			{
				if (_resampler != null)
				{
					_resampler.Dispose();
					_resampler = null;
				}
			}
		}

		#endregion
	}
}
