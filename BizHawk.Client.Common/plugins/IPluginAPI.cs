using System;
using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	public interface IPluginAPI
	{
		EmulatorPluginLibrary EmuLib { get; }
		GameInfoPluginLibrary GameInfoLib { get; }
		GUIDrawPluginBase GUILib { get; }
		JoypadPluginLibrary JoypadLib { get; }
		MemoryPluginLibrary MemLib { get; }
		MemoryEventsPluginLibrary MemEventsLib { get; }
		MemorySavestatePluginLibrary MemStateLib { get; }
		MoviePluginLibrary MovieLib { get; }
		SQLPluginLibrary SQLLib { get; }
		UserDataPluginLibrary UserDataLib { get; }
		Dictionary<Type, PluginLibraryBase> Libraries { get; }
	}
}
