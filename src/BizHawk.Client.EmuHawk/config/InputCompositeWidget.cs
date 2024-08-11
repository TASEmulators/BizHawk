using System.Collections.Generic;
using System.Windows.Forms;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class InputCompositeWidget : UserControl
	{
		private readonly IReadOnlyList<string> _effectiveModList;

		public InputCompositeWidget(IReadOnlyList<string> effectiveModList)
		{
			_effectiveModList = effectiveModList;

			InitializeComponent();
			btnSpecial.Image = Properties.Resources.ArrowBlackDown;

			_dropdownMenu = new ContextMenuStrip();
			_dropdownMenu.ItemClicked += DropdownMenu_ItemClicked;
			_dropdownMenu.PreviewKeyDown += DropdownMenu_PreviewKeyDown;
			foreach (var spec in InputWidget.SpecialBindings)
			{
				var tsi = new ToolStripMenuItem(spec.BindingName) { ToolTipText = spec.TooltipText };
				_dropdownMenu.Items.Add(tsi);
			}
		
			btnSpecial.ContextMenuStrip = _dropdownMenu;

			widget.CompositeWidget = this;
		}

		private static readonly string WidgetTooltipText = "* Escape clears a key mapping\r\n* Disable Auto Tab to multiply bind";
		private ToolTip _tooltip;
		private string _bindingTooltipText;

		public void SetupTooltip(ToolTip tip, string bindingText)
		{
			_tooltip = tip;
			_tooltip.SetToolTip(btnSpecial, "Click here for special tricky bindings");
			_bindingTooltipText = bindingText;
			RefreshTooltip();
		}

		public void RefreshTooltip()
		{
			string widgetText = $"Current Binding: {widget.Text}";
			if (_bindingTooltipText != null)
			{
				widgetText = $"{widgetText}\r\n---\r\n{_bindingTooltipText}";
			}

			widgetText = $"{widgetText}\r\n---\r\n{WidgetTooltipText}";
			_tooltip.SetToolTip(widget, widgetText);
		}

		private void DropdownMenu_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
		{
			// suppress handling of ALT keys, so that we can receive them as binding modifiers
			if (e.KeyCode == Keys.Menu)
			{
				e.IsInputKey = true;
			}
		}

		public void TabNext()
		{
			Parent.SelectNextControl(btnSpecial, true, true, true, true);
		}

		private readonly ContextMenuStrip _dropdownMenu;

		public bool AutoTab { get => widget.AutoTab; set => widget.AutoTab = value; }
		public string WidgetName { get => widget.WidgetName; set => widget.WidgetName = value; }
		public string Bindings { get => widget.Bindings; set => widget.Bindings = value; }

		public void Clear()
		{
			widget.ClearAll();
		}

		private void BtnSpecial_Click(object sender, EventArgs e)
		{
			_dropdownMenu.Show(MousePosition);
		}

		private void DropdownMenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
		{
			var mods = 0U;

			if ((ModifierKeys & Keys.Shift) != 0)
			{
				mods |= LogicalButton.MASK_SHIFT;
			}

			if ((ModifierKeys & Keys.Control) != 0)
			{
				mods |= LogicalButton.MASK_CTRL;
			}

			if ((ModifierKeys & Keys.Alt) != 0)
			{
				mods |= LogicalButton.MASK_ALT;
			}

			LogicalButton lb = new(e.ClickedItem.Text, mods, () => _effectiveModList);

			widget.SetBinding(lb.ToString());
		}
	}
}
