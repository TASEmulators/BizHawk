using System;
using System.Linq;

using BizHawk.Client.Common;
using BizHawk.Common;

namespace BizHawk.Client.EmuHawk
{
	public sealed class ToolApi : ITool
	{
		public Type GetTool(string name)
		{
			var toolType = Util.GetTypeByName(name).FirstOrDefault(x => typeof(IToolForm).IsAssignableFrom(x) && !x.IsInterface);
			if (toolType != null) GlobalWin.Tools.Load(toolType);
			return GlobalWin.Tools.AvailableTools.FirstOrDefault(tool => tool.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase));
		}

		public object CreateInstance(string name)
		{
			var found = Util.GetTypeByName(name).FirstOrDefault();
			return found != null ? Activator.CreateInstance(found) : null;
		}

		public void OpenCheats() => GlobalWin.Tools.Load<Cheats>();

		public void OpenHexEditor() => GlobalWin.Tools.Load<HexEditor>();

		public void OpenRamWatch() => GlobalWin.Tools.LoadRamWatch(loadDialog: true);

		public void OpenRamSearch() => GlobalWin.Tools.Load<RamSearch>();

		public void OpenTasStudio() => GlobalWin.Tools.Load<TAStudio>();

		public void OpenToolBox() => GlobalWin.Tools.Load<ToolBox>();

		public void OpenTraceLogger() => GlobalWin.Tools.Load<TraceLogger>();
	}
}
