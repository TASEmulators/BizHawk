using System;
using System.IO;
using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.W65816;

// http://wiki.superfamicom.org/snes/show/Backgrounds

namespace BizHawk.Emulation.Cores.Nintendo.BSNES
{
	[PortedCore(CoreNames.Bsnes115, "bsnes team", "v115+", "https://github.com/bsnes-emu/bsnes")]
	[ServiceNotApplicable(new[] { typeof(IDriveLight) })]
	public unsafe partial class BsnesCore : IEmulator, IDebuggable, IVideoProvider, ISaveRam, IStatable, IInputPollable, IRegionable, ISettable<BsnesCore.SnesSettings, BsnesCore.SnesSyncSettings>
	{
		[CoreConstructor(VSystemID.Raw.SGB)]
		[CoreConstructor(VSystemID.Raw.SNES)]
		public BsnesCore(CoreLoadParameters<SnesSettings, SnesSyncSettings> loadParameters)
		{
			var ser = new BasicServiceProvider(this);
			ServiceProvider = ser;

			this._romPath = Path.ChangeExtension(loadParameters.Roms[0].RomPath, null);
			CoreComm = loadParameters.Comm;
			_settings = loadParameters.Settings ?? new SnesSettings();
			_syncSettings = loadParameters.SyncSettings ?? new SnesSyncSettings();

			IsSGB = loadParameters.Game.System == VSystemID.Raw.SGB;
			byte[] sgbRomData = null;
			if (IsSGB)
			{
				if ((loadParameters.Roms[0].RomData[0x143] & 0xc0) == 0xc0)
				{
					throw new CGBNotSupportedException();
				}

				sgbRomData = _syncSettings.UseSGB2
					? CoreComm.CoreFileProvider.GetFirmwareOrThrow(new("SNES", "Rom_SGB2"), "SGB2 Rom is required for SGB2 emulation.")
					: CoreComm.CoreFileProvider.GetFirmwareOrThrow(new("SNES", "Rom_SGB"), "SGB1 Rom is required for SGB1 emulation.");

				loadParameters.Game.FirmwareHash = SHA1Checksum.ComputeDigestHex(sgbRomData);
			}

			BsnesApi.SnesCallbacks callbacks = new()
			{
				inputPollCb = snes_input_poll,
				noLagCb = snes_no_lag,
				controllerLatchCb = snes_controller_latch,
				videoFrameCb = snes_video_refresh,
				audioSampleCb = snes_audio_sample,
				pathRequestCb = snes_path_request,
				traceCb = snes_trace,
				readHookCb = ReadHook,
				writeHookCb = WriteHook,
				execHookCb = ExecHook,
				msuOpenCb = msu_open,
				msuSeekCb = msu_seek,
				msuReadCb = msu_read,
				msuEndCb = msu_end
			};

			Api = new BsnesApi(CoreComm.CoreFileProvider.DllPath(), CoreComm, callbacks.AllDelegatesInMemoryOrder());

			_controllers = new BsnesControllers(_syncSettings);

			generate_palette();
			BsnesApi.SnesInitData snesInitData = new()
			{
				entropy = _syncSettings.Entropy,
				left_port = _syncSettings.LeftPort,
				right_port = _syncSettings.RightPort,
				hotfixes = _syncSettings.Hotfixes,
				fast_ppu = _syncSettings.FastPPU,
				fast_dsp = _syncSettings.FastDSP,
				fast_coprocessors = _syncSettings.FastCoprocessors,
				region_override = _syncSettings.RegionOverride,
			};
			Api.core.snes_init(ref snesInitData);
			Api.SetCallbacks(callbacks);

			// start up audio resampler
			InitAudio();
			ser.Register<ISoundProvider>(_resampler);

			if (IsSGB)
			{
				ser.Register<IBoardInfo>(new SGBBoardInfo());

				Api.core.snes_load_cartridge_super_gameboy(sgbRomData, loadParameters.Roms[0].RomData,
					sgbRomData!.Length, loadParameters.Roms[0].RomData.Length);
			}
			else
			{
				Api.core.snes_load_cartridge_normal(loadParameters.Roms[0].RomData, loadParameters.Roms[0].RomData.Length);
			}

			_region = Api.core.snes_get_region();

			if (_region == BsnesApi.SNES_REGION.NTSC)
			{
				// taken from bsnes source
				VsyncNumerator = 21477272;
				VsyncDenominator = 357366;
			}
			else
			{
				// http://forums.nesdev.com/viewtopic.php?t=5367&start=19
				VsyncNumerator = 21281370;
				VsyncDenominator = 4 * 341 * 312;
			}

			SetMemoryDomains();

			const string TRACE_HEADER = "65816: PC, mnemonic, operands, registers (A, X, Y, S, D, B, flags (NVMXDIZC), V, H)";
			_tracer = new TraceBuffer(TRACE_HEADER);
			ser.Register<IDisassemblable>(new W65816_DisassemblerService());
			ser.Register(_tracer);

			Api.Seal();
		}

		private CoreComm CoreComm { get; }

		private readonly BsnesControllers _controllers;
		private readonly ITraceable _tracer;

		private IController _controller;
		private SpeexResampler _resampler;
		private readonly string _romPath;
		private bool _disposed;

		public bool IsSGB { get; }

		private class SGBBoardInfo : IBoardInfo
		{
			public string BoardName => "SGB";
		}

		private BsnesApi Api { get; }

		private string snes_path_request(int slot, string hint, bool required)
		{
			switch (hint)
			{
				case "manifest.bml":
					Api.AddReadonlyFile($"{_romPath}.bml", hint);
					return hint;
				case "msu1/data.rom":
					Api.AddReadonlyFile($"{_romPath}.msu", hint);
					return hint;
				case "save.ram":
					// core asked for saveram, but the interface isn't designed to be able to handle this.
					// so, we'll just return nothing and the frontend will set the saveram itself later
					return null;
			}

			string firmwareId;
			string firmwareSystem = "SNES";

			switch (hint)
			{
				case "cx4": firmwareId = "CX4"; break;
				case "dsp1": firmwareId = "DSP1"; break;
				case "dsp1b": firmwareId = "DSP1b"; break;
				case "dsp2": firmwareId = "DSP2"; break;
				case "dsp3": firmwareId = "DSP3"; break;
				case "dsp4": firmwareId = "DSP4"; break;
				case "st010": firmwareId = "ST010"; break;
				case "st011": firmwareId = "ST011"; break;
				case "st018": firmwareId = "ST018"; break;
				case "sgb": firmwareId = "SGB"; firmwareSystem = "GB"; break;
				case "sgb2": firmwareId = "SGB2"; firmwareSystem = "GB"; break;
				default:
					CoreComm.ShowMessage($"Unrecognized SNES firmware request \"{hint}\".");
					return "";
			}

			string ret = "";
			FirmwareID fwid = new(firmwareSystem, firmwareId);
			const string MISSING_FIRMWARE_MSG = "Game may function incorrectly without the requested firmware.";
			byte[] data = required
				? CoreComm.CoreFileProvider.GetFirmwareOrThrow(fwid, MISSING_FIRMWARE_MSG)
				: CoreComm.CoreFileProvider.GetFirmware(fwid, MISSING_FIRMWARE_MSG);
			if (data != null)
			{
				ret = hint;
				Api.AddReadonlyFile(data, hint);
			}

			Console.WriteLine("Served bsnescore request for firmware \"{0}\"", hint);

			// return the path we built
			return ret;
		}

		/// <param name="port">0 or 1, corresponding to L and R physical ports on the snes</param>
		/// <param name="index">meaningless for most controllers.  for multitap, 0-3 for which multitap controller</param>
		/// <param name="id">button ID enum; in the case of a regular controller, this corresponds to shift register position</param>
		/// <returns>for regular controllers, one bit D0 of button status.  for other controls, varying ranges depending on id</returns>
		private short snes_input_poll(int port, int index, int id)
		{
			return _controllers.CoreInputPoll(_controller, port, index, id);
		}

		private void snes_no_lag(bool sgbPoll)
		{
			// gets called whenever there was input read in the core
			if (!IsSGB || sgbPoll)
			{
				IsLagFrame = false;
			}
		}

		private void snes_controller_latch()
		{
			InputCallbacks.Call();
		}

		private readonly int[] palette = new int[0x8000];

		private void generate_palette()
		{
			for (int color = 0; color < 32768; color++) {
				int r = (color >> 10) & 31;
				int g = (color >>  5) & 31;
				int b = (color >>  0) & 31;

				r = r << 3 | r >> 2; r = r << 8 | r << 0;
				g = g << 3 | g >> 2; g = g << 8 | g << 0;
				b = b << 3 | b >> 2; b = b << 8 | b << 0;

				palette[color] = r >> 8 << 16 | g >> 8 <<  8 | b >> 8 << 0;
			}
		}

		private void snes_video_refresh(ushort* data, int width, int height, int pitch)
		{
			int widthMultiplier = 1;
			int heightMultiplier = 1;
			if (_settings.CropSGBFrame && IsSGB)
			{
				BufferWidth = 160;
				BufferHeight = 144;
			}
			else
			{
				if (_settings.AlwaysDoubleSize)
				{
					if (width == 256) widthMultiplier = 2;
					if (height == 224) heightMultiplier = 2;
				}
				BufferWidth = width * widthMultiplier;
				BufferHeight = height * heightMultiplier;
			}

			int size = BufferWidth * BufferHeight;
			if (_videoBuffer.Length != size)
			{
				_videoBuffer = new int[size];
			}

			int di = 0;
			if (_settings.CropSGBFrame && IsSGB)
			{
				for (int y = 39; y < 39 + 144; y++)
				{
					ushort* sp = data + y * pitch + 48;
					for (int x = 0; x < 160; x++)
					{
						_videoBuffer[di++] = palette[*sp++ & 0x7FFF];
					}
				}
				return;
			}

			for (int y = 0; y < height * heightMultiplier; y++)
			{
				int si = y / heightMultiplier * pitch;
				for (int x = 0; x < width * widthMultiplier; x++)
				{
					_videoBuffer[di++] = palette[data[si + x / widthMultiplier] & 0x7FFF];
				}
			}
		}

		private void InitAudio()
		{
			_resampler = new SpeexResampler(SpeexResampler.Quality.QUALITY_DESKTOP, 64080, 88200, 32040, 44100);
		}

		private void snes_audio_sample(short left, short right)
		{
			_resampler.EnqueueSample(left, right);
		}

		private void snes_trace(string disassembly, string registerInfo)
			=> _tracer.Put(new(disassembly: disassembly, registerInfo: registerInfo));

		private void ReadHook(uint addr)
		{
			if (MemoryCallbacks.HasReads)
			{
				MemoryCallbacks.CallMemoryCallbacks(addr, 0, (uint) MemoryCallbackFlags.AccessRead, "System Bus");
			}
		}

		private void WriteHook(uint addr, byte value)
		{
			if (MemoryCallbacks.HasWrites)
			{
				MemoryCallbacks.CallMemoryCallbacks(addr, value, (uint) MemoryCallbackFlags.AccessWrite, "System Bus");
			}
		}

		private void ExecHook(uint addr)
		{
			if (MemoryCallbacks.HasExecutes)
			{
				MemoryCallbacks.CallMemoryCallbacks(addr, 0, (uint) MemoryCallbackFlags.AccessExecute, "System Bus");
			}
		}

		private FileStream _currentMsuTrack;

		private void msu_seek(long offset, bool relative)
		{
			_currentMsuTrack?.Seek(offset, relative ? SeekOrigin.Current : SeekOrigin.Begin);
		}
		private byte msu_read()
		{
			return (byte) (_currentMsuTrack?.ReadByte() ?? 0);
		}

		private void msu_open(ushort trackId)
		{
			_currentMsuTrack?.Dispose();
			try
			{
				_currentMsuTrack = File.OpenRead($"{_romPath}-{trackId}.pcm");
			}
			catch
			{
				_currentMsuTrack = null;
			}
		}
		private bool msu_end()
		{
			return _currentMsuTrack.Position == _currentMsuTrack.Length;
		}
	}
}
