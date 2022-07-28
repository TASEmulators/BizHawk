#nullable enable

using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.Nintendo.SNES
{
	public interface IBSNESForGfxDebugger : ISpecializedEmulatorService
	{
		public interface SettingsObj
		{
			bool ShowBG1_0 { get; set; }

			bool ShowBG1_1 { get; set; }

			bool ShowBG2_0 { get; set; }

			bool ShowBG2_1 { get; set; }

			bool ShowBG3_0 { get; set; }

			bool ShowBG3_1 { get; set; }

			bool ShowBG4_0 { get; set; }

			bool ShowBG4_1 { get; set; }

			bool ShowOBJ_0 { get; set; }

			bool ShowOBJ_1 { get; set; }

			bool ShowOBJ_2 { get; set; }

			bool ShowOBJ_3 { get; set; }
		}

		SnesColors.ColorType CurrPalette { get; }

		ScanlineHookManager? ScanlineHookManager { get; }

		ISNESGraphicsDecoder CreateGraphicsDecoder();

		SettingsObj GetSettings();

		void PutSettings(SettingsObj s);

		void SetPalette(SnesColors.ColorType palette);
	}
}
