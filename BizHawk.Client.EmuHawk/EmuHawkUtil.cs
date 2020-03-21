using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk.CustomControls;
using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Emulation.Cores.Consoles.Nintendo.QuickNES;
using BizHawk.Emulation.Cores.Nintendo.SNES9X;
using Cores = BizHawk.Emulation.Cores;

namespace BizHawk.Client.EmuHawk
{
	public static class EmuHawkUtil
	{
		public static bool EnsureCoreIsAccurate(IEmulator emulator)
		{
			static bool PromptToSwitchCore(string currentCore, string recommendedCore, Action disableCurrentCore)
			{
				using var box = new MsgBox(
					$"While the {currentCore} core is faster, it is not nearly as accurate as {recommendedCore}.{Environment.NewLine}It is recommended that you switch to the {recommendedCore} core for movie recording.{Environment.NewLine}Switch to {recommendedCore}?",
					"Accuracy Warning",
					MessageBoxIcon.Warning);

				box.SetButtons(
					new[] { "Switch", "Continue" },
					new[] { DialogResult.Yes, DialogResult.Cancel });

				box.MaximumSize = UIHelper.Scale(new Size(575, 175));
				box.SetMessageToAutoSize();

				var result = box.ShowDialog();

				if (result != DialogResult.Yes)
				{
					return false;
				}

				disableCurrentCore();
				GlobalWin.MainForm.RebootCore();
				return true;
			}

			return emulator switch
			{
				Snes9x _ => PromptToSwitchCore("Snes9x", "bsnes", () => Global.Config.SnesInSnes9x = false),
				QuickNES _ => PromptToSwitchCore("QuickNes", "NesHawk", () => Global.Config.NesInQuickNes = false),
				_ => true
			};
		}

		/// <remarks>http://stackoverflow.com/questions/139010/how-to-resolve-a-lnk-in-c-sharp</remarks>
		public static string ResolveShortcut(string filename)
		{
			if (filename.Contains("|") || OSTailoredCode.IsUnixHost || !".lnk".Equals(Path.GetExtension(filename), StringComparison.InvariantCultureIgnoreCase)) return filename; // archive internal files are never shortcuts (and choke when analyzing any further)
			var link = new ShellLinkImports.ShellLink();
			const uint STGM_READ = 0;
			((ShellLinkImports.IPersistFile) link).Load(filename, STGM_READ);
#if false
			// TODO: if I can get hold of the hwnd call resolve first. This handles moved and renamed files.
			((ShellLinkImports.IShellLinkW) link).Resolve(hwnd, 0);
#endif
			var sb = new StringBuilder(Win32Imports.MAX_PATH);
			((ShellLinkImports.IShellLinkW) link).GetPath(sb, sb.Capacity, out _, 0);
			return sb.Length == 0 ? filename : sb.ToString(); // maybe? what if it's invalid?
		}
	}
}
