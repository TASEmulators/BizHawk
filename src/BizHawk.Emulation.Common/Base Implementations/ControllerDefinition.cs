using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// Defines the schema for all the currently available controls for an IEmulator instance
	/// </summary>
	/// <seealso cref="IEmulator" /> 
	public class ControllerDefinition
	{
		public ControllerDefinition() {}

		public ControllerDefinition(ControllerDefinition source)
			: this()
		{
			Name = source.Name;
			BoolButtons.AddRange(source.BoolButtons);
			foreach (var kvp in source.Axes) Axes.Add(kvp);
			CategoryLabels = source.CategoryLabels;
			HapticsChannels = source.HapticsChannels;
		}

		/// <summary>
		/// Gets or sets the name of the controller definition
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// Gets or sets a list of all button types that have a boolean (on/off) value
		/// </summary>
		public List<string> BoolButtons { get; set; } = new List<string>();

		public readonly AxisDict Axes = new AxisDict();

		/// <summary>Contains names of virtual haptic feedback channels, e.g. <c>{ "P1 Mono" }</c>, <c>{ "P2 Left", "P2 Right" }</c>.</summary>
		public List<string> HapticsChannels { get; } = new();

		/// <summary>
		/// Contains channel names for haptic feedback.
		/// </summary>
		/// <remarks>
		/// For example, a gamepad with a single haptics motor is represented as 
		/// <c>{ "X0 Mono" }</c>, and a gamepad with one left and one right 
		/// motor would be <c>{ "X0 Left", "X0 Right" }</c>.
		/// </remarks>
		public List<string> HapticsChannels { get; set; } = new List<string>();

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
