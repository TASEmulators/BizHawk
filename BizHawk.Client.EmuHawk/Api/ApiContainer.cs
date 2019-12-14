using System;
using System.Collections.Generic;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public sealed class ApiContainer : APISubsetContainer
	{
		public IComm Comm => (IComm) Libraries[typeof(CommApi)];
		public IGui Gui => (IGui) Libraries[typeof(GuiApi)];
		public IInput Input => (IInput) Libraries[typeof(InputApi)];
		public ISaveState SaveState => (ISaveState) Libraries[typeof(SaveStateApi)];
		public ITool Tool => (ITool) Libraries[typeof(ToolApi)];

		public ApiContainer(Dictionary<Type, IExternalApi> libs) : base(libs) {}
	}
}
