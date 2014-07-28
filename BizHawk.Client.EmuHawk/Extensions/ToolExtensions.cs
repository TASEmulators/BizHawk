using System;
using System.Collections.Generic;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk.ToolExtensions
{
	public static class ToolExtensions
	{
		public static ToolStripItem[] RecentMenu(this RecentFiles recent, Action<string> loadFileCallback, bool autoload = false)
		{
			var items = new List<ToolStripItem>();

			if (recent.Empty)
			{
				var none = new ToolStripMenuItem { Enabled = false, Text = "None" };
				items.Add(none);
			}
			else
			{
				foreach (var filename in recent)
				{
					var temp = filename;
					var item = new ToolStripMenuItem { Text = temp };
					item.Click += (o, ev) => loadFileCallback(temp);
					items.Add(item);
				}
			}

			items.Add(new ToolStripSeparator());

			var clearitem = new ToolStripMenuItem { Text = "&Clear" };
			clearitem.Click += (o, ev) => recent.Clear();
			items.Add(clearitem);

			if (autoload)
			{
				var auto = new ToolStripMenuItem { Text = "&Autoload", Checked = recent.AutoLoad };
				auto.Click += (o, ev) => recent.ToggleAutoLoad();
				items.Add(auto);
			}

			return items.ToArray();
		}

		public static void HandleLoadError(this RecentFiles recent, string path)
		{
			GlobalWin.Sound.StopSound();
			var result = MessageBox.Show("Could not open " + path + "\nRemove from list?", "File not found", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
			if (result == DialogResult.Yes)
			{
				recent.Remove(path);
			}

			GlobalWin.Sound.StartSound();
		}
	}
}
