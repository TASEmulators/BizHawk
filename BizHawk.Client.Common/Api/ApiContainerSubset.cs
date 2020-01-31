using System;
using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	public interface ApiContainerSubset
	{
		IEmulation Emulation { get; }

		IGameInfo GameInfo { get; }

		IInputMovie InputMovie { get; }

		IJoypad Joypad { get; }

		IReadOnlyDictionary<Type, IExternalApi> LibraryDict { get; }

		IMemoryAccess MemoryAccess { get; }

		IMemoryEvents MemoryEvents { get; }

		IMemorySaveState MemorySaveState { get; }

		ISQLite SQLite { get; }

		IUserData UserData { get; }
	}
}
