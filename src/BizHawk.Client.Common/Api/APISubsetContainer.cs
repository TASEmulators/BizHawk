using System;
using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	public class ApiSubsetContainer : IApiContainer
	{
		public Dictionary<Type, IExternalApi> Libraries { get; set; }

		public IEmu Emu => (IEmu) Libraries[typeof(IEmu)];
		public IGameInfoApi GameInfo => (IGameInfoApi) Libraries[typeof(IGameInfoApi)];
		public IJoypad Joypad => (IJoypad) Libraries[typeof(IJoypad)];
		public IMem Mem => (IMem) Libraries[typeof(IMem)];
		public IMemEvents MemEvents => (IMemEvents) Libraries[typeof(IMemEvents)];
		public IMemorySaveState MemorySaveState => (IMemorySaveState) Libraries[typeof(IMemorySaveState)];
		public IInputMovie Movie => (IInputMovie) Libraries[typeof(IInputMovie)];
		public ISql Sql => (ISql) Libraries[typeof(ISql)];
		public IUserData UserData => (IUserData) Libraries[typeof(IUserData)];

		public ApiSubsetContainer(Dictionary<Type, IExternalApi> libs)
		{
			Libraries = libs;
		}
	}
}
