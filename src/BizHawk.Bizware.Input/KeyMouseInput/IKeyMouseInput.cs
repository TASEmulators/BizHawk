#nullable enable

using System.Collections.Generic;

namespace BizHawk.Bizware.Input
{
	internal interface IKeyMouseInput : IDisposable
	{
		IEnumerable<KeyEvent> UpdateKeyInputs(bool handleAltKbLayouts);

		(int DeltaX, int DeltaY) UpdateMouseInput();
	}
}
