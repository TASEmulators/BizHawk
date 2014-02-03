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
		public LuaDocumentation()
		{
			FunctionList = new List<LibraryFunction>();
		}

		public List<LibraryFunction> FunctionList { get; set; }

		public void Add(string methodLib, string methodName, MethodInfo method, string description)
		{
			FunctionList.Add(
				new LibraryFunction(methodLib, methodName, method, description));
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
			private readonly string _returnType = string.Empty;

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

			public string Description { get; set; }

			public string ParameterList
			{
				get
				{
					var list = new StringBuilder();
					list.Append('(');
					for (var i = 0; i < Parameters.Count; i++)
					{
						var param =
							Parameters[i].Replace("System", string.Empty)
										 .Replace(" ", string.Empty)
										 .Replace(".", string.Empty)
										 .Replace("LuaInterface", string.Empty)
										 .Replace("Object", "object ")
										 .Replace("Boolean", "bool ")
										 .Replace("String", "string ")
										 .Replace("LuaTable", "table ")
										 .Replace("LuaFunction", "func ")
										 .Replace("Nullable`1[Int32]", "int? ")
										 .Replace("Nullable`1[UInt32]", "uint? ")
										 .Replace("Byte", "byte ")
										 .Replace("Int16", "short ")
										 .Replace("Int32", "int ")
										 .Replace("Int64", "long ")
										 .Replace("Ushort", "ushort ")
										 .Replace("Ulong", "ulong ")
										 .Replace("UInt32", "uint ")
										 .Replace("UInt64", "ulong ")
										 .Replace("Double", "double ")
										 .Replace("Uint", "uint ");

						list.Append(param);
						if (i < Parameters.Count - 1)
						{
							list.Append(", ");
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
						.Replace("System.", string.Empty)
						.Replace("LuaInterface.", string.Empty)
						.ToLower()
						.Trim();
				}
			}
		}
	}
}
