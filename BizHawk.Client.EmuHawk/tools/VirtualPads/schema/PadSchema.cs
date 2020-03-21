using System.Collections.Generic;
using System.Drawing;

namespace BizHawk.Client.EmuHawk
{
	public class PadSchema
	{
		// Default size of the pad
		public Size DefaultSize { get; set; }
		public Size? MaxSize { get; set; }
		public bool IsConsole { get; set; }
		public IEnumerable<ButtonSchema> Buttons { get; set; }
		public string DisplayName { get; set; } // The name of the pad itself, presumably will be displayed by the given pad time if supplied
	}
}
