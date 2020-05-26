using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

using BizHawk.Common;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// Defines the schema for all the currently available controls for an IEmulator instance
	/// </summary>
	/// <seealso cref="IEmulator" /> 
	public class ControllerDefinition
	{
		public ControllerDefinition()
		{
		}

		public ControllerDefinition(ControllerDefinition source)
			: this()
		{
			Name = source.Name;
			BoolButtons.AddRange(source.BoolButtons);
			AxisControls.AddRange(source.AxisControls);
			AxisRanges.AddRange(source.AxisRanges);
			AxisConstraints.AddRange(source.AxisConstraints);
			CategoryLabels = source.CategoryLabels;
		}

		/// <summary>
		/// Gets or sets the name of the controller definition
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets a list of all button types that have a boolean (on/off) value
		/// </summary>
		public List<string> BoolButtons { get; set; } = new List<string>();

		/// <summary>
		/// Gets a list of all non-boolean types, that can be represented by a numerical value (such as analog controls, stylus coordinates, etc
		/// </summary>
		public List<string> AxisControls { get; } = new List<string>();

		/// <summary>
		/// Gets a list of all axis ranges for each axis control (must be one to one with AxisControls)
		/// AxisRanges include the min/max/default values
		/// </summary>
		public List<AxisRange> AxisRanges { get; set; } = new List<AxisRange>();

		/// <summary>
		/// Gets the axis constraints that apply artificial constraints to float values
		/// For instance, a N64 controller's analog range is actually larger than the amount allowed by the plastic that artificially constrains it to lower values
		/// Axis constraints provide a way to technically allow the full range but have a user option to constrain down to typical values that a real control would have
		/// </summary>
		public List<AxisConstraint> AxisConstraints { get; } = new List<AxisConstraint>();

		/// <summary>
		/// Gets the category labels. These labels provide a means of categorizing controls in various controller display and config screens
		/// </summary>
		public Dictionary<string, string> CategoryLabels { get; } = new Dictionary<string, string>();

		public void ApplyAxisConstraints(string constraintClass, IDictionary<string, int> axes)
		{
			if (AxisConstraints == null)
			{
				return;
			}

			foreach (var constraint in AxisConstraints)
			{
				if (constraint.Class != constraintClass)
				{
					continue;
				}

				switch (constraint.Type)
				{
					case AxisConstraintType.Circular:
						{
							string xAxis = constraint.Params[0] as string ?? "";
							string yAxis = constraint.Params[1] as string ?? "";
							float range = (float)constraint.Params[2];
							if (!axes.ContainsKey(xAxis)) break;
							if (!axes.ContainsKey(yAxis)) break;
							double xVal = axes[xAxis];
							double yVal = axes[yAxis];
							double length = Math.Sqrt((xVal * xVal) + (yVal * yVal));
							if (length > range)
							{
								double ratio = range / length;
								xVal *= ratio;
								yVal *= ratio;
							}

							axes[xAxis] = (int) xVal;
							axes[yAxis] = (int) yVal;
							break;
						}
				}
			}
		}

		public readonly struct AxisRange
		{
			public readonly bool IsReversed;

			public readonly int Max;

			/// <remarks>used as default/neutral/unset</remarks>
			public readonly int Mid;

			public readonly int Min;

			public Range<float> FloatRange => ((float) Min).RangeTo(Max);

			/// <value>maximum decimal digits analog input can occupy with no-args ToString</value>
			/// <remarks>does not include the extra char needed for a minus sign</remarks>
			public int MaxDigits => Math.Max(Math.Abs(Min).ToString().Length, Math.Abs(Max).ToString().Length);

			public Range<int> Range => Min.RangeTo(Max);

			public AxisRange(int min, int mid, int max, bool isReversed = false)
			{
				const string ReversedBoundsExceptionMessage = nameof(AxisRange) + " must not have " + nameof(max) + " < " + nameof(min) + ". pass " + nameof(isReversed) + ": true to ctor instead, or use " + nameof(CreateAxisRangePair);
				if (max < min) throw new ArgumentOutOfRangeException(nameof(max), max, ReversedBoundsExceptionMessage);
				IsReversed = isReversed;
				Max = max;
				Mid = mid;
				Min = min;
			}
		}

		public static List<AxisRange> CreateAxisRangePair(int min, int mid, int max, AxisPairOrientation pDir) => new List<AxisRange>
		{
			new AxisRange(min, mid, max, ((byte) pDir & 2) != 0),
			new AxisRange(min, mid, max, ((byte) pDir & 1) != 0)
		};

		/// <summary>represents the direction of <c>(+, +)</c></summary>
		/// <remarks>docs of individual controllers are being collected in comments of https://github.com/TASVideos/BizHawk/issues/1200</remarks>
		public enum AxisPairOrientation : byte
		{
			RightAndUp = 0,
			RightAndDown = 1,
			LeftAndUp = 2,
			LeftAndDown = 3
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

		/// <summary>
		/// Gets a list of controls put in a logical order such as by controller number,
		/// This is a default implementation that should work most of the time
		/// </summary>
		public virtual IEnumerable<IEnumerable<string>> ControlsOrdered
		{
			get
			{
				var list = new List<string>(AxisControls);
				list.AddRange(BoolButtons);

				// starts with console buttons, then each player's buttons individually
				var ret = new List<string>[PlayerCount + 1];
				for (int i = 0; i < ret.Length; i++)
				{
					ret[i] = new List<string>();
				}

				foreach (string btn in list)
				{
					ret[PlayerNumber(btn)].Add(btn);
				}

				return ret;
			}
		}

		public int PlayerNumber(string buttonName)
		{
			var match = PlayerRegex.Match(buttonName);
			return match.Success
				? int.Parse(match.Groups[1].Value)
				: 0;
		}

		private static readonly Regex PlayerRegex = new Regex("^P(\\d+) ");

		public int PlayerCount
		{
			get
			{
				var allNames = AxisControls.Concat(BoolButtons).ToList();
				var player = allNames
					.Select(PlayerNumber)
					.DefaultIfEmpty(0)
					.Max();

				if (player > 0)
				{
					return player;
				}

				// Hack for things like gameboy/ti-83 as opposed to genesis with no controllers plugged in
				return allNames.Any(b => b.StartsWith("Up")) ? 1 : 0;
			}
		}

		public bool Any()
		{
			return BoolButtons.Any() || AxisControls.Any();
		}
	}
}
