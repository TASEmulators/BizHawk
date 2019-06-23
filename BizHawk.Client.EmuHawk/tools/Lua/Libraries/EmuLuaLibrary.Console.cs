using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using BizHawk.Client.Common;
using NLua;

namespace BizHawk.Client.EmuHawk
{
	public sealed class ConsoleLuaLibrary : LuaLibraryBase
	{
		public ConsoleLuaLibrary(Lua lua)
			: base(lua) { }

		public ConsoleLuaLibrary(Lua lua, Action<string> logOutputCallback)
			: base(lua, logOutputCallback) { }

		public override string Name => "console";

		[LuaMethodExample("console.clear( );")]
		[LuaMethod("clear", "clears the output box of the Lua Console window")]
		public static void Clear()
		{
			if (GlobalWin.Tools.Has<LuaConsole>())
			{
				GlobalWin.Tools.LuaConsole.ClearOutputWindow();
			}
		}

		[LuaMethodExample("local stconget = console.getluafunctionslist( );")]
		[LuaMethod("getluafunctionslist", "returns a list of implemented functions")]
		public static string GetLuaFunctionsList()
		{
			var list = new StringBuilder();
			foreach (var function in GlobalWin.Tools.LuaConsole.LuaImp.Docs)
			{
				list.AppendLine(function.Name);
			}

			return list.ToString();
		}

		[LuaMethodExample("console.log( \"New log.\" );")]
		[LuaMethod("log", "Outputs the given object to the output box on the Lua Console dialog. Note: Can accept a LuaTable")]
		public static void Log(params object[] outputs)
		{
			if (GlobalWin.Tools.Has<LuaConsole>())
			{
				LogWithSeparator("\t", "\n", outputs);
			}
		}

		//// Single param version is used by logOutputCallback of some libraries.
		public static void LogOutput(object output)
		{
			if (GlobalWin.Tools.Has<LuaConsole>())
			{
				Log(output);
			}
		}

		[LuaMethodExample("console.writeline( \"New log line.\" );")]
		[LuaMethod("writeline", "Outputs the given object to the output box on the Lua Console dialog. Note: Can accept a LuaTable")]
		public static void WriteLine(params object[] outputs)
		{
			if (GlobalWin.Tools.Has<LuaConsole>())
			{
				LogWithSeparator("\n", "\n", outputs);
			}
		}

		[LuaMethodExample("console.write( \"New log message.\" );")]
		[LuaMethod("write", "Outputs the given object to the output box on the Lua Console dialog. Note: Can accept a LuaTable")]
		public static void Write(params object[] outputs)
		{
			LogWithSeparator("", "", outputs);
		}

		// Outputs the given object to the output box on the Lua Console dialog. Note: Can accept a LuaTable
		private static void LogWithSeparator(string separator, string terminator, params object[] outputs)
		{
			if (!GlobalWin.Tools.Has<LuaConsole>())
			{
				return;
			}

			if (outputs == null)
			{
				GlobalWin.Tools.LuaConsole.WriteToOutputWindow($"(no return){terminator}");
				return;
			}

			for (var outIndex = 0; outIndex < outputs.Length; outIndex++)
			{
				var output = outputs[outIndex];

				if (outIndex != 0)
				{
					GlobalWin.Tools.LuaConsole.WriteToOutputWindow(separator);
				}

				if (output == null)
				{
					GlobalWin.Tools.LuaConsole.WriteToOutputWindow("nil");
				}
				else
				{
					if (output is LuaTable)
					{
						var sb = new StringBuilder();
						var lti = output as LuaTable;

						var keys = (from object key in lti.Keys select key.ToString()).ToList();
						var values = (from object value in lti.Values select value.ToString()).ToList();

						var kvps = new List<KeyValuePair<string, string>>();
						for (var i = 0; i < keys.Count; i++)
						{
							if (i < values.Count)
							{
								kvps.Add(new KeyValuePair<string, string>(keys[i], values[i]));
							}
						}

						foreach (var kvp in kvps.OrderBy(x => x.Key))
						{
							sb
								.Append("\"")
								.Append(kvp.Key)
								.Append("\": \"")
								.Append(kvp.Value)
								.Append("\"")
								.AppendLine();
						}

						GlobalWin.Tools.LuaConsole.WriteToOutputWindow(sb.ToString());
					}
					else
					{
						GlobalWin.Tools.LuaConsole.WriteToOutputWindow(output.ToString());
					}
				}
			}

			GlobalWin.Tools.LuaConsole.WriteToOutputWindow(terminator);
		}
	}
}
