using System.Collections.Generic;
using System.Drawing;
using System.IO;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores;
using BizHawk.Emulation.Cores.Nintendo.Gameboy;
using BizHawk.Emulation.Cores.Nintendo.GBHawk;
using BizHawk.Emulation.Cores.Nintendo.Sameboy;

using static BizHawk.Emulation.Cores.Nintendo.Gameboy.Gameboy;
using static BizHawk.Emulation.Cores.Nintendo.GBHawk.GBHawk;
using static BizHawk.Emulation.Cores.Nintendo.Sameboy.Sameboy;

namespace BizHawk.Tests.Testroms.GB
{
	public static class GBHelper
	{
		public enum ConsoleVariant { CGB_C, CGB_D, DMG, DMG_B }

		public readonly struct CoreSetup
		{
			public static IReadOnlyCollection<CoreSetup> ValidSetupsFor(ConsoleVariant variant)
				=> new CoreSetup[]
				{
					new(CoreNames.Gambatte, variant),
					new(CoreNames.Gambatte, variant, useBios: false),
					new(CoreNames.GbHawk, variant),
					new(CoreNames.Sameboy, variant),
					new(CoreNames.Sameboy, variant, useBios: false),
				};

			public readonly string CoreName;

			public readonly bool UseBIOS;

			public readonly ConsoleVariant Variant;

			public CoreSetup(string coreName, ConsoleVariant variant, bool useBios = true)
			{
				CoreName = coreName;
				UseBIOS = useBios;
				Variant = variant;
			}

			public readonly override string ToString()
				=> $"{Variant} in {CoreName}{(UseBIOS ? string.Empty : " (no BIOS)")}";
		}

		public readonly struct DummyRomAsset : IRomAsset
		{
			private readonly byte[] _fileData;

			public string? Extension
				=> throw new NotImplementedException();

			public byte[]? FileData
				=> _fileData;

			public GameInfo? Game
				=> throw new NotImplementedException();

			public byte[]? RomData
				=> throw new NotImplementedException();

			public string? RomPath
				=> throw new NotImplementedException();

			public DummyRomAsset(byte[] fileData)
				=> _fileData = fileData;
		}

		private static readonly GambatteSettings GambatteSettings = new() { CGBColors = GBColors.ColorType.vivid };

		private static readonly GambatteSyncSettings GambatteSyncSettings_GB_NOBIOS = new() { ConsoleMode = GambatteSyncSettings.ConsoleModeType.GB, EnableBIOS = false, FrameLength = GambatteSyncSettings.FrameLengthType.EqualLengthFrames };

		private static readonly GambatteSyncSettings GambatteSyncSettings_GB_USEBIOS = new() { ConsoleMode = GambatteSyncSettings.ConsoleModeType.GB, EnableBIOS = true, FrameLength = GambatteSyncSettings.FrameLengthType.EqualLengthFrames };

		private static readonly GambatteSyncSettings GambatteSyncSettings_GBC_NOBIOS = new() { ConsoleMode = GambatteSyncSettings.ConsoleModeType.GBC, EnableBIOS = false, FrameLength = GambatteSyncSettings.FrameLengthType.EqualLengthFrames };

		private static readonly GambatteSyncSettings GambatteSyncSettings_GBC_USEBIOS = new() { ConsoleMode = GambatteSyncSettings.ConsoleModeType.GBC, EnableBIOS = true, FrameLength = GambatteSyncSettings.FrameLengthType.EqualLengthFrames };

		private static readonly GBSyncSettings GBHawkSyncSettings_GB = new() { ConsoleMode = GBSyncSettings.ConsoleModeType.GB };

		private static readonly GBSyncSettings GBHawkSyncSettings_GBC = new() { ConsoleMode = GBSyncSettings.ConsoleModeType.GBC };

		private static readonly SameboySettings SameBoySettings = new() { ColorCorrection = SameboySettings.ColorCorrectionMode.DISABLED };

		private static readonly SameboySyncSettings SameBoySyncSettings_GB_NOBIOS = new() { ConsoleMode = SameboySyncSettings.GBModel.GB_MODEL_DMG_B, EnableBIOS = false };

		private static readonly SameboySyncSettings SameBoySyncSettings_GB_USEBIOS = new() { ConsoleMode = SameboySyncSettings.GBModel.GB_MODEL_DMG_B, EnableBIOS = true };

		private static readonly SameboySyncSettings SameBoySyncSettings_GBC_C_NOBIOS = new() { ConsoleMode = SameboySyncSettings.GBModel.GB_MODEL_CGB_C, EnableBIOS = false };

		private static readonly SameboySyncSettings SameBoySyncSettings_GBC_C_USEBIOS = new() { ConsoleMode = SameboySyncSettings.GBModel.GB_MODEL_CGB_C, EnableBIOS = true };

		private static readonly SameboySyncSettings SameBoySyncSettings_GBC_D_NOBIOS = new() { ConsoleMode = SameboySyncSettings.GBModel.GB_MODEL_CGB_D, EnableBIOS = false };

		private static readonly SameboySyncSettings SameBoySyncSettings_GBC_D_USEBIOS = new() { ConsoleMode = SameboySyncSettings.GBModel.GB_MODEL_CGB_D, EnableBIOS = true };

		public static readonly IReadOnlyDictionary<int, int> MattCurriePaletteMap = new Dictionary<int, int>
		{
			[0x0F3EAA] = 0x0000FF,
			[0x137213] = 0x009C00,
			[0x187890] = 0x0063C6,
			[0x695423] = 0x737300,
			[0x7BC8D5] = 0x6BBDFF,
			[0x7F3848] = 0x943939,
			[0x83C656] = 0x7BFF31,
			[0x9D7E34] = 0xADAD00,
			[0xE18096] = 0xFF8484,
			[0xE8BA4D] = 0xFFFF00,
			[0xF8F8F8] = 0xFFFFFF,
		};

		public static readonly IReadOnlyDictionary<int, int> UnVividGBCPaletteMap = new Dictionary<int, int>
		{
			[0x0063C5] = 0x0063C6,
			[0x00CE00] = 0x199619,
			[0x089C84] = 0x21926C,
			[0x424242] = 0x404040,
			[0x52AD52] = 0x5B925B,
			[0x943A3A] = 0x943939,
			[0xA5A5A5] = 0xA0A0A0,
			[0xAD52AD] = 0x9D669D,
			[0xFFFFFF] = 0xF8F8F8,
		};

		public static readonly IReadOnlyDictionary<int, int> UnVividGBPaletteMap = new Dictionary<int, int>
		{
			[0x525252] = 0x555555,
			[0xADADAD] = 0xAAAAAA,
		};

		private static bool AddEmbeddedGBBIOS(this DummyFrontend.EmbeddedFirmwareProvider efp, ConsoleVariant variant)
			=> variant.IsColour()
				? efp.AddIfExists(new("GBC", "World"), false ? "res.fw.GBC__World__AGB.bin" : "res.fw.GBC__World__CGB.bin")
				: efp.AddIfExists(new("GB", "World"), "res.fw.GB__World__DMG.bin");

		public static TestUtils.TestSuccessState GBScreenshotsEqual(
			Stream expectFile,
			Image? actualUnnormalised,
			bool expectingNotEqual,
			CoreSetup setup,
			(string Suite, string Case) id,
			IReadOnlyDictionary<int, int>? extraPaletteMap = null)
		{
			if (actualUnnormalised is null)
			{
				Assert.Fail("actual screenshot was null");
				return TestUtils.TestSuccessState.Failure; // never hit
			}
			var actual = NormaliseGBScreenshot(actualUnnormalised, setup);
//			ImageUtils.PrintPalette(Image.FromStream(expectFile), "expected image", actual, "actual image (after normalisation, before extra map)");
			return ImageUtils.ScreenshotsEqualMagickDotNET(
				expectFile,
				extraPaletteMap is null ? actual : ImageUtils.PaletteSwap(actual, extraPaletteMap),
				expectingNotEqual,
				id);
		}

		public static GambatteSyncSettings GetGambatteSyncSettings(ConsoleVariant variant, bool biosAvailable)
			=> variant.IsColour()
				? biosAvailable ? GambatteSyncSettings_GBC_USEBIOS : GambatteSyncSettings_GBC_NOBIOS
				: biosAvailable ? GambatteSyncSettings_GB_USEBIOS : GambatteSyncSettings_GB_NOBIOS;

		public static GBSyncSettings GetGBHawkSyncSettings(ConsoleVariant variant)
			=> variant.IsColour()
				? GBHawkSyncSettings_GBC
				: GBHawkSyncSettings_GB;

		public static SameboySyncSettings GetSameBoySyncSettings(ConsoleVariant variant, bool biosAvailable)
			=> variant switch
			{
				ConsoleVariant.CGB_C => biosAvailable ? SameBoySyncSettings_GBC_C_USEBIOS : SameBoySyncSettings_GBC_C_NOBIOS,
				ConsoleVariant.CGB_D => biosAvailable ? SameBoySyncSettings_GBC_D_USEBIOS : SameBoySyncSettings_GBC_D_NOBIOS,
				ConsoleVariant.DMG or ConsoleVariant.DMG_B => biosAvailable ? SameBoySyncSettings_GB_USEBIOS : SameBoySyncSettings_GB_NOBIOS,
				_ => throw new InvalidOperationException()
			};

		public static DummyFrontend.ClassInitCallbackDelegate InitGBCore(CoreSetup setup, string romFilename, byte[] rom)
			=> (efp, _, coreComm) =>
			{
				if (setup.UseBIOS && !efp.AddEmbeddedGBBIOS(setup.Variant)) Assert.Inconclusive("BIOS not provided");
				var game = Database.GetGameInfo(rom, romFilename);
				IEmulator newCore = setup.CoreName switch
				{
					CoreNames.Gambatte => new Gameboy(coreComm, game, rom, GambatteSettings, GetGambatteSyncSettings(setup.Variant, setup.UseBIOS), deterministic: true),
					CoreNames.GbHawk => new GBHawk(coreComm, game, rom, new(), GetGBHawkSyncSettings(setup.Variant)),
					CoreNames.Sameboy => new Sameboy(new CoreLoadParameters<SameboySettings, SameboySyncSettings>
					{
						Comm = coreComm,
						DeterministicEmulationRequested = true,
						Game = game,
						Roms = { new DummyRomAsset(rom) },
						Settings = SameBoySettings,
						SyncSettings = GetSameBoySyncSettings(setup.Variant, setup.UseBIOS),
					}),
					_ => throw new InvalidOperationException("unknown GB core")
				};
				var biosWaitDuration = setup.UseBIOS || setup.CoreName is CoreNames.Sameboy
					? setup.Variant.IsColour()
						? 186
						: 334
					: 0;
				return (newCore, biosWaitDuration);
			};

		public static bool IsColour(this ConsoleVariant variant)
			=> variant is ConsoleVariant.CGB_C or ConsoleVariant.CGB_D;

		/// <summary>converts Gambatte's GBC palette to GBHawk's; GB palette is the same</summary>
		public static Image NormaliseGBScreenshot(Image img, CoreSetup setup)
			=> setup.CoreName switch
			{
				CoreNames.Gambatte => ImageUtils.PaletteSwap(img, setup.Variant.IsColour() ? UnVividGBCPaletteMap : UnVividGBPaletteMap),
				CoreNames.Sameboy => setup.Variant.IsColour() ? ImageUtils.PaletteSwap(img, UnVividGBCPaletteMap) : img,
				_ => img
			};
	}
}
