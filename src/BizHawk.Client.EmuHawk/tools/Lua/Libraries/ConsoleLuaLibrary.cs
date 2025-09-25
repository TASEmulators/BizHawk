using System.Collections.Generic;
using System.Linq;
using System.Text;

using BizHawk.Client.Common;
using BizHawk.Common.CollectionExtensions;

using NLua;

namespace BizHawk.Client.EmuHawk
{
	public sealed class ConsoleLuaLibrary : LuaLibraryBase
	{
		public Lazy<string> AllAPINames { get; set; }

		public ToolManager Tools { get; set; }

		public ConsoleLuaLibrary(ILuaLibraries luaLibsImpl, ApiContainer apiContainer, Action<string> logOutputCallback)
			: base(luaLibsImpl, apiContainer, logOutputCallback) {}

		public override string Name => "console";

		[LuaMethodExample("console.clear( );")]
		[LuaMethod("clear", "clears the output box of the Lua Console window")]
		public void Clear()
		{
			if (Tools.Has<LuaConsole>())
			{
				Tools.LuaConsole.ClearOutputWindow();
			}
		}

		[LuaMethodExample("local stconget = console.getluafunctionslist( );")]
		[LuaMethod("getluafunctionslist", "returns a list of implemented functions")]
		public string GetLuaFunctionsList()
			=> AllAPINames.Value;

		[LuaMethodExample("console.log( \"New log.\" );")]
		[LuaMethod("log", "Outputs the given object to the output box on the Lua Console dialog. Note: Can accept a LuaTable")]
		public void Log(params object[] outputs)
		{
			LogWithSeparator("\t", "\n", outputs);
		}

		[LuaMethodExample("console.writeline( \"New log line.\" );")]
		[LuaMethod("writeline", "Outputs the given object to the output box on the Lua Console dialog. Note: Can accept a LuaTable")]
		public void WriteLine(params object[] outputs)
		{
			LogWithSeparator("\n", "\n", outputs);
		}

		[LuaMethodExample("console.write( \"New log message.\" );")]
		[LuaMethod("write", "Outputs the given object to the output box on the Lua Console dialog. Note: Can accept a LuaTable")]
		public void Write(params object[] outputs)
		{
			LogWithSeparator("", "", outputs);
		}

		// Outputs the given object to the output box on the Lua Console dialog. Note: Can accept a LuaTable
		private void LogWithSeparator(string separator, string terminator, params object[] outputs)
		{
			static string SerializeTable(LuaTable lti)
			{
				var sorted = lti
					.OrderBy(static item => item.Key switch
					{
						long => 0,
						string => 1,
						double => 2,
						bool => 3,
						_ => 4, // tables, functions, ...
					})
					.ThenBy(static item => item.Key as long?)
					.ThenBy(static item => item.Key as double?)
					.ThenBy(static item => item.ToString());

				var sb = new StringBuilder();
				foreach (var item in sorted)
				{
					Append(sb, item.Key);
					sb.Append(": ");
					Append(sb, item.Value);
					sb.Append('\n');
				}
				return sb.ToString();			

				static void Append(StringBuilder sb, object value)
				{
					if (value is string str) sb.Append('"').Append(str).Append('"');
					else sb.Append(value);
				}
			}

			if (!Tools.Has<LuaConsole>())
			{
				return;
			}

			var sb = new StringBuilder();

			void SerializeAndWrite(object output)
				=> sb.Append(output switch
				{
					null => "nil",
					LuaTable table => SerializeTable(table),
					_ => output.ToString(),
				});

			if (outputs == null || outputs.Length == 0 || (outputs.Length == 1 && outputs[0] is null))
			{
				Tools.LuaConsole.WriteToOutputWindow($"(no return){terminator}");
				return;
			}

			SerializeAndWrite(outputs[0]);
			for (int outIndex = 1, indexAfterLast = outputs.Length; outIndex != indexAfterLast; outIndex++)
			{
				sb.Append(separator);
				SerializeAndWrite(outputs[outIndex]);
			}

			if (!string.IsNullOrEmpty(terminator))
			{
				sb.Append(terminator);
			}

			Tools.LuaConsole.WriteToOutputWindow(sb.ToString());
		}
	}
}
