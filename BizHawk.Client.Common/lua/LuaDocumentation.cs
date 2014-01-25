using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.Client.Common
{
	public interface ILuaDocumentation
	{
		void Add(string method_lib, string method_name, System.Reflection.MethodInfo method, string description);
	}

	public class LuaDocumentation : ILuaDocumentation
	{
		public List<LibraryFunction> FunctionList = new List<LibraryFunction>();

		public void Add(string method_lib, string method_name, System.Reflection.MethodInfo method, string description)
		{
			FunctionList.Add(
				new LibraryFunction(method_lib, method_name, method, description)
			);
		}

		public void Clear()
		{
			FunctionList = new List<LibraryFunction>();
		}

		public void Sort()
		{
			FunctionList = FunctionList.OrderBy(x => x.Library).ThenBy(x => x.Name).ToList();
		}

		public IEnumerable<string> GetLibraryList()
		{
			return FunctionList.Select(x => x.Library);
		}

		public List<string> GetFunctionsByLibrary(string library)
		{
			return (from t in FunctionList where t.Library == library select t.Name).ToList();
		}

		public class LibraryFunction
		{
			public LibraryFunction(string method_lib, string method_name, System.Reflection.MethodInfo method, string description)
			{
				Library = method_lib;
				Name = method_name;
				var info = method.GetParameters();
				foreach (var p in info)
				{
					Parameters.Add(p.ToString());
				}

				return_type = method.ReturnType.ToString();

				Description = description;
			}
			
			public string Library = String.Empty;
			public string Name = String.Empty;
			public List<string> Parameters = new List<string>();
			public string return_type = String.Empty;

			public string Description { get; set; }

			public string ParameterList
			{
				get
				{
					var list = new StringBuilder();
					list.Append('(');
					for (int i = 0; i < Parameters.Count; i++)
					{
						var param = Parameters[i].Replace("System", "").Replace("Object", "").Replace(" ", "").Replace(".", "").Replace("LuaInterface", "");
						list.Append(param);
						if (i < Parameters.Count - 1)
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
					return return_type.Replace("System.", "").Replace("LuaInterface.", "").ToLower().Trim();
				}
			}
		}
	}
}
