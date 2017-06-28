using System;
using System.Collections.Generic;
using System.Text;
using Eto.Forms;

namespace EtoHawk
{
    public static class EtoExtensions
    {
        /// <summary>
        /// Simulation of SelectNextControl.
        /// To do: Doesn't support tabStopOnly flag correctly. I dont think it's possible in Eto right now?
        /// </summary>
        public static bool SelectNextControl(this Container host, Control ctl, bool forward, bool tabStopOnly, bool nested, bool wrap)
        {
            //Enumerating the entire list into an array is less efficient, but the code is cleaner.
            List<Control> allControls = new List<Control>(nested ? host.Children : host.Controls);
            bool found = false;
            if (tabStopOnly)
            {
                //Huge cheat to make up for lack of a Tab stop flag
                for (int i = allControls.Count - 1; i >= 0; i--)
                {
                    Control ctrl = allControls[i];
                    if (!(ctrl is TextBox || ctrl is TextArea || ctrl is CheckBox || ctrl is RadioButton || ctrl is Button))
                    {
                        allControls.RemoveAt(i); //Delete anything that isn't keyboard controlled.
                    }
                }
            }
            for (int i = 0; i < allControls.Count; i++)
            {
                if (allControls[i] == ctl)
                {
                    Control next = null;
                    if (forward)
                    {
                        if (wrap || i+1<allControls.Count)
                        {
                            next = allControls[(i + 1) % allControls.Count];
                        }
                    }
                    else
                    {
                        if (wrap || i - 1 > 0)
                        {
                            next = allControls[(i > 0) ? i - 1 : allControls.Count - 1];
                        }
                    }
                    next.Focus();
                }
            }
            return found;
        }
    }
}
