using System.Collections.Generic;
using System.Drawing;

namespace BizHawk.Emulation.Common
{
	public class PadSchema
	{
		public Size Size { get; set; }
		public bool IsConsole { get; protected set; }
		public IEnumerable<PadSchemaControl> Buttons { get; set; } = new List<PadSchemaControl>();
		public string DisplayName { get; set; } // The name of the pad itself, presumably will be displayed by the given pad time if supplied
	}

	public class ConsoleSchema : PadSchema
	{
		public ConsoleSchema()
		{
			DisplayName = "Console";
			IsConsole = true;
		}
	}
}
