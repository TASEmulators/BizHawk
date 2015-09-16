using System;
using System.Collections.Generic;
using System.Text;
using Eto;
using Eto.Forms;
using Eto.Drawing;
using BizHawk.Client.EtoHawk;
using System.Linq;
using System.Threading;

namespace EtoHawk.Config
{
    public sealed class InputWidget : TextBox
    {
        // TODO: when binding, make sure that the new key combo is not in one of the other bindings
        private readonly List<string> _bindings = new List<string>();

        public InputCompositeWidget CompositeWidget;
        public ControllerConfig ParentConfig { get; set; }

        public class SpecialBindingInfo
        {
            public string BindingName;
            public string TooltipText;
        }

        /// <summary>
        /// These bindings get ignored by the widget and can only be entered by SetBinding() via the contextmenu from the InputCompositeWidget
        /// </summary>
        public static readonly SpecialBindingInfo[] SpecialBindings = {
			 new SpecialBindingInfo { BindingName = "Escape", TooltipText = "Binds the Escape key" },
			new SpecialBindingInfo { BindingName = "WMouse L", TooltipText =  "Binds the left mouse button"},
				new SpecialBindingInfo { BindingName = "WMouse M", TooltipText =  "Binds the middle mouse button"},
				new SpecialBindingInfo { BindingName = "WMouse R", TooltipText =  "Binds the right mouse button"},
			new SpecialBindingInfo { BindingName = "WMouse 1", TooltipText =  "Binds the mouse auxiliary button 1" }, 
				new SpecialBindingInfo {	BindingName = "WMouse 2", TooltipText =  "Binds the mouse auxiliary button 2" },
		};


        public InputWidget(ControllerConfig parent)
        {
            ParentConfig = parent;
            //ContextMenu = new ContextMenu();
            ClearBindings();
            AutoTab = true;
            Cursor = Cursors.Arrow;
        }

        public bool AutoTab { get; set; }
        public string WidgetName { get; set; }

        public string Bindings
        {
            get
            {
                return Text;
            }
            set
            {
                ClearBindings();
                var newBindings = value.Trim().Split(',');
                _bindings.AddRange(newBindings);
                UpdateLabel();
            }
        }

        public void ClearAll()
        {
            ClearBindings();
            base.Text = "";
        }

        private void ClearBindings()
        {
            _bindings.Clear();
        }

        protected override void OnGotFocus(EventArgs e)
        {
            base.OnGotFocus(e);

            ParentConfig.FocusedWidget = this;

            //Console.WriteLine (this.Bindings + " got focus");
            BackgroundColor = Color.FromArgb(unchecked((int)0xFFC0FFFF)); // Color.LightCyan is too light on Windows 8, this is a bit darker
        }

        protected override void OnLostFocus(EventArgs e)
        {
            base.OnLostFocus(e);
            //Console.WriteLine (this.Bindings + " lost focus");
            UpdateLabel();
            BackgroundColor = Colors.White; //SystemColors.Window;
        }

        public void HandleMappingInput(string bindingStr){
            //Differs from SetBinding because it has the logic in it.
            //has special meaning for the binding UI system (clear it).
            //you can set it through the special bindings dropdown menu
            if (bindingStr == "Escape")
            {
                EraseMappings();
                Increment();
                return;
            }

            //seriously, we refuse to allow you to bind this to anything else.
            if (bindingStr == "Alt+F4")
            {
                return;
            }

            //ignore special bindings
            foreach (var spec in SpecialBindings)
                if (spec.BindingName == bindingStr)
                    return;

            if (!IsDuplicate(bindingStr))
            {
                if (AutoTab)
                {
                    ClearBindings();
                }

                _bindings.Add(bindingStr);
            }

            UpdateLabel();
            Increment();
        }

        public void EraseMappings()
        {
            ClearBindings();
            Text = string.Empty;
        }

        /// <summary>
        /// sets a binding manually. This may not be implemented quite right.
        /// </summary>
        public void SetBinding(string bindingStr)
        {
            _bindings.Add(bindingStr);
            UpdateLabel();
            Increment();
        }

        private bool IsDuplicate(string binding)
        {
            return _bindings.FirstOrDefault(x => x == binding) != null;
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            if (e.Key == Keys.F4 && e.Modifiers == Keys.Alt)
            {
                base.OnKeyUp(e);
            }
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (e.Key == Keys.F4 && e.Modifiers == Keys.Alt)
            {
                base.OnKeyDown(e);
                return;
            }

            e.Handled = true;
        }

        // Advances to the next widget depending on the autotab setting
        public void Increment()
        {
            if (AutoTab)
            {
                CompositeWidget.TabNext();
            }
        }

        public void Decrement()
        {
            if (AutoTab)
            {
                Parent.SelectNextControl(this, false, true, true, true);
            }
        }

        public void UpdateLabel()
        {
            Text = string.Join(",", _bindings.Where(str => !string.IsNullOrWhiteSpace(str)));
            CompositeWidget.RefreshTooltip();
        }
        
        /*protected override void OnKeyPress(KeyPressEventArgs e)
        {
            e.Handled = true;
        }*/
        
        protected override void OnMouseWheel(MouseEventArgs e)
        {
            if (e.Delta.Height > 0)
            {
                Decrement();
            }
            else
            {
                Increment();
            }

            base.OnMouseWheel(e);
        }

        /*protected override void OnGotFocus(EventArgs e)
        {
            HideCaret(Handle);
        }*/

        /*protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            return !(keyData.ToString() == "F4" || keyData.ToString().Contains("Alt"));
        }*/
    }
}
