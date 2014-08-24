using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Linq;

using BizHawk.Emulation.Common;
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

			var clearitem = new ToolStripMenuItem { Text = "&Clear", Enabled = !recent.Frozen };
			clearitem.Click += (o, ev) => recent.Clear();
			items.Add(clearitem);

			var freezeitem = new ToolStripMenuItem { Text = recent.Frozen ? "&Unfreeze" : "&Freeze" };
			freezeitem.Click += (o, ev) => recent.Frozen ^= true;
			items.Add(freezeitem);

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
			if (recent.Frozen)
			{
				var result = MessageBox.Show("Could not open " + path, "File not found", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
			else
			{
				var result = MessageBox.Show("Could not open " + path + "\nRemove from list?", "File not found", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
				if (result == DialogResult.Yes)
				{
					recent.Remove(path);
				}
			}

			GlobalWin.Sound.StartSound();
		}

		public static void FreezeAll(this IEnumerable<Watch> watches)
		{
			Global.CheatList.AddRange(
				watches
				.Where(w => !w.IsSeparator)
				.Select(w => new Cheat(w, w.Value ?? 0)));
		}

		public static void UnfreezeAll(this IEnumerable<Watch> watches)
		{
			Global.CheatList.RemoveRange(watches.Where(watch => !watch.IsSeparator));
		}

		public static IEnumerable<ToolStripItem> MenuItems(this MemoryDomainList domains, Action<string> setCallback, string selected = "", int? maxSize = null)
		{
			foreach (var domain in domains)
			{
				var name = domain.Name;
				var item = new ToolStripMenuItem
				{
					Text = name,
					Enabled = !(maxSize.HasValue && domain.Size > maxSize.Value),
					Checked = name == selected
				};
				item.Click += (o, ev) => setCallback(name);

				yield return item;
			}
		}
	}
}
