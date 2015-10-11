using System.Collections.Generic;
using System.Drawing;

namespace BizHawk.Client.EmuHawk
{
	public class PadSchema
	{
		public enum PadInputType
		{
			Boolean,		// A single on/off button
			AnalogStick,	// An analog stick X,Y Pair
			FloatSingle,	// A single analog button (pressure sensitive button for instance)
			TargetedPair,	// A X,Y pair intended to be a screen cooridnate (for zappers, mouse, stylus, etc)
			DiscManager
		}

		// Default size of the pad
		public Size DefaultSize { get; set; }
		public Size? MaxSize { get; set; }
		public bool IsConsole { get; set; }
		public IEnumerable<ButtonScema> Buttons { get; set; }
		public string DisplayName { get; set; } // The name of the pad itself, presumably will be displayed by the given pad time if supplied

		public class ButtonScema
		{
			public string Name { get; set; }
			public string DisplayName { get; set; }
			public PadInputType Type { get; set; }
			public Point Location { get; set; }
			public Bitmap Icon { get; set; }
			public Size TargetSize { get; set; } // Specifically for TargetedPair, specifies the screen size
			public string[] SecondaryNames { get; set; } // Any other buttons necessary to operate (such as the Y axis)
			public int MaxValue { get; set; } // For non-boolean values, specifies the maximum value the button allows
			public int MidValue { get; set; } // For non-boolean values, specifies the mid (zero) value for the button
			public int MinValue { get; set; } // For non-boolean values, specifies the minimum value the button allows
			public int MaxValueSec { get; set; }
			public int MidValueSec { get; set; }
			public int MinValueSec { get; set; }
			public object OwnerEmulator { get; set; }
		}
	}


}
