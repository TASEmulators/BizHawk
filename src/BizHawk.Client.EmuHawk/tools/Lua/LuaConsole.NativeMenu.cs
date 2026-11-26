using System.Drawing;
using System.Windows.Forms;
using BizHawk.Client.EmuHawk.Properties;

namespace BizHawk.Client.EmuHawk
{
	public partial class LuaConsole
	{
		// Menu accessibility is handled centrally by FormBase.InstallNativeMenuShim.
		// This file provides the accessible-toolbar replacement (a ListView in place of
		// the standard ToolStrip, since ToolStrip doesn't fire MSAA focus events) and
		// non-menu accessibility properties.

		private ListView _toolbarListView;
		private ImageList _toolbarImageList;

		private void InitializeNativeMenu()
		{
			CreateAccessibleToolbar();
			SetupFormAccessibility();
		}

		private void CreateAccessibleToolbar()
		{
			toolStrip1.Visible = false;

			_toolbarImageList = new ImageList();
			_toolbarImageList.ImageSize = new Size(20, 20);
			_toolbarImageList.ColorDepth = ColorDepth.Depth32Bit;
			_toolbarImageList.Images.Add("New", Resources.NewFile);
			_toolbarImageList.Images.Add("Open", Resources.OpenFile);
			_toolbarImageList.Images.Add("Toggle", Resources.Checkbox);
			_toolbarImageList.Images.Add("Refresh", Resources.Refresh);
			_toolbarImageList.Images.Add("Pause", Resources.Pause);
			_toolbarImageList.Images.Add("Edit", Resources.Pencil);
			_toolbarImageList.Images.Add("Remove", Resources.Delete);
			_toolbarImageList.Images.Add("Copy", Resources.Duplicate);
			_toolbarImageList.Images.Add("Clear", Resources.ClearConsole);
			_toolbarImageList.Images.Add("Up", Resources.MoveUp);
			_toolbarImageList.Images.Add("Down", Resources.MoveDown);

			_toolbarListView = new ListView
			{
				Name = "ToolbarListView",
				AccessibleName = "Script Toolbar",
				AccessibleRole = AccessibleRole.ToolBar,
				View = View.List,
				SmallImageList = _toolbarImageList,
				Dock = DockStyle.Top,
				Height = 30,
				MultiSelect = false,
				TabIndex = 0,
				TabStop = true,
				HideSelection = false,
				Activation = ItemActivation.OneClick,
				FullRowSelect = true,
			};

			_toolbarListView.Items.Add(new ListViewItem("New Script", "New") { Tag = "New" });
			_toolbarListView.Items.Add(new ListViewItem("Open Script", "Open") { Tag = "Open" });
			_toolbarListView.Items.Add(new ListViewItem("Toggle", "Toggle") { Tag = "Toggle" });
			_toolbarListView.Items.Add(new ListViewItem("Refresh", "Refresh") { Tag = "Refresh" });
			_toolbarListView.Items.Add(new ListViewItem("Pause", "Pause") { Tag = "Pause" });
			_toolbarListView.Items.Add(new ListViewItem("Edit", "Edit") { Tag = "Edit" });
			_toolbarListView.Items.Add(new ListViewItem("Remove", "Remove") { Tag = "Remove" });
			_toolbarListView.Items.Add(new ListViewItem("Copy", "Copy") { Tag = "Copy" });
			_toolbarListView.Items.Add(new ListViewItem("Clear", "Clear") { Tag = "Clear" });
			_toolbarListView.Items.Add(new ListViewItem("Move Up", "Up") { Tag = "Up" });
			_toolbarListView.Items.Add(new ListViewItem("Move Down", "Down") { Tag = "Down" });

			_toolbarListView.ItemActivate += ToolbarListView_ItemActivate;
			_toolbarListView.KeyDown += ToolbarListView_KeyDown;

			Controls.Add(_toolbarListView);
			_toolbarListView.BringToFront();
		}

		private void ToolbarListView_ItemActivate(object sender, EventArgs e)
		{
			if (_toolbarListView.SelectedItems.Count == 0) return;
			var tag = _toolbarListView.SelectedItems[0].Tag?.ToString();
			ExecuteToolbarAction(tag);
		}

		private void ToolbarListView_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Space)
			{
				if (_toolbarListView.SelectedItems.Count > 0)
				{
					var tag = _toolbarListView.SelectedItems[0].Tag?.ToString();
					ExecuteToolbarAction(tag);
					e.Handled = true;
				}
			}
		}

		private void ExecuteToolbarAction(string action)
		{
			switch (action)
			{
				case "New": NewScriptMenuItem_Click(this, EventArgs.Empty); break;
				case "Open": OpenScriptMenuItem_Click(this, EventArgs.Empty); break;
				case "Toggle": ToggleScriptMenuItem_Click(this, EventArgs.Empty); break;
				case "Refresh": RefreshScriptMenuItem_Click(this, EventArgs.Empty); break;
				case "Pause": PauseScriptMenuItem_Click(this, EventArgs.Empty); break;
				case "Edit": EditScriptMenuItem_Click(this, EventArgs.Empty); break;
				case "Remove": RemoveScriptMenuItem_Click(this, EventArgs.Empty); break;
				case "Copy": DuplicateScriptMenuItem_Click(this, EventArgs.Empty); break;
				case "Clear": ClearConsoleMenuItem_Click(this, EventArgs.Empty); break;
				case "Up": MoveUpMenuItem_Click(this, EventArgs.Empty); break;
				case "Down": MoveDownMenuItem_Click(this, EventArgs.Empty); break;
			}
		}

		private void SetupFormAccessibility()
		{
			AccessibleName = "Lua Console";
			AccessibleDescription = "Lua scripting console for BizHawk";
			AccessibleRole = AccessibleRole.Window;

			OutputBox.AccessibleName = "Lua Output";
			OutputBox.AccessibleDescription = "Displays output from Lua scripts";

			InputBox.AccessibleName = "Lua Command Input";
			InputBox.AccessibleDescription = "Enter Lua commands here";

			LuaListView.AccessibleName = "Script List";
			LuaListView.AccessibleDescription = "List of loaded Lua scripts";

			groupBox1.AccessibleName = "Output Panel";
		}
	}
}
