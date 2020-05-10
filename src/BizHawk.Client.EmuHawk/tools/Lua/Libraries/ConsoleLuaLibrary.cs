using System;
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
			LogWithSeparator("\t", "\n", outputs);
		}

		// Single param version is used by logOutputCallback of some libraries.
		public static void LogOutput(object output)
		{
			Log(output);
		}

		[LuaMethodExample("console.writeline( \"New log line.\" );")]
		[LuaMethod("writeline", "Outputs the given object to the output box on the Lua Console dialog. Note: Can accept a LuaTable")]
		public static void WriteLine(params object[] outputs)
		{
			LogWithSeparator("\n", "\n", outputs);
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
			static string SerializeTable(LuaTable lti)
			{
				var keyObjs = lti.Keys;
				var valueObjs = lti.Values;
				if (keyObjs.Count != valueObjs.Count)
				{
					throw new IndexOutOfRangeException("each value must be paired with one key, they differ in number");
				}

				var values = new object[keyObjs.Count];
				var kvpIndex = 0;
				foreach (var valueObj in valueObjs)
				{
					values[kvpIndex++] = valueObj;
				}

				return string.Concat(keyObjs.Cast<object>()
					.Select((kObj, i) => $"\"{kObj}\": \"{values[i]}\"\n")
					.OrderBy(s => s)
				);
			}

			static void SerializeAndWrite(object output) => GlobalWin.Tools.LuaConsole.WriteToOutputWindow(
				output is LuaTable table
					? SerializeTable(table)
					: output?.ToString() ?? "nil"
			);

			if (!GlobalWin.Tools.Has<LuaConsole>())
			{
				return;
			}

			if (outputs == null)
			{
				GlobalWin.Tools.LuaConsole.WriteToOutputWindow($"(no return){terminator}");
				return;
			}

			SerializeAndWrite(outputs[0]);
			for (int outIndex = 1, indexAfterLast = outputs.Length; outIndex != indexAfterLast; outIndex++)
			{
				GlobalWin.Tools.LuaConsole.WriteToOutputWindow(separator);
				SerializeAndWrite(outputs[outIndex]);
			}

			if (!string.IsNullOrEmpty(terminator))
			{
				GlobalWin.Tools.LuaConsole.WriteToOutputWindow(terminator);
			}
		}
	}
}
