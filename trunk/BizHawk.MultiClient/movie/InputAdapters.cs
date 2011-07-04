using System;
using System.Text;
using System.Collections.Generic;

namespace BizHawk.MultiClient
{
	public class MnemonicsGenerator
	{
		IController Source;
		public void SetSource(IController source)
		{
			Source = source;
			ControlType = source.Type.Name;
		}
		string ControlType;

		bool IsBasePressed(string name) { 
			bool ret = Source.IsPressed(name);
			if(ret)
			{
				//int zzz=9;
			}
			return ret;
		}

		public string GetControllersAsMnemonic()
		{
			StringBuilder input = new StringBuilder("|");

			if (ControlType == "SMS Controller")
			{
				input.Append(IsBasePressed("P1 Up") ? "U" : ".");
				input.Append(IsBasePressed("P1 Down") ? "D" : ".");
				input.Append(IsBasePressed("P1 Left") ? "L" : ".");
				input.Append(IsBasePressed("P1 Right") ? "R" : ".");
				input.Append(IsBasePressed("P1 B1") ? "1" : ".");
				input.Append(IsBasePressed("P1 B2") ? "2" : ".");
				input.Append("|");
				input.Append(IsBasePressed("P2 Up") ? "U" : ".");
				input.Append(IsBasePressed("P2 Down") ? "D" : ".");
				input.Append(IsBasePressed("P2 Left") ? "L" : ".");
				input.Append(IsBasePressed("P2 Right") ? "R" : ".");
				input.Append(IsBasePressed("P2 B1") ? "1" : ".");
				input.Append(IsBasePressed("P2 B2") ? "2" : ".");
				input.Append("|");
				input.Append(IsBasePressed("Pause") ? "P" : ".");
				input.Append(IsBasePressed("Reset") ? "R" : ".");
				input.Append("|");
				return input.ToString();
			}

			if (ControlType == "PC Engine Controller")
			{
				input.Append("."); //TODO: reset goes here   - the turbografx DOES NOT HAVE A RESET BUTTON. but I assume this is for pcejin movie file compatibility, which is a fools errand anyway......... I'll leave it for now, but marked for deletion
				input.Append("|");
				for (int player = 1; player < 6; player++)
				{
					input.Append(IsBasePressed("P" + player.ToString() + " Up") ? "U" : ".");
					input.Append(IsBasePressed("P" + player.ToString() + " Down") ? "D" : ".");
					input.Append(IsBasePressed("P" + player.ToString() + " Left") ? "L" : ".");
					input.Append(IsBasePressed("P" + player.ToString() + " Right") ? "R" : ".");
					input.Append(IsBasePressed("P" + player.ToString() + " B1") ? "1" : ".");
					input.Append(IsBasePressed("P" + player.ToString() + " B2") ? "2" : ".");
					input.Append(IsBasePressed("P" + player.ToString() + " Run") ? "R" : ".");
					input.Append(IsBasePressed("P" + player.ToString() + " Select") ? "S" : ".");
					input.Append("|");

				}
				return input.ToString();
			}

			if (ControlType == "Gameboy Controller")
			{
				input.Append(".|"); //TODO: reset goes here
				input.Append(IsBasePressed("Right") ? "R" : ".");
				input.Append(IsBasePressed("Left") ? "L" : ".");
				input.Append(IsBasePressed("Down") ? "D" : ".");
				input.Append(IsBasePressed("Up") ? "U" : ".");
				input.Append(IsBasePressed("Start") ? "S" : ".");
				input.Append(IsBasePressed("Select") ? "s" : ".");
				input.Append(IsBasePressed("B") ? "B" : ".");
				input.Append(IsBasePressed("A") ? "A" : ".");
                input.Append("|");

                return input.ToString();
			}

			if (ControlType == "NES Controls")
			{
				input.Append(IsBasePressed("Reset") ? "r" : ".");
				input.Append("|");
				for (int player = 1; player <= 2; player++)
				{
					input.Append(IsBasePressed("P" + player.ToString() + " Right") ? "R" : ".");
					input.Append(IsBasePressed("P" + player.ToString() + " Left") ? "L" : ".");
					input.Append(IsBasePressed("P" + player.ToString() + " Down") ? "D" : ".");
					input.Append(IsBasePressed("P" + player.ToString() + " Up") ? "U" : ".");
					input.Append(IsBasePressed("P" + player.ToString() + " Start") ? "S" : ".");
					input.Append(IsBasePressed("P" + player.ToString() + " Select") ? "s" : ".");
					input.Append(IsBasePressed("P" + player.ToString() + " B") ? "B" : ".");
					input.Append(IsBasePressed("P" + player.ToString() + " A") ? "A" : ".");
					input.Append("|");
				}
				return input.ToString();
			}

			if (ControlType == "TI83 Controls")
			{
				input.Append(IsBasePressed("0") ? "0" : ".");
				input.Append(IsBasePressed("1") ? "1" : ".");
				input.Append(IsBasePressed("2") ? "2" : ".");
				input.Append(IsBasePressed("3") ? "3" : ".");
				input.Append(IsBasePressed("4") ? "4" : ".");
				input.Append(IsBasePressed("5") ? "5" : ".");
				input.Append(IsBasePressed("6") ? "6" : ".");
				input.Append(IsBasePressed("7") ? "7" : ".");
				input.Append(IsBasePressed("8") ? "8" : ".");
				input.Append(IsBasePressed("9") ? "9" : ".");
				input.Append(IsBasePressed("DOT") ? "`" : ".");
				input.Append(IsBasePressed("ON") ? "O" : ".");
				input.Append(IsBasePressed("ENTER") ? "=" : ".");
				input.Append(IsBasePressed("UP") ? "U" : ".");
				input.Append(IsBasePressed("DOWN") ? "D" : ".");
				input.Append(IsBasePressed("LEFT") ? "L" : ".");
				input.Append(IsBasePressed("RIGHT") ? "R" : ".");
				input.Append(IsBasePressed("PLUS") ? "+" : ".");
				input.Append(IsBasePressed("MINUS") ? "_" : ".");
				input.Append(IsBasePressed("MULTIPLY") ? "*" : ".");
				input.Append(IsBasePressed("DIVIDE") ? "/" : ".");
				input.Append(IsBasePressed("CLEAR") ? "c" : ".");
				input.Append(IsBasePressed("EXP") ? "^" : ".");
				input.Append(IsBasePressed("DASH") ? "-" : ".");
				input.Append(IsBasePressed("PARAOPEN") ? "(" : ".");
				input.Append(IsBasePressed("PARACLOSE") ? ")" : ".");
				input.Append(IsBasePressed("TAN") ? "T" : ".");
				input.Append(IsBasePressed("VARS") ? "V" : ".");
				input.Append(IsBasePressed("COS") ? "C" : ".");
				input.Append(IsBasePressed("PRGM") ? "P" : ".");
				input.Append(IsBasePressed("STAT") ? "s" : ".");
				input.Append(IsBasePressed("MATRIX") ? "m" : ".");
				input.Append(IsBasePressed("X") ? "X" : ".");
				input.Append(IsBasePressed("STO") ? ">" : ".");
				input.Append(IsBasePressed("LN") ? "n" : ".");
				input.Append(IsBasePressed("LOG") ? "L" : ".");
				input.Append(IsBasePressed("SQUARED") ? "2" : ".");
				input.Append(IsBasePressed("NEG1") ? "1" : ".");
				input.Append(IsBasePressed("MATH") ? "H" : ".");
				input.Append(IsBasePressed("ALPHA") ? "A" : ".");
				input.Append(IsBasePressed("GRAPH") ? "G" : ".");
				input.Append(IsBasePressed("TRACE") ? "t" : ".");
				input.Append(IsBasePressed("ZOOM") ? "Z" : ".");
				input.Append(IsBasePressed("WINDOW") ? "W" : ".");
				input.Append(IsBasePressed("Y") ? "Y" : ".");
				input.Append(IsBasePressed("2ND") ? "&" : ".");
				input.Append(IsBasePressed("MODE") ? "O" : ".");
				input.Append(IsBasePressed("DEL") ? "D" : ".");
				input.Append(IsBasePressed("COMMA") ? "," : ".");
				input.Append(IsBasePressed("SIN") ? "S" : ".");
				input.Append("|.|"); //TODO: perhaps ON should go here?
				return input.ToString();
			}
			return "?";
		}
	}

	/// <summary>
	/// just copies source to sink, or returns whatever a NullController would if it is disconnected. useful for immovable hardpoints.
	/// </summary>
	public class CopyControllerAdapter : IController
	{
		public IController Source;
		NullController _null = new NullController();

		IController Curr { get {
			if(Source == null) return _null;
			else return Source;
		}
		}

        public ControllerDefinition Type { get { return Curr.Type; } }
        public bool this[string button] { get { return Curr[button]; } }
        public bool IsPressed(string button) { return Curr.IsPressed(button); }
        public float GetFloat(string name) { return Curr.GetFloat(name); }
        public void UpdateControls(int frame) { Curr.UpdateControls(frame); }
	}

	class ButtonNameParser
	{
		ButtonNameParser()
		{
		}

		public static ButtonNameParser Parse(string button)
		{
			//see if we're being asked for a button that we know how to rewire
			string[] parts = button.Split(' ');
			if (parts.Length < 2) return null;
			if (parts[0][0] != 'P') return null;
			int player = 0;
			if (!int.TryParse(parts[0].Substring(1), out player))
				return null;
			var bnp = new ButtonNameParser();
			bnp.PlayerNum = player;
			bnp.ButtonPart = button.Substring(parts[0].Length + 1);
			return bnp;
		}

		public int PlayerNum;
		public string ButtonPart;

		public override string ToString()
		{
			return string.Format("P{0} {1}", PlayerNum, ButtonPart);
		}
	}

	/// <summary>
	/// rewires player1 controls to playerN
	/// </summary>
	public class MultitrackRewiringControllerAdapter : IController
	{
		public IController Source;
		public int PlayerSource = 1;
		public int PlayerTargetMask = 0;

        public ControllerDefinition Type { get { return Source.Type; } }
        public bool this[string button] { get { return this.IsPressed(button); } }
        public float GetFloat(string name) { return Source.GetFloat(name); }
        public void UpdateControls(int frame) { Source.UpdateControls(frame); }
        
		public bool IsPressed(string button)
		{
			//do we even have a source?
			if(PlayerSource == -1) return Source.IsPressed(button);

			//see if we're being asked for a button that we know how to rewire
			ButtonNameParser bnp = ButtonNameParser.Parse(button);
			if(bnp == null) return Source.IsPressed(button);

			//ok, this looks like a normal `P1 Button` type thing. we can handle it
			//were we supposed to replace this one?
			int foundPlayerMask = (1 << bnp.PlayerNum);
			if((PlayerTargetMask & foundPlayerMask)==0) return Source.IsPressed(button);
			//ok, we were. swap out the source player and then grab his button
			bnp.PlayerNum = PlayerSource;
			return Source.IsPressed(bnp.ToString());
		}
	}

	public class MovieControllerAdapter : IController
	{
		public MovieControllerAdapter()
		{
			//OutputController = new ForceControllerAdapter();
		}

		IController Source;

		public void SetSource(IController source)
		{
			//OutputController.Controller = source;
			Source = source;
		}

		//IController implementation:
		public ControllerDefinition Type { get { return Source.Type; } }
		public bool this[string button] { get { return MyBoolButtons[button]; } }
		public bool IsPressed(string button) { return MyBoolButtons[button]; }
		public float GetFloat(string name) { return Source.GetFloat(name); }
		public void UpdateControls(int frame) { Source.UpdateControls(frame); }
		//--------

		Dictionary<string, bool> MyBoolButtons = new Dictionary<string, bool>();

		void Force(string button, bool state)
		{
			MyBoolButtons[button] = state;
		}

		string ControlType { get { return Type.Name; } }

		class MnemonicChecker
		{
			public MnemonicChecker(string _m)
			{
				m = _m;
			}
			string m;
			public bool this[int c]
			{
				get { return m[c] != '.'; }
			}
		}


		/// <summary>
		/// latches one player from the source
		/// </summary>
		public void LatchPlayerFromSource(int playerNum)
		{
			foreach (string button in Source.Type.BoolButtons)
			{
				ButtonNameParser bnp = ButtonNameParser.Parse(button);
				if (bnp == null) continue;
				if (bnp.PlayerNum != playerNum) continue;
				bool val = Source[button];
				MyBoolButtons[button] = val;
			}
		}

		/// <summary>
		/// latches all buttons from the upstream source
		/// </summary>
		public void LatchFromSource()
		{
			foreach (string button in Type.BoolButtons)
			{
				MyBoolButtons[button] = Source[button];
			}
		}

		/// <summary>
		/// latches all buttons from the supplied mnemonic string
		/// </summary>
		public void SetControllersAsMnemonic(string mnemonic)
		{
			MnemonicChecker c = new MnemonicChecker(mnemonic);
			
			MyBoolButtons.Clear();

			if (ControlType == "SMS Controller")
			{
				Force("P1 Up", c[1]);
				Force("P1 Down", c[2]);
				Force("P1 Left", c[3]);
				Force("P1 Right", c[4]);
				Force("P1 B1", c[5]);
				Force("P1 B2", c[6]);

				Force("P2 Up", c[8]);
				Force("P2 Down", c[9]);
				Force("P2 Left", c[10]);
				Force("P2 Right", c[11]);
				Force("P2 B1", c[12]);
				Force("P2 B2", c[13]);

				Force("Pause",c[15]);
				Force("Reset",c[16]);
			}

			if (ControlType == "PC Engine Controller")
			{
				for (int i = 1; i < 6; i++)
				{
					int playerNum = i;
					int srcindex = (playerNum - 1) * 9;
					Force("P" + i + " Up", c[srcindex + 3]);
					Force("P" + i + " Down", c[srcindex + 4]);
					Force("P" + i + " Left", c[srcindex + 5]);
					Force("P" + i + " Right", c[srcindex + 6]);
					Force("P" + i + " B1", c[srcindex + 7]);
					Force("P" + i + " B2", c[srcindex + 8]);
					Force("P" + i + " Run", c[srcindex + 9]);
					Force("P" + i + " Select", c[srcindex + 10]);
				}
			}

			if (ControlType == "NES Controls")
			{
				if (mnemonic.Length < 10) return;
				Force("Reset", mnemonic[1] != '.' && mnemonic[1] != '0');
				int ctr = 3;
				Force("P1 Right",c[ctr++]);
				Force("P1 Left", c[ctr++]);
				Force("P1 Down", c[ctr++]);
				Force("P1 Up", c[ctr++]);
				Force("P1 Start", c[ctr++]);
				Force("P1 Select", c[ctr++]);
				Force("P1 B", c[ctr++]);
				Force("P1 A", c[ctr++]);

				if (mnemonic.Length < 20) return;
				ctr = 12;
				Force("P2 Right",c[ctr++]);
				Force("P2 Left", c[ctr++]);
				Force("P2 Down", c[ctr++]);
				Force("P2 Up", c[ctr++]);
				Force("P2 Start", c[ctr++]);
				Force("P2 Select", c[ctr++]);
				Force("P2 B", c[ctr++]);
				Force("P2 A", c[ctr++]);
			}


			if (ControlType == "Gameboy Controller")
			{
				if (mnemonic.Length < 10) return;
				//if (mnemonic[1] != '.' && mnemonic[1] != '0') programmaticallyPressedButtons.Add("Reset");
				int ctr = 3;
				Force("P1 Right", c[ctr++]);
				Force("P1 Left", c[ctr++]);
				Force("P1 Down", c[ctr++]);
				Force("P1 Up", c[ctr++]);
				Force("P1 Start", c[ctr++]);
				Force("P1 Select", c[ctr++]);
				Force("P1 B", c[ctr++]);
				Force("P1 A", c[ctr++]);
			}

			if (ControlType == "TI83 Controls")
			{
				if (mnemonic.Length < 50) return;
				int ctr = 1;

				Force("0",c[ctr++]);
				Force("1", c[ctr++]);
				Force("2", c[ctr++]);
				Force("3", c[ctr++]);
				Force("4", c[ctr++]);
				Force("5", c[ctr++]);
				Force("6", c[ctr++]);
				Force("7", c[ctr++]);
				Force("8", c[ctr++]);
				Force("9", c[ctr++]);
				Force("DOT", c[ctr++]);
				Force("ON", c[ctr++]);
				Force("ENTER", c[ctr++]);
				Force("UP", c[ctr++]);
				Force("DOWN", c[ctr++]);
				Force("LEFT", c[ctr++]);
				Force("RIGHT", c[ctr++]);
				Force("PLUS", c[ctr++]);
				Force("MINUS", c[ctr++]);
				Force("MULTIPLY", c[ctr++]);
				Force("DIVIDE", c[ctr++]);
				Force("CLEAR", c[ctr++]);
				Force("EXP", c[ctr++]);
				Force("DASH", c[ctr++]);
				Force("PARAOPEN", c[ctr++]);
				Force("PARACLOSE", c[ctr++]);
				Force("TAN", c[ctr++]);
				Force("VARS", c[ctr++]);
				Force("COS", c[ctr++]);
				Force("PGRM", c[ctr++]);
				Force("STAT", c[ctr++]);
				Force("MATRIX", c[ctr++]);
				Force("X", c[ctr++]);
				Force("STO", c[ctr++]);
				Force("LN", c[ctr++]);
				Force("LOG", c[ctr++]);
				Force("SQUARED", c[ctr++]);
				Force("NEG", c[ctr++]);
				Force("MATH", c[ctr++]);
				Force("ALPHA", c[ctr++]);
				Force("GRAPH", c[ctr++]);
				Force("TRACE", c[ctr++]);
				Force("ZOOM", c[ctr++]);
				Force("WINDOW", c[ctr++]);
				Force("Y", c[ctr++]);
				Force("2ND", c[ctr++]);
				Force("MODE", c[ctr++]);
				Force("DEL", c[ctr++]);
				Force("COMMA", c[ctr++]);
				Force("SIN", c[ctr++]);
			}
		}
	}

	//not being used..

	///// <summary>
	///// adapts an IController to force some buttons to a different state.
	///// unforced button states will flow through to the adaptee
	///// </summary>
	//public class ForceControllerAdapter : IController
	//{
	//    public IController Controller;

	//    public Dictionary<string, bool> Forces = new Dictionary<string, bool>();
	//    public void Clear()
	//    {
	//        Forces.Clear();
	//    }


	//    public ControllerDefinition Type { get { return Controller.Type; } }

	//    public bool this[string button] { get { return IsPressed(button); } }

	//    public bool IsPressed(string button)
	//    {
	//        if (Forces.ContainsKey(button))
	//            return Forces[button];
	//        else return Controller.IsPressed(button);
	//    }

	//    public float GetFloat(string name)
	//    {
	//        return Controller.GetFloat(name); //TODO!
	//    }

	//    public void UpdateControls(int frame)
	//    {
	//        Controller.UpdateControls(frame);
	//    }
	//}
}