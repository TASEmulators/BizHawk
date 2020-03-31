#nullable enable

using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

using BizHawk.Client.EmuHawk.Properties;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public abstract class PadSchemaControl
	{
		protected PadSchemaControl(Point location, string name)
		{
			Location = location;
			Name = name;
		}

		public Point Location { get; }
		public string Name { get; }
	}

	/// <summary>A single on/off button</summary>
	public sealed class ButtonSchema : PadSchemaControl
	{
		public string DisplayName { get; set; }

		public Bitmap? Icon { get; set; }

		public ButtonSchema(int x, int y, string name)
			: base(new Point(x, y), name)
			=> DisplayName = name;

		public ButtonSchema(int x, int y, int controller, string name)
			: base(new Point(x, y), $"P{controller} {name}")
			=> DisplayName = name;

		public static ButtonSchema Up(int x, int y, string? name = null)
			=> new ButtonSchema(x, y, name ?? "Up") { Icon = Resources.BlueUp };

		public static ButtonSchema Up(int x, int y, int controller)
			=> new ButtonSchema(x, y, controller, "Up") { Icon = Resources.BlueUp };

		public static ButtonSchema Down(int x, int y, string? name = null)
			=> new ButtonSchema(x, y, name ?? "Down") { Icon = Resources.BlueDown };

		public static ButtonSchema Down(int x, int y, int controller)
			=> new ButtonSchema(x, y, controller, "Down") { Icon = Resources.BlueDown };

		public static ButtonSchema Left(int x, int y, string? name = null)
			=> new ButtonSchema(x, y, name ?? "Left") { Icon = Resources.Back };

		public static ButtonSchema Left(int x, int y, int controller)
			=> new ButtonSchema(x, y, controller, "Left") { Icon = Resources.Back };

		public static ButtonSchema Right(int x, int y, string? name = null)
			=> new ButtonSchema(x, y, name ?? "Right") { Icon = Resources.Forward };

		public static ButtonSchema Right(int x, int y, int controller)
			=> new ButtonSchema(x, y, controller, "Right") { Icon = Resources.Forward };
	}

	/// <summary>A single analog control (e.g. pressure sensitive button)</summary>
	public sealed class SingleAxisSchema : PadSchemaControl
	{
		public string DisplayName { get; set; }

		public int MaxValue { get; set; }

		public int MinValue { get; set; }

		public readonly Orientation Orientation;

		public Size TargetSize { get; set; }

		public SingleAxisSchema(int x, int y, string name, bool isVertical = false)
			: base(new Point(x, y), name)
		{
			DisplayName = name;
			Orientation = isVertical ? Orientation.Vertical : Orientation.Horizontal;
		}

		public SingleAxisSchema(int x, int y, int controller, string name, bool isVertical = false)
			: this(x, y, $"P{controller} {name}", isVertical) {}
	}

	/// <summary>An analog stick (X, Y) pair</summary>
	public sealed class AnalogSchema : PadSchemaControl
	{
		public ControllerDefinition.AxisRange AxisRange { get; set; }

		public ControllerDefinition.AxisRange SecondaryAxisRange { get; set; }

		public string SecondaryName { get; set; }

		public AnalogSchema(int x, int y, string nameX)
			: base(new Point(x, y), nameX)
			=> SecondaryName = nameX.Replace("X", "Y");
	}

	/// <summary>An (X, Y) pair intended to be a screen coordinate (for zappers, mouse, stylus, etc.)</summary>
	public sealed class TargetedPairSchema : PadSchemaControl
	{
		public readonly int MaxValue;

		public readonly string SecondaryName;

		public Size TargetSize { get; set; }

		public TargetedPairSchema(int x, int y, string nameX, int maxValue = default)
			: base(new Point(x, y), nameX)
		{
			MaxValue = maxValue;
			SecondaryName = nameX.Replace("X", "Y");
		}
	}

	public sealed class DiscManagerSchema : PadSchemaControl
	{
		public readonly IEmulator OwnerEmulator;

		public readonly IReadOnlyList<string> SecondaryNames;

		public readonly Size TargetSize;

		public DiscManagerSchema(int x, int y, Size targetSize, IEmulator owner, IReadOnlyList<string> secondaryNames)
			: base(new Point(x, y), "Disc Select")
		{
			OwnerEmulator = owner;
			SecondaryNames = secondaryNames;
			TargetSize = targetSize;
		}
	}
}
