using System.Collections.Generic;
using System.Linq;

using BizHawk.Client.Common;
using BizHawk.Common;

namespace BizHawk.Client.EmuHawk
{
	public sealed class ToolApi : IToolApi
	{
		static ToolApi()
		{
			ApiManager.AddApiType(typeof(ToolApi));
		}

		private readonly IToolLoader _toolLoader;

		public IEnumerable<Type> AvailableTools => _toolLoader.AvailableTools.ToList(); // defensive copy in case ToolManager's implementation changes

		public ToolApi(IToolLoader toolLoader) => _toolLoader = toolLoader;

		public IToolForm GetTool(string name)
		{
			var toolType = Util.GetTypeByName(name).FirstOrDefault(x => typeof(IToolForm).IsAssignableFrom(x) && !x.IsInterface);
			if (toolType == null) return null;
			return _toolLoader.Load(toolType);
		}

		public object CreateInstance(string name)
		{
			var found = Util.GetTypeByName(name).FirstOrDefault();
			return found != null ? Activator.CreateInstance(found) : null;
		}

		public void OpenCheats() => _toolLoader.Load<Cheats>();

		public void OpenHexEditor() => _toolLoader.Load<HexEditor>();

		public void OpenRamWatch() => _toolLoader.LoadRamWatch(loadDialog: true);

		public void OpenRamSearch() => _toolLoader.Load<RamSearch>();

		public void OpenTasStudio() => _toolLoader.Load<TAStudio>();

		public void OpenToolBox() => _toolLoader.Load<ToolBox>();

		public void OpenTraceLogger() => _toolLoader.Load<TraceLogger>();
	}
}
