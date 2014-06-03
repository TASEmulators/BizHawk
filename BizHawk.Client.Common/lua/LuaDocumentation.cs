using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace BizHawk.Client.Common
{
	public class LuaDocumentation : List<LibraryFunction>
	{
		public LuaDocumentation()
			:base() { }

		public void ToTASVideosWikiMarkup()
		{
			var sb = new StringBuilder();
			
			sb
				.AppendLine("[module:ListParents]")
				.AppendLine()
				.AppendLine("This page documents the the behavior and parameters of Lua functions available for the [BizHawk] emulator.")
				.AppendLine()
				.AppendLine();

			foreach (var library in this.Select(x => new { Name = x.Library, Description = x.LibraryDescription }).Distinct())
			{
				sb
					.AppendFormat("%%TAB {0}%%", library.Name)
					.AppendLine()
					.AppendLine();
				if (!string.IsNullOrWhiteSpace(library.Description))
				{
					sb
						.Append(library.Description)
						.AppendLine()
						.AppendLine();
				}

				foreach (var func in this.Where(x => x.Library == library.Name))
				{
					sb
						.AppendFormat("__{0}.{1}__%%%", func.Library, func.Name)
						.AppendLine().AppendLine()
						.AppendFormat("* {0} {1}.{2}{3}", func.ReturnType, func.Library, func.Name, func.ParameterList)
						.AppendLine().AppendLine()
						.AppendFormat("* {0}", func.Description)
						.AppendLine().AppendLine();
				}
			}

			File.WriteAllText(Path.Combine(PathManager.GetExeDirectoryAbsolute(), "LuaDocumentationWiki.txt"), sb.ToString());
		}
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

			_returnType = method.ReturnType.ToString();

			Description = description;
		}

		public string Library { get; set; }
		public string Name { get; set; }
		public List<string> Parameters { get; set; }

		public string Description { get; set; }
		public string LibraryDescription { get; set; }

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
									 .Replace("Object[]", "object[] ")
									 .Replace("Object", "object ")
									 .Replace("Boolean[]", "bool[] ")
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
									 .Replace("Uint", "uint ")
									 .Replace("Nullable`1[DrawingColor]", "Color? ")
									 .Replace("DrawingColor", "Color ");

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
