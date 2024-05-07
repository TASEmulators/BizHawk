using System;
using System.Linq;
using System.Runtime.InteropServices;
using BizHawk.BizInvoke;
using BizHawk.Common;
using BizHawk.Common.PathExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Consoles.Atari.Stella;
using BizHawk.Emulation.Cores.Waterbox;

namespace BizHawk.Emulation.Cores.Atari.Stella
{
	[Core(CoreNames.Stella, "The Stella Team")]
	[ServiceNotApplicable(new[] { typeof(IDriveLight), typeof(ISaveRam) })]
	public partial class Stella : IEmulator, IVideoProvider, IDebuggable, IInputPollable, IRomInfo, IRegionable,
		ICreateGameDBEntries, ISettable<Stella.A2600Settings, Stella.A2600SyncSettings>
	{
		internal static class RomChecksums
		{
			public const string CongoBongo = "SHA1:3A77DB43B6583E8689435F0F14AA04B9E57BDDED";

			public const string KangarooNotInGameDB = "SHA1:982B8016B393A9AA7DD110295A53C4612ECF2141";

			public const string Tapper = "SHA1:E986E1818E747BEB9B33CE4DFF1CDC6B55BDB620";
		}

		[CoreConstructor(VSystemID.Raw.A26)]
		public Stella(CoreLoadParameters<Stella.A2600Settings, Stella.A2600SyncSettings> lp)
		{
			var ser = new BasicServiceProvider(this);
			ServiceProvider = ser;
			SyncSettings = lp.SyncSettings ?? new A2600SyncSettings();
			Settings = lp.Settings ?? new A2600Settings();
			_controllerDeck = new Atari2600ControllerDeck(SyncSettings.Port1, SyncSettings.Port2);

			_elf = new WaterboxHost(new WaterboxOptions
			{
				Path = PathUtils.DllDirectoryPath,
				Filename = "stella.wbx",
				SbrkHeapSizeKB = 4 * 1024,
				SealedHeapSizeKB = 4 * 1024,
				InvisibleHeapSizeKB = 4 * 1024,
				PlainHeapSizeKB = 4 * 1024,
				MmapHeapSizeKB = 4 * 1024,
				SkipCoreConsistencyCheck = lp.Comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxCoreConsistencyCheck),
				SkipMemoryConsistencyCheck = lp.Comm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxMemoryConsistencyCheck),
			});

			LoadCallback = load_archive;
			_inputCallback = input_callback;

			var callingConventionAdapter = CallingConventionAdapters.MakeWaterbox(new Delegate[]
			{
				LoadCallback, _inputCallback
			}, _elf);
			
			using (_elf.EnterExit())
			{
				Core = BizInvoker.GetInvoker<CInterface>(_elf, _elf, callingConventionAdapter);
				SyncSettings = lp.SyncSettings ?? new A2600SyncSettings();
				Settings = lp.Settings ?? new A2600Settings();

				CoreComm = lp.Comm;

				_romfile = lp.Roms.FirstOrDefault()?.RomData;
                string romPath = lp.Roms.FirstOrDefault()?.RomPath;

				var initResult = Core.stella_init(romPath, LoadCallback, SyncSettings.GetNativeSettings(lp.Game));

				if (!initResult) throw new Exception($"{nameof(Core.stella_init)}() failed");

				Core.stella_get_frame_rate(out int fps);
				
				int regionId = Core.stella_get_region();
				if (regionId == 0) _region = DisplayType.NTSC;
				if (regionId == 1) _region = DisplayType.PAL;
				if (regionId == 2) _region = DisplayType.SECAM;

				VsyncNumerator = fps;
				VsyncDenominator = 1;

				Core.stella_set_input_callback(_inputCallback);

				_elf.Seal();
			}

			// pull the default video size from the core
			UpdateVideo();
				
		}

		// IRegionable
		public DisplayType Region => _region;

		private DisplayType _region;

		private CInterface.load_archive_cb LoadCallback;

		private readonly byte[] _romfile;
		private readonly CInterface Core;
		private readonly WaterboxHost _elf;
		private CoreComm CoreComm { get; }

		public string RomDetails { get; private set; }

		private readonly Atari2600ControllerDeck _controllerDeck;

		private ITraceable Tracer { get; }

		// ICreateGameDBEntries
		public CompactGameInfo GenerateGameDbEntry()
		{
			return new CompactGameInfo
			{
			};
		}

		// IBoardInfo
		private static bool DetectPal(GameInfo game, byte[] rom)
		{
			return true;
		}

		/// <summary>
		/// core callback for file loading
		/// </summary>
		/// <param name="filename">string identifying file to be loaded</param>
		/// <param name="buffer">buffer to load file to</param>
		/// <param name="maxsize">maximum length buffer can hold</param>
		/// <returns>actual size loaded, or 0 on failure</returns>
		private int load_archive(string filename, IntPtr buffer, int maxsize)
		{
			byte[] srcdata = null;

			if (buffer == IntPtr.Zero)
			{
				Console.WriteLine("Couldn't satisfy firmware request {0} because buffer == NULL", filename);
				return 0;
			}

			if (filename == "PRIMARY_ROM")
			{
				if (_romfile == null)
				{
					Console.WriteLine("Couldn't satisfy firmware request PRIMARY_ROM because none was provided.");
					return 0;
				}
				srcdata = _romfile;
			}

			if (srcdata != null)
			{
				if (srcdata.Length > maxsize)
				{
					Console.WriteLine("Couldn't satisfy firmware request {0} because {1} > {2}", filename, srcdata.Length, maxsize);
					return 0;
				}
				else
				{
					Console.WriteLine("Copying Data from " + srcdata + " to " + buffer + " Size: " + srcdata.Length);
					Marshal.Copy(srcdata, 0, buffer, srcdata.Length);
					Console.WriteLine("Firmware request {0} satisfied at size {1}", filename, srcdata.Length);
					return srcdata.Length;
				}
			}
			else
			{
				throw new InvalidOperationException("Unknown error processing firmware");
			}

		}
	}
}
