using System.Collections.Generic;
using System.Drawing;
using System.IO;

using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores;
using BizHawk.Emulation.Cores.Consoles.Sega.gpgx;

using SMSHawk = BizHawk.Emulation.Cores.Sega.MasterSystem.SMS;

using static BizHawk.Emulation.Cores.Consoles.Sega.gpgx.GPGX;
using static BizHawk.Emulation.Cores.Consoles.Sega.gpgx.LibGPGX.InitSettings;
using static BizHawk.Emulation.Cores.Sega.MasterSystem.SMS;

namespace BizHawk.Tests.Testroms.SMS
{
	public static class SMSHelper
	{
		public readonly struct CoreSetup
		{
			public const string GPGX_MAME = $"{CoreNames.Gpgx}_MAMESound";

			public const string GPGX_NUKED = $"{CoreNames.Gpgx}_NukedSound";

			public static IReadOnlyCollection<CoreSetup> ValidSetupsFor()
				=> new CoreSetup[]
				{
					new(GPGX_MAME),
					new(GPGX_MAME, useBios: false),
					new(GPGX_NUKED),
					new(GPGX_NUKED, useBios: false),
					new(CoreNames.SMSHawk),
					new(CoreNames.SMSHawk, useBios: false),
				};

			public readonly string CoreName;

			public readonly bool UseBIOS;

			public CoreSetup(string coreName, bool useBios = true)
			{
				CoreName = coreName;
				UseBIOS = useBios;
			}

			public readonly override string ToString()
				=> $"{CoreName}{(UseBIOS ? string.Empty : " (no BIOS)")}";
		}

		private static readonly GPGXSyncSettings GPGXSyncSettings_MAME_NOBIOS = new() { LoadBIOS = false, SMSFMSoundChip = SMSFMSoundChipType.YM2413_MAME };

		private static readonly GPGXSyncSettings GPGXSyncSettings_MAME_USEBIOS = new() { LoadBIOS = true, SMSFMSoundChip = SMSFMSoundChipType.YM2413_MAME };

		private static readonly GPGXSyncSettings GPGXSyncSettings_NUKED_NOBIOS = new() { LoadBIOS = false, SMSFMSoundChip = SMSFMSoundChipType.YM2413_NUKED };

		private static readonly GPGXSyncSettings GPGXSyncSettings_NUKED_USEBIOS = new() { LoadBIOS = true, SMSFMSoundChip = SMSFMSoundChipType.YM2413_NUKED };

		private static readonly SmsSyncSettings SMSHawkSyncSettings_NOBIOS = new() { UseBios = false };

		private static readonly SmsSyncSettings SMSHawkSyncSettings_USEBIOS = new() { UseBios = true };

		private static bool AddEmbeddedSMSBIOS(this DummyFrontend.EmbeddedFirmwareProvider efp)
			=> efp.AddIfExists(new("SMS", "Export"), "res.fw.SMS__Export__U_1_3.bin");

		public static GPGXSyncSettings GetGPGXSyncSettings(string coreVariant, bool biosAvailable)
			=> coreVariant is CoreSetup.GPGX_NUKED
				? biosAvailable ? GPGXSyncSettings_NUKED_USEBIOS : GPGXSyncSettings_NUKED_NOBIOS
				: biosAvailable ? GPGXSyncSettings_MAME_USEBIOS : GPGXSyncSettings_MAME_NOBIOS;

		public static SmsSyncSettings GetSMSHawkSyncSettings(bool biosAvailable)
			=> biosAvailable ? SMSHawkSyncSettings_USEBIOS : SMSHawkSyncSettings_NOBIOS;

		public static DummyFrontend.ClassInitCallbackDelegate InitSMSCore(CoreSetup setup, string romFilename, byte[] rom)
			=> (efp, _, coreComm) =>
			{
				efp.UseReflectionCache(ReflectionCache.EmbeddedResourceList(), ReflectionCache.EmbeddedResourceStream);
				if (setup.UseBIOS && !efp.AddEmbeddedSMSBIOS()) Assert.Inconclusive("BIOS not provided");
				var game = Database.GetGameInfo(rom, romFilename);
				IEmulator newCore = setup.CoreName switch
				{
					CoreSetup.GPGX_MAME or CoreSetup.GPGX_NUKED => new GPGX(new CoreLoadParameters<GPGX.GPGXSettings, GPGX.GPGXSyncSettings>
					{
						Comm = coreComm,
						DeterministicEmulationRequested = true,
						Game = game,
						Roms = { new DummyRomAsset(rom, VSystemID.Raw.SMS) },
						Settings = new(),
						SyncSettings = GetGPGXSyncSettings(setup.CoreName, setup.UseBIOS),
					}),
					CoreNames.SMSHawk => new SMSHawk(coreComm, game, rom, new(), GetSMSHawkSyncSettings(setup.UseBIOS)),
					_ => throw new InvalidOperationException("unknown SMS core"),
				};
				return (newCore, setup.UseBIOS ? 388 : 5);
			};

		public static TestUtils.TestSuccessState SMSScreenshotsEqual(
			Stream expectFile,
			Image? actual,
			bool expectingNotEqual,
			CoreSetup setup,
			(string Suite, string Case) id,
			IReadOnlyDictionary<int, int>? extraPaletteMap = null)
		{
			if (actual is null)
			{
				Assert.Fail("actual screenshot was null");
				return TestUtils.TestSuccessState.Failure; // never hit
			}
			return ImageUtils.ScreenshotsEqualMagickDotNET(
				expectFile,
				extraPaletteMap is null ? actual : ImageUtils.PaletteSwap(actual, extraPaletteMap),
				expectingNotEqual,
				id);
		}
	}
}
