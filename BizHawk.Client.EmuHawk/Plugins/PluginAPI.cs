using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public sealed class PluginAPI : IPluginAPI
	{
		public EmulatorPluginLibrary EmuLib => (EmulatorPluginLibrary)Libraries[typeof(EmulatorPluginLibrary)];
		public GameInfoPluginLibrary GameInfoLib => (GameInfoPluginLibrary)Libraries[typeof(GameInfoPluginLibrary)];
		public GUIDrawPluginBase GUILib => (GUIDrawPluginBase)Libraries[typeof(GUIPluginLibrary)];
		public JoypadPluginLibrary JoypadLib => (JoypadPluginLibrary)Libraries[typeof(JoypadPluginLibrary)];
		public MemoryPluginLibrary MemLib => (MemoryPluginLibrary)Libraries[typeof(MemoryPluginLibrary)];
		public MemoryEventsPluginLibrary MemEventsLib => (MemoryEventsPluginLibrary)Libraries[typeof(MemoryEventsPluginLibrary)];
		public MemorySavestatePluginLibrary MemStateLib => (MemorySavestatePluginLibrary)Libraries[typeof(MemorySavestatePluginLibrary)];
		public MoviePluginLibrary MovieLib => (MoviePluginLibrary)Libraries[typeof(MoviePluginLibrary)];
		public SQLPluginLibrary SQLLib => (SQLPluginLibrary)Libraries[typeof(SQLPluginLibrary)];
		public UserDataPluginLibrary UserDataLib => (UserDataPluginLibrary)Libraries[typeof(UserDataPluginLibrary)];
		public Dictionary<Type, PluginLibraryBase> Libraries { get; set; }
		public PluginAPI(Dictionary<Type, PluginLibraryBase> libs)
		{
			Libraries = libs;
		}
	}
}
