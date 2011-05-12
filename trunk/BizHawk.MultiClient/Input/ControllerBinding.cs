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
                input.Append(IsPressed("P1 Up")     ? "U" : ".");
                input.Append(IsPressed("P1 Down")   ? "D" : ".");
                input.Append(IsPressed("P1 Left")   ? "L" : ".");
                input.Append(IsPressed("P1 Right")  ? "R" : ".");
                input.Append(IsPressed("P1 B1")     ? "1" : ".");
                input.Append(IsPressed("P1 B2")     ? "2" : ".");
                input.Append(IsPressed("P1 Run")    ? "R" : ".");
                input.Append(IsPressed("P1 Select") ? "S" : ".");
                input.Append("|");

                input.Append(IsPressed("P2 Up")     ? "U" : ".");
                input.Append(IsPressed("P2 Down")   ? "D" : ".");
                input.Append(IsPressed("P2 Left")   ? "L" : ".");
                input.Append(IsPressed("P2 Right")  ? "R" : ".");
                input.Append(IsPressed("P2 B1")     ? "1" : ".");
                input.Append(IsPressed("P2 B2")     ? "2" : ".");
                input.Append(IsPressed("P2 Run")    ? "R" : ".");
                input.Append(IsPressed("P2 Select") ? "S" : ".");
                input.Append("|");

                input.Append(IsPressed("P3 Up")     ? "U" : ".");
                input.Append(IsPressed("P3 Down")   ? "D" : ".");
                input.Append(IsPressed("P3 Left")   ? "L" : ".");
                input.Append(IsPressed("P3 Right")  ? "R" : ".");
                input.Append(IsPressed("P3 B1")     ? "1" : ".");
                input.Append(IsPressed("P3 B2")     ? "2" : ".");
                input.Append(IsPressed("P3 Run")    ? "R" : ".");
                input.Append(IsPressed("P3 Select") ? "S" : ".");
                input.Append("|");

                input.Append(IsPressed("P4 Up")     ? "U" : ".");
                input.Append(IsPressed("P4 Down")   ? "D" : ".");
                input.Append(IsPressed("P4 Left")   ? "L" : ".");
                input.Append(IsPressed("P4 Right")  ? "R" : ".");
                input.Append(IsPressed("P4 B1")     ? "1" : ".");
                input.Append(IsPressed("P4 B2")     ? "2" : ".");
                input.Append(IsPressed("P4 Run")    ? "R" : ".");
                input.Append(IsPressed("P4 Select") ? "S" : ".");
                input.Append("|");

                input.Append(IsPressed("P5 Up")     ? "U" : ".");
                input.Append(IsPressed("P5 Down")   ? "D" : ".");
                input.Append(IsPressed("P5 Left")   ? "L" : ".");
                input.Append(IsPressed("P5 Right")  ? "R" : ".");
                input.Append(IsPressed("P5 B1")     ? "1" : ".");
                input.Append(IsPressed("P5 B2")     ? "2" : ".");
                input.Append(IsPressed("P5 Run")    ? "R" : ".");
                input.Append(IsPressed("P5 Select") ? "S" : ".");
                input.Append("|");

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
                if (mnemonic[1]  != '.') programmaticallyPressedButtons.Add("P1 Up");
                if (mnemonic[2]  != '.') programmaticallyPressedButtons.Add("P1 Down");
                if (mnemonic[3]  != '.') programmaticallyPressedButtons.Add("P1 Left");
                if (mnemonic[4]  != '.') programmaticallyPressedButtons.Add("P1 Right");
                if (mnemonic[5]  != '.') programmaticallyPressedButtons.Add("P1 B1");
                if (mnemonic[6]  != '.') programmaticallyPressedButtons.Add("P1 B2");

                if (mnemonic[8]  != '.') programmaticallyPressedButtons.Add("P2 Up");
                if (mnemonic[9]  != '.') programmaticallyPressedButtons.Add("P2 Down");
                if (mnemonic[10] != '.') programmaticallyPressedButtons.Add("P2 Left");
                if (mnemonic[11] != '.') programmaticallyPressedButtons.Add("P2 Right");
                if (mnemonic[12] != '.') programmaticallyPressedButtons.Add("P2 B1");
                if (mnemonic[13] != '.') programmaticallyPressedButtons.Add("P2 B2");

                if (mnemonic[15] != '.') programmaticallyPressedButtons.Add("Pause");
                if (mnemonic[16] != '.') programmaticallyPressedButtons.Add("Reset");
            }

            if (type.Name == "PC Engine Controller")
            {
                if (mnemonic[1]  != '.') programmaticallyPressedButtons.Add("P1 Up");
                if (mnemonic[2]  != '.') programmaticallyPressedButtons.Add("P1 Down");
                if (mnemonic[3]  != '.') programmaticallyPressedButtons.Add("P1 Left");
                if (mnemonic[4]  != '.') programmaticallyPressedButtons.Add("P1 Right");
                if (mnemonic[5]  != '.') programmaticallyPressedButtons.Add("P1 B1");
                if (mnemonic[6]  != '.') programmaticallyPressedButtons.Add("P1 B2");
                if (mnemonic[7]  != '.') programmaticallyPressedButtons.Add("P1 Run");
                if (mnemonic[8]  != '.') programmaticallyPressedButtons.Add("P1 Select");

                if (mnemonic[10] != '.') programmaticallyPressedButtons.Add("P2 Up");
                if (mnemonic[11] != '.') programmaticallyPressedButtons.Add("P2 Down");
                if (mnemonic[12] != '.') programmaticallyPressedButtons.Add("P2 Left");
                if (mnemonic[13] != '.') programmaticallyPressedButtons.Add("P2 Right");
                if (mnemonic[14] != '.') programmaticallyPressedButtons.Add("P2 B1");
                if (mnemonic[15] != '.') programmaticallyPressedButtons.Add("P2 B2");
                if (mnemonic[16] != '.') programmaticallyPressedButtons.Add("P2 Run");
                if (mnemonic[17] != '.') programmaticallyPressedButtons.Add("P2 Select");

                if (mnemonic[19] != '.') programmaticallyPressedButtons.Add("P3 Up");
                if (mnemonic[20] != '.') programmaticallyPressedButtons.Add("P3 Down");
                if (mnemonic[21] != '.') programmaticallyPressedButtons.Add("P3 Left");
                if (mnemonic[22] != '.') programmaticallyPressedButtons.Add("P3 Right");
                if (mnemonic[23] != '.') programmaticallyPressedButtons.Add("P3 B1");
                if (mnemonic[24] != '.') programmaticallyPressedButtons.Add("P3 B2");
                if (mnemonic[25] != '.') programmaticallyPressedButtons.Add("P3 Run");
                if (mnemonic[26] != '.') programmaticallyPressedButtons.Add("P3 Select");

                if (mnemonic[28] != '.') programmaticallyPressedButtons.Add("P4 Up");
                if (mnemonic[29] != '.') programmaticallyPressedButtons.Add("P4 Down");
                if (mnemonic[30] != '.') programmaticallyPressedButtons.Add("P4 Left");
                if (mnemonic[31] != '.') programmaticallyPressedButtons.Add("P4 Right");
                if (mnemonic[32] != '.') programmaticallyPressedButtons.Add("P4 B1");
                if (mnemonic[33] != '.') programmaticallyPressedButtons.Add("P4 B2");
                if (mnemonic[34] != '.') programmaticallyPressedButtons.Add("P4 Run");
                if (mnemonic[35] != '.') programmaticallyPressedButtons.Add("P4 Select");

                if (mnemonic[37] != '.') programmaticallyPressedButtons.Add("P5 Up");
                if (mnemonic[38] != '.') programmaticallyPressedButtons.Add("P5 Down");
                if (mnemonic[39] != '.') programmaticallyPressedButtons.Add("P5 Left");
                if (mnemonic[40] != '.') programmaticallyPressedButtons.Add("P5 Right");
                if (mnemonic[41] != '.') programmaticallyPressedButtons.Add("P5 B1");
                if (mnemonic[42] != '.') programmaticallyPressedButtons.Add("P5 B2");
                if (mnemonic[43] != '.') programmaticallyPressedButtons.Add("P5 Run");
                if (mnemonic[44] != '.') programmaticallyPressedButtons.Add("P5 Select");
            }

            if (type.Name == "NES Controls")
            {
                //if (mnemonic[1] != '.') programmaticallyPressedButtons.Add("Reset");
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