using System.Drawing;
using System.Windows.Forms;
using BizHawk.Client.EmuHawk.Properties;

namespace BizHawk.Client.EmuHawk
{
	public partial class RamWatch
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
			SetupAccessibility();
		}

		private void CreateAccessibleToolbar()
		{
			toolStrip1.Visible = false;

			_toolbarImageList = new ImageList();
			_toolbarImageList.ImageSize = new Size(20, 20);
			_toolbarImageList.ColorDepth = ColorDepth.Depth32Bit;
			_toolbarImageList.Images.Add("New", Resources.NewFile);
			_toolbarImageList.Images.Add("Open", Resources.OpenFile);
			_toolbarImageList.Images.Add("Save", Resources.SaveAs);
			_toolbarImageList.Images.Add("NewWatch", Resources.Find);
			_toolbarImageList.Images.Add("Edit", Resources.Pencil);
			_toolbarImageList.Images.Add("Remove", Resources.Delete);
			_toolbarImageList.Images.Add("Clear", Resources.Refresh);
			_toolbarImageList.Images.Add("Duplicate", Resources.Duplicate);
			_toolbarImageList.Images.Add("Split", Resources.Placeholder);
			_toolbarImageList.Images.Add("Poke", Resources.Poke);
			_toolbarImageList.Images.Add("Freeze", Resources.Freeze);
			_toolbarImageList.Images.Add("Separator", Resources.InsertSeparator);
			_toolbarImageList.Images.Add("Up", Resources.MoveUp);
			_toolbarImageList.Images.Add("Down", Resources.MoveDown);

			_toolbarListView = new ListView
			{
				Name = "ToolbarListView",
				AccessibleName = "RAM Watch Toolbar",
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

			_toolbarListView.Items.Add(new ListViewItem("New List", "New") { Tag = "New" });
			_toolbarListView.Items.Add(new ListViewItem("Open", "Open") { Tag = "Open" });
			_toolbarListView.Items.Add(new ListViewItem("Save", "Save") { Tag = "Save" });
			_toolbarListView.Items.Add(new ListViewItem("New Watch", "NewWatch") { Tag = "NewWatch" });
			_toolbarListView.Items.Add(new ListViewItem("Edit Watch", "Edit") { Tag = "Edit" });
			_toolbarListView.Items.Add(new ListViewItem("Remove", "Remove") { Tag = "Remove" });
			_toolbarListView.Items.Add(new ListViewItem("Clear Counts", "Clear") { Tag = "Clear" });
			_toolbarListView.Items.Add(new ListViewItem("Duplicate", "Duplicate") { Tag = "Duplicate" });
			_toolbarListView.Items.Add(new ListViewItem("Split", "Split") { Tag = "Split" });
			_toolbarListView.Items.Add(new ListViewItem("Poke", "Poke") { Tag = "Poke" });
			_toolbarListView.Items.Add(new ListViewItem("Freeze", "Freeze") { Tag = "Freeze" });
			_toolbarListView.Items.Add(new ListViewItem("Separator", "Separator") { Tag = "Separator" });
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
				case "New": NewListMenuItem_Click(this, EventArgs.Empty); break;
				case "Open": OpenMenuItem_Click(this, EventArgs.Empty); break;
				case "Save": SaveMenuItem_Click(this, EventArgs.Empty); break;
				case "NewWatch": NewWatchMenuItem_Click(this, EventArgs.Empty); break;
				case "Edit": EditWatchMenuItem_Click(this, EventArgs.Empty); break;
				case "Remove": RemoveWatchMenuItem_Click(this, EventArgs.Empty); break;
				case "Clear": ClearChangeCountsMenuItem_Click(this, EventArgs.Empty); break;
				case "Duplicate": DuplicateWatchMenuItem_Click(this, EventArgs.Empty); break;
				case "Split": SplitWatchMenuItem_Click(this, EventArgs.Empty); break;
				case "Poke": PokeAddressMenuItem_Click(this, EventArgs.Empty); break;
				case "Freeze": FreezeAddressMenuItem_Click(this, EventArgs.Empty); break;
				case "Separator": InsertSeparatorMenuItem_Click(this, EventArgs.Empty); break;
				case "Up": MoveUpMenuItem_Click(this, EventArgs.Empty); break;
				case "Down": MoveDownMenuItem_Click(this, EventArgs.Empty); break;
			}
		}

		private void SetupAccessibility()
		{
			AccessibleName = "RAM Watch";
			AccessibleDescription = "RAM Watch tool for monitoring memory addresses";
			AccessibleRole = AccessibleRole.Window;

			WatchListView.AccessibleName = "Watch List";
			WatchListView.AccessibleDescription = "List of watched memory addresses";
			WatchListView.AccessibleRole = AccessibleRole.List;
		}
	}
}
