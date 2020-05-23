#nullable enable

using System;
using System.Linq;

using BizHawk.API.ApiHawk;
using BizHawk.API.Base;
using BizHawk.Client.Common;
using BizHawk.Common;

namespace BizHawk.Client.EmuHawk.APIImpl.ApiHawk
{
	internal sealed class ToolFormLibLegacyImpl : LibBase<GlobalsAccessAPIEnvironment>, IToolFormLib, ITool
	{
		public ToolFormLibLegacyImpl(out Action<GlobalsAccessAPIEnvironment> updateEnv) : base(out updateEnv) {}

		[LegacyApiHawk]
		public object? CreateInstance(string name)
		{
			var found = Util.GetTypeByName(name).FirstOrDefault();
			return found != null ? Activator.CreateInstance(found) : null;
		}

		public bool EnsureToolLoaded(string typeName)
		{
			var toolType = Util.GetTypeByName(typeName).FirstOrDefault(t => typeof(IToolForm).IsAssignableFrom(t) && !t.IsInterface);
			if (toolType == null) return false;
			try
			{
				return Env.GlobalToolManager.Load(toolType) != null;
			}
			catch (ArgumentException)
			{
				return false;
			}
		}

		[LegacyApiHawk]
		public Type? GetTool(string name) => EnsureToolLoaded(name)
			? Env.GlobalToolManager.AvailableTools.FirstOrDefault(tool => string.Equals(tool.Name, name, StringComparison.InvariantCultureIgnoreCase))
			: null;

		[LegacyApiHawk]
		public void OpenCheats() => Env.GlobalToolManager.Load<Cheats>();

		[LegacyApiHawk]
		public void OpenHexEditor() => Env.GlobalToolManager.Load<HexEditor>();

		[LegacyApiHawk]
		public void OpenRamSearch() => Env.GlobalToolManager.Load<RamSearch>();

		[LegacyApiHawk]
		public void OpenRamWatch() => Env.GlobalToolManager.LoadRamWatch(loadDialog: true);

		[LegacyApiHawk]
		public void OpenTasStudio() => Env.GlobalToolManager.Load<TAStudio>();

		[LegacyApiHawk]
		public void OpenToolBox() => Env.GlobalToolManager.Load<ToolBox>();

		[LegacyApiHawk]
		public void OpenTraceLogger() => Env.GlobalToolManager.Load<TraceLogger>();
	}
}
