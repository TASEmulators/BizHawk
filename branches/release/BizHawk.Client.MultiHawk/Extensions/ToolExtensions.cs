using System;
using System.IO;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Linq;

using BizHawk.Common;

using BizHawk.Emulation.Common;
using BizHawk.Client.Common;

namespace BizHawk.Client.MultiHawk.ToolExtensions
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
					//TODO - do TSMI and TSDD need disposing? yuck
					var temp = filename;
					var item = new ToolStripMenuItem { Text = temp };
					items.Add(item);

					item.Click += (o, ev) =>
					{
						loadFileCallback(temp);
					};
					
					//TODO - use standard methods to split filename (hawkfile acquire?)
					var hf = new HawkFile();
					hf.Parse(temp);
					bool canExplore = true;
					if (!File.Exists(hf.FullPathWithoutMember))
						canExplore = false;

					var tsdd = new ToolStripDropDownMenu();

					// TODO
					if (canExplore)
					{
						//make a menuitem to show the last modified timestamp
					//	var timestamp = File.GetLastWriteTime(hf.FullPathWithoutMember);
					//	var tsmiTimestamp = new ToolStripLabel { Text = timestamp.ToString() };

					//	tsdd.Items.Add(tsmiTimestamp);
					//	tsdd.Items.Add(new ToolStripSeparator());



					//	if (hf.IsArchive)
					//	{
					//		//make a menuitem to let you copy the path
					//		var tsmiCopyCanonicalPath = new ToolStripMenuItem { Text = "&Copy Canonical Path" };
					//		tsmiCopyCanonicalPath.Click += (o, ev) => { System.Windows.Forms.Clipboard.SetText(temp); };
					//		tsdd.Items.Add(tsmiCopyCanonicalPath);

					//		var tsmiCopyArchivePath = new ToolStripMenuItem { Text = "Copy Archive Path" };
					//		tsmiCopyArchivePath.Click += (o, ev) => { System.Windows.Forms.Clipboard.SetText(hf.FullPathWithoutMember); };
					//		tsdd.Items.Add(tsmiCopyArchivePath);

					//		var tsmiOpenArchive = new ToolStripMenuItem { Text = "Open &Archive" };
					//		tsmiOpenArchive.Click += (o, ev) => { System.Diagnostics.Process.Start(hf.FullPathWithoutMember); };
					//		tsdd.Items.Add(tsmiOpenArchive);
					//	}
					//	else
					//	{
					//		//make a menuitem to let you copy the path
					//		var tsmiCopyPath = new ToolStripMenuItem { Text = "&Copy Path" };
					//		tsmiCopyPath.Click += (o, ev) => { System.Windows.Forms.Clipboard.SetText(temp); };
					//		tsdd.Items.Add(tsmiCopyPath);
					//	}

					//	tsdd.Items.Add(new ToolStripSeparator());

					//	//make a menuitem to let you explore to it
					//	var tsmiExplore = new ToolStripMenuItem { Text = "&Explore" };
					//	string explorePath = "\"" + hf.FullPathWithoutMember + "\"";
					//	tsmiExplore.Click += (o, ev) => { System.Diagnostics.Process.Start("explorer.exe", "/select, " + explorePath); };
					//	tsdd.Items.Add(tsmiExplore);

					//	var tsmiCopyFile = new ToolStripMenuItem { Text = "Copy &File" };
					//	var lame = new System.Collections.Specialized.StringCollection();
					//	lame.Add(hf.FullPathWithoutMember);
					//	tsmiCopyFile.Click += (o, ev) => { System.Windows.Forms.Clipboard.SetFileDropList(lame); };
					//	tsdd.Items.Add(tsmiCopyFile);

					//	var tsmiTest = new ToolStripMenuItem { Text = "&Shell Context Menu" };
					//	tsmiTest.Click += (o, ev) => {
					//		var si = new GongSolutions.Shell.ShellItem(hf.FullPathWithoutMember);
					//		var scm = new GongSolutions.Shell.ShellContextMenu(si);
					//		var tsddi = o as ToolStripDropDownItem;
					//		tsddi.Owner.Update();
					//		scm.ShowContextMenu(tsddi.Owner, new System.Drawing.Point(0, 0));
					//	};
					//	tsdd.Items.Add(tsmiTest);

					//	tsdd.Items.Add(new ToolStripSeparator());
					//}
					//else
					//{
					//	//make a menuitem to show the last modified timestamp
					//	var tsmiMissingFile = new ToolStripLabel { Text = "-Missing-" };
					//	tsdd.Items.Add(tsmiMissingFile);
					//	tsdd.Items.Add(new ToolStripSeparator());
					//}
					
					////in either case, make a menuitem to let you remove the path
					//var tsmiRemovePath = new ToolStripMenuItem { Text = "&Remove" };
					//tsmiRemovePath.Click += (o, ev) => { recent.Remove(temp); };

					//tsdd.Items.Add(tsmiRemovePath);

					//////experiment of popping open a submenu. doesnt work well.
					////item.MouseDown += (o, mev) =>
					////{
					////  if (mev.Button != MouseButtons.Right) return;
					////  //location of the menu containing this item that was just rightclicked
					////  var pos = item.Owner.Bounds.Location;
					////  //the offset within that menu of this item
					////  var tsddi = item as ToolStripDropDownItem;
					////  pos.Offset(tsddi.Bounds.Location);
					////  //the offset of the click
					////  pos.Offset(mev.Location);
					////	//tsdd.OwnerItem = item; //has interesting promise, but breaks things otherwise
					////  tsdd.Show(pos);
					////};

					////just add it to the submenu for now
					//item.MouseDown += (o, mev) =>
					//{
					//	if (mev.Button != MouseButtons.Right) return;
					//	if (item.DropDown != null)
					//		item.DropDown = tsdd;
					//	item.ShowDropDown();
					};
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

			// TODO
			//var settingsitem = new ToolStripMenuItem { Text = "&Recent Settings..." };
			//settingsitem.Click += (o, ev) =>
			//{
			//	using (var prompt = new InputPrompt
			//	{
			//		TextInputType = InputPrompt.InputType.Unsigned,
			//		Message = "Number of recent files to track",
			//		InitialValue = recent.MAX_RECENT_FILES.ToString()
			//	})
			//	{
			//		var result = prompt.ShowDialog();
			//		if (result == DialogResult.OK)
			//		{
			//			int val = int.Parse(prompt.PromptText);
			//			if (val > 0)
			//				recent.MAX_RECENT_FILES = val;
			//		}
			//	}
			//};
			//items.Add(settingsitem);

			return items.ToArray();
		}

		public static void HandleLoadError(this RecentFiles recent, string path)
		{
			// TODO
			//GlobalWin.Sound.StopSound();
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

			//GlobalWin.Sound.StartSound();
		}

		public static IEnumerable<ToolStripItem> MenuItems(this IMemoryDomains domains, Action<string> setCallback, string selected = "", int? maxSize = null)
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
