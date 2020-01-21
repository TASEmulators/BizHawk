using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	// Represents the defaults used in defctrl.json
	public class DefaultControls
	{
		public Dictionary<string, Dictionary<string, string>> AllTrollers = new Dictionary<string, Dictionary<string, string>>();
		public Dictionary<string, Dictionary<string, string>> AllTrollersAutoFire = new Dictionary<string, Dictionary<string, string>>();
		public Dictionary<string, Dictionary<string, AnalogBind>> AllTrollersAnalog = new Dictionary<string, Dictionary<string, AnalogBind>>();
	}
}
