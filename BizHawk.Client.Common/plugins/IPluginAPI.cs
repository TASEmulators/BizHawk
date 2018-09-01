using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BizHawk.Client.Common
{
	interface IPluginAPI
	{
		EmulatorPluginLibrary EmuLib { get; set; }
		GameInfoPluginLibrary GameInfoLib { get; set; }
		GUIDrawPluginLibrary GuiLib { get; set; }
		JoypadPluginLibrary JoypadLib { get; set; }
		MemoryPluginLibrary MemLib { get; set; }
		MemoryEventsPluginLibrary MemEventsLib { get; set; }
		MemorySavestatePluginLibrary MemStateLib { get; set; }
		MoviePluginLibrary MovieLib { get; set; }
		SQLPluginLibrary SQLLib { get; set; }
		UserDataPluginLibrary UserDataLib { get; set; }
		List<PluginLibraryBase> ClientLibs { get; set; }
	}
}
