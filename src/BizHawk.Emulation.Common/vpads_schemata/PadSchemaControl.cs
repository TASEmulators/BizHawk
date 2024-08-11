using System.Collections.Generic;
using System.Drawing;

namespace BizHawk.Emulation.Common
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

		public VGamepadButtonImage? Icon { get; set; }

		public ButtonSchema(int x, int y, string name)
			: base(new Point(x, y), name)
			=> DisplayName = name;

		public ButtonSchema(int x, int y, int controller, string name)
			: base(new Point(x, y), $"P{controller} {name}")
			=> DisplayName = name;

		public ButtonSchema(int x, int y, int controller, string name, string displayName)
			: this(x, y, controller, name)
			=> DisplayName = displayName;

		public static ButtonSchema Up(int x, int y, string? name = null)
			=> new ButtonSchema(x, y, name ?? "Up") { Icon = VGamepadButtonImage.BlueArrN };

		public static ButtonSchema Up(int x, int y, int controller)
			=> new ButtonSchema(x, y, controller, "Up") { Icon = VGamepadButtonImage.BlueArrN };

		public static ButtonSchema Down(int x, int y, string? name = null)
			=> new ButtonSchema(x, y, name ?? "Down") { Icon = VGamepadButtonImage.BlueArrS };

		public static ButtonSchema Down(int x, int y, int controller)
			=> new ButtonSchema(x, y, controller, "Down") { Icon = VGamepadButtonImage.BlueArrS };

		public static ButtonSchema Left(int x, int y, string? name = null)
			=> new ButtonSchema(x, y, name ?? "Left") { Icon = VGamepadButtonImage.BlueArrW };

		public static ButtonSchema Left(int x, int y, int controller)
			=> new ButtonSchema(x, y, controller, "Left") { Icon = VGamepadButtonImage.BlueArrW };

		public static ButtonSchema Right(int x, int y, string? name = null)
			=> new ButtonSchema(x, y, name ?? "Right") { Icon = VGamepadButtonImage.BlueArrE };

		public static ButtonSchema Right(int x, int y, int controller)
			=> new ButtonSchema(x, y, controller, "Right") { Icon = VGamepadButtonImage.BlueArrE };
	}

	/// <summary>A single analog control (e.g. pressure sensitive button)</summary>
	public sealed class SingleAxisSchema : PadSchemaControl
	{
		public string DisplayName { get; set; }

		public bool IsVertical { get; }

		public int MaxValue { get; set; }

		public int MinValue { get; set; }

		public Size TargetSize { get; set; }

		public SingleAxisSchema(int x, int y, string name, bool isVertical = false)
			: base(new Point(x, y), name)
		{
			DisplayName = name;
			IsVertical = isVertical;
		}

		public SingleAxisSchema(int x, int y, int controller, string name, bool isVertical = false)
			: this(x, y, $"P{controller} {name}", isVertical) {}
	}

	/// <summary>An analog stick (X, Y) pair</summary>
	public sealed class AnalogSchema : PadSchemaControl
	{
		public AxisSpec Spec { get; set; }

		public AxisSpec SecondarySpec { get; set; }

		public string SecondaryName { get; set; }

		public AnalogSchema(int x, int y, string nameX)
			: base(new Point(x, y), nameX)
			=> SecondaryName = nameX.Replace('X', 'Y');
	}

	/// <summary>An (X, Y) pair intended to be a screen coordinate (for zappers, mouse, stylus, etc.)</summary>
	public sealed class TargetedPairSchema : PadSchemaControl
	{
		public int MaxX { get; }

		public int MaxY { get; }

		public string SecondaryName { get; set; }

		public Size TargetSize { get; set; }

		/// <remarks>Using this ctor, the valid ranges for the X and Y axes are taken to be <c>(0..TargetSize.Width)</c> and <c>(0..TargetSize.Height)</c>.</remarks>
		public TargetedPairSchema(int x, int y, string nameX)
			: base(new Point(x, y), nameX)
			=> SecondaryName = nameX.Replace('X', 'Y');

		/// <remarks>Using this ctor, the valid ranges for the X and Y axes are taken to be <c>(0..maxX)</c> and <c>(0..maxY)</c>.</remarks>
		public TargetedPairSchema(int x, int y, string nameX, int maxX, int maxY)
			: this(x, y, nameX)
		{
			MaxX = maxX;
			MaxY = maxY;
		}
	}

	public sealed class DiscManagerSchema : PadSchemaControl
	{
		public IEmulator OwnerEmulator { get; }

		public IReadOnlyList<string> SecondaryNames { get; }

		public Size TargetSize { get; }

		public DiscManagerSchema(int x, int y, Size targetSize, IEmulator owner, IReadOnlyList<string> secondaryNames)
			: base(new Point(x, y), "Disc Select")
		{
			OwnerEmulator = owner;
			SecondaryNames = secondaryNames;
			TargetSize = targetSize;
		}
	}
}
