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

		public void Unclick(string button)
		{
			Pressed.Remove(button);
		}

		public void Toggle(string button)
		{
			if (IsPressed(button))
			{
				Pressed.Remove(button);
			}
			else
			{
				Pressed.Add(button);
			}
		}

		readonly HashSet<string> Pressed = new HashSet<string>();
	}

	//filters input for things called Up and Down while considering the client's AllowUD_LR option.
	//this is a bit gross but it is unclear how to do it more nicely
	public class UD_LR_ControllerAdapter : IController
	{
		public ControllerDefinition Type { get { return Source.Type; } }
		public IController Source;

		public bool this[string button] { get { return IsPressed(button); } }
		// the float format implies no U+D and no L+R no matter what, so just passthru
		public float GetFloat(string name) { return Source.GetFloat(name); }
		public void UpdateControls(int frame) { }

		public bool IsPressed(string button)
		{
			if (Global.Config.AllowUD_LR)
			{
				return Source.IsPressed(button);
			}

			string prefix;

			if (button.Contains("Down") && !button.Contains(" C "))
			{
				prefix = button.GetPrecedingString("Down");
				if (Source.IsPressed(prefix + "Up"))
				{
					return false;
				}
			}
			if (button.Contains("Right") && !button.Contains(" C "))
			{
				prefix = button.GetPrecedingString("Right");
				if (Source.IsPressed(prefix + "Left"))
				{
					return false;
				}
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
		// pass floats solely from the original source
		// this works in the code because SourceOr is the autofire controller
		public float GetFloat(string name) { return Source.GetFloat(name); }
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

	public class ForceOffAdaptor : IController
	{
		public bool IsPressed(string button) { return this[button]; }
		// what exactly would we want to do here with floats?
		// ForceOffAdaptor is only used by lua, and the code there looks like a big mess...
		public float GetFloat(string name) { return Source.GetFloat(name); }
		public void UpdateControls(int frame) { }

		protected HashSet<string> stickySet = new HashSet<string>();
		public IController Source;
		public IController SourceOr;
		public ControllerDefinition Type { get { return Source.Type; } set { throw new InvalidOperationException(); } }

		public bool this[string button]
		{
			get
			{
				if (stickySet.Contains(button))
				{
					return false;
				}
				else
				{
					return Source[button];
				}
			}
			set { throw new InvalidOperationException(); }
		}

		public void SetSticky(string button, bool isSticky)
		{
			if (isSticky)
				stickySet.Add(button);
			else stickySet.Remove(button);
		}
	}

	public class StickyXORAdapter : IController
	{
		protected HashSet<string> stickySet = new HashSet<string>();
		public IController Source;

		public ControllerDefinition Type { get { return Source.Type; } set { throw new InvalidOperationException(); } }
		public bool Locked = false; //Pretty much a hack, 

		public bool IsPressed(string button) { return this[button]; }

		// if SetFloat() is called (typically virtual pads), then that float will entirely override the Source input
		// otherwise, the source is passed thru.
		WorkingDictionary<string,float?> FloatSet = new WorkingDictionary<string,float?>();
		public void SetFloat(string name, float value)
		{
			FloatSet[name] = value;
		}
		public float GetFloat(string name)
		{
			return FloatSet[name] ?? Source.GetFloat(name);
		}
		public void ClearStickyFloats()
		{
			FloatSet.Clear();
		}


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
		
		private readonly HashSet<string> stickySet = new HashSet<string>();

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

		// dumb passthrough for floats, because autofire doesn't care about them
		public float GetFloat(string name) { return Source.GetFloat(name); }
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

	/// <summary>
	/// just copies source to sink, or returns whatever a NullController would if it is disconnected. useful for immovable hardpoints.
	/// </summary>
	public class CopyControllerAdapter : IController
	{
		public IController Source;
		
		private readonly NullController _null = new NullController();

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
			int player;
			if (!int.TryParse(parts[0].Substring(1), out player))
			{
				return null;
			}
			else
			{
				return new ButtonNameParser { PlayerNum = player, ButtonPart = button.Substring(parts[0].Length + 1) };
			}
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
		public bool this[string button] { get { return IsPressed(button); } }
		// floats can be player number remapped just like boolbuttons
		public float GetFloat(string name) { return Source.GetFloat(RemapButtonName(name)); }
		public void UpdateControls(int frame) { Source.UpdateControls(frame); }

		string RemapButtonName(string button)
		{
			//do we even have a source?
			if (PlayerSource == -1) return button;

			//see if we're being asked for a button that we know how to rewire
			ButtonNameParser bnp = ButtonNameParser.Parse(button);
			if (bnp == null) return button;

			//ok, this looks like a normal `P1 Button` type thing. we can handle it
			//were we supposed to replace this one?
			int foundPlayerMask = (1 << bnp.PlayerNum);
			if ((PlayerTargetMask & foundPlayerMask) == 0) return button;
			//ok, we were. swap out the source player and then grab his button
			bnp.PlayerNum = PlayerSource;
			return bnp.ToString();
		}

		public bool IsPressed(string button)
		{
			return Source.IsPressed(RemapButtonName(button));
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