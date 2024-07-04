#nullable enable

using System.Collections.Generic;

using BizHawk.Client.Common;

namespace BizHawk.Bizware.Input
{
	internal interface IKeyInput : IDisposable
	{
		IEnumerable<KeyEvent> Update(bool handleAltKbLayouts);
	}
}
