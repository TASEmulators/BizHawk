using System;
using System.Collections.Generic;

using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public sealed class ApiContainerEmu : ApiContainer
	{
		private IGui _gui;

		public ApiContainerEmu(IReadOnlyDictionary<Type, IExternalApi> libs) : base(libs)
		{
			Comm = (IComm) LibraryDict[typeof(CommApi)];
			Input = (IInput) LibraryDict[typeof(InputApi)];
			SaveState = (ISaveState) LibraryDict[typeof(SaveStateApi)];
			Tool = (ITool) LibraryDict[typeof(ToolApi)];
		}

		public override IComm Comm { get; }

		public override IGui Gui => _gui ??= (IGui) LibraryDict[typeof(GuiApi)];

		public override IInput Input { get; }

		public override ISaveState SaveState { get; }

		public override ITool Tool { get; }
	}
}
