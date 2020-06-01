#nullable enable

using System;

namespace BizHawk.Client.Common
{
	[LegacyApiHawk]
	public interface ITool : IExternalApi
	{
		[LegacyApiHawk]
		object? CreateInstance(string name);

		[LegacyApiHawk]
		Type? GetTool(string name);

		[LegacyApiHawk]
		void OpenCheats();

		[LegacyApiHawk]
		void OpenHexEditor();

		[LegacyApiHawk]
		void OpenRamSearch();

		[LegacyApiHawk]
		void OpenRamWatch();

		[LegacyApiHawk]
		void OpenTasStudio();

		[LegacyApiHawk]
		void OpenToolBox();

		[LegacyApiHawk]
		void OpenTraceLogger();
	}
}
