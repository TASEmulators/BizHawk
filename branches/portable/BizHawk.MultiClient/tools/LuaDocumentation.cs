using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace BizHawk.MultiClient.tools
{
	public class LuaDocumentation
	{
		public List<LibraryFunction> FunctionList = new List<LibraryFunction>();

		public void Add(string method_lib, string method_name, System.Reflection.MethodInfo method)
		{
			LibraryFunction f = new LibraryFunction(method_lib, method_name, method);
			FunctionList.Add(f);
		}

		public void Clear()
		{
			FunctionList = new List<LibraryFunction>();
		}

		public void Sort()
		{
			FunctionList = FunctionList.OrderBy(x => x.library).ThenBy(x => x.name).ToList();
		}

		public List<string> GetLibraryList()
		{
			HashSet<string> libs = new HashSet<string>();
			foreach (LibraryFunction function in FunctionList)
			{
				libs.Add(function.library);
			}

			return libs.ToList();
		}

		public List<string> GetFunctionsByLibrary(string library)
		{
			List<string> functions = new List<string>();
			for (int i = 0; i < FunctionList.Count; i++)
			{
				if (FunctionList[i].library == library)
				{
					functions.Add(FunctionList[i].name);
				}
			}

			return functions;
		}

		public class LibraryFunction
		{
			public LibraryFunction(string method_lib, string method_name, System.Reflection.MethodInfo method)
			{
				library = method_lib;
				name = method_name;
				System.Reflection.ParameterInfo[] info = method.GetParameters();
				foreach (System.Reflection.ParameterInfo p in info)
				{
					parameters.Add(p.ToString());
				}
				return_type = method.ReturnType.ToString();
			}
			
			public string library = "";
			public string name = "";
			public List<string> parameters = new List<string>();
			public string return_type = "";

			public string ParameterList
			{
				get
				{
					StringBuilder list = new StringBuilder();
					list.Append('(');
					for (int i = 0; i < parameters.Count; i++)
					{
						string param = parameters[i].Replace("System", "").Replace("Object", "").Replace(" ", "").Replace(".", "").Replace("LuaInterface", "");
						list.Append(param);
						if (i < parameters.Count - 1)
						{
							list.Append(',');
						}
					}
					list.Append(')');
					return list.ToString();
				}
			}

			public string ReturnType
			{
				get
				{
					string r = "";
					r = return_type.Replace("System.", "").Replace("LuaInterface.", "").ToLower().Trim();
					return r;
				}
			}
		}
	}
}
