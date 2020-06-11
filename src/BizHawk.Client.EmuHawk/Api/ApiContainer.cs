using System;
using System.Collections.Generic;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public sealed class ApiContainer : ApiSubsetContainer
	{
		public ICommApi Comm => (ICommApi) Libraries[typeof(ICommApi)];
		public IGuiApi Gui => (IGuiApi) Libraries[typeof(IGuiApi)];
		public IInputApi Input => (IInputApi) Libraries[typeof(IInputApi)];
		public ISaveStateApi SaveState => (ISaveStateApi) Libraries[typeof(ISaveStateApi)];
		public IToolApi Tool => (IToolApi) Libraries[typeof(IToolApi)];

		public ApiContainer(Dictionary<Type, IExternalApi> libs) : base(libs) {}
	}
}
