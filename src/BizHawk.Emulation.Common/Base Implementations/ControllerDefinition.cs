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

		public ControllerDefinition(string name) : this() => Name = name;

		public ControllerDefinition(ControllerDefinition source, string name = null)
			: this(name ?? source.Name)
		{
			BoolButtons.AddRange(source.BoolButtons);
			foreach (var kvp in source.Axes) Axes.Add(kvp);
			CategoryLabels = source.CategoryLabels;
		}

		public string Name { get; }

		/// <summary>
		/// Gets or sets a list of all button types that have a boolean (on/off) value
		/// </summary>
		public List<string> BoolButtons { get; set; } = new List<string>();

		public readonly AxisDict Axes = new AxisDict();

		/// <summary>
		/// Gets the category labels. These labels provide a means of categorizing controls in various controller display and config screens
		/// </summary>
		public Dictionary<string, string> CategoryLabels { get; } = new Dictionary<string, string>();

		/// <summary>
		/// Gets a list of controls put in a logical order such as by controller number,
		/// This is a default implementation that should work most of the time
		/// </summary>
		public virtual IEnumerable<IEnumerable<string>> ControlsOrdered
			=> Axes.Keys.Concat(BoolButtons).GroupBy(PlayerNumber).OrderBy(grouping => grouping.Key);

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
	}
}
