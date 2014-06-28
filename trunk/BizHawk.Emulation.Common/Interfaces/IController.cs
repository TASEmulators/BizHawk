using System;
using System.Collections.Generic;
using System.Linq;

namespace BizHawk.Emulation.Common
{
	// doesn't do what is desired
	// http://connect.microsoft.com/VisualStudio/feedback/details/459307/extension-add-methods-are-not-considered-in-c-collection-initializers
	/*
	public static class UltimateMagic
	{
		public static void Add(this List<ControllerDefinition.FloatRange l, float Min, float Mid, float Max)
		{
			l.Add(new ControllerDefinition.FloatRange(Min, Mid, Max);
		}
	}
	*/

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

		public List<string> BoolButtons { get; set; }
		public List<string> FloatControls { get; private set; }
		public List<FloatRange> FloatRanges { get; private set; }
		public List<AxisConstraint> AxisConstraints { get; private set; }
		
		public ControllerDefinition(ControllerDefinition source)
			: this()
		{
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
				var list = FloatControls.Union(BoolButtons);

				yield return list
					.Where(x => !x.StartsWith("P1 ") && !x.StartsWith("P2 ") && !x.StartsWith("P3 ") && !x.StartsWith("P4 "));

				yield return list
					.Where(x => x.StartsWith("P1 "));

				yield return list
					.Where(x => x.StartsWith("P2 "));

				yield return list
					.Where(x => x.StartsWith("P3 "));

				yield return list
					.Where(x => x.StartsWith("P4 "));
			}
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
	}

	public interface IController
	{
		ControllerDefinition Type { get; }

		// TODO - it is obnoxious for this to be here. must be removed.
		bool this[string button] { get; }
		
		// TODO - this can stay but it needs to be changed to go through the float
		bool IsPressed(string button);

		float GetFloat(string name);
	}
}
