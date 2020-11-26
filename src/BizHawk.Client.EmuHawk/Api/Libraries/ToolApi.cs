using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Client.Common;
using BizHawk.Common;

namespace BizHawk.Client.EmuHawk
{
	public sealed class ToolApi : IToolApi
	{
		private readonly ToolManager _toolManager;

		public IEnumerable<Type> AvailableTools => _toolManager.AvailableTools.ToList(); // defensive copy in case ToolManager's implementation changes

		public ToolApi(ToolManager toolManager) => _toolManager = toolManager;

		public Type GetTool(string name)
		{
			var toolType = Util.GetTypeByName(name).FirstOrDefault(x => typeof(IToolForm).IsAssignableFrom(x) && !x.IsInterface);
			if (toolType != null) _toolManager.Load(toolType);
			return _toolManager.AvailableTools.FirstOrDefault(tool => tool.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
		}

		public object CreateInstance(string name)
		{
			var found = Util.GetTypeByName(name).FirstOrDefault();
			return found != null ? Activator.CreateInstance(found) : null;
		}

		public void OpenCheats() => _toolManager.Load<Cheats>();

		public void OpenHexEditor() => _toolManager.Load<HexEditor>();

		public void OpenRamWatch() => _toolManager.LoadRamWatch(loadDialog: true);

		public void OpenRamSearch() => _toolManager.Load<RamSearch>();

		public void OpenTasStudio() => _toolManager.Load<TAStudio>();

		public void OpenToolBox() => _toolManager.Load<ToolBox>();

		public void OpenTraceLogger() => _toolManager.Load<TraceLogger>();
	}
}
