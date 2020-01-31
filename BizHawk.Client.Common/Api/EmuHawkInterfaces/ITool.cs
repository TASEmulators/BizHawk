using System;

namespace BizHawk.Client.Common
{
	public interface ITool : IExternalApi
	{
		object CreateInstance(string name);

		Type GetTool(string name);

		void OpenCheats();

		void OpenHexEditor();

		void OpenRAMSearch();

		void OpenRAMWatch();

		void OpenTASStudio();

		void OpenToolBox();

		void OpenTraceLogger();
	}
}
