#nullable enable

using System;
using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	public sealed class ApiContainer
	{
		public readonly IReadOnlyDictionary<Type, IExternalApi> Libraries;

		public ICommApi Comm => (ICommApi) Libraries[typeof(ICommApi)];
		public IEmuClientApi EmuClient => (IEmuClientApi) Libraries[typeof(IEmuClientApi)];
		public IEmulationApi Emulation => (IEmulationApi) Libraries[typeof(IEmulationApi)];
		public IGameInfoApi GameInfo => (IGameInfoApi) Libraries[typeof(IGameInfoApi)];
		public IGuiApi Gui => (IGuiApi) Libraries[typeof(IGuiApi)];
		public IInputApi Input => (IInputApi) Libraries[typeof(IInputApi)];
		public IJoypadApi Joypad => (IJoypadApi) Libraries[typeof(IJoypadApi)];
		public IMemoryApi Memory => (IMemoryApi) Libraries[typeof(IMemoryApi)];
		public IMemoryEventsApi MemoryEvents => (IMemoryEventsApi) Libraries[typeof(IMemoryEventsApi)];
		public IMemorySaveStateApi MemorySaveState => (IMemorySaveStateApi) Libraries[typeof(IMemorySaveStateApi)];
		public IMovieApi Movie => (IMovieApi) Libraries[typeof(IMovieApi)];
		public ISaveStateApi SaveState => (ISaveStateApi) Libraries[typeof(ISaveStateApi)];
		public ISQLiteApi SQLite => (ISQLiteApi) Libraries[typeof(ISQLiteApi)];
		public IUserDataApi UserData => (IUserDataApi) Libraries[typeof(IUserDataApi)];
		public IToolApi Tool => (IToolApi) Libraries[typeof(IToolApi)];

		public ApiContainer(IReadOnlyDictionary<Type, IExternalApi> libs) => Libraries = libs;
	}
}
