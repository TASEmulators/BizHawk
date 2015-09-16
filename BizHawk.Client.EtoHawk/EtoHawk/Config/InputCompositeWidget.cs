using System;
using System.Collections.Generic;
using System.Text;
using Eto;
using Eto.Forms;
using BizHawk.Client.EtoHawk;


namespace EtoHawk.Config
{
    public partial class InputCompositeWidget : Panel
    {
        private ControllerConfig _parent;
        
        public InputCompositeWidget(ControllerConfig parent)
        {
            _parent = parent;
            InitializeComponent();

            DropdownMenu = new ContextMenu();

            //DropdownMenu.ItemClicked += DropdownMenu_ItemClicked;
            //DropdownMenu.PreviewKeyDown += DropdownMenu_PreviewKeyDown;
            foreach (var spec in InputWidget.SpecialBindings)
            {
                var tsi = new ButtonMenuItem();
                tsi.Text = spec.BindingName;
                tsi.ToolTip = spec.TooltipText;
                tsi.Click += DropdownMenu_ItemClicked;
                DropdownMenu.Items.Add(tsi);
            }

            //btnSpecial.ContextMenuStrip = DropdownMenu;

            widget.CompositeWidget = this;
        }

        static readonly string WidgetTooltipText = "* Escape clears a key mapping\r\n* Disable Auto Tab to multiply bind";
        string _bindingTooltipText;

        public void SetupTooltip(string bindingText)
        {
            btnSpecial.ToolTip = "Click here for special tricky bindings";
            _bindingTooltipText = bindingText;
            RefreshTooltip();
        }

        public void RefreshTooltip()
        {
            string widgetText = "Current Binding: " + widget.Text;
            if (_bindingTooltipText != null)
                widgetText = widgetText + "\r\n---\r\n" + _bindingTooltipText;
            widgetText = widgetText + "\r\n---\r\n" + WidgetTooltipText;
            widget.ToolTip = widgetText;
        }

        /*void DropdownMenu_PreviewKeyDown(object sender, PreviewKeyDownEventArgs e)
        {
            //suppress handling of ALT keys, so that we can receive them as binding modifiers
            if (e.KeyCode == Keys.Menu)
                e.IsInputKey = true;
        }*/

        public void TabNext()
        {
            Parent.SelectNextControl(btnSpecial, true, true, true, true);
        }

        ContextMenu DropdownMenu;

        public bool AutoTab { get { return widget.AutoTab; } set { widget.AutoTab = value; } }
        public string WidgetName { get { return widget.WidgetName; } set { widget.WidgetName = value; } }

        public string Bindings { get { return widget.Bindings; } set { widget.Bindings = value; } }

        public void Clear()
        {
            widget.ClearAll();
        }

        private void btnSpecial_Click(object sender, EventArgs e)
        {
            DropdownMenu.Show(btnSpecial);
        }

        void DropdownMenu_ItemClicked(object sender, EventArgs e)
        {
            Input.ModifierKey mods = new Input.ModifierKey();
            
            /*if ((Control.ModifierKeys & Keys.Shift) != 0)
                mods |= Input.ModifierKey.Shift;
            if ((Control.ModifierKeys & Keys.Control) != 0)
                mods |= Input.ModifierKey.Control;
            if ((Control.ModifierKeys & Keys.Alt) != 0)
                mods |= Input.ModifierKey.Alt;*/

            if (sender is ButtonMenuItem)
            {
                Input.LogicalButton lb = new Input.LogicalButton(((ButtonMenuItem)sender).Text, mods);
                widget.SetBinding(lb.ToString());
            }
        }
    }
}
