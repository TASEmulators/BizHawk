using System.Runtime.InteropServices;

using BizHawk.BizInvoke;
using BizHawk.Common;
using BizHawk.Common.PathExtensions;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Waterbox;

namespace BizHawk.Emulation.Cores.Atari.Stella
{
	[PortedCore(
		name: CoreNames.Stella,
		author: "The Stella Team",
		portedVersion: "6.8+09be43c50", // in the middle of 6.7.1 and 7.0
		portedUrl: "https://stella-emu.github.io")]
	[ServiceNotApplicable(typeof(ISaveRam))]
	public partial class Stella : IRomInfo, IRegionable
	{
		[CoreConstructor(VSystemID.Raw.A26)]
		public Stella(CoreLoadParameters<object, A2600SyncSettings> lp)
		{
			var ser = new BasicServiceProvider(this);
			ServiceProvider = ser;
			_syncSettings = lp.SyncSettings ?? new A2600SyncSettings();
			_controllerDeck = new Atari2600ControllerDeck(_syncSettings.Port1, _syncSettings.Port2);

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

			try
			{
				_loadCallback = LoadCallback;
				_inputCallback = InputCallback;

				var callingConventionAdapter = CallingConventionAdapters.MakeWaterbox(
				[
					_loadCallback, _inputCallback
				], _elf);

				using (_elf.EnterExit())
				{
					Core = BizInvoker.GetInvoker<CInterface>(_elf, _elf, callingConventionAdapter);

					_romfile = lp.Roms[0].RomData;
					var initResult = Core.stella_init("rom.a26", _loadCallback, _syncSettings.GetNativeSettings(lp.Game));

					if (!initResult) throw new Exception($"{nameof(Core.stella_init)}() failed");

					Core.stella_get_frame_rate(out var fps);

					InitSound(fps);

					var regionId = Core.stella_get_region();
					Region = regionId switch
					{
						0 => DisplayType.NTSC,
						1 => DisplayType.PAL,
						2 => DisplayType.SECAM,
						_ => throw new InvalidOperationException()
					};

					// ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
					_vidPalette = Region switch
					{
						DisplayType.NTSC => NTSCPalette,
						DisplayType.PAL => PALPalette,
						DisplayType.SECAM => SecamPalette,
						_ => throw new InvalidOperationException()
					};

					VsyncNumerator = fps;
					VsyncDenominator = 1;

					Core.stella_set_input_callback(_inputCallback);

					var ptr = Core.stella_get_cart_type();
					var cartType = Marshal.PtrToStringAnsi(ptr);
					Console.WriteLine($"[Stella] Cart type loaded: {cartType}");

					RomDetails = $"{lp.Game.Name}\r\n{SHA1Checksum.ComputePrefixedHex(_romfile)}\r\n{MD5Checksum.ComputePrefixedHex(_romfile)}\r\nMapper Impl \"{cartType}\"";

					_elf.Seal();
				}

				// pull the default video size from the core
				UpdateVideo();

				// Registering memory domains
				SetupMemoryDomains();
			}
			catch
			{
				Dispose();
				throw;
			}
		}

		// IRegionable
		public DisplayType Region { get; }

		// IRomInfo
		public string RomDetails { get; }

		// ReSharper disable once PrivateFieldCanBeConvertedToLocalVariable
		private readonly CInterface.load_archive_cb _loadCallback;

		private readonly byte[] _romfile;
		private readonly CInterface Core;
		private readonly WaterboxHost _elf;

		private readonly Atari2600ControllerDeck _controllerDeck;

		/// <summary>
		/// core callback for file loading
		/// </summary>
		/// <param name="filename">string identifying file to be loaded</param>
		/// <param name="buffer">buffer to load file to</param>
		/// <param name="maxsize">maximum length buffer can hold</param>
		/// <returns>actual size loaded, or 0 on failure</returns>
		private int LoadCallback(string filename, IntPtr buffer, int maxsize)
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
