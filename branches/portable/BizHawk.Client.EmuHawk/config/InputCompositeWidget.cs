using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.Client.EmuHawk
{
	public partial class InputCompositeWidget : UserControl
	{
		public InputCompositeWidget()
		{
			InitializeComponent();

			DropdownMenu = new ContextMenuStrip();

			DropdownMenu.ItemClicked += new ToolStripItemClickedEventHandler(DropdownMenu_ItemClicked);
			DropdownMenu.PreviewKeyDown += new PreviewKeyDownEventHandler(DropdownMenu_PreviewKeyDown);
			foreach (var str in InputWidget.SpecialBindings)
			{
				var tsi = new ToolStripMenuItem(str);
				DropdownMenu.Items.Add(tsi);
			}
		
			btnSpecial.ContextMenuStrip = DropdownMenu;

			widget.CompositeWidget = this;
		}

		void DropdownMenu_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
		{
			//suppress handling of ALT keys, so that we can receive them as binding modifiers
			if (e.KeyCode == Keys.Menu)
				e.IsInputKey = true;
		}

		public void TabNext()
		{
			Parent.SelectNextControl(btnSpecial, true, true, true, true);
		}

		ContextMenuStrip DropdownMenu;

		public bool AutoTab { get { return widget.AutoTab; } set { widget.AutoTab = true; } }
		public string WidgetName { get { return widget.WidgetName; } set { widget.WidgetName = value; } }

		public string Bindings { get { return widget.Bindings; } set { widget.Bindings = value; } }

		public void Clear()
		{
			widget.Clear();
		}

		private void btnSpecial_Click(object sender, EventArgs e)
		{
			DropdownMenu.Show(Control.MousePosition);
		}

		void DropdownMenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
		{
			Input.ModifierKey mods = new Input.ModifierKey();
			
			if ((Control.ModifierKeys & Keys.Shift) != 0)
				mods |= Input.ModifierKey.Shift;
			if ((Control.ModifierKeys & Keys.Control) != 0)
				mods |= Input.ModifierKey.Control;
			if ((Control.ModifierKeys & Keys.Alt) != 0)
				mods |= Input.ModifierKey.Alt;

			Input.LogicalButton lb = new Input.LogicalButton(e.ClickedItem.Text,mods);

			widget.SetBinding(lb.ToString());
		}
	}
}
