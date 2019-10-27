using System;
using System.Drawing;
using System.Windows.Forms;
using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.CustomControls;
using BizHawk.Emulation.Common;
using Cores = BizHawk.Emulation.Cores;

namespace BizHawk.Client.EmuHawk
{
	public static class EmuHawkUtil
	{
		public static bool EnsureCoreIsAccurate(IEmulator emulator)
		{
			static bool PromptToSwitchCore(string currentCore, string recommendedCore, Action disableCurrentCore)
			{
				var box = new MsgBox(
					$"While the {currentCore} core is faster, it is not nearly as accurate as {recommendedCore}.{Environment.NewLine}It is recommended that you switch to the {recommendedCore} core for movie recording. {Environment.NewLine}Switch to {recommendedCore}?",
					"Accuracy Warning",
					MessageBoxIcon.Warning);

				box.SetButtons(
					new[] { "Switch", "Continue" },
					new[] { DialogResult.Yes, DialogResult.Cancel });

				box.MaximumSize = UIHelper.Scale(new Size(575, 175));
				box.SetMessageToAutoSize();

				var result = box.ShowDialog();
				box.Dispose();

				if (result != DialogResult.Yes)
				{
					return false;
				}

				disableCurrentCore();
				GlobalWin.MainForm.RebootCore();
				return true;
			}

			if (emulator is Cores.Nintendo.SNES9X.Snes9x)
			{
				return PromptToSwitchCore("Snes9x", "bsnes", () => Global.Config.SNES_InSnes9x = false);
			}
			if (emulator is Cores.Consoles.Nintendo.QuickNES.QuickNES)
			{
				return PromptToSwitchCore("QuickNes", "NesHawk", () => Global.Config.NES_InQuickNES = false);
			}

			return true;
		}
	}
}
