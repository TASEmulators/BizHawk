using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	public interface IToolApi : IExternalApi
	{
		IEnumerable<Type> AvailableTools { get; }

		object CreateInstance(string name);

		IToolForm GetTool(string name);

		void OpenCheats();

		void OpenHexEditor();

		void OpenRamSearch();

		void OpenRamWatch();

		void OpenTasStudio();

		void OpenToolBox();

		void OpenTraceLogger();
	}
}
