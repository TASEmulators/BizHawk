using System;
using System.Collections.Generic;

namespace BizHawk.Client.Common
{
	public interface IToolApi : IExternalApi
	{
		IEnumerable<Type> AvailableTools { get; }
		Type GetTool(string name);
		object CreateInstance(string name);
		void OpenCheats();
		void OpenHexEditor();
		void OpenRamWatch();
		void OpenRamSearch();
		void OpenTasStudio();
		void OpenToolBox();
		void OpenTraceLogger();
	}
}
