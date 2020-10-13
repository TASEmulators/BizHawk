#nullable enable

using System.Collections.Generic;

namespace BizHawk.Emulation.Common
{
	public interface IAxisDict : IReadOnlyDictionary<string, AxisSpec>
	{
		bool HasContraints { get; }

		string this[int index] { get; }

		int IndexOf(string key);
	}
}
