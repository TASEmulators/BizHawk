using System;
using System.Text;
using System.Collections.Generic;

namespace BizHawk.MultiClient
{
	/// <summary>
	/// will hold buttons for 1 frame and then release them. (Calling Click() from your button click is what you want to do)
	/// TODO - should the duration be controllable?
	/// </summary>
	public class ClickyVirtualPadController : IController
	{
		public ControllerDefinition Type { get; set; }
		public bool this[string button] { get { return IsPressed(button); } }
		public float GetFloat(string name) { return 0.0f; } //TODO
		public void UpdateControls(int frame) { }
		public bool IsPressed(string button)
		{
			return Pressed.Contains(button);
		}
		/// <summary>
		/// call this once per frame to do the timekeeping for the hold and release
		/// </summary>
		public void FrameTick()
		{
			Pressed.Clear();
		}

		/// <summary>
		/// call this to hold the button down for one frame
		/// </summary>
		public void Click(string button)
		{
			Pressed.Add(button);
		}

		HashSet<string> Pressed = new HashSet<string>();
	}

	//filters input for things called Up and Down while considering the client's AllowUD_LR option.
	//this is a bit gross but it is unclear how to do it more nicely
	public class UD_LR_ControllerAdapter : IController
	{
		public ControllerDefinition Type { get { return Source.Type; } }
		public IController Source;

		public bool this[string button] { get { return IsPressed(button); } }
		public float GetFloat(string name) { return 0.0f; } //TODO
		public void UpdateControls(int frame) { }

		public bool IsPressed(string button)
		{
			if (Global.Config.AllowUD_LR == true)
				return Source.IsPressed(button);

			string prefix;

			if (button.Contains("Down"))
			{
				prefix = button.GetPrecedingString("Down");
				if (Source.IsPressed(prefix + "Up"))
					return false;
			}
			if (button.Contains("Right"))
			{
				prefix = button.GetPrecedingString("Right");
				if (Source.IsPressed(prefix + "Left"))
					return false;
			}

			return Source.IsPressed(button);
		}
	}

	public class SimpleController : IController
	{
		public ControllerDefinition Type { get; set; }

		protected WorkingDictionary<string, bool> Buttons = new WorkingDictionary<string, bool>();
		public virtual bool this[string button] { get { return Buttons[button]; } set { Buttons[button] = value; } }
		public virtual bool IsPressed(string button) { return this[button]; }
		public float GetFloat(string name) { return 0.0f; } //TODO
		public void UpdateControls(int frame) { }

		public IEnumerable<KeyValuePair<string, bool>> BoolButtons()
		{
			foreach (var kvp in Buttons) yield return kvp;
		}

		public virtual void LatchFrom(IController source)
		{
			foreach (string button in source.Type.BoolButtons)
			{
				Buttons[button] = source[button];
			}
		}
	}

	public class ORAdapter : IController
	{
		public bool IsPressed(string button) { return this[button]; }
		public float GetFloat(string name) { return 0.0f; } //TODO
		public void UpdateControls(int frame) { }

		public IController Source;
		public IController SourceOr;
		public ControllerDefinition Type { get { return Source.Type; } set { throw new InvalidOperationException(); } }

		public bool this[string button]
		{
			get
			{
				bool source = Source[button] | SourceOr[button];
				return source;
			}
			set { throw new InvalidOperationException(); }
		}

	}

	public class StickyXORAdapter : IController
	{
		private HashSet<string> stickySet = new HashSet<string>();
		public IController Source;

		public ControllerDefinition Type { get { return Source.Type; } set { throw new InvalidOperationException(); } }
		public bool Locked = false; //Pretty much a hack, 

		public bool IsPressed(string button) { return this[button]; }
		public float GetFloat(string name) { return 0.0f; } //TODO
		public void UpdateControls(int frame) { }

		public bool this[string button] { 
			get 
			{
				bool source = Source[button];
				if (source)
				{
				}
				source ^= stickySet.Contains(button);
				return source;
			}
			set { throw new InvalidOperationException(); }
		}

		public void SetSticky(string button, bool isSticky)
		{
			if(isSticky)
				stickySet.Add(button);
			else stickySet.Remove(button);
		}

		public bool IsSticky(string button)
		{
			return stickySet.Contains(button);
		}

		public HashSet<string> CurrentStickies
		{
			get
			{
				return stickySet;
			}
		}

		public void ClearStickies()
		{
			stickySet.Clear();
		}

		public void MassToggleStickyState(List<string> buttons)
		{
			foreach (string button in buttons)
			{
				if (!JustPressed.Contains(button))
				{
					if (stickySet.Contains(button))
					{
						stickySet.Remove(button);
					}
					else
					{
						stickySet.Add(button);
					}
				}
			}
			JustPressed = buttons;
		}

		private List<string> JustPressed = new List<string>();
	}

	public class AutoFireStickyXORAdapter : IController
	{
		public int On { get; set; }
		public int Off { get; set; }
		public WorkingDictionary<string, int> buttonStarts = new WorkingDictionary<string, int>();
		
		private HashSet<string> stickySet = new HashSet<string>();

		public IController Source;

		public void SetOnOffPatternFromConfig()
		{
			On = Global.Config.AutofireOn < 1 ? 0 : Global.Config.AutofireOn;
			Off = Global.Config.AutofireOff < 1 ? 0 : Global.Config.AutofireOff;
		}

		public AutoFireStickyXORAdapter()
		{
			//On = Global.Config.AutofireOn < 1 ? 0 : Global.Config.AutofireOn;
			//Off = Global.Config.AutofireOff < 1 ? 0 : Global.Config.AutofireOff;
			On = 1;
			Off = 1;
		}

		public bool IsPressed(string button)
		{
			if (stickySet.Contains(button))
			{
				int a = (Global.Emulator.Frame - buttonStarts[button]) % (On + Off);
				if (a < On)
					return this[button];
				else
					return false;
			}
			else
			{
				return Source[button];
			}
		}

		public bool this[string button]
		{
			get
			{
				bool source = Source[button];
				if (source)
				{
				}
				if (stickySet.Contains(button))
				{


					int a = (Global.Emulator.Frame - buttonStarts[button]) % (On + Off);
					if (a < On)
					{
						source ^= true;
					}
					else
					{
						source ^= false;
					}
				}
				
				return source;
			}
			set { throw new InvalidOperationException(); }
		}




		public ControllerDefinition Type { get { return Source.Type; } set { throw new InvalidOperationException(); } }
		public bool Locked = false; //Pretty much a hack, 


		public float GetFloat(string name) { return 0.0f; } //TODO
		public void UpdateControls(int frame) { }

		public void SetSticky(string button, bool isSticky)
		{
			if (isSticky)
				stickySet.Add(button);
			else stickySet.Remove(button);
		}

		public bool IsSticky(string button)
		{
			return stickySet.Contains(button);
		}

		public HashSet<string> CurrentStickies
		{
			get
			{
				return stickySet;
			}
		}

		public void ClearStickies()
		{
			stickySet.Clear();
		}

		public void MassToggleStickyState(List<string> buttons)
		{
			foreach (string button in buttons)
			{
				if (!JustPressed.Contains(button))
				{
					if (stickySet.Contains(button))
					{
						stickySet.Remove(button);
					}
					else
					{
						stickySet.Add(button);
					}
				}
			}
			JustPressed = buttons;
		}

		private List<string> JustPressed = new List<string>();
	}

	public class MnemonicsGenerator
	{
		IController Source;
		public void SetSource(IController source)
		{
			Source = source;
			ControlType = source.Type.Name;
		}
		string ControlType;

		bool IsBasePressed(string name)
		{
			bool ret = Source.IsPressed(name);
			return ret;
		}

		public string GetEmptyMnemonic()
		{
			switch (Global.Emulator.SystemId)
			{
				default:
				case "NULL":
					return "|.|";
				case "A26":
					return "|..|.....|.....|";
				case "TI83":
					return "|..................................................|.|";
				case "NES":
					return "|.|........|........|........|........|";
				case "SNES":
					return "|.|............|............|............|............|";
				case "SMS":
				case "GG":
				case "SG":
					return "|......|......|..|";
				case "GEN":
					return "|.|........|........|";
				case "GB":
					return "|.|........|";
				case "PCE":
				case "PCECD":
				case "SGX":
					return "|.|........|........|........|........|........|";
			}
		}

		//adelikat: I"m going to do all controllers like this, so what if it is redundant! It is better than reducing lines of code with convoluted logic that is difficult to expand to support new platforms
		private string GetSNESControllersAsMnemonic()
		{
			StringBuilder input = new StringBuilder("|");

			if (IsBasePressed("Power"))
			{
				input.Append(Global.COMMANDS[ControlType]["Power"]);
			}
			else if (IsBasePressed("Reset"))
			{
				input.Append(Global.COMMANDS[ControlType]["Reset"]);
			}

			input.Append("|");
			for (int player = 1; player <= Global.PLAYERS[ControlType]; player++)
			{
				foreach (string button in Global.BUTTONS[ControlType].Keys)
				{
					
					input.Append(IsBasePressed("P" + player + " " + button) ? Global.BUTTONS[ControlType][button] : ".");
				}
				input.Append("|");
			}

			return input.ToString();
		}


		public string GetControllersAsMnemonic()
		{
			if (ControlType == "Null Controller")
			{
				return "|.|";
			}
			else if (ControlType == "SNES Controller")
			{
				return GetSNESControllersAsMnemonic();
			}

			StringBuilder input = new StringBuilder("|");

			if (
				ControlType == "Genesis 3-Button Controller" || // ControlType == "Gameboy Controller" ||
				ControlType == "PC Engine Controller"
			)
			{
				input.Append(".");
			}
			if (ControlType == "Atari 2600 Basic Controller")
			{
				input.Append(IsBasePressed("Reset") ? "r" : ".");
				input.Append(IsBasePressed("Select") ? "s" : ".");
			}
			if (ControlType == "NES Controller")
			{
				input.Append(IsBasePressed("Reset") ? Global.COMMANDS[ControlType]["Reset"] : ".");
			}
			if (ControlType == "Gameboy Controller")
			{
				input.Append(IsBasePressed("Power") ? Global.COMMANDS[ControlType]["Power"] : ".");
			}
			if (ControlType != "SMS Controller" && ControlType != "TI83 Controller")
			{
				input.Append("|");
			}
			for (int player = 1; player <= Global.PLAYERS[ControlType]; player++)
			{
				string prefix = "";
				if (ControlType != "Gameboy Controller" && ControlType != "TI83 Controller")
				{
					prefix = "P" + player.ToString() + " ";
				}
				foreach (string button in Global.BUTTONS[ControlType].Keys)
				{
					input.Append(IsBasePressed(prefix + button) ? Global.BUTTONS[ControlType][button] : ".");
				}
				input.Append("|");
			}
			if (ControlType == "SMS Controller")
			{
				foreach (string command in Global.COMMANDS[ControlType].Keys)
				{
					input.Append(IsBasePressed(command) ? Global.COMMANDS[ControlType][command] : ".");
				}
				input.Append("|");
			}
			if (ControlType == "TI83 Controller")
			{
				input.Append(".|"); //TODO: perhaps ON should go here?
			}
			return input.ToString();
		}
	}
	/// <summary>
	/// just copies source to sink, or returns whatever a NullController would if it is disconnected. useful for immovable hardpoints.
	/// </summary>
	public class CopyControllerAdapter : IController
	{
		public IController Source;
		NullController _null = new NullController();

		IController Curr
		{
			get
			{
				if (Source == null) return _null;
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
			if (PlayerSource == -1) return Source.IsPressed(button);

			//see if we're being asked for a button that we know how to rewire
			ButtonNameParser bnp = ButtonNameParser.Parse(button);
			if (bnp == null) return Source.IsPressed(button);

			//ok, this looks like a normal `P1 Button` type thing. we can handle it
			//were we supposed to replace this one?
			int foundPlayerMask = (1 << bnp.PlayerNum);
			if ((PlayerTargetMask & foundPlayerMask) == 0) return Source.IsPressed(button);
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

		//IController implementation:
		public ControllerDefinition Type { get; set; }
		public bool this[string button] { get { return MyBoolButtons[button]; } }
		public bool IsPressed(string button) { return MyBoolButtons[button]; }
		public float GetFloat(string name) { return 0; }
		public void UpdateControls(int frame) {  }
		//--------

		WorkingDictionary<string, bool> MyBoolButtons = new WorkingDictionary<string, bool>();

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
		public void LatchPlayerFromSource(IController playerSource, int playerNum)
		{
			foreach (string button in playerSource.Type.BoolButtons)
			{
				ButtonNameParser bnp = ButtonNameParser.Parse(button);
				if (bnp == null) continue;
				if (bnp.PlayerNum != playerNum) continue;
				bool val = playerSource[button];
				MyBoolButtons[button] = val;
			}
		}

		/// <summary>
		/// latches all buttons from the provided source
		/// </summary>
		public void LatchFromSource(IController source)
		{
			foreach (string button in Type.BoolButtons)
			{
				MyBoolButtons[button] = source[button];
			}
		}

		//Redundancy beats crazy if logic that makes new consoles annoying to add
		private void SetSNESControllersAsMnemonic(string mnemonic)
		{
			MnemonicChecker c = new MnemonicChecker(mnemonic);
			MyBoolButtons.Clear();

			if (mnemonic.Length < 2)
			{
				return;
			}

			if (mnemonic[1] == 'P')
			{
				Force("Power", true);
			}
			else if (mnemonic[1] != '.' && mnemonic[1] != '0')
			{
				Force("Reset", true);
			}
			
			for (int player = 1; player <= Global.PLAYERS[ControlType]; player++)
			{
				int srcindex = (player - 1) * (Global.BUTTONS[ControlType].Count + 1);
				
				if (mnemonic.Length < srcindex + 3 + Global.BUTTONS[ControlType].Count - 1)
				{
					return;
				}

				int start = 3;
				foreach (string button in Global.BUTTONS[ControlType].Keys)
				{
					Force("P" + player + " " + button, c[srcindex + start++]);
				}
			}

		}

		/// <summary>
		/// latches all buttons from the supplied mnemonic string
		/// </summary>
		public void SetControllersAsMnemonic(string mnemonic)
		{
			if (ControlType == "Null Controller")
			{
				return;
			}
			else if (ControlType == "SNES Controller")
			{
				SetSNESControllersAsMnemonic(mnemonic);
			}
			MnemonicChecker c = new MnemonicChecker(mnemonic);

			MyBoolButtons.Clear();

			int start = 3;
			if (ControlType == "NES Controller")
			{
				if (mnemonic.Length < 2) return;
				Force("Reset", mnemonic[1] != '.' && mnemonic[1] != '0' && mnemonic[1] != 'l');
			}
			if (ControlType == "Gameboy Controller")
			{
				if (mnemonic.Length < 2) return;
				Force("Power", mnemonic[1] != '.');
			}
			if (ControlType == "SMS Controller" || ControlType == "TI83 Controller")
			{
				start = 1;
			}
			if (ControlType == "Atari 2600 Basic Controller")
			{
				if (mnemonic.Length < 2) return;
				Force("Reset", mnemonic[1] != '.' && mnemonic[1] != '0');
				Force("Select", mnemonic[2] != '.' && mnemonic[2] != '0');
				start = 4;
			}
			for (int player = 1; player <= Global.PLAYERS[ControlType]; player++)
			{
				int srcindex = (player - 1) * (Global.BUTTONS[ControlType].Count + 1);
				int ctr = start;
				if (mnemonic.Length < srcindex + ctr + Global.BUTTONS[ControlType].Count - 1)
				{
					return;
				}
				string prefix = "";
				if (ControlType != "Gameboy Controller" && ControlType != "TI83 Controller")
				{
					prefix = "P" + player + " ";
				}
				foreach (string button in Global.BUTTONS[ControlType].Keys)
				{
					Force(prefix + button, c[srcindex + ctr++]);
				}
			}
			if (ControlType == "SMS Controller")
			{
				int srcindex = Global.PLAYERS[ControlType] * (Global.BUTTONS[ControlType].Count + 1);
				int ctr = start;
				foreach (string command in Global.COMMANDS[ControlType].Keys)
				{
					Force(command, c[srcindex + ctr++]);
				}
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