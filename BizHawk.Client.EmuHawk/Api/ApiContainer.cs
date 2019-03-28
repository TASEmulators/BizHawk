using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

using BizHawk.Client.ApiHawk;

namespace BizHawk.Client.EmuHawk
{
	public sealed class ApiContainer : IApiContainer
	{
		public IComm Comm => (IComm)Libraries[typeof(CommApi)];
		public IEmu Emu => (IEmu)Libraries[typeof(EmuApi)];
		public IGameInfo GameInfo => (IGameInfo)Libraries[typeof(GameInfoApi)];
		public IGui Gui => (IGui)Libraries[typeof(GuiApi)];
		public IInput Input => (IInput)Libraries[typeof(InputApi)];
		public IJoypad Joypad => (IJoypad)Libraries[typeof(JoypadApi)];
		public IMem Mem => (IMem)Libraries[typeof(MemApi)];
		public IMemEvents MemEvents => (IMemEvents)Libraries[typeof(MemEventsApi)];
		public IMemorySaveState MemorySaveState => (IMemorySaveState)Libraries[typeof(MemorySaveStateApi)];
		public IMovie Movie => (IMovie)Libraries[typeof(MovieApi)];
		public ISaveState SaveState => (ISaveState)Libraries[typeof(SaveStateApi)];
		public ISql Sql => (ISql)Libraries[typeof(SqlApi)];
		public ITool Tool => (ITool)Libraries[typeof(ToolApi)];
		public IUserData UserData => (IUserData)Libraries[typeof(UserDataApi)];
		public Dictionary<Type, IExternalApi> Libraries { get; set; }
		public ApiContainer(Dictionary<Type, IExternalApi> libs)
		{
			Libraries = libs;
		}
	}
}
