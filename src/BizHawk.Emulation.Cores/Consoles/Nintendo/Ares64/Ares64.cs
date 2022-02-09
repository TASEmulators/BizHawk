using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Properties;
using BizHawk.Emulation.Cores.Waterbox;

namespace BizHawk.Emulation.Cores.Consoles.Nintendo.Ares64
{
	[PortedCore(CoreNames.Ares64, "ares team, Near", "v126", "https://ares-emulator.github.io/")]
	[ServiceNotApplicable(new[] { typeof(IDriveLight), })]
	public partial class Ares64 : WaterboxCore, IRegionable
	{
		private readonly LibAres64 _core;

		[CoreConstructor(VSystemID.Raw.N64, Priority = CorePriority.High )]
		public Ares64(CoreLoadParameters<object, Ares64SyncSettings> lp)
			: base(lp.Comm, new Configuration
			{
				DefaultWidth = 640,
				DefaultHeight = 480,
				MaxWidth = 640,
				MaxHeight = 576,
				MaxSamples = 1024,
				DefaultFpsNumerator = 60000,
				DefaultFpsDenominator = 1001,
				SystemId = VSystemID.Raw.N64,
			})
		{
			_syncSettings = lp.SyncSettings ?? new();

			_core = PreInit<LibAres64>(new WaterboxOptions
			{
				Filename = "ares64.wbx",
				SbrkHeapSizeKB = 2 * 1024,
				SealedHeapSizeKB = 4,
				InvisibleHeapSizeKB = 6 * 1024,
				PlainHeapSizeKB = 4,
				MmapHeapSizeKB = 512 * 1024,
				SkipCoreConsistencyCheck = CoreComm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxCoreConsistencyCheck),
				SkipMemoryConsistencyCheck = CoreComm.CorePreferences.HasFlag(CoreComm.CorePreferencesFlags.WaterboxMemoryConsistencyCheck),
			});

			var rom = lp.Roms[0].RomData;

			Region = rom[0x3E] switch
			{
				0x44 or 0x46 or 0x49 or 0x50 or 0x53 or 0x55 or 0x58 or 0x59 => DisplayType.PAL,
				_ => DisplayType.NTSC,
			};

			var pal = Region == DisplayType.PAL;

			if (pal)
			{
				VsyncNumerator = 50;
				VsyncDenominator = 1;
			}

			var pif = Util.DecompressGzipFile(new MemoryStream(pal ? Resources.PIF_PAL_ROM.Value : Resources.PIF_NTSC_ROM.Value));

			_exe.AddReadonlyFile(pif, pal ? "pif.pal.rom" : "pif.ntsc.rom");
			_exe.AddReadonlyFile(rom, "program.rom");

			var controllers = new LibAres64.ControllerType[4]
			{
				_syncSettings.P1Controller,
				_syncSettings.P2Controller,
				_syncSettings.P3Controller,
				_syncSettings.P4Controller,
			};

			if (!_core.Init(controllers, pal))
			{
				throw new InvalidOperationException("Init returned false!");
			}

			_exe.RemoveReadonlyFile(pal ? "pif.pal.rom" : "pif.ntsc.rom");
			_exe.RemoveReadonlyFile("program.rom");

			PostInit();
			DeterministicEmulation = true;
		}

		public DisplayType Region { get; }

		public override ControllerDefinition ControllerDefinition => N64Controller;

		public static readonly ControllerDefinition N64Controller = new ControllerDefinition("N64 Controller")
		{
			BoolButtons =
			{
			}
		}.MakeImmutable();

		protected override LibWaterboxCore.FrameInfo FrameAdvancePrep(IController controller, bool render, bool rendersound)
		{
			return new LibAres64.FrameInfo
			{
			};
		}

		protected override void FrameAdvancePost()
		{
			if (BufferWidth == 0)
			{
				BufferWidth = BufferHeight == 239 ? 320 : 640;
			}
		}
	}
}
