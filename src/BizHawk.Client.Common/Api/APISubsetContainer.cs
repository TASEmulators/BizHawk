using System;
using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	public class ApiSubsetContainer : IApiContainer
	{
		public Dictionary<Type, IExternalApi> Libraries { get; set; }

		public IEmuApi Emu => (IEmuApi) Libraries[typeof(IEmuApi)];
		public IGameInfoApi GameInfo => (IGameInfoApi) Libraries[typeof(IGameInfoApi)];
		public IJoypadApi Joypad => (IJoypadApi) Libraries[typeof(IJoypadApi)];
		public IMemApi Mem => (IMemApi) Libraries[typeof(IMemApi)];
		public IMemEventsApi MemEvents => (IMemEventsApi) Libraries[typeof(IMemEventsApi)];
		public IMemorySaveStateApi MemorySaveState => (IMemorySaveStateApi) Libraries[typeof(IMemorySaveStateApi)];
		public IInputMovieApi Movie => (IInputMovieApi) Libraries[typeof(IInputMovieApi)];
		public ISqlApi Sql => (ISqlApi) Libraries[typeof(ISqlApi)];
		public IUserDataApi UserData => (IUserDataApi) Libraries[typeof(IUserDataApi)];

		public ApiSubsetContainer(Dictionary<Type, IExternalApi> libs)
		{
			Libraries = libs;
		}
	}
}
