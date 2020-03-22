using System.Drawing;
using System.Windows.Forms;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public enum PadInputType
	{
		Boolean,		// A single on/off button
		AnalogStick,	// An analog stick X,Y Pair
		FloatSingle,	// A single analog button (pressure sensitive button for instance)
		TargetedPair,	// A X,Y pair intended to be a screen coordinate (for zappers, mouse, stylus, etc)
		DiscManager
	}

	public class ButtonSchema
	{
		public ButtonSchema()
		{
		}

		public ButtonSchema(int x, int y) => Location = new Point(x, y);

		public string Name { get; set; }
		public string DisplayName { get; set; }
		public PadInputType Type { get; set; } = PadInputType.Boolean;
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

		public Orientation Orientation { get; set; } // For Single Float controls

		// for Analog Stick controls
		public ControllerDefinition.AxisRange? AxisRange { get; set; }
		public ControllerDefinition.AxisRange? SecondaryAxisRange { get; set; }

		public static ButtonSchema Up(string name, int x, int y) => new ButtonSchema
		{
			Name = name,
			Icon = Properties.Resources.BlueUp,
			Location = new Point(x, y)
		};

		public static ButtonSchema Down(string name, int x, int y) => new ButtonSchema
		{
			Name = name,
			Icon = Properties.Resources.BlueDown,
			Location = new Point(x, y)
		};

		public static ButtonSchema Left(string name, int x, int y) => new ButtonSchema
		{
			Name = name,
			Icon = Properties.Resources.Back,
			Location = new Point(x, y)
		};

		public static ButtonSchema Right(string name, int x, int y) => new ButtonSchema
		{
			Name = name,
			Icon = Properties.Resources.Forward,
			Location = new Point(x, y)
		};
	}
}
