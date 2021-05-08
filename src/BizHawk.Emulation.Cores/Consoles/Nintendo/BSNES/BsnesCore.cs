using System;
using System.Linq;
using System.Xml;
using System.IO;

using BizHawk.Common.BufferExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Components.W65816;

// TODO - add serializer (?)

// http://wiki.superfamicom.org/snes/show/Backgrounds

// TODO
// libsnes needs to be modified to support multiple instances - THIS IS NECESSARY - or else loading one game and then another breaks things
// edit - this is a lot of work
// wrap dll code around some kind of library-accessing interface so that it doesn't malfunction if the dll is unavailable
namespace BizHawk.Emulation.Cores.Nintendo.SNES
{
	[Core(
		CoreNames.Bsnes115,
		"bsnes team",
		isPorted: true,
		isReleased: true,
		portedVersion: "v115",
		portedUrl: "https://bsnes.dev",
		singleInstance: false)]
	[ServiceNotApplicable(new[] { typeof(IDriveLight) })]
	public unsafe partial class BsnesCore : IEmulator, IVideoProvider, IStatable, IInputPollable, IRegionable, ISettable<BsnesCore.SnesSettings, BsnesCore.SnesSyncSettings>
	{
		// TODO: will need to be moved out to IMemoryDomains once I can get myself to that bullshit
		private BsnesApi.SNES_REGION? _region;
		private BsnesApi.SNES_MAPPER? _mapper;

		// [CoreConstructor("SGB")]
		[CoreConstructor("SNES")]
		public BsnesCore(GameInfo game, byte[] rom, CoreComm comm,
			SnesSettings settings, SnesSyncSettings syncSettings)
			:this(game, rom, null, null, comm, settings, syncSettings)
		{}

		public BsnesCore(GameInfo game, byte[] romData, byte[] xmlData, string baseRomPath, CoreComm comm,
			SnesSettings settings, SnesSyncSettings syncSettings)
		{
			_baseRomPath = baseRomPath;
			var ser = new BasicServiceProvider(this);
			ServiceProvider = ser;

			_tracer = new TraceBuffer
			{
				Header = "65816: PC, mnemonic, operands, registers (A, X, Y, S, D, DB, flags (NVMXDIZC), V, H)"
			};

			ser.Register<IDisassemblable>(new W65816_DisassemblerService());

			_game = game;
			CoreComm = comm;
			byte[] sgbRomData = null;

			if (game.System == "SGB")
			{
				if ((romData[0x143] & 0xc0) == 0xc0)
				{
					throw new CGBNotSupportedException();
				}

				sgbRomData = CoreComm.CoreFileProvider.GetFirmware("SNES", "Rom_SGB", true, "SGB Rom is required for SGB emulation.");
				game.FirmwareHash = sgbRomData.HashSHA1();
			}

			_settings = settings ?? new SnesSettings();
			_syncSettings = syncSettings ?? new SnesSyncSettings();

			BsnesApi.snes_video_frame_t videocb = snes_video_refresh;
			BsnesApi.snes_audio_sample_t audiocb = snes_audio_sample;
			BsnesApi.snes_input_poll_t inputpollcb = snes_input_poll;
			BsnesApi.snes_input_state_t inputstatecb = snes_input_state;
			BsnesApi.snes_no_lag_t nolagcb = snes_no_lag;
			_scanlineStartCb = snes_scanlineStart;
			_tracecb = snes_trace;
			BsnesApi.snes_path_request_t pathrequestcb = snes_path_request;

			// TODO: pass profile here
			Api = new BsnesApi(this, CoreComm.CoreFileProvider.DllPath(), CoreComm, new Delegate[]
			{
				videocb,
				audiocb,
				inputpollcb,
				inputstatecb,
				nolagcb,
				_scanlineStartCb,
				_tracecb,
				pathrequestcb
			});
			// {
				// ReadHook = u =>  ReadHook,
				// ExecHook = ExecHook,
				// WriteHook = WriteHook,
				// ReadHook_SMP = ReadHook_SMP,
				// ExecHook_SMP = ExecHook_SMP,
				// WriteHook_SMP = WriteHook_SMP,
			// };

			// ScanlineHookManager = new MyScanlineHookManager(this);

			_controllers = new BsnesControllers(_syncSettings);

			generate_palette();
			Api._core.snes_init(_syncSettings.Entropy, _controllers._ports[0].DeviceType, _controllers._ports[1].DeviceType,
				_syncSettings.Hotfixes, _syncSettings.FastPPU);
			Api._core.snes_set_callbacks(inputpollcb, inputstatecb, nolagcb, videocb, audiocb, pathrequestcb);

			// start up audio resampler
			InitAudio();
			ser.Register<ISoundProvider>(_resampler);

			if (game.System == "SGB")
			{
				IsSGB = true;
				SystemId = "SNES";
				ser.Register<IBoardInfo>(new SGBBoardInfo());

				_currLoadParams = new LoadParams
				{
					type = LoadParamType.SuperGameBoy,
					baseRomPath = baseRomPath,
					romData = romData,
					sgbRomData = sgbRomData
				};
			}
			else
			{
				// we may need to get some information out of the cart, even during the following bootup/load process
				if (xmlData != null)
				{
					_romxml = new XmlDocument();
					_romxml.Load(new MemoryStream(xmlData));

					// bsnes wont inspect the xml to load the necessary sfc file.
					// so, we have to do that here and pass it in as the romData :/

					// TODO: uhh i have no idea what the xml is or whether this below code is needed
					if (_romxml["cartridge"]?["rom"] != null)
					{
						romData = File.ReadAllBytes(PathSubfile(_romxml["cartridge"]["rom"].Attributes["name"].Value));
					}
					else
					{
						throw new Exception("Could not find rom file specification in xml file. Please check the integrity of your xml file");
					}
				}

				SystemId = "SNES";
				_currLoadParams = new LoadParams
				{
					type = LoadParamType.Normal,
					baseRomPath = baseRomPath,
					romData = romData
				};
			}
			LoadCurrent();

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

			// SetupMemoryDomains(romData, sgbRomData);

			ser.Register<ITraceable>(_tracer);

			Api.Seal();
		}

		internal CoreComm CoreComm { get; }

		private readonly string _baseRomPath;

		private string PathSubfile(string fname) => Path.Combine(_baseRomPath, fname);

		private readonly GameInfo _game;
		private readonly BsnesControllers _controllers;
		private readonly ITraceable _tracer;
		private readonly XmlDocument _romxml;
		private readonly BsnesApi.snes_scanlineStart_t _scanlineStartCb;
		private readonly BsnesApi.snes_trace_t _tracecb;

		private IController _controller;
		private readonly LoadParams _currLoadParams;
		private SpeexResampler _resampler;
		private bool _disposed;

		public bool IsSGB { get; }

		private class SGBBoardInfo : IBoardInfo
		{
			public string BoardName => "SGB";
		}

		public BsnesApi Api { get; }

		public MyScanlineHookManager ScanlineHookManager { get; }

		public class MyScanlineHookManager : ScanlineHookManager
		{
			private readonly BsnesCore _core;

			public MyScanlineHookManager(BsnesCore core)
			{
				_core = core;
			}

			// protected override void OnHooksChanged()
			// {
				// _core.OnScanlineHooksChanged();
			// }
		}


		private void snes_scanlineStart(int line)
		{
			ScanlineHookManager.HandleScanline(line);
		}

		private string snes_path_request(int slot, string hint)
		{
			// every rom requests msu1.rom... why? who knows.
			// also handle msu-1 pcm files here
			bool isMsu1Rom = hint == "msu1/data.rom";
			bool isMsu1Pcm = Path.GetExtension(hint).ToLower() == ".pcm";
			if (isMsu1Rom || isMsu1Pcm)
			{
				// well, check if we have an msu-1 xml
				if (_romxml?["cartridge"]?["msu1"] != null)
				{
					var msu1 = _romxml["cartridge"]["msu1"];
					if (isMsu1Rom && msu1["rom"]?.Attributes["name"] != null)
					{
						return PathSubfile(msu1["rom"].Attributes["name"].Value);
					}

					if (isMsu1Pcm)
					{
						// return @"D:\roms\snes\SuperRoadBlaster\SuperRoadBlaster-1.pcm";
						// return "";
						int wantsTrackNumber = int.Parse(hint.Replace("track-", "").Replace(".pcm", ""));
						wantsTrackNumber++;
						string wantsTrackString = wantsTrackNumber.ToString();
						foreach (var child in msu1.ChildNodes.Cast<XmlNode>())
						{
							if (child.Name == "track" && child.Attributes["number"].Value == wantsTrackString)
							{
								return PathSubfile(child.Attributes["name"].Value);
							}
						}
					}
				}

				// not found.. what to do? (every rom will get here when msu1.rom is requested)
				return "";
			}

			// not MSU-1.  ok.
			if (hint == "save.ram")
			{
				// TODO handle saveram at some point
			}

			string firmwareId;

			switch (hint)
			{
				case "cx4.rom": firmwareId = "CX4"; break;
				case "dsp1.rom": firmwareId = "DSP1"; break;
				case "dsp1b.rom": firmwareId = "DSP1b"; break;
				case "dsp2.rom": firmwareId = "DSP2"; break;
				case "dsp3.rom": firmwareId = "DSP3"; break;
				case "dsp4.rom": firmwareId = "DSP4"; break;
				case "st010.rom": firmwareId = "ST010"; break;
				case "st011.rom": firmwareId = "ST011"; break;
				case "st018.rom": firmwareId = "ST018"; break;
				default:
					CoreComm.ShowMessage($"Unrecognized SNES firmware request \"{hint}\".");
					return "";
			}

			string ret;
			var data = CoreComm.CoreFileProvider.GetFirmware("SNES", firmwareId, false, "Game may function incorrectly without the requested firmware.");
			if (data != null)
			{
				ret = hint;
				Api.AddReadonlyFile(data, hint);
			}
			else
			{
				ret = "";
			}

			Console.WriteLine("Served bsnescore request for firmware \"{0}\"", hint);

			// return the path we built
			return ret;
		}

		private void snes_trace(uint which, string msg)
		{
			// no idea what this is but it has to go
			// TODO: get them out of the core split up and remove this hackery
			const string splitStr = "A:";

			if (which == (uint)BsnesApi.eTRACE.CPU)
			{
				var split = msg.Split(new[] { splitStr }, 2, StringSplitOptions.None);

				_tracer.Put(new TraceInfo
				{
					Disassembly = split[0].PadRight(34),
					RegisterInfo = splitStr + split[1]
				});
			}
			else if (which == (uint)BsnesApi.eTRACE.SMP)
			{
				int idx = msg.IndexOf("YA:");
				string dis = msg.Substring(0,idx).TrimEnd();
				string regs = msg.Substring(idx);
				_tracer.Put(new TraceInfo
				{
					Disassembly = dis,
					RegisterInfo = regs
				});
			}
			else if (which == (uint)BsnesApi.eTRACE.GB)
			{
				int idx = msg.IndexOf("AF:");
				string dis = msg.Substring(0,idx).TrimEnd();
				string regs = msg.Substring(idx);
				_tracer.Put(new TraceInfo
				{
					Disassembly = dis,
					RegisterInfo = regs
				});
			}
		}

		// private void ReadHook(uint addr)
		// {
		// 	if (MemoryCallbacks.HasReads)
		// 	{
		// 		uint flags = (uint)MemoryCallbackFlags.AccessRead;
		// 		MemoryCallbacks.CallMemoryCallbacks(addr, 0, flags, "System Bus");
		// 		// we RefreshMemoryCallbacks() after the trigger in case the trigger turns itself off at that point
		// 		// EDIT: for now, theres some IPC re-entrancy problem
		// 		// RefreshMemoryCallbacks();
		// 	}
		// }

		// private void ExecHook(uint addr)
		// {
		// 	if (MemoryCallbacks.HasExecutes)
		// 	{
		// 		uint flags = (uint)MemoryCallbackFlags.AccessExecute;
		// 		MemoryCallbacks.CallMemoryCallbacks(addr, 0, flags, "System Bus");
		// 		// we RefreshMemoryCallbacks() after the trigger in case the trigger turns itself off at that point
		// 		// EDIT: for now, theres some IPC re-entrancy problem
		// 		// RefreshMemoryCallbacks();
		// 	}
		// }

		// private void WriteHook(uint addr, byte val)
		// {
		// 	if (MemoryCallbacks.HasWrites)
		// 	{
		// 		uint flags = (uint)MemoryCallbackFlags.AccessWrite;
		// 		MemoryCallbacks.CallMemoryCallbacks(addr, val, flags, "System Bus");
		// 		// we RefreshMemoryCallbacks() after the trigger in case the trigger turns itself off at that point
		// 		// EDIT: for now, theres some IPC re-entrancy problem
		// 		// RefreshMemoryCallbacks();
		// 	}
		// }
		//
		// private void ReadHook_SMP(uint addr)
		// {
		// 	if (MemoryCallbacks.HasReads)
		// 	{
		// 		uint flags = (uint)MemoryCallbackFlags.AccessRead;
		// 		MemoryCallbacks.CallMemoryCallbacks(addr, 0, flags, "SMP");
		// 	}
		// }
		//
		// private void ExecHook_SMP(uint addr)
		// {
		// 	if (MemoryCallbacks.HasExecutes)
		// 	{
		// 		uint flags = (uint)MemoryCallbackFlags.AccessExecute;
		// 		MemoryCallbacks.CallMemoryCallbacks(addr, 0, flags, "SMP");
		// 	}
		// }
		//
		// private void WriteHook_SMP(uint addr, byte val)
		// {
		// 	if (MemoryCallbacks.HasWrites)
		// 	{
		// 		uint flags = (uint)MemoryCallbackFlags.AccessWrite;
		// 		MemoryCallbacks.CallMemoryCallbacks(addr, val, flags, "SMP");
		// 	}
		// }

		private enum LoadParamType
		{
			Normal, SuperGameBoy
		}

		private struct LoadParams
		{
			public LoadParamType type;
			public string baseRomPath;
			public byte[] romData;
			public byte[] sgbRomData;
		}

		private void LoadCurrent()
		{
			if (_currLoadParams.type == LoadParamType.Normal)
				Api._core.snes_load_cartridge_normal(_currLoadParams.baseRomPath, _currLoadParams.romData, _currLoadParams.romData.Length);
			else
				Api._core.snes_load_cartridge_super_gameboy(_currLoadParams.baseRomPath, _currLoadParams.romData, _currLoadParams.romData.Length,
					_currLoadParams.sgbRomData, _currLoadParams.sgbRomData.Length);

			_region = Api._core.snes_get_region();
			_mapper = Api._core.snes_get_mapper();
		}

		// poll which updates the controller state
		private void snes_input_poll()
		{
			_controllers.CoreInputPoll(_controller);
		}

		/// <param name="port">0 or 1, corresponding to L and R physical ports on the snes</param>
		/// <param name="device">LibsnesApi.SNES_DEVICE enum index specifying type of device</param>
		/// <param name="index">meaningless for most controllers.  for multitap, 0-3 for which multitap controller</param>
		/// <param name="id">button ID enum; in the case of a regular controller, this corresponds to shift register position</param>
		/// <returns>for regular controllers, one bit D0 of button status.  for other controls, varying ranges depending on id</returns>
		private short snes_input_state(int port, int device, int index, int id)
		{
			// we're not using device here... should we?
			return _controllers.CoreInputState(port, index, id);
		}

		private void snes_no_lag()
		{
			// gets called whenever there was input polled, aka no lag
			IsLagFrame = false;
		}

		private readonly int[] palette = new int[32768];

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

		// i have no idea how all this logic works, but it does. should probably uh be looked at again
		private void snes_video_refresh(ushort* data, int width, int height, int pitch)
		{
			int widthMultiplier = 1;
			int heightMultiplier = 1;
			if (_settings.AlwaysDoubleSize)
			{
				if (width == 256) widthMultiplier = 2;
				if (height == 224) heightMultiplier = 2;
			}
			BufferWidth = width * widthMultiplier;
			BufferHeight = height * heightMultiplier;

			int dpitch = pitch;
			if (height == 448)
				dpitch <<= 1;
			if (width == 512)
				dpitch <<= 1;

			int size = BufferWidth * BufferHeight;
			if (_videoBuffer.Length != size)
			{
				_videoBuffer = new int[size];
			}

			for (int y = 0; y < height * heightMultiplier; y++)
			{
				int si = y / heightMultiplier * pitch;
				int di = y * widthMultiplier * dpitch / 4;
				for (int x = 0; x < width * widthMultiplier; x++)
				{
					_videoBuffer[di++] = palette[data[si + x / widthMultiplier]];
				}
			}
		}

		// private void RefreshMemoryCallbacks(bool suppress)
		// {
			// var mcs = MemoryCallbacks;
			// Api.QUERY_set_state_hook_exec(!suppress && mcs.HasExecutesForScope("System Bus"));
			// Api.QUERY_set_state_hook_read(!suppress && mcs.HasReadsForScope("System Bus"));
			// Api.QUERY_set_state_hook_write(!suppress && mcs.HasWritesForScope("System Bus"));
		// }

		//public byte[] snes_get_memory_data_read(LibsnesApi.SNES_MEMORY id)
		//{
		//  var size = (int)api.snes_get_memory_size(id);
		//  if (size == 0) return new byte[0];
		//  var ret = api.snes_get_memory_data(id);
		//  return ret;
		//}

		private void InitAudio()
		{
			_resampler = new SpeexResampler(SpeexResampler.Quality.QUALITY_DESKTOP, 64080, 88200, 32040, 44100);
		}

		private void snes_audio_sample(short left, short right)
		{
			_resampler.EnqueueSample(left, right);
		}
	}
}
