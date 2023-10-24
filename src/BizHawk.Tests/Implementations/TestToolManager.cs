using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using BizHawk.Client.Common;
using BizHawk.Tests.Mocks;

namespace BizHawk.Tests.Implementations
{
	internal class TestToolManager : ToolManagerBase
	{
		public TestToolManager(IMainFormForApi mainFormApi, Config config, DisplayManagerBase displayManager)
			: base(new MockMainFormForTools(),
				  mainFormApi,
				  config,
				  displayManager,
				  new ExternalToolManager(config, () => ("", "")),
				  null,
				  mainFormApi.Emulator,
				  mainFormApi.MovieSession,
				  null)
		{ }


		protected override IList<string> PossibleToolTypeNames { get; } = ReflectionCache.Types
			.Where(static (t) => typeof(IExternalApi).IsAssignableFrom(t))
			.Select(static (t) => t.AssemblyQualifiedName!)
			.ToList();
		protected override bool CaptureIconAndName(object tool, Type toolType, ref Image? icon, ref string name)
		{
			ExternalToolAttribute? eta = tool.GetType().GetCustomAttribute<ExternalToolAttribute>();
			if (eta == null)
				throw new NotImplementedException(); // not an external tool

			icon = null;
			name = eta.Name;
			return true;
		}

		protected override void SetFormParent(IToolForm form) { }
		protected override void SetBaseProperties(IToolForm form) { }

		protected override IExternalToolForm CreateInstanceFrom(string dllPath, string toolTypeName)
		{
			return (Activator.CreateInstanceFrom(dllPath, toolTypeName)!.Unwrap() as IExternalToolForm)!;
		}

		public override IEnumerable<Type> AvailableTools => throw new NotImplementedException();

		public override (Image Icon, string Name) GetIconAndNameFor(Type toolType) => throw new NotImplementedException();
		public override bool IsOnScreen(Point topLeft) => throw new NotImplementedException();
		public override void LoadRamWatch(bool loadDialog) => throw new NotImplementedException();
		public override void UpdateCheatRelatedTools(object sender, CheatCollection.CheatListEventArgs e) => throw new NotImplementedException();
		protected override void AttachSettingHooks(IToolFormAutoConfig tool, ToolDialogSettings settings) => throw new NotImplementedException();

		protected override void MaybeClearCheats() => throw new NotImplementedException();
		protected override void SetFormClosingEvent(IToolForm form, Action action) => throw new NotImplementedException();
	}
}
