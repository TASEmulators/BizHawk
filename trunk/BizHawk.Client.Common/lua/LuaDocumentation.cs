using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace BizHawk.Client.Common
{
	public interface ILuaDocumentation
	{
		void Add(string methodLib, string methodName, MethodInfo method, string description);
	}

	public class LuaDocumentation : ILuaDocumentation
	{
		public List<LibraryFunction> FunctionList { get; set; }

		public LuaDocumentation()
		{
			FunctionList = new List<LibraryFunction>();
		}

		public void Add(string methodLib, string methodName, MethodInfo method, string description)
		{
			FunctionList.Add(
				new LibraryFunction(methodLib, methodName, method, description)
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

		public IEnumerable<string> GetFunctionsByLibrary(string library)
		{
			return FunctionList
				.Where(func => func.Library == library)
				.Select(func => func.Name);
		}

		public class LibraryFunction
		{
			public LibraryFunction(string methodLib, string methodName, MethodInfo method, string description)
			{
				Library = methodLib;
				Name = methodName;
				var info = method.GetParameters();

				Parameters = new List<string>();
				foreach (var p in info)
				{
					Parameters.Add(p.ToString());
				}

				this._returnType = method.ReturnType.ToString();

				Description = description;
			}

			public string Library { get; set; }
			public string Name { get; set; }
			public List<string> Parameters { get; set; }
			private readonly string _returnType = String.Empty;

			public string Description { get; set; }

			public string ParameterList
			{
				get
				{
					var list = new StringBuilder();
					list.Append('(');
					for (var i = 0; i < Parameters.Count; i++)
					{
						var param = Parameters[i]
							.Replace("System", String.Empty)
							.Replace("Object", String.Empty)
							.Replace(" ", String.Empty)
							.Replace(".", String.Empty)
							.Replace("LuaInterface", String.Empty);

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
					return _returnType
						.Replace("System.", String.Empty)
						.Replace("LuaInterface.", String.Empty)
						.ToLower()
						.Trim();
				}
			}
		}
	}
}
