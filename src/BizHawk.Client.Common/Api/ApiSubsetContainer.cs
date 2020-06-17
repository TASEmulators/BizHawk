using System;
using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	public class ApiSubsetContainer : IApiContainer
	{
		public Dictionary<Type, IExternalApi> Libraries { get; set; }

		public IEmulationApi Emulation => (IEmulationApi) Libraries[typeof(IEmulationApi)];
		public IGameInfoApi GameInfo => (IGameInfoApi) Libraries[typeof(IGameInfoApi)];
		public IJoypadApi Joypad => (IJoypadApi) Libraries[typeof(IJoypadApi)];
		public IMemoryApi Memory => (IMemoryApi) Libraries[typeof(IMemoryApi)];
		public IMemoryEventsApi MemoryEvents => (IMemoryEventsApi) Libraries[typeof(IMemoryEventsApi)];
		public IMemorySaveStateApi MemorySaveState => (IMemorySaveStateApi) Libraries[typeof(IMemorySaveStateApi)];
		public IMovieApi Movie => (IMovieApi) Libraries[typeof(IMovieApi)];
		public ISQLiteApi SQLite => (ISQLiteApi) Libraries[typeof(ISQLiteApi)];
		public IUserDataApi UserData => (IUserDataApi) Libraries[typeof(IUserDataApi)];

		public ApiSubsetContainer(Dictionary<Type, IExternalApi> libs)
		{
			Libraries = libs;
		}
	}
}
