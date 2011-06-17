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

            if (Global.Config.AllowUD_LR == false)
            {
                string prefix;

                if (button.Contains("Down"))
                {
                    prefix = button.GetPrecedingString("Down");
                    if (IsPressed(prefix + "Up"))
                        return false;
                }
                if (button.Contains("Right"))
                {
                    prefix = button.GetPrecedingString("Right");
                    if (IsPressed(prefix + "Left"))
                        return false;
                }
            }

            return IsPressedActually(button);
        }

        public bool IsPressedActually(string button)
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
                input.Append("|");
                return input.ToString();
            }

            if (type.Name == "PC Engine Controller")
            {
                input.Append("."); //TODO: reset goes here   - the turbografx DOES NOT HAVE A RESET BUTTON. but I assume this is for pcejin movie file compatibility, which is a fools errand anyway......... I'll leave it for now, but marked for deletion
                input.Append("|");
                for (int player = 1; player < 6; player++)
                {            
                    input.Append(IsPressed("P" + player.ToString() + " Up") ? "U" : ".");
                    input.Append(IsPressed("P" + player.ToString() + " Down") ? "D" : ".");
                    input.Append(IsPressed("P" + player.ToString() + " Left") ? "L" : ".");
                    input.Append(IsPressed("P" + player.ToString() + " Right") ? "R" : ".");
                    input.Append(IsPressed("P" + player.ToString() + " B1") ? "1" : ".");
                    input.Append(IsPressed("P" + player.ToString() + " B2") ? "2" : ".");
                    input.Append(IsPressed("P" + player.ToString() + " Run") ? "R" : ".");
                    input.Append(IsPressed("P" + player.ToString() + " Select") ? "S" : ".");
                    input.Append("|");                    
                   
                }
                return input.ToString();
            }

            if (type.Name == "NES Controls")
            {
                input.Append(IsPressed("Reset")  ? "r" : ".");
                input.Append("|");
                input.Append(IsPressed("Right")  ? "R" : ".");
                input.Append(IsPressed("Left")   ? "L" : ".");
                input.Append(IsPressed("Down")   ? "D" : ".");
                input.Append(IsPressed("Up")     ? "U" : ".");
                input.Append(IsPressed("Start")  ? "S" : ".");
                input.Append(IsPressed("Select") ? "s" : ".");
                input.Append(IsPressed("B")      ? "B" : ".");
                input.Append(IsPressed("A")      ? "A" : ".");
                input.Append("|");
                return input.ToString();
            }

            if (type.Name == "TI83 Controls")
            {
                input.Append(IsPressed("0") ? "0" : ".");
                input.Append(IsPressed("1") ? "1" : ".");
                input.Append(IsPressed("2") ? "2" : ".");
                input.Append(IsPressed("3") ? "3" : ".");
                input.Append(IsPressed("4") ? "4" : ".");
                input.Append(IsPressed("5") ? "5" : ".");
                input.Append(IsPressed("6") ? "6" : ".");
                input.Append(IsPressed("7") ? "7" : ".");
                input.Append(IsPressed("8") ? "8" : ".");
                input.Append(IsPressed("9") ? "9" : ".");
                input.Append(IsPressed("DOT") ? "`" : ".");
                input.Append(IsPressed("ON") ? "O" : ".");
                input.Append(IsPressed("ENTER") ? "=" : ".");
                input.Append(IsPressed("UP") ? "U" : ".");
                input.Append(IsPressed("DOWN") ? "D" : ".");
                input.Append(IsPressed("LEFT") ? "L" : ".");
                input.Append(IsPressed("RIGHT") ? "R": ".");
                input.Append(IsPressed("PLUS") ? "+": ".");
                input.Append(IsPressed("MINUS") ? "_": ".");
                input.Append(IsPressed("MULTIPLY") ? "*": ".");
                input.Append(IsPressed("DIVIDE") ? "/": ".");
                input.Append(IsPressed("CLEAR") ? "c": ".");
                input.Append(IsPressed("EXP") ? "^": ".");
                input.Append(IsPressed("DASH") ? "-": ".");
                input.Append(IsPressed("PARAOPEN") ? "(" : ".");
                input.Append(IsPressed("PARACLOSE") ? ")" : ".");
                input.Append(IsPressed("TAN") ? "T" : ".");
                input.Append(IsPressed("VARS") ? "V" : ".");
                input.Append(IsPressed("COS") ? "C" : ".");
                input.Append(IsPressed("PRGM") ? "P" : ".");
                input.Append(IsPressed("STAT") ? "s" : ".");
                input.Append(IsPressed("MATRIX") ? "m" : ".");
                input.Append(IsPressed("X") ? "X" : ".");
                input.Append(IsPressed("STO") ? ">" : ".");
                input.Append(IsPressed("LN") ? "n" : ".");
                input.Append(IsPressed("LOG") ? "L" : ".");
                input.Append(IsPressed("SQUARED") ? "2" : ".");
                input.Append(IsPressed("NEG1") ? "1" : ".");
                input.Append(IsPressed("MATH") ? "H" : ".");
                input.Append(IsPressed("ALPHA") ? "A" : ".");
                input.Append(IsPressed("GRAPH") ? "G" : ".");
                input.Append(IsPressed("TRACE") ? "t" : ".");
                input.Append(IsPressed("ZOOM") ? "Z" : ".");
                input.Append(IsPressed("WINDOW") ? "W" : ".");
                input.Append(IsPressed("Y") ? "Y" : ".");
                input.Append(IsPressed("2ND") ? "&" : ".");
                input.Append(IsPressed("MODE") ? "O" : ".");
                input.Append(IsPressed("DEL") ? "D" : ".");
                input.Append(IsPressed("COMMA") ? "," : ".");
                input.Append(IsPressed("SIN") ? "S" : ".");
                input.Append("|.|"); //TODO: perhaps ON should go here?
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
                if (mnemonic[1] != '.')  programmaticallyPressedButtons.Add("P1 Up");
                if (mnemonic[2] != '.')  programmaticallyPressedButtons.Add("P1 Down");
                if (mnemonic[3] != '.')  programmaticallyPressedButtons.Add("P1 Left");
                if (mnemonic[4] != '.')  programmaticallyPressedButtons.Add("P1 Right");
                if (mnemonic[5] != '.')  programmaticallyPressedButtons.Add("P1 B1");
                if (mnemonic[6] != '.')  programmaticallyPressedButtons.Add("P1 B2");

                if (mnemonic[8] != '.')  programmaticallyPressedButtons.Add("P2 Up");
                if (mnemonic[9] != '.')  programmaticallyPressedButtons.Add("P2 Down");
                if (mnemonic[10] != '.') programmaticallyPressedButtons.Add("P2 Left");
                if (mnemonic[11] != '.') programmaticallyPressedButtons.Add("P2 Right");
                if (mnemonic[12] != '.') programmaticallyPressedButtons.Add("P2 B1");
                if (mnemonic[13] != '.') programmaticallyPressedButtons.Add("P2 B2");

                if (mnemonic[15] != '.') programmaticallyPressedButtons.Add("Pause");
                if (mnemonic[16] != '.') programmaticallyPressedButtons.Add("Reset");
            }

            if (type.Name == "PC Engine Controller")
            {
                if (!Global.MainForm.UserMovie.MultiTrack.isActive || (Global.MainForm.UserMovie.GetMovieMode() == MOVIEMODE.PLAY))
                {
                    for (int i = 1; i < 6; i++)
                    {
                        if (mnemonic.Length < (1 + i * 9)) return;
                        if (mnemonic[(i - 1) * 9 + 3] != '.') programmaticallyPressedButtons.Add("P" + i + " Up");
                        if (mnemonic[(i - 1) * 9 + 4] != '.') programmaticallyPressedButtons.Add("P" + i + " Down");
                        if (mnemonic[(i - 1) * 9 + 5] != '.') programmaticallyPressedButtons.Add("P" + i + " Left");
                        if (mnemonic[(i - 1) * 9 + 6] != '.') programmaticallyPressedButtons.Add("P" + i + " Right");
                        if (mnemonic[(i - 1) * 9 + 7] != '.') programmaticallyPressedButtons.Add("P" + i + " B1");
                        if (mnemonic[(i - 1) * 9 + 8] != '.') programmaticallyPressedButtons.Add("P" + i + " B2");
                        if (mnemonic[(i - 1) * 9 + 9] != '.') programmaticallyPressedButtons.Add("P" + i + " Run");
                        if (mnemonic[(i - 1) * 9 + 10] != '.') programmaticallyPressedButtons.Add("P" + i + " Select");
                    }
                }
                else
                {                    
                    for (int i = 1; i < 6; i++)
                    {
                        if ((Global.MainForm.UserMovie.MultiTrack.CurrentPlayer == i) || Global.MainForm.UserMovie.MultiTrack.RecordAll)
                        {
                            if (IsPressedActually("P1 Up")) programmaticallyPressedButtons.Add("P" + i + " Up");
                            if (IsPressedActually("P1 Down")) programmaticallyPressedButtons.Add("P" + i + " Down");
                            if (IsPressedActually("P1 Left")) programmaticallyPressedButtons.Add("P" + i + " Left");
                            if (IsPressedActually("P1 Right")) programmaticallyPressedButtons.Add("P" + i + " Right");
                            if (IsPressedActually("P1 B1")) programmaticallyPressedButtons.Add("P" + i + " B1");
                            if (IsPressedActually("P1 B2")) programmaticallyPressedButtons.Add("P" + i + " B2");
                            if (IsPressedActually("P1 Run")) programmaticallyPressedButtons.Add("P" + i + " Run");
                            if (IsPressedActually("P1 Select")) programmaticallyPressedButtons.Add("P" + i + " Select");
                        }
                        else
                        {
                            if (mnemonic.Length >= (1 + i * 9))
                            {
                                if (mnemonic[(i - 1) * 9 + 3] != '.') programmaticallyPressedButtons.Add("P" + i + " Up");
                                if (mnemonic[(i - 1) * 9 + 4] != '.') programmaticallyPressedButtons.Add("P" + i + " Down");
                                if (mnemonic[(i - 1) * 9 + 5] != '.') programmaticallyPressedButtons.Add("P" + i + " Left");
                                if (mnemonic[(i - 1) * 9 + 6] != '.') programmaticallyPressedButtons.Add("P" + i + " Right");
                                if (mnemonic[(i - 1) * 9 + 7] != '.') programmaticallyPressedButtons.Add("P" + i + " B1");
                                if (mnemonic[(i - 1) * 9 + 8] != '.') programmaticallyPressedButtons.Add("P" + i + " B2");
                                if (mnemonic[(i - 1) * 9 + 9] != '.') programmaticallyPressedButtons.Add("P" + i + " Run");
                                if (mnemonic[(i - 1) * 9 + 10] != '.') programmaticallyPressedButtons.Add("P" + i + " Select");
                            }
                        }
                    }
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

            if (type.Name == "TI83 Controls")
            {
                if (mnemonic.Length < 50) return;

                if (mnemonic[1] != '.')
                    programmaticallyPressedButtons.Add("0");
                if (mnemonic[2] != '.')
                    programmaticallyPressedButtons.Add("1");
                if (mnemonic[3] != '.')
                    programmaticallyPressedButtons.Add("2");
                if (mnemonic[4] != '.')
                    programmaticallyPressedButtons.Add("3");
                if (mnemonic[5] != '.')
                    programmaticallyPressedButtons.Add("4");
                if (mnemonic[6] != '.')
                    programmaticallyPressedButtons.Add("5");
                if (mnemonic[7] != '.')
                    programmaticallyPressedButtons.Add("6");
                if (mnemonic[8] != '.')
                    programmaticallyPressedButtons.Add("7");
                if (mnemonic[9] != '.')
                    programmaticallyPressedButtons.Add("8");
                if (mnemonic[10] != '.')
                    programmaticallyPressedButtons.Add("9");
                if (mnemonic[11] != '.')
                    programmaticallyPressedButtons.Add("DOT");
                if (mnemonic[12] != '.')
                    programmaticallyPressedButtons.Add("ON");
                if (mnemonic[13] != '.')
                    programmaticallyPressedButtons.Add("ENTER");
                if (mnemonic[14] != '.')
                    programmaticallyPressedButtons.Add("UP");
                if (mnemonic[15] != '.')
                    programmaticallyPressedButtons.Add("DOWN");
                if (mnemonic[16] != '.')
                    programmaticallyPressedButtons.Add("LEFT");
                if (mnemonic[17] != '.')
                    programmaticallyPressedButtons.Add("RIGHT");
                if (mnemonic[18] != '.')
                    programmaticallyPressedButtons.Add("PLUS");
                if (mnemonic[19] != '.')
                    programmaticallyPressedButtons.Add("MINUS");
                if (mnemonic[20] != '.')
                    programmaticallyPressedButtons.Add("MULTIPLY");
                if (mnemonic[21] != '.')
                    programmaticallyPressedButtons.Add("DIVIDE");
                if (mnemonic[22] != '.')
                    programmaticallyPressedButtons.Add("CLEAR");
                if (mnemonic[23] != '.')
                    programmaticallyPressedButtons.Add("EXP");
                if (mnemonic[24] != '.')
                    programmaticallyPressedButtons.Add("DASH");
                if (mnemonic[25] != '.')
                    programmaticallyPressedButtons.Add("PARAOPEN");
                if (mnemonic[26] != '.')
                    programmaticallyPressedButtons.Add("PARACLOSE");
                if (mnemonic[27] != '.')
                    programmaticallyPressedButtons.Add("TAN");
                if (mnemonic[28] != '.')
                    programmaticallyPressedButtons.Add("VARS");
                if (mnemonic[29] != '.')
                    programmaticallyPressedButtons.Add("COS");
                if (mnemonic[30] != '.')
                    programmaticallyPressedButtons.Add("PGRM");
                if (mnemonic[31] != '.')
                    programmaticallyPressedButtons.Add("STAT");
                if (mnemonic[32] != '.')
                    programmaticallyPressedButtons.Add("MATRIX");
                if (mnemonic[33] != '.')
                    programmaticallyPressedButtons.Add("X");
                if (mnemonic[34] != '.')
                    programmaticallyPressedButtons.Add("STO");
                if (mnemonic[35] != '.')
                    programmaticallyPressedButtons.Add("LN");
                if (mnemonic[36] != '.')
                    programmaticallyPressedButtons.Add("LOG");
                if (mnemonic[37] != '.')
                    programmaticallyPressedButtons.Add("SQUARED");
                if (mnemonic[38] != '.')
                    programmaticallyPressedButtons.Add("NEG");
                if (mnemonic[39] != '.')
                    programmaticallyPressedButtons.Add("MATH");
                if (mnemonic[40] != '.')
                    programmaticallyPressedButtons.Add("ALPHA");
                if (mnemonic[41] != '.')
                    programmaticallyPressedButtons.Add("GRAPH");
                if (mnemonic[42] != '.')
                    programmaticallyPressedButtons.Add("TRACE");
                if (mnemonic[43] != '.')
                    programmaticallyPressedButtons.Add("ZOOM");
                if (mnemonic[44] != '.')
                    programmaticallyPressedButtons.Add("WINDOW");
                if (mnemonic[45] != '.')
                    programmaticallyPressedButtons.Add("Y");
                if (mnemonic[46] != '.')
                    programmaticallyPressedButtons.Add("2ND");
                if (mnemonic[47] != '.')
                    programmaticallyPressedButtons.Add("MODE");
                if (mnemonic[48] != '.')
                    programmaticallyPressedButtons.Add("DEL");
                if (mnemonic[49] != '.')
                    programmaticallyPressedButtons.Add("COMMA");
                if (mnemonic[50] != '.')
                    programmaticallyPressedButtons.Add("SIN");
            }
        }
    }
}