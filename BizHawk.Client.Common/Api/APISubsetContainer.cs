using System;
using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	public sealed class APISubsetContainer : IApiContainer
	{
		public Dictionary<Type, IExternalApi> Libraries { get; set; }

		public IEmu Emu => (IEmu) Libraries[typeof(EmuApi)];
		public IGameInfo GameInfo => (IGameInfo) Libraries[typeof(GameInfoApi)];
		public IJoypad Joypad => (IJoypad) Libraries[typeof(JoypadApi)];
		public IMem Mem => (IMem) Libraries[typeof(MemApi)];
		public IMemEvents MemEvents => (IMemEvents) Libraries[typeof(MemEventsApi)];
		public IMemorySaveState MemorySaveState => (IMemorySaveState) Libraries[typeof(MemorySaveStateApi)];
		public IInputMovie Movie => (IInputMovie) Libraries[typeof(MovieApi)];
		public ISql Sql => (ISql) Libraries[typeof(SqlApi)];
		public IUserData UserData => (IUserData) Libraries[typeof(UserDataApi)];

		public APISubsetContainer(Dictionary<Type, IExternalApi> libs)
		{
			Libraries = libs;
		}
	}
}
