using BizHawk.Client.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public interface IMainFormForRetroAchievements : IMainFormForTools
	{
		IEmulator Emulator { get; }

		bool FrameInch { get; set; }

		GameInfo Game { get; }

		IMovieSession MovieSession { get; }

		FirmwareManager FirmwareManager { get; }

		event BeforeQuickLoadEventHandler QuicksaveLoad;

		SettingsAdapter GetSettingsAdapterForLoadedCoreUntyped();

		bool RebootCore();
	}
}
