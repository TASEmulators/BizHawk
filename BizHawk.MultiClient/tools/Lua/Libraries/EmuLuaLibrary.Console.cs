using System.Collections.Generic;
using System.Linq;
using System.Text;

using LuaInterface;

namespace BizHawk.MultiClient
{
	public static class ConsoleLuaLibrary
	{
		public static string Name = "console";
		public static string[] Functions = new[]
		{
			"clear",
			"getluafunctionslist",
			"log",
			"output",
		};

		public static void console_clear()
		{
			GlobalWinF.MainForm.LuaConsole1.ClearOutputWindow();
		}

		public static string console_getluafunctionslist()
		{
			string list = "";
			foreach (LuaDocumentation.LibraryFunction l in GlobalWinF.MainForm.LuaConsole1.LuaImp.Docs.FunctionList)
			{
				list += l.name + "\n";
			}

			return list;
		}

		public static void console_log(object lua_input)
		{
			console_output(lua_input);
		}

		public static void console_output(object lua_input)
		{
			if (lua_input == null)
			{
				GlobalWinF.MainForm.LuaConsole1.WriteToOutputWindow("NULL");
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

					GlobalWinF.MainForm.LuaConsole1.WriteToOutputWindow(sb.ToString());
				}
				else
				{
					GlobalWinF.MainForm.LuaConsole1.WriteToOutputWindow(lua_input.ToString());
				}
			}
		}
	}
}
