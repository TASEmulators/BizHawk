using System.Collections.Generic;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	internal class BkmControllerDefinition(string name) : ControllerDefinition(name)
	{
		// same as ControllerDefinition.GenOrderedControls, just with Axes after BoolButtons
		protected override IReadOnlyList<IReadOnlyList<(string Name, AxisSpec? AxisSpec)>> GenOrderedControls()
		{
			var ret = new List<(string, AxisSpec?)>[PlayerCount + 1];
			for (var i = 0; i < ret.Length; i++) ret[i] = new();
			foreach (var btn in BoolButtons) ret[PlayerNumber(btn)].Add((btn, null));
			foreach ((string buttonName, var axisSpec) in Axes) ret[PlayerNumber(buttonName)].Add((buttonName, axisSpec));
			return ret;
		}
	}
}
