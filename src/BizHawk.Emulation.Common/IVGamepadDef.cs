#nullable enable

using System.Collections.Generic;

namespace BizHawk.Emulation.Common
{
	public interface IVGamepadDef
	{
		/// <value>All axes and other inputs whose state is an integer, possibly signed</value>
		IAxisDict Axes { get; }

		/// <value>All buttons and other inputs whose state is a boolean</value>
		IReadOnlyList<string> BoolButtons { get; }

		/// <summary>Maps individual buttons and axes to categories, allowing various controller display and config screens to use consistent groupings</summary>
		IReadOnlyDictionary<string, string> CategoryLabels { get; }

		/// <value>A list of all buttons and axes put in a logical order, such as by controller number</value>
		IEnumerable<IEnumerable<string>> ControlsOrdered { get; }

		string Name { get; }

		int PlayerCount { get; }

		int PlayerNumber(string buttonName);
	}
}
