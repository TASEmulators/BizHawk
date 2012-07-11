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
			FunctionList = FunctionList.OrderBy(x => x.name).ToList();
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
		}
	}
}
