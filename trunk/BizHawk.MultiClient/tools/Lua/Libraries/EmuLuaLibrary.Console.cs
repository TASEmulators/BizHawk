using System.Collections.Generic;
using System.Linq;
using System.Text;

using LuaInterface;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	public class ConsoleLuaLibrary : LuaLibraryBase
	{
		public override string Name { get { return "console"; } }
		public override string[] Functions
		{
			get
			{
				return new[]
				{
					"clear",
					"getluafunctionslist",
					"log",
					"output",
				};
			}
		}

		public static void console_clear()
		{
			GlobalWinF.Tools.LuaConsole.ClearOutputWindow();
		}

		public static string console_getluafunctionslist()
		{
			StringBuilder list = new StringBuilder();
			foreach (var function in GlobalWinF.Tools.LuaConsole.LuaImp.Docs.FunctionList)
			{
				list.AppendLine(function.Name);
			}
			return list.ToString();
		}

		public static void console_log(object lua_input)
		{
			console_output(lua_input);
		}

		public static void console_output(object lua_input)
		{
			if (lua_input == null)
			{
				GlobalWinF.Tools.LuaConsole.WriteToOutputWindow("NULL");
			}
			else
			{
				if (lua_input is LuaTable)
				{
					StringBuilder sb = new StringBuilder();
					var lti = (lua_input as LuaTable);

					List<string> Keys = (from object key in lti.Keys select key.ToString()).ToList();
					List<string> Values = (from object value in lti.Values select value.ToString()).ToList();

					List<KeyValuePair<string, string>> KVPs = new List<KeyValuePair<string, string>>();
					for (int i = 0; i < Keys.Count; i++)
					{
						if (i < Values.Count)
						{
							KeyValuePair<string, string> kvp = new KeyValuePair<string, string>(Keys[i], Values[i]);
							KVPs.Add(kvp);
						}
					}
					KVPs = KVPs.OrderBy(x => x.Key).ToList();
					foreach (var kvp in KVPs)
					{
						sb
							.Append("\"")
							.Append(kvp.Key)
							.Append("\": \"")
							.Append(kvp.Value)
							.Append("\"")
							.AppendLine();
					}

					GlobalWinF.Tools.LuaConsole.WriteToOutputWindow(sb.ToString());
				}
				else
				{
					GlobalWinF.Tools.LuaConsole.WriteToOutputWindow(lua_input.ToString());
				}
			}
		}
	}
}
