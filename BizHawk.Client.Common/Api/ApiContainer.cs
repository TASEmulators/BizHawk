using System;
using System.Collections.Generic;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public abstract class ApiContainer : ApiContainerSubset
	{
		private IEmulation _emulation;

		private IMemoryAccess _memoryAccess;

		private IMemoryEvents _memoryEvents;

		private IMemorySaveState _memorySaveState;

		/// <remarks>API implementations without any <see cref="RequiredServiceAttribute">RequiredServices</see> don't need to be retrieved lazily.</remarks>
		public ApiContainer(IReadOnlyDictionary<Type, IExternalApi> libs)
		{
			GameInfo = (IGameInfo) libs[typeof(GameInfoApi)];
			InputMovie = (IInputMovie) libs[typeof(InputMovieApi)];
			Joypad = (IJoypad) libs[typeof(JoypadApi)];
			SQLite = (ISQLite) libs[typeof(SQLiteApi)];
			UserData = (IUserData) libs[typeof(UserDataApi)];
			LibraryDict = libs;
		}

		public abstract IComm Comm { get; }

		public IEmulation Emulation => _emulation ??= (IEmulation) LibraryDict[typeof(EmulationApi)];

		public IGameInfo GameInfo { get; }

		public abstract IGui Gui { get; }

		public abstract IInput Input { get; }

		public IInputMovie InputMovie { get; }

		public IJoypad Joypad { get; }

		public IReadOnlyDictionary<Type, IExternalApi> LibraryDict { get; }

		public IMemoryAccess MemoryAccess => _memoryAccess ??= (IMemoryAccess) LibraryDict[typeof(MemoryAccessApi)];

		public IMemoryEvents MemoryEvents => _memoryEvents ??= (IMemoryEvents) LibraryDict[typeof(MemoryEventsApi)];

		public IMemorySaveState MemorySaveState => _memorySaveState ??= (IMemorySaveState) LibraryDict[typeof(MemorySaveStateApi)];

		public abstract ISaveState SaveState { get; }

		public ISQLite SQLite { get; }

		public abstract ITool Tool { get; }

		public IUserData UserData { get; }
	}
}
