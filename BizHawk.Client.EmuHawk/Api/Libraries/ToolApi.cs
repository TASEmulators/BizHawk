using System;
using System.Collections.Generic;
using System.Linq;

using BizHawk.Common;
using BizHawk.Emulation.Common;
using BizHawk.Client.ApiHawk;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public sealed class ToolApi : ITool
	{
		private class ToolStatic
		{
			public Type GetTool(string name)
			{
				var toolType = ReflectionUtil.GetTypeByName(name)
					.FirstOrDefault(x => typeof(IToolForm).IsAssignableFrom(x) && !x.IsInterface);

				if (toolType != null)
				{
					GlobalWin.Tools.Load(toolType);
				}

				var selectedTool = GlobalWin.Tools.AvailableTools
					.FirstOrDefault(tool => tool.GetType().Name.ToLower() == name.ToLower());

				if (selectedTool != null)
				{
					return selectedTool;
				}

				return null;
			}

			public object CreateInstance(string name)
			{
				var possibleTypes = ReflectionUtil.GetTypeByName(name);

				if (possibleTypes.Any())
				{
					return Activator.CreateInstance(possibleTypes.First());
				}

				return null;
			}

			public static void OpenCheats()
			{
				GlobalWin.Tools.Load<Cheats>();
			}

			public static void OpenHexEditor()
			{
				GlobalWin.Tools.Load<HexEditor>();
			}

			public static void OpenRamWatch()
			{
				GlobalWin.Tools.LoadRamWatch(loadDialog: true);
			}

			public static void OpenRamSearch()
			{
				GlobalWin.Tools.Load<RamSearch>();
			}

			public static void OpenTasStudio()
			{
				GlobalWin.Tools.Load<TAStudio>();
			}

			public static void OpenToolBox()
			{
				GlobalWin.Tools.Load<ToolBox>();
			}

			public static void OpenTraceLogger()
			{
				GlobalWin.Tools.Load<TraceLogger>();
			}

		}
		[RequiredService]
		private static IEmulator Emulator { get; set; }

		[RequiredService]
		private static IVideoProvider VideoProvider { get; set; }

		public ToolApi()
		{ }

		public Type GetTool(string name)
		{
			var toolType = ReflectionUtil.GetTypeByName(name)
				.FirstOrDefault(x => typeof(IToolForm).IsAssignableFrom(x) && !x.IsInterface);

			if (toolType != null)
			{
				GlobalWin.Tools.Load(toolType);
			}

			var selectedTool = GlobalWin.Tools.AvailableTools
				.FirstOrDefault(tool => tool.GetType().Name.ToLower() == name.ToLower());

			if (selectedTool != null)
			{
				return selectedTool;
			}

			return null;
		}

		public object CreateInstance(string name)
		{
			var possibleTypes = ReflectionUtil.GetTypeByName(name);

			if (possibleTypes.Any())
			{
				return Activator.CreateInstance(possibleTypes.First());
			}

			return null;
		}

		public void OpenCheats()
		{
			ToolStatic.OpenCheats();
		}

		public void OpenHexEditor()
		{
			ToolStatic.OpenHexEditor();
		}

		public void OpenRamWatch()
		{
			ToolStatic.OpenRamWatch();
		}

		public void OpenRamSearch()
		{
			ToolStatic.OpenRamSearch();
		}

		public void OpenTasStudio()
		{
			ToolStatic.OpenTasStudio();
		}

		public void OpenToolBox()
		{
			ToolStatic.OpenToolBox();
		}

		public void OpenTraceLogger()
		{
			ToolStatic.OpenTraceLogger();
		}
	}
}
