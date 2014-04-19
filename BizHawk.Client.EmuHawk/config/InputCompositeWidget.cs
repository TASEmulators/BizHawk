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
			foreach (var str in InputWidget.SpecialBindings)
			{
				var tsi = new ToolStripMenuItem(str);
				DropdownMenu.Items.Add(tsi);
			}
		
			btnSpecial.ContextMenuStrip = DropdownMenu;
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
			widget.SetBinding(e.ClickedItem.Text);
		}
	}
}
