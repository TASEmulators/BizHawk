using System;
using System.Collections;
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
			foreach (var kvp in source.Axes) Axes.Add(kvp);
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

		public sealed class AxisDict : IReadOnlyDictionary<string, AxisSpec>
		{
			private readonly IList<string> _keys = new List<string>();

			private readonly IDictionary<string, AxisSpec> _specs = new Dictionary<string, AxisSpec>();

			public int Count => _keys.Count;

			public bool HasContraints { get; private set; }

			public IEnumerable<string> Keys => _keys;

			public IEnumerable<AxisSpec> Values => _specs.Values;

			public string this[int index] => _keys[index];

			public AxisSpec this[string index]
			{
				get => _specs[index];
				set => _specs[index] = value;
			}

			public void Add(string key, AxisSpec value)
			{
				_keys.Add(key);
				_specs.Add(key, value);
				if (value.Constraint != null) HasContraints = true;
			}

			public void Add(KeyValuePair<string, AxisSpec> item) => Add(item.Key, item.Value);

			public void Clear()
			{
				_keys.Clear();
				_specs.Clear();
				HasContraints = false;
			}

			public bool ContainsKey(string key) => _keys.Contains(key);

			public IEnumerator<KeyValuePair<string, AxisSpec>> GetEnumerator() => _specs.GetEnumerator();

			IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

			public int IndexOf(string key) => _keys.IndexOf(key);

			public AxisSpec SpecAtIndex(int index) => this[_keys[index]];

			public bool TryGetValue(string key, out AxisSpec value) => _specs.TryGetValue(key, out value);
		}

		public readonly AxisDict Axes = new AxisDict();

		public readonly struct AxisSpec
		{
			/// <summary>
			/// Gets the axis constraints that apply artificial constraints to float values
			/// For instance, a N64 controller's analog range is actually larger than the amount allowed by the plastic that artificially constrains it to lower values
			/// Axis constraints provide a way to technically allow the full range but have a user option to constrain down to typical values that a real control would have
			/// </summary>
			public readonly AxisConstraint Constraint;

			public Range<float> FloatRange => ((float) Min).RangeTo(Max);

			public readonly bool IsReversed;

			public int Max => Range.EndInclusive;

			/// <value>maximum decimal digits analog input can occupy with no-args ToString</value>
			/// <remarks>does not include the extra char needed for a minus sign</remarks>
			public int MaxDigits => Math.Max(Math.Abs(Min).ToString().Length, Math.Abs(Max).ToString().Length);

			public readonly int Mid;

			public int Min => Range.Start;

			public readonly Range<int> Range;

			public AxisSpec(Range<int> range, int mid, bool isReversed = false, AxisConstraint constraint = null)
			{
				Constraint = constraint;
				IsReversed = isReversed;
				Mid = mid;
				Range = range;
			}
		}

		/// <summary>
		/// Gets the category labels. These labels provide a means of categorizing controls in various controller display and config screens
		/// </summary>
		public Dictionary<string, string> CategoryLabels { get; } = new Dictionary<string, string>();

		public void ApplyAxisConstraints(string constraintClass, IDictionary<string, int> axes)
		{
			if (!Axes.HasContraints) return;
			foreach (var kvp in Axes)
			{
				var constraint = kvp.Value.Constraint;
				if (constraint == null || constraint.Class != constraintClass) continue;
				switch (constraint)
				{
					case CircularAxisConstraint circular:
						var xAxis = kvp.Key;
						var yAxis = circular.PairedAxis;
						(axes[xAxis], axes[yAxis]) = circular.ApplyTo(axes[xAxis], axes[yAxis]);
						break;
				}
			}
		}

		/// <summary>represents the direction of <c>(+, +)</c></summary>
		/// <remarks>docs of individual controllers are being collected in comments of https://github.com/TASVideos/BizHawk/issues/1200</remarks>
		public enum AxisPairOrientation : byte
		{
			RightAndUp = 0,
			RightAndDown = 1,
			LeftAndUp = 2,
			LeftAndDown = 3
		}

		public interface AxisConstraint
		{
			public string Class { get; }

			public string PairedAxis { get; }
		}

		public sealed class CircularAxisConstraint : AxisConstraint
		{
			public string Class { get; }

			private readonly float Magnitude;

			public string PairedAxis { get; }

			public CircularAxisConstraint(string @class, string pairedAxis, float magnitude)
			{
				Class = @class;
				Magnitude = magnitude;
				PairedAxis = pairedAxis;
			}

			public (int X, int Y) ApplyTo(int rawX, int rawY)
			{
				var xVal = (double) rawX;
				var yVal = (double) rawY;
				var length = Math.Sqrt(xVal * xVal + yVal * yVal);
				var ratio = Magnitude / length;
				return ratio < 1.0
					? ((int) (xVal * ratio), (int) (yVal * ratio))
					: ((int) xVal, (int) yVal);
			}
		}

		/// <summary>
		/// Gets a list of controls put in a logical order such as by controller number,
		/// This is a default implementation that should work most of the time
		/// </summary>
		public virtual IEnumerable<IEnumerable<string>> ControlsOrdered
		{
			get
			{
				var list = new List<string>(Axes.Keys);
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
				var allNames = Axes.Keys.Concat(BoolButtons).ToList();
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
			return BoolButtons.Any() || Axes.Any();
		}
	}
}
