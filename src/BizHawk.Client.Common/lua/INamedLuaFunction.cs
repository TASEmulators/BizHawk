using System;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public interface INamedLuaFunction
	{
		Action Callback { get; }

		Guid Guid { get; }

		MemoryCallbackDelegate MemCallback { get; }

		string Name { get; }
	}
}
