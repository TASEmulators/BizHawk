using System;
using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Emulation.Common
{
	public class ControllerDefinition
	{
		public void ApplyAxisConstraints(string constraintClass, IDictionary<string, float> floatButtons)
		{
			if (AxisConstraints == null) return;

			foreach (var constraint in AxisConstraints)
			{
				if (constraint.Class != constraintClass)
					continue;
				switch (constraint.Type)
				{
					case AxisConstraintType.Circular:
						{
							string xaxis = constraint.Params[0] as string;
							string yaxis = constraint.Params[1] as string;
							float range = (float)constraint.Params[2];
							double xval = floatButtons[xaxis];
							double yval = floatButtons[yaxis];
							double length = Math.Sqrt(xval * xval + yval * yval);
							if (length > range)
							{
								double ratio = range / length;
								xval *= ratio;
								yval *= ratio;
							}
							floatButtons[xaxis] = (float)xval;
							floatButtons[yaxis] = (float)yval;
							break;
						}
				}
			}
		}

		public struct FloatRange
		{
			public readonly float Min;
			public readonly float Max;

			/// <summary>
			/// default position
			/// </summary>
			public readonly float Mid;

			public FloatRange(float min, float mid, float max)
			{
				Min = min;
				Mid = mid;
				Max = max;
			}

			// for terse construction
			public static implicit operator FloatRange(float[] f)
			{
				if (f.Length != 3)
				{
					throw new ArgumentException();
				}

				return new FloatRange(f[0], f[1], f[2]);
			}

			/// <summary>
			/// Gets maximum decimal digits analog input can occupy. Discards negative sign and possible fractional part (analog devices don't use floats anyway).
			/// </summary>
			public int MaxDigits()
			{
				return Math.Max(
					Math.Abs((int)Min).ToString().Length,
					Math.Abs((int)Max).ToString().Length);
			}
		}

		public enum AxisConstraintType
		{
			Circular
		}

		public struct AxisConstraint
		{
			public string Class;
			public AxisConstraintType Type;
			public object[] Params;
		}

		public string Name { get; set; }

		public Dictionary<string, string> CategoryLabels = new Dictionary<string, string>();
		public List<string> BoolButtons { get; set; }
		public List<string> FloatControls { get; private set; }
		public List<FloatRange> FloatRanges { get; private set; }
		public List<AxisConstraint> AxisConstraints { get; private set; }

		public ControllerDefinition(ControllerDefinition source)
			: this()
		{
			CategoryLabels = source.CategoryLabels;
			Name = source.Name;
			BoolButtons.AddRange(source.BoolButtons);
			FloatControls.AddRange(source.FloatControls);
			FloatRanges.AddRange(source.FloatRanges);
			AxisConstraints.AddRange(source.AxisConstraints);
		}

		public ControllerDefinition()
		{
			BoolButtons = new List<string>();
			FloatControls = new List<string>();
			FloatRanges = new List<FloatRange>();
			AxisConstraints = new List<AxisConstraint>();
		}

		/// <summary>
		/// Puts the controls in a logical order such as by controller number,
		/// This is a default implementation that should work most of the time
		/// </summary>
		public virtual IEnumerable<IEnumerable<string>> ControlsOrdered
		{
			get
			{
				List<string> list = new List<string>(FloatControls);
				list.AddRange(BoolButtons);

				List<string>[] ret = new List<string>[9];
				for (int i = 0; i < ret.Length; i++)
				{
					ret[i] = new List<string>();
				}

				for (int i = 0; i < list.Count; i++)
				{
					ret[PlayerNumber(list[i])].Add(list[i]);
				}

				return ret;
			}
		}

		public int PlayerNumber(string buttonName)
		{
			int player = 0;
			if (buttonName.Length > 3 && buttonName.StartsWith("P") && char.IsNumber(buttonName[1]))
			{
				player = buttonName[1] - '0';
			}

			return player;
		}

		// TODO: a more respectable logic here, and possibly per core implementation
		public virtual int PlayerCount
		{
			get
			{
				var list = FloatControls.Union(BoolButtons);
				if (list.Any(b => b.StartsWith("P8"))) { return 8; }
				if (list.Any(b => b.StartsWith("P7"))) { return 7; }
				if (list.Any(b => b.StartsWith("P6"))) { return 6; }
				if (list.Any(b => b.StartsWith("P5"))) { return 5; }
				if (list.Any(b => b.StartsWith("P4"))) { return 4; }
				if (list.Any(b => b.StartsWith("P3"))) { return 3; }
				if (list.Any(b => b.StartsWith("P2"))) { return 2; }
				if (list.Any(b => b.StartsWith("P1"))) { return 1; }
				if (list.Any(b => b.StartsWith("Up"))) { return 1; } // Hack for things like gameboy/ti-83 as opposed to genesis with no controllers plugged in

				return 0;
			}
		}

		public virtual bool Any()
		{
			return BoolButtons.Any() || FloatControls.Any();
		}
	}

}
