using System.Drawing;
using System.Windows.Forms;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public enum PadInputType
	{
		Boolean,		// A single on/off button
		AnalogStick,	// An analog stick X,Y Pair
		SingleFloat,	// A single analog button (pressure sensitive button for instance)
		TargetedPair,	// A X,Y pair intended to be a screen coordinate (for zappers, mouse, stylus, etc)
		DiscManager
	}

	public class ButtonSchema
	{
		public ButtonSchema(int x, int y, string name)
		{
			Location = new Point(x, y);
			Name = name;
		}

		public ButtonSchema(int x, int y, int controller, string name)
			: this(x, y, $"P{controller} {name}")
		{
			DisplayName = name;
		}

		public string Name { get; protected set; }
		public string DisplayName { get; set; }
		public PadInputType Type { get; protected set; } = PadInputType.Boolean;
		public Point Location { get; protected set; }
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

		public static ButtonSchema Up(int x, int y, string name = null)
			=> new ButtonSchema(x, y, name ?? "Up")
			{
				Icon = Properties.Resources.BlueUp
			};

		public static ButtonSchema Up(int x, int y, int controller)
			=> new ButtonSchema(x, y, controller, "Up")
			{
				Icon = Properties.Resources.BlueUp
			};


		public static ButtonSchema Down(int x, int y, string name = null)
			=> new ButtonSchema(x, y, name ?? "Down")
			{
				Icon = Properties.Resources.BlueDown
			};

		public static ButtonSchema Down(int x, int y, int controller)
			=> new ButtonSchema(x, y, controller, "Down")
			{
				Icon = Properties.Resources.BlueDown
			};

		public static ButtonSchema Left(int x, int y, string name = null)
			=> new ButtonSchema(x, y, name ?? "Left")
			{
				Icon = Properties.Resources.Back
			};

		public static ButtonSchema Left(int x, int y, int controller)
			=> new ButtonSchema(x, y, controller, "Left")
			{
				Icon = Properties.Resources.Back
			};

		public static ButtonSchema Right(int x, int y, string name = null)
			=> new ButtonSchema(x, y, name ?? "Right")
			{
				Icon = Properties.Resources.Forward
			};

		public static ButtonSchema Right(int x, int y, int controller)
			=> new ButtonSchema(x, y, controller, "Right")
			{
				Icon = Properties.Resources.Forward
			};
	}

	public class SingleFloatSchema : ButtonSchema
	{
		public SingleFloatSchema(int x, int y, string name)
			: base(x, y, name)
		{
			Type = PadInputType.SingleFloat;
		}

		public SingleFloatSchema(int x, int y, int controller, string name)
			: base(x, y, controller, name)
		{
			Type = PadInputType.SingleFloat;
		}
	}

	public class TargetedPairSchema : ButtonSchema
	{
		public TargetedPairSchema(int x, int y, string nameX)
			: base(x, y, nameX)
		{
			Type = PadInputType.TargetedPair;
			SecondaryNames = new[]
			{
				nameX.Replace("X", "Y")
			};
		}
	}

	public class AnalogSchema : ButtonSchema
	{
		public AnalogSchema(int x, int y, string nameX)
			: base(x, y, nameX)
		{
			Type = PadInputType.AnalogStick;
			SecondaryNames = new[]
			{
				nameX.Replace("X", "Y")
			};
		}
	}

	public class DiscManagerSchema : ButtonSchema
	{
		public DiscManagerSchema(int x, int y)
			: base(x, y, "Disc Select")
		{
			Type = PadInputType.DiscManager;
		}
	}
}
