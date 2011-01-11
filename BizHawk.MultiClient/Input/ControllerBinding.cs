using System;
using System.Collections.Generic;

namespace BizHawk.MultiClient
{
    public class Controller : IController
    {
        private ControllerDefinition type;
        private Dictionary<string,List<string>> bindings = new Dictionary<string, List<string>>();
        private List<string> unpressedButtons = new List<string>();

        public Controller(ControllerDefinition definition)
        {
            type = definition;

            foreach (var b in type.BoolButtons)
                bindings[b] = new List<string>();

            foreach (var f in type.FloatControls)
                bindings[f] = new List<string>();
        }
        
        public void BindButton(string button, string control)
        {
            bindings[button].Add(control);
        }

        public void BindMulti(string button, string controlString)
        {
            string[] controlbindings = controlString.Split(',');
            foreach (string control in controlbindings)
                bindings[button].Add(control.Trim());
        }

        public ControllerDefinition Type
        {
            get { return type; }
        }

        public bool this[string name]
        {
            get { return IsPressed(name); }
        }
        
        public bool IsPressed(string name)
        {
            if (unpressedButtons.Contains(name))
            {
                if (IsPressedActually(name) == false)
                    unpressedButtons.Remove(name);

                return false;
            }

            return IsPressedActually(name);
        }

        private bool IsPressedActually(string name)
        {
            foreach (var control in bindings[name])
                if (Input.IsPressed(control))
                    return true;

            return false;            
        }

        public float GetFloat(string name)
        {
            throw new NotImplementedException();
        }

        public void UnpressButton(string name)
        {
            unpressedButtons.Add(name);
        }

        private int frameNumber;
        public int FrameNumber
        {
            get
            {
                return frameNumber;
            }
            set
            {
                if (value != frameNumber)
                {
                    // update
                    unpressedButtons.RemoveAll(button => IsPressedActually(button) == false);
                }
                frameNumber = value;
            }
        }
    }
}
