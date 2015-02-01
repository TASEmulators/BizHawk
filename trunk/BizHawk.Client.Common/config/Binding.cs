using System.Collections;
using System.Collections.Generic;
using System.Linq;

//TODO [LARP] - It's pointless and annoying to store such a big structure filled with static information
//use this instead
//public class UserBinding
//{
//  public string DisplayName;
//  public string Bindings;
//}
//...also. We should consider using something other than DisplayName for keying, maybe make a KEYNAME distinct from displayname. 
//displayname is OK for now though.

namespace BizHawk.Client.Common
{
	public class Binding
	{
		public string DisplayName;
		public string Bindings;
		public string DefaultBinding;
		public string TabGroup;
		public string ToolTip;
		public int Ordinal = 0;
	}

	[Newtonsoft.Json.JsonObject]
	public class BindingCollection : IEnumerable<Binding>
	{
		public List<Binding> Bindings { get; private set; }

		[Newtonsoft.Json.JsonConstructor]
		public BindingCollection(List<Binding> Bindings)
		{
			this.Bindings = Bindings;
		}

		public BindingCollection()
		{
			Bindings = new List<Binding>();
			Bindings.AddRange(DefaultValues);
		}

		public void Add(Binding b)
		{
			Bindings.Add(b);
		}

		public IEnumerator<Binding> GetEnumerator()
		{
			return Bindings.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public Binding this[string index]
		{
			get
			{
				return Bindings.FirstOrDefault(x => x.DisplayName == index) ?? new Binding();
			}
		}

		public void ResolveWithDefaults()
		{
			//TODO - this method is potentially disastrously O(N^2) slow due to linear search nested in loop

			//Add missing entries
			foreach (Binding default_binding in DefaultValues)
			{
				var binding = Bindings.FirstOrDefault(x => x.DisplayName == default_binding.DisplayName);
				if (binding == null)
				{
					Bindings.Add(default_binding);
				}
				else
				{
					//patch entries with updated settings (necessary because of TODO LARP
					binding.Ordinal = default_binding.Ordinal;
					binding.DefaultBinding = default_binding.DefaultBinding;
					binding.TabGroup = default_binding.TabGroup;
					binding.ToolTip = default_binding.ToolTip;
					binding.Ordinal = default_binding.Ordinal;
				}
			}

			List<Binding> entriesToRemove = (from entry in Bindings let binding = DefaultValues.FirstOrDefault(x => x.DisplayName == entry.DisplayName) where binding == null select entry).ToList();

			//Remove entries that no longer exist in defaults

			foreach (Binding entry in entriesToRemove)
			{
				Bindings.Remove(entry);
			}

		}

		static List<Binding> s_DefaultValues;

		public static List<Binding> DefaultValues
		{
			get
			{
				if (s_DefaultValues == null)
				{
					s_DefaultValues = new List<Binding>
						{
						//General
						new Binding { DisplayName = "Frame Advance", Bindings = "F", TabGroup = "General", DefaultBinding = "F" },
						new Binding { DisplayName = "Rewind", Bindings = "Shift+R, J1 B7, X1 LeftTrigger", TabGroup = "General", DefaultBinding = "Shift+R, J1 B7, X1 Left Trigger" },
						new Binding { DisplayName = "Pause", Bindings = "Pause", TabGroup = "General", DefaultBinding = "Pause" },
						new Binding { DisplayName = "Fast Forward", Bindings = "Tab, J1 B8, X1 RightTrigger", TabGroup = "General", DefaultBinding = "Tab, J1 B8, X1 Right Trigger" },
						new Binding { DisplayName = "Turbo", Bindings = "Shift+Tab", TabGroup = "General", DefaultBinding = "Shift+Tab" },
						new Binding { DisplayName = "Toggle Throttle", Bindings = "", TabGroup = "General", DefaultBinding = "" },
						new Binding { DisplayName = "Soft Reset", Bindings = "", TabGroup = "General", DefaultBinding = "" },
						new Binding { DisplayName = "Hard Reset", Bindings = "", TabGroup = "General", DefaultBinding = "" },
						new Binding { DisplayName = "Quick Load", Bindings = "P", TabGroup = "General", DefaultBinding = "P" },
						new Binding { DisplayName = "Quick Save", Bindings = "I", TabGroup = "General", DefaultBinding = "I" },
						new Binding { DisplayName = "Autohold", Bindings = "", TabGroup = "General", DefaultBinding = "" },
						new Binding { DisplayName = "Clear Autohold", Bindings = "", TabGroup = "General", DefaultBinding = "" },
						new Binding { DisplayName = "Screenshot", Bindings = "F12", TabGroup = "General", DefaultBinding = "F12" },
						new Binding { DisplayName = "Full Screen", Bindings = "Alt+Return", TabGroup = "General", DefaultBinding = "Alt+Return" },
						new Binding { DisplayName = "Open ROM", Bindings = "Ctrl+O", TabGroup = "General", DefaultBinding = "Ctrl+O" },
						new Binding { DisplayName = "Close ROM", Bindings = "Ctrl+W", TabGroup = "General", DefaultBinding = "Ctrl+W" },
						new Binding { DisplayName = "Display FPS", Bindings = "", TabGroup = "General", DefaultBinding = "" },
						new Binding { DisplayName = "Frame Counter", Bindings = "", TabGroup = "General", DefaultBinding = "" },
						new Binding { DisplayName = "Lag Counter", Bindings = "", TabGroup = "General", DefaultBinding = "" },
						new Binding { DisplayName = "Input Display", Bindings = "", TabGroup = "General", DefaultBinding = "" },
						new Binding { DisplayName = "Toggle BG Input", Bindings = "", TabGroup = "General", DefaultBinding = "" },
						new Binding { DisplayName = "Toggle Menu", Bindings = "", TabGroup = "General", DefaultBinding = "" },
						new Binding { DisplayName = "Volume Up", Bindings = "", TabGroup = "General", DefaultBinding = "" },
						new Binding { DisplayName = "Volume Down", Bindings = "", TabGroup = "General", DefaultBinding = "" },
						new Binding { DisplayName = "Record A/V", Bindings = "", TabGroup = "General", DefaultBinding = "" },
						new Binding { DisplayName = "Stop A/V", Bindings = "", TabGroup = "General", DefaultBinding = "" },
						new Binding { DisplayName = "Larger Window", Bindings = "Alt+UpArrow", TabGroup = "General", DefaultBinding = "Alt+UpArrow" },
						new Binding { DisplayName = "Smaller Window", Bindings = "Alt+DownArrow", TabGroup = "General", DefaultBinding = "Alt+DownArrow" },
						new Binding { DisplayName = "Increase Speed", Bindings = "Equals", TabGroup = "General", DefaultBinding = "Equals" },
						new Binding { DisplayName = "Decrease Speed", Bindings = "Minus", TabGroup = "General", DefaultBinding = "Minus" },
						new Binding { DisplayName = "Reboot Core", Bindings = "Ctrl+R", TabGroup = "General", DefaultBinding = "Ctrl+R" },
						new Binding { DisplayName = "Autofire", Bindings = "", TabGroup = "General", DefaultBinding = "" },
						new Binding { DisplayName = "Toggle Sound", Bindings = "", TabGroup = "General", DefaultBinding = "" },
						new Binding { DisplayName = "Exit Program", Bindings = "", TabGroup = "General", DefaultBinding = "" },

						//Save States
						new Binding { DisplayName = "Save State 0", Bindings = "Shift+F10", TabGroup = "Save States", DefaultBinding = "Shift+F10" },
						new Binding { DisplayName = "Save State 1", Bindings = "Shift+F1", TabGroup = "Save States", DefaultBinding = "Shift+F1" },
						new Binding { DisplayName = "Save State 2", Bindings = "Shift+F2", TabGroup = "Save States", DefaultBinding = "Shift+F2" },
						new Binding { DisplayName = "Save State 3", Bindings = "Shift+F3", TabGroup = "Save States", DefaultBinding = "Shift+F3" },
						new Binding { DisplayName = "Save State 4", Bindings = "Shift+F4", TabGroup = "Save States", DefaultBinding = "Shift+F4" },
						new Binding { DisplayName = "Save State 5", Bindings = "Shift+F5", TabGroup = "Save States", DefaultBinding = "Shift+F5" },
						new Binding { DisplayName = "Save State 6", Bindings = "Shift+F6", TabGroup = "Save States", DefaultBinding = "Shift+F6" },
						new Binding { DisplayName = "Save State 7", Bindings = "Shift+F7", TabGroup = "Save States", DefaultBinding = "Shift+F7" },
						new Binding { DisplayName = "Save State 8", Bindings = "Shift+F8", TabGroup = "Save States", DefaultBinding = "Shift+F8" },
						new Binding { DisplayName = "Save State 9", Bindings = "Shift+F9", TabGroup = "Save States", DefaultBinding = "Shift+F9" },
						new Binding { DisplayName = "Load State 0", Bindings = "F10", TabGroup = "Save States", DefaultBinding = "F10" },
						new Binding { DisplayName = "Load State 1", Bindings = "F1", TabGroup = "Save States", DefaultBinding = "F1" },
						new Binding { DisplayName = "Load State 2", Bindings = "F2", TabGroup = "Save States", DefaultBinding = "F2" },
						new Binding { DisplayName = "Load State 3", Bindings = "F3", TabGroup = "Save States", DefaultBinding = "F3" },
						new Binding { DisplayName = "Load State 4", Bindings = "F4", TabGroup = "Save States", DefaultBinding = "F4" },
						new Binding { DisplayName = "Load State 5", Bindings = "F5", TabGroup = "Save States", DefaultBinding = "F5" },
						new Binding { DisplayName = "Load State 6", Bindings = "F6", TabGroup = "Save States", DefaultBinding = "F6" },
						new Binding { DisplayName = "Load State 7", Bindings = "F7", TabGroup = "Save States", DefaultBinding = "F7" },
						new Binding { DisplayName = "Load State 8", Bindings = "F8", TabGroup = "Save States", DefaultBinding = "F8" },
						new Binding { DisplayName = "Load State 9", Bindings = "F9", TabGroup = "Save States", DefaultBinding = "F9" },
						new Binding { DisplayName = "Select State 0", Bindings = "D0", TabGroup = "Save States", DefaultBinding = "D0" },
						new Binding { DisplayName = "Select State 1", Bindings = "D1", TabGroup = "Save States", DefaultBinding = "D1" },
						new Binding { DisplayName = "Select State 2", Bindings = "D2", TabGroup = "Save States", DefaultBinding = "D2" },
						new Binding { DisplayName = "Select State 3", Bindings = "D3", TabGroup = "Save States", DefaultBinding = "D3" },
						new Binding { DisplayName = "Select State 4", Bindings = "D4", TabGroup = "Save States", DefaultBinding = "D4" },
						new Binding { DisplayName = "Select State 5", Bindings = "D5", TabGroup = "Save States", DefaultBinding = "D5" },
						new Binding { DisplayName = "Select State 6", Bindings = "D6", TabGroup = "Save States", DefaultBinding = "D6" },
						new Binding { DisplayName = "Select State 7", Bindings = "D7", TabGroup = "Save States", DefaultBinding = "D7" },
						new Binding { DisplayName = "Select State 8", Bindings = "D8", TabGroup = "Save States", DefaultBinding = "D8" },
						new Binding { DisplayName = "Select State 9", Bindings = "D9", TabGroup = "Save States", DefaultBinding = "D9" },
						new Binding { DisplayName = "Save Named State", Bindings = "", TabGroup = "Save States", DefaultBinding = "" },
						new Binding { DisplayName = "Load Named State", Bindings = "", TabGroup = "Save States", DefaultBinding = "" },
						new Binding { DisplayName = "Previous Slot", Bindings = "", TabGroup = "Save States", DefaultBinding = "" },
						new Binding { DisplayName = "Next Slot", Bindings = "", TabGroup = "Save States", DefaultBinding = "" },

						//Movie
						new Binding { DisplayName = "Toggle read-only", Bindings = "Q", TabGroup = "Movie", DefaultBinding = "Q" },
						new Binding { DisplayName = "Play Movie", Bindings = "", TabGroup = "Movie", DefaultBinding = "" },
						new Binding { DisplayName = "Record Movie", Bindings = "", TabGroup = "Movie", DefaultBinding = "" },
						new Binding { DisplayName = "Stop Movie", Bindings = "", TabGroup = "Movie", DefaultBinding = "" },
						new Binding { DisplayName = "Play from beginning", Bindings = "", TabGroup = "Movie", DefaultBinding = "" },
						new Binding { DisplayName = "Save Movie", Bindings = "", TabGroup = "Movie", DefaultBinding = "" },
						new Binding { DisplayName = "Toggle MultiTrack", Bindings = "", TabGroup = "Movie", DefaultBinding = "" },
						new Binding { DisplayName = "MT Select All", Bindings = "", TabGroup = "Movie", DefaultBinding = "" },
						new Binding { DisplayName = "MT Select None", Bindings = "", TabGroup = "Movie", DefaultBinding = "" },
						new Binding { DisplayName = "MT Increment Player", Bindings = "", TabGroup = "Movie", DefaultBinding = "" },
						new Binding { DisplayName = "MT Decrement Player", Bindings = "", TabGroup = "Movie", DefaultBinding = "" },
						new Binding { DisplayName = "Movie Poke", Bindings = "", TabGroup = "Movie", DefaultBinding = "" },
						new Binding { DisplayName = "Scrub Input", Bindings = "", TabGroup = "Movie", DefaultBinding = "" },

						//Tools
						new Binding { DisplayName = "Ram Watch", Bindings = "", TabGroup = "Tools", DefaultBinding = "" },
						new Binding { DisplayName = "Ram Search", Bindings = "", TabGroup = "Tools", DefaultBinding = "" },
						new Binding { DisplayName = "Hex Editor", Bindings = "", TabGroup = "Tools", DefaultBinding = "" },
						new Binding { DisplayName = "Trace Logger", Bindings = "", TabGroup = "Tools", DefaultBinding = "" },
						new Binding { DisplayName = "Lua Console", Bindings = "", TabGroup = "Tools", DefaultBinding = "" },
						new Binding { DisplayName = "Cheats", Bindings = "", TabGroup = "Tools", DefaultBinding = "" },
						new Binding { DisplayName = "TAStudio", Bindings = "", TabGroup = "Tools", DefaultBinding = "" },
						new Binding { DisplayName = "ToolBox", Bindings = "Shift+T", TabGroup = "Tools", DefaultBinding = "" },
						new Binding { DisplayName = "Virtual Pad", Bindings = "", TabGroup = "Tools", DefaultBinding = "" },

						new Binding { DisplayName = "New Search", Bindings = "", TabGroup = "Ram Search", DefaultBinding = "" },
						new Binding { DisplayName = "Do Search", Bindings = "", TabGroup = "Ram Search", DefaultBinding = "" },
						new Binding { DisplayName = "Previous Compare To", Bindings = "", TabGroup = "Ram Search", DefaultBinding = "" },
						new Binding { DisplayName = "Next Compare To", Bindings = "", TabGroup = "Ram Search", DefaultBinding = "" },
						new Binding { DisplayName = "Previous Operator", Bindings = "", TabGroup = "Ram Search", DefaultBinding = "" },
						new Binding { DisplayName = "Next Operator", Bindings = "", TabGroup = "Ram Search", DefaultBinding = "" },

						//SNES
						new Binding { DisplayName = "Toggle BG 1", Bindings = "", TabGroup = "SNES", DefaultBinding = "" },
						new Binding { DisplayName = "Toggle BG 2", Bindings = "", TabGroup = "SNES", DefaultBinding = "" },
						new Binding { DisplayName = "Toggle BG 3", Bindings = "", TabGroup = "SNES", DefaultBinding = "" },
						new Binding { DisplayName = "Toggle BG 4", Bindings = "", TabGroup = "SNES", DefaultBinding = "" },
						new Binding { DisplayName = "Toggle OBJ 1", Bindings = "", TabGroup = "SNES", DefaultBinding = "" },
						new Binding { DisplayName = "Toggle OBJ 2", Bindings = "", TabGroup = "SNES", DefaultBinding = "" },
						new Binding { DisplayName = "Toggle OBJ 3", Bindings = "", TabGroup = "SNES", DefaultBinding = "" },
						new Binding { DisplayName = "Toggle OBJ 4", Bindings = "", TabGroup = "SNES", DefaultBinding = "" },

						//Analog
						new Binding { DisplayName = "Y Up Small", Bindings = "", TabGroup = "Analog", DefaultBinding = "", ToolTip = "For Virtual Pad" },
						new Binding { DisplayName = "Y Up Large", Bindings = "", TabGroup = "Analog", DefaultBinding = "", ToolTip = "For Virtual Pad" },
						new Binding { DisplayName = "Y Down Small", Bindings = "", TabGroup = "Analog", DefaultBinding = "", ToolTip = "For Virtual Pad" },
						new Binding { DisplayName = "Y Down Large", Bindings = "", TabGroup = "Analog", DefaultBinding = "", ToolTip = "For Virtual Pad" },
						new Binding { DisplayName = "X Up Small", Bindings = "", TabGroup = "Analog", DefaultBinding = "", ToolTip = "For Virtual Pad" },
						new Binding { DisplayName = "X Up Large", Bindings = "", TabGroup = "Analog", DefaultBinding = "", ToolTip = "For Virtual Pad" },
						new Binding { DisplayName = "X Down Small", Bindings = "", TabGroup = "Analog", DefaultBinding = "", ToolTip = "For Virtual Pad" },
						new Binding { DisplayName = "X Down Large", Bindings = "", TabGroup = "Analog", DefaultBinding = "", ToolTip = "For Virtual Pad" },
			
						};

					//set ordinals based on order in list
					for (int i = 0; i < s_DefaultValues.Count; i++)
						s_DefaultValues[i].Ordinal = i;
				} //if (s_DefaultValues == null)

				return s_DefaultValues;
			}
		}
	}
}
