using System.Collections;
using System.Collections.Generic;
using System.Linq;

// TODO [LARP] - It's pointless and annoying to store such a big structure filled with static information
// use this instead
// public class UserBinding
// {
//  public string DisplayName;
//  public string Bindings;
// }
// ...also. We should consider using something other than DisplayName for keying, maybe make a KEYNAME distinct from displayname. 
// displayname is OK for now though.
namespace BizHawk.Client.Common
{
	public class Binding
	{
		public string DisplayName { get; set; }
		public string Bindings { get; set; }
		public string DefaultBinding { get; set; }
		public string TabGroup { get; set; }
		public string ToolTip { get; set; }
		public int Ordinal { get; set; }
	}

	[Newtonsoft.Json.JsonObject]
	public class BindingCollection : IEnumerable<Binding>
	{
		public List<Binding> Bindings { get; }

		[Newtonsoft.Json.JsonConstructor]
		public BindingCollection(List<Binding> bindings)
		{
			Bindings = bindings;
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
				return Bindings.FirstOrDefault(b => b.DisplayName == index) ?? new Binding();
			}
		}

		private static Binding Bind(string tabGroup, string displayName, string bindings = "", string defaultBinding = "", string toolTip = "")
		{
			if (string.IsNullOrEmpty(defaultBinding))
			{
				defaultBinding = bindings;
			}

			return new Binding { DisplayName = displayName, Bindings = bindings, TabGroup = tabGroup, DefaultBinding = defaultBinding, ToolTip = toolTip };
		}

		public void ResolveWithDefaults()
		{
			// TODO - this method is potentially disastrously O(N^2) slow due to linear search nested in loop

			// Add missing entries
			foreach (Binding defaultBinding in DefaultValues)
			{
				var binding = Bindings.FirstOrDefault(b => b.DisplayName == defaultBinding.DisplayName);
				if (binding == null)
				{
					Bindings.Add(defaultBinding);
				}
				else
				{
					// patch entries with updated settings (necessary because of TODO LARP
					binding.Ordinal = defaultBinding.Ordinal;
					binding.DefaultBinding = defaultBinding.DefaultBinding;
					binding.TabGroup = defaultBinding.TabGroup;
					binding.ToolTip = defaultBinding.ToolTip;
					binding.Ordinal = defaultBinding.Ordinal;
				}
			}

			// Remove entries that no longer exist in defaults
			Bindings.RemoveAll(entry => DefaultValues.All(b => b.DisplayName != entry.DisplayName));
		}

		private static List<Binding> _defaultValues;

		public static List<Binding> DefaultValues
		{
			get
			{
				if (_defaultValues == null)
				{
					_defaultValues = new List<Binding>
					{
						Bind("General", "Frame Advance", "F"),
						Bind("General", "Rewind", "Shift+R, J1 B7, X1 LeftTrigger"),
						Bind("General", "Pause", "Pause"),
						Bind("General", "Fast Forward", "Tab, J1 B8, X1 RightTrigger"),
						Bind("General", "Turbo", "Shift+Tab"),
						Bind("General", "Toggle Throttle"),
						Bind("General", "Soft Reset"),
						Bind("General", "Hard Reset"),
						Bind("General", "Quick Load", "P"),
						Bind("General", "Quick Save", "I"),
						Bind("General", "Autofire"),
						Bind("General", "Autohold"),
						Bind("General", "Clear Autohold"),
						Bind("General", "Screenshot", "F12"),
						Bind("General", "Full Screen", "Alt+Return"),
						Bind("General", "Open ROM", "Ctrl+O"),
						Bind("General", "Close ROM", "Ctrl+W"),
						Bind("General", "Load Last ROM"),
						Bind("General", "Flush SaveRAM", "Ctrl+S"),
						Bind("General", "Display FPS"),
						Bind("General", "Frame Counter"),
						Bind("General", "Lag Counter"),
						Bind("General", "Input Display"),
						Bind("General", "Toggle BG Input"),
						Bind("General", "Toggle Menu"),
						Bind("General", "Volume Up"),
						Bind("General", "Volume Down"),
						Bind("General", "Record A/V"),
						Bind("General", "Stop A/V"),
						Bind("General", "Larger Window", "Alt+UpArrow"),
						Bind("General", "Smaller Window", "Alt+DownArrow"),
						Bind("General", "Increase Speed", "Equals"),
						Bind("General", "Decrease Speed", "Minus"),
						Bind("General", "Reboot Core", "Ctrl+R"),
						Bind("General", "Toggle Sound"),
						Bind("General", "Exit Program"),
						Bind("General", "Screen Raw to Clipboard", "Ctrl+C"),
						Bind("General", "Screen Client to Clipboard", "Ctrl+Shift+C"),
						Bind("General", "Toggle Skip Lag Frame"),
						Bind("General", "Toggle Key Priority"),

						Bind("Save States", "Save State 0", "Shift+F10"),
						Bind("Save States", "Save State 1", "Shift+F1"),
						Bind("Save States", "Save State 2", "Shift+F2"),
						Bind("Save States", "Save State 3", "Shift+F3"),
						Bind("Save States", "Save State 4", "Shift+F4"),
						Bind("Save States", "Save State 5", "Shift+F5"),
						Bind("Save States", "Save State 6", "Shift+F6"),
						Bind("Save States", "Save State 7", "Shift+F7"),
						Bind("Save States", "Save State 8", "Shift+F8"),
						Bind("Save States", "Save State 9", "Shift+F9"),
						Bind("Save States", "Load State 0", "F10"),
						Bind("Save States", "Load State 1", "F1"),
						Bind("Save States", "Load State 2", "F2"),
						Bind("Save States", "Load State 3", "F3"),
						Bind("Save States", "Load State 4", "F4"),
						Bind("Save States", "Load State 5", "F5"),
						Bind("Save States", "Load State 6", "F6"),
						Bind("Save States", "Load State 7", "F7"),
						Bind("Save States", "Load State 8", "F8"),
						Bind("Save States", "Load State 9", "F9"),
						Bind("Save States", "Select State 0", "D0"),
						Bind("Save States", "Select State 1", "D1"),
						Bind("Save States", "Select State 2", "D2"),
						Bind("Save States", "Select State 3", "D3"),
						Bind("Save States", "Select State 4", "D4"),
						Bind("Save States", "Select State 5", "D5"),
						Bind("Save States", "Select State 6", "D6"),
						Bind("Save States", "Select State 7", "D7"),
						Bind("Save States", "Select State 8", "D8"),
						Bind("Save States", "Select State 9", "D9"),
						Bind("Save States", "Save Named State"),
						Bind("Save States", "Load Named State"),
						Bind("Save States", "Previous Slot"),
						Bind("Save States", "Next Slot"),

						Bind("Movie", "Toggle read-only", "Q"),
						Bind("Movie", "Play Movie"),
						Bind("Movie", "Record Movie"),
						Bind("Movie", "Stop Movie"),
						Bind("Movie", "Play from beginning"),
						Bind("Movie", "Save Movie"),
						Bind("Movie", "Toggle MultiTrack"),
						Bind("Movie", "MT Select All"),
						Bind("Movie", "MT Select None"),
						Bind("Movie", "MT Increment Player"),
						Bind("Movie", "MT Decrement Player"),
						Bind("Movie", "Movie Poke"),
						Bind("Movie", "Scrub Input"),

						Bind("Tools", "RAM Watch"),
						Bind("Tools", "RAM Search"),
						Bind("Tools", "Hex Editor"),
						Bind("Tools", "Trace Logger"),
						Bind("Tools", "Lua Console"),
						Bind("Tools", "Cheats"),
						Bind("Tools", "TAStudio"),
						Bind("Tools", "ToolBox", "Shift+T"),
						Bind("Tools", "Virtual Pad"),

						Bind("RAM Search", "New Search"),
						Bind("RAM Search", "Do Search"),
						Bind("RAM Search", "Previous Compare To"),
						Bind("RAM Search", "Next Compare To"),
						Bind("RAM Search", "Previous Operator"),
						Bind("RAM Search", "Next Operator"),

						Bind("TAStudio", "Add Branch", "Alt+Insert"),
						Bind("TAStudio", "Delete Branch", "Alt+Delete"),
						Bind("TAStudio", "Show Cursor"),
						Bind("TAStudio", "Toggle Follow Cursor", "Shift+F"),
						Bind("TAStudio", "Toggle Auto-Restore", "Shift+R"),
						Bind("TAStudio", "Toggle Turbo Seek", "Shift+S"),
						Bind("TAStudio", "Clear Frames", "Delete"),
						Bind("TAStudio", "Insert Frame", "Insert"),
						Bind("TAStudio", "Delete Frames", "Ctrl+Delete"),
						Bind("TAStudio", "Clone Frames", "Ctrl+Insert"),
						Bind("TAStudio", "Analog Increment", "UpArrow"),
						Bind("TAStudio", "Analog Decrement", "DownArrow"),
						Bind("TAStudio", "Analog Incr. by 10", "Shift+UpArrow"),
						Bind("TAStudio", "Analog Decr. by 10", "Shift+DownArrow"),
						Bind("TAStudio", "Analog Maximum", "RightArrow"),
						Bind("TAStudio", "Analog Minimum", "LeftArrow"),

						Bind("SNES", "Toggle BG 1"),
						Bind("SNES", "Toggle BG 2"),
						Bind("SNES", "Toggle BG 3"),
						Bind("SNES", "Toggle BG 4"),
						Bind("SNES", "Toggle OBJ 1"),
						Bind("SNES", "Toggle OBJ 2"),
						Bind("SNES", "Toggle OBJ 3"),
						Bind("SNES", "Toggle OBJ 4"),

						Bind("GB", "GB Toggle BG"),
						Bind("GB", "GB Toggle Obj"),

						Bind("Analog", "Y Up Small", toolTip: "For Virtual Pad"),
						Bind("Analog", "Y Up Large", toolTip: "For Virtual Pad"),
						Bind("Analog", "Y Down Small", toolTip: "For Virtual Pad"),
						Bind("Analog", "Y Down Large", toolTip: "For Virtual Pad"),
						Bind("Analog", "X Up Small", toolTip: "For Virtual Pad"),
						Bind("Analog", "X Up Large", toolTip: "For Virtual Pad"),
						Bind("Analog", "X Down Small", toolTip: "For Virtual Pad"),
						Bind("Analog", "X Down Large", toolTip: "For Virtual Pad"),

						Bind("Tools", "Toggle All Cheats"),
					};

					// set ordinals based on order in list
					for (int i = 0; i < _defaultValues.Count; i++)
					{
						_defaultValues[i].Ordinal = i;
					}
				} // if (s_DefaultValues == null)

				return _defaultValues;
			}
		}
	}
}
