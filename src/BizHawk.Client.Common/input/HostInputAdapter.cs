#nullable enable

using System;
using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	/// <remarks>this was easier than trying to make static classes instantiable...</remarks>
	public interface HostInputAdapter
	{
		void DeInitAll();

		void FirstInitAll(IntPtr mainFormHandle);

		void ReInitGamepads(IntPtr mainFormHandle);

		void PreprocessHostGamepads();

		void ProcessHostGamepads(Action<string?, bool, ClientInputFocus> handleButton, Action<string?, int> handleAxis);

		IEnumerable<KeyEvent> ProcessHostKeyboards();
	}
}
