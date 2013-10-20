using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.MultiClient
{
	class ToolHelpers
	{
		public static ToolStripMenuItem GenerateAutoLoadItem(RecentFiles recent)
		{
			var auto = new ToolStripMenuItem { Text = "&Auto-Load", Checked = recent.AutoLoad };
			auto.Click += (o, ev) => recent.ToggleAutoLoad();
			return auto;
		}

		public static ToolStripItem[] GenerateRecentMenu(RecentFiles recent, Action<string> loadFileCallback)
		{
			var items = new List<ToolStripItem>();

			if (recent.Empty)
			{
				var none = new ToolStripMenuItem { Enabled = false, Text = "None" };
				items.Add(none);
			}
			else
			{
				foreach (string filename in recent)
				{
					string temp = filename;
					var item = new ToolStripMenuItem { Text = temp };
					item.Click += (o, ev) => loadFileCallback(temp);
					items.Add(item);
				}
			}

			items.Add(new ToolStripSeparator());

			var clearitem = new ToolStripMenuItem { Text = "&Clear" };
			clearitem.Click += (o, ev) => recent.Clear();
			items.Add(clearitem);

			return items.ToArray();
		}

		public static void HandleLoadError(RecentFiles recent, string path)
		{
			GlobalWinF.Sound.StopSound();
			DialogResult result = MessageBox.Show("Could not open " + path + "\nRemove from list?", "File not found", MessageBoxButtons.YesNo, MessageBoxIcon.Error);
			if (result == DialogResult.Yes)
			{
				recent.Remove(path);
			}

			GlobalWinF.Sound.StartSound();
		}

		public static ToolStripMenuItem[] GenerateMemoryDomainMenuItems(Action<int> SetCallback, string SelectedDomain = "", int? maxSize = null)
		{
			var items = new List<ToolStripMenuItem>();

			if (GlobalWinF.Emulator.MemoryDomains.Any())
			{
				int counter = 0;
				foreach (var domain in GlobalWinF.Emulator.MemoryDomains)
				{
					string temp = domain.ToString();
					var item = new ToolStripMenuItem { Text = temp };

					int index = counter;
					item.Click += (o, ev) => SetCallback(index);

					if (temp == SelectedDomain)
					{
						item.Checked = true;
					}

					if (maxSize.HasValue && domain.Size > maxSize.Value)
					{
						item.Enabled = false;
					}

					items.Add(item);
					counter++;
				}
			}

			return items.ToArray();
		}

		public static void PopulateMemoryDomainDropdown(ref ComboBox dropdown, MemoryDomain startDomain)
		{
			dropdown.Items.Clear();
			if (GlobalWinF.Emulator.MemoryDomains.Count > 0)
			{
				foreach (var domain in GlobalWinF.Emulator.MemoryDomains)
				{
					var result = dropdown.Items.Add(domain.ToString());
					if (domain.Name == startDomain.Name)
					{
						dropdown.SelectedIndex = result;
					}
				}
			}
		}

		public static void UpdateCheatRelatedTools()
		{
			GlobalWinF.MainForm.RamWatch1.UpdateValues();
			GlobalWinF.MainForm.HexEditor1.UpdateValues();
			GlobalWinF.MainForm.Cheats_UpdateValues();
			GlobalWinF.MainForm.RamSearch1.UpdateValues();
			GlobalWinF.MainForm.UpdateCheatStatus();
		}

		public static void UnfreezeAll()
		{
			GlobalWinF.CheatList.DisableAll();
			UpdateCheatRelatedTools();
		}

		public static void FreezeAddress(List<Watch> watches)
		{
			foreach(var watch in watches)
			{
				if (!watch.IsSeparator)
				{
					GlobalWinF.CheatList.Add(
						new Cheat(watch, watch.Value.Value, compare: null, enabled: true)
					);
				}
			}

			UpdateCheatRelatedTools();
		}

		public static void UnfreezeAddress(List<Watch> watches)
		{
			foreach (var watch in watches)
			{
				if (!watch.IsSeparator)
				{
					GlobalWinF.CheatList.Remove(watch);
				}
			}

			UpdateCheatRelatedTools();
		}

		public static void ViewInHexEditor(MemoryDomain domain, IEnumerable<int> addresses)
		{
			GlobalWinF.MainForm.LoadHexEditor();
			GlobalWinF.MainForm.HexEditor1.SetDomain(domain);
			GlobalWinF.MainForm.HexEditor1.SetToAddresses(addresses.ToList());
		}

		public static MemoryDomain DomainByName(string name)
		{
			//Attempts to find the memory domain by name, if it fails, it defaults to index 0
			foreach (MemoryDomain domain in GlobalWinF.Emulator.MemoryDomains)
			{
				if (domain.Name == name)
				{
					return domain;
				}
			}

			return GlobalWinF.Emulator.MainMemory;
		}

		public static void AddColumn(ListView listView, string columnName, bool enabled, int columnWidth)
		{
			if (enabled)
			{
				if (listView.Columns[columnName] == null)
				{
					ColumnHeader column = new ColumnHeader
					{
						Name = columnName,
						Text = columnName.Replace("Column", ""),
						Width = columnWidth,
					};

					listView.Columns.Add(column);
				}
			}
		}
	}
}
