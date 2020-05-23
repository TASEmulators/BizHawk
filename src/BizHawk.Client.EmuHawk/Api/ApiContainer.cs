using System;
using System.Collections.Generic;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public sealed class ApiContainer : ApiSubsetContainer
	{
		public IComm Comm => (IComm) Libraries[typeof(IComm)];
		public IGui Gui => (IGui) Libraries[typeof(IGui)];
		public IInput Input => (IInput) Libraries[typeof(IInput)];
		public ISaveState SaveState => (ISaveState) Libraries[typeof(ISaveState)];
		public ITool Tool => (ITool) Libraries[typeof(ITool)];

		public ApiContainer(Dictionary<Type, IExternalApi> libs) : base(libs) {}
	}
}
