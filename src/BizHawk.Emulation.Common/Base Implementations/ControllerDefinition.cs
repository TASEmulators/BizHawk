using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace BizHawk.Emulation.Common
{
	/// <summary>
	/// Defines the schema for all the currently available controls for an IEmulator instance
	/// </summary>
	/// <seealso cref="IEmulator" /> 
	public class ControllerDefinition : IVGamepadDef
	{
		public ControllerDefinition() {}

		public ControllerDefinition(string name) : this() => Name = name;

		public ControllerDefinition(IVGamepadDef source, string name = null)
			: this(name ?? source.Name)
		{
			BoolButtons.AddRange(source.BoolButtons);
			foreach (var kvp in source.Axes) Axes.Add(kvp);
			CategoryLabels = source.CategoryLabels as Dictionary<string, string>
				?? source.CategoryLabels.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
		}

		public string Name { get; }

		public List<string> BoolButtons { get; set; } = new List<string>();

		IReadOnlyList<string> IVGamepadDef.BoolButtons => BoolButtons;

		public AxisDict Axes { get; } = new AxisDict();

		IAxisDict IVGamepadDef.Axes => Axes;

		public Dictionary<string, string> CategoryLabels { get; } = new Dictionary<string, string>();

		IReadOnlyDictionary<string, string> IVGamepadDef.CategoryLabels => CategoryLabels;

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
