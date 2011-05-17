using System;
using System.Collections.Generic;
using System.Text;

namespace BizHawk.MultiClient
{
    public class Controller : IController
    {
        private ControllerDefinition type;
        private Dictionary<string,List<string>> bindings = new Dictionary<string, List<string>>();
        private Dictionary<string,bool> stickyButtons = new Dictionary<string, bool>();
        private List<string> unpressedButtons = new List<string>();
        private List<string> forcePressedButtons = new List<string>();
        private List<string> removeFromForcePressedButtons = new List<string>();
        private List<string> programmaticallyPressedButtons = new List<string>();

        private bool movieMode;
        public bool MovieMode
        { 
            get { return movieMode; }
            set 
            { 
                movieMode = value; 
                if (value == false)
                    programmaticallyPressedButtons.Clear();
            }
        }

        public Controller(ControllerDefinition definition)
        {
            type = definition;

            foreach (var b in type.BoolButtons)
            {
                bindings[b] = new List<string>();
                stickyButtons[b] = false;
            }

            foreach (var f in type.FloatControls)
                bindings[f] = new List<string>();
        }
        
        public void BindButton(string button, string control)
        {
            bindings[button].Add(control);
        }

        public void BindMulti(string button, string controlString)
        {
            if (string.IsNullOrEmpty(controlString))
                return;
            string[] controlbindings = controlString.Split(',');
            foreach (string control in controlbindings)
                bindings[button].Add(control.Trim());
        }

        public ControllerDefinition Type
        {
            get { return type; }
        }

        public bool this[string button]
        {
            get { return IsPressed(button); }
        }
        
        public bool IsPressed(string button)
        {
            if (MovieMode)
            {
                return programmaticallyPressedButtons.Contains(button);
            }
            if (forcePressedButtons.Contains(button))
            {
                removeFromForcePressedButtons.Add(button);
                return true;
            }
            if (unpressedButtons.Contains(button))
            {
                if (IsPressedActually(button) == false)
                    unpressedButtons.Remove(button);

                return false;
            }

            return IsPressedActually(button);
        }

        private bool IsPressedActually(string button)
        {
            bool sticky = stickyButtons[button];

            foreach (var control in bindings[button])
                if (Input.IsPressed(control))
                    return sticky ? false : true;

            return sticky ? true : false;
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

        public void UpdateControls(int frame)
        {
            if (frame != frameNumber)
            {
                // update
                unpressedButtons.RemoveAll(button => IsPressedActually(button) == false);
                forcePressedButtons.RemoveAll(button => removeFromForcePressedButtons.Contains(button));
                removeFromForcePressedButtons.Clear();
            }
            frameNumber = frame;
        }

        public void SetSticky(string button, bool sticky)
        {
            stickyButtons[button] = sticky;
        }

        public bool IsSticky(string button)
        {
            return stickyButtons[button];
        }

        public void ForceButton(string button)
        {
            forcePressedButtons.Add(button);
        }

        public string GetControllersAsMnemonic()
        {
            StringBuilder input = new StringBuilder("|");
            
            if (type.Name == "SMS Controller")
            {
                input.Append(IsPressed("P1 Up")    ? "U" : ".");
                input.Append(IsPressed("P1 Down")  ? "D" : ".");
                input.Append(IsPressed("P1 Left")  ? "L" : ".");
                input.Append(IsPressed("P1 Right") ? "R" : ".");
                input.Append(IsPressed("P1 B1")    ? "1" : ".");
                input.Append(IsPressed("P1 B2")    ? "2" : ".");
                input.Append("|");
                input.Append(IsPressed("P2 Up")    ? "U" : ".");
                input.Append(IsPressed("P2 Down")  ? "D" : ".");
                input.Append(IsPressed("P2 Left")  ? "L" : ".");
                input.Append(IsPressed("P2 Right") ? "R" : ".");
                input.Append(IsPressed("P2 B1")    ? "1" : ".");
                input.Append(IsPressed("P2 B2")    ? "2" : ".");
                input.Append("|");
                input.Append(IsPressed("Pause")    ? "P" : ".");
                input.Append(IsPressed("Reset")    ? "R" : ".");
                return input.ToString();
            }

            if (type.Name == "PC Engine Controller")
            {
                for (int i = 1; i < 6; i++)
                {
                    input.Append(IsPressed("P" + i.ToString() + " Up") ? "U" : ".");
                    input.Append(IsPressed("P" + i.ToString() + " Down") ? "D" : ".");
                    input.Append(IsPressed("P" + i.ToString() + " Left") ? "L" : ".");
                    input.Append(IsPressed("P" + i.ToString() + " Right") ? "R" : ".");
                    input.Append(IsPressed("P" + i.ToString() + " B1") ? "1" : ".");
                    input.Append(IsPressed("P" + i.ToString() + " B2") ? "2" : ".");
                    input.Append(IsPressed("P" + i.ToString() + " Run") ? "R" : ".");
                    input.Append(IsPressed("P" + i.ToString() + " Select") ? "S" : ".");
                    input.Append("|");
                }
                return input.ToString();
            }

            if (type.Name == "NES Controls")
            {
                input.Append(IsPressed("Reset") ? "r" : ".");
                input.Append("|");
                input.Append(IsPressed("Right") ? "R" : ".");
                input.Append(IsPressed("Left") ? "L" : ".");
                input.Append(IsPressed("Down") ? "D" : ".");
                input.Append(IsPressed("Up") ? "U" : ".");
                input.Append(IsPressed("Start") ? "S" : ".");
                input.Append(IsPressed("Select") ? "s" : ".");
                input.Append(IsPressed("B") ? "B" : ".");
                input.Append(IsPressed("A") ? "A" : ".");
                input.Append("|");
                return input.ToString();
            }

            return "?";
        }

        public void SetControllersAsMnemonic(string mnemonic)
        {
            MovieMode = true;
            programmaticallyPressedButtons.Clear();

            if (type.Name == "SMS Controller")
            {
                for (int i = 1; i < 3; i++)
                {
                    if (mnemonic.Length < (1+7*i)) return;
                    //if (mnemonic[1] != '.' && mnemonic[1] != '0') programmaticallyPressedButtons.Add("Reset");
                    if (mnemonic[(i - 1) * 7 +3] != '.') programmaticallyPressedButtons.Add("P" + i.ToString() + " Up");
                    if (mnemonic[(i - 1) * 7 + 4] != '.') programmaticallyPressedButtons.Add("P" + i.ToString() + " Down");
                    if (mnemonic[(i - 1) * 7 + 5] != '.') programmaticallyPressedButtons.Add("P" + i.ToString() + " Left");
                    if (mnemonic[(i - 1) * 7 + 6] != '.') programmaticallyPressedButtons.Add("P" + i.ToString() + " Right");
                    if (mnemonic[(i - 1) * 7 + 7] != '.') programmaticallyPressedButtons.Add("P" + i.ToString() + " B1");
                    if (mnemonic[(i - 1) * 7 + 8] != '.') programmaticallyPressedButtons.Add("P" + i.ToString() + " B2");
                }
                if (mnemonic.Length < 18) return;
                if (mnemonic[17] != '.') programmaticallyPressedButtons.Add("Pause");
                if (mnemonic[18] != '.') programmaticallyPressedButtons.Add("Reset");
            }

            if (type.Name == "PC Engine Controller")
            {
                for (int i = 1; i < 6; i++)
                {
                    if (mnemonic.Length < (1 + i * 9)) return;
                    if (mnemonic[(i - 1) * 9 + 3] != '.') programmaticallyPressedButtons.Add("P" + i.ToString() + " Up");
                    if (mnemonic[(i - 1) * 9 + 4] != '.') programmaticallyPressedButtons.Add("P" + i.ToString() + " Down");
                    if (mnemonic[(i - 1) * 9 + 5] != '.') programmaticallyPressedButtons.Add("P" + i.ToString() + " Left");
                    if (mnemonic[(i - 1) * 9 + 6] != '.') programmaticallyPressedButtons.Add("P" + i.ToString() + " Right");
                    if (mnemonic[(i - 1) * 9 + 7] != '.') programmaticallyPressedButtons.Add("P" + i.ToString() + " B1");
                    if (mnemonic[(i - 1) * 9 + 8] != '.') programmaticallyPressedButtons.Add("P" + i.ToString() + " B2");
                    if (mnemonic[(i - 1) * 9 + 9] != '.') programmaticallyPressedButtons.Add("P" + i.ToString() + " Run");
                    if (mnemonic[(i - 1) * 9 + 10] != '.') programmaticallyPressedButtons.Add("P" + i.ToString() + " Select");
                }
            }

            if (type.Name == "NES Controls")
            {
                if (mnemonic.Length < 10) return;
                //if (mnemonic[1] != '.' && mnemonic[1] != '0') programmaticallyPressedButtons.Add("Reset");
                if (mnemonic[3] != '.') programmaticallyPressedButtons.Add("Right");
                if (mnemonic[4] != '.') programmaticallyPressedButtons.Add("Left");
                if (mnemonic[5] != '.') programmaticallyPressedButtons.Add("Down");
                if (mnemonic[6] != '.') programmaticallyPressedButtons.Add("Up");
                if (mnemonic[7] != '.') programmaticallyPressedButtons.Add("Start");
                if (mnemonic[8] != '.') programmaticallyPressedButtons.Add("Select");
                if (mnemonic[9] != '.') programmaticallyPressedButtons.Add("B");
                if (mnemonic[10] != '.') programmaticallyPressedButtons.Add("A");
            }
        }
    }
}