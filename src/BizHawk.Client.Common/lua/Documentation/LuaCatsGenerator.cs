#nullable enable

#pragma warning disable MA0136 // Raw String contains an implicit end of line character, line endings will be normalized

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using BizHawk.Common;
using NLua;

namespace BizHawk.Client.Common;

/// <summary>
/// Generates API definitions in the LuaCATS format.
/// </summary>
/// <remarks>
/// See https://luals.github.io/wiki/annotations
/// </remarks>
internal static class LuaCatsGenerator
{
	private static readonly Dictionary<Type, string> TypeConversions = new()
	{
		[typeof(object)] = "any",
		[typeof(byte)] = "integer",
		[typeof(sbyte)] = "integer",
		[typeof(int)] = "integer",
		[typeof(uint)] = "integer",
		[typeof(short)] = "integer",
		[typeof(ushort)] = "integer",
		[typeof(long)] = "integer",
		[typeof(ulong)] = "integer",
		[typeof(float)] = "number",
		[typeof(double)] = "number",
		[typeof(decimal)] = "number",
		[typeof(string)] = "string",
		[typeof(bool)] = "boolean",
		[typeof(byte[])] = "string",
		[typeof(Memory<byte>)] = "string",
		[typeof(ReadOnlyMemory<byte>)] = "string",
		[typeof(LuaFunction)] = "function",
		[typeof(LuaTable)] = "table",
		[typeof(System.Drawing.Color)] = "dotnetcolor",
	};

	private const string Classes = """
---@class dotnetcolor : userdata

---A color in one of the following formats:
--- - Number in the format `0xAARRGGBB`
--- - String in the format `"#RRGGBB"` or `"#AARRGGBB"`
--- - A CSS3/X11 color name e.g. `"blue"`, `"palegoldenrod"`
--- - Color created with `forms.createcolor`
---@alias color integer | string | dotnetcolor

---@alias surface
---| "emucore" # Draw on the emulated screen. Resolution depends on emulated system and game. Drawing is scaled with the rest of the display.
---| "client" # Draw on the BizHawk window. Resolution depends on the window size. Drawing is not scaled.
""";

	private const string Preamble = """
-- https://tasvideos.org/Bizhawk

error("This is a definition file for Lua Language Server and not a usable script")

---@meta _
""";

	private static string? GetHardcodedType(ParameterInfo parameter)
	{
		// technically any string parameter can be passed a number, but let's just focus on the ones where it's commonly used
		// like `gui.text` and `forms.settext` instead of polluting the entire API surface
		if (parameter.Name is "message" or "caption" && parameter.ParameterType == typeof(string))
		{
			return "string | number";
		}

		if (parameter.Member.DeclaringType == typeof(GuiLuaLibrary) && parameter.Name == "surfaceName" && parameter.ParameterType == typeof(string))
		{
			return "surface";
		}

		return null;
	}

	public static void Generate(LuaDocumentation docs, string path)
	{
		var sb0 = new StringBuilder();

		sb0.AppendLine($"-- Lua functions available in EmuHawk {VersionInfo.MainVersion}");
		sb0.AppendLine(Preamble);
		sb0.AppendLine();
		sb0.AppendLine(Classes);
		sb0.AppendLine();

		File.WriteAllText(Path.Combine(path, "classes.d.lua"), sb0.ToString().ReplaceLineEndings());

		foreach (var libraryGroup in docs.GroupBy(func => func.Library).OrderBy(group => group.Key))
		{
			string library = libraryGroup.Key;
			string libraryDescription = libraryGroup.First().LibraryDescription;
			var libraryType = libraryGroup.First().Method.DeclaringType;
			var filePath = Path.Combine(path, library + ".d.lua");
			var sb = new StringBuilder();

			sb.AppendLine($"-- Lua functions available in EmuHawk {VersionInfo.MainVersion}");
			sb.AppendLine(Preamble);
			sb.AppendLine();

			if (!string.IsNullOrEmpty(libraryDescription))
			{
				sb.AppendLine(FormatMarkdown(libraryDescription));
			}

			sb.AppendLine($"---@class {library}");
			if (!typeof(LuaLibraryBase).IsAssignableFrom(libraryType)) sb.Append("local "); // don't make LuaCanvas global
			sb.AppendLine($"{library} = {{}}");
			sb.AppendLine();

			foreach (var func in libraryGroup.OrderBy(func => func.Name))
			{
				if (!string.IsNullOrEmpty(func.Description))
				{
					sb.AppendLine(FormatMarkdown(func.Description));
				}

				if (func.Example != null)
				{
					sb.AppendLine("---");
					sb.AppendLine("---Example:");
					sb.AppendLine("---");
					sb.AppendLine(FormatMarkdown(func.Example, "---\t"));
				}

				if (func.IsDeprecated)
				{
					sb.AppendLine("---@deprecated");
				}

				foreach (var parameter in func.Method.GetParameters())
				{
					if (IsParams(parameter))
					{
						sb.Append("---@vararg");
					}
					else
					{
						sb.Append($"---@param {parameter.Name}");
						if (parameter.IsOptional || IsNullable(parameter.ParameterType))
						{
							sb.Append('?');
						}
					}

					sb.Append(' ');
					sb.Append(GetLuaType(parameter));
					if (parameter.HasDefaultValue && parameter.DefaultValue is not null and not "")
					{
						sb.Append($" Defaults to `{FormatValue(parameter.DefaultValue)}`");
					}
					sb.AppendLine();
				}

				if (func.Method.ReturnType != typeof(void))
				{
					sb.Append("---@return ");
					sb.AppendLine(GetLuaType(func.Method.ReturnType));
				}

				sb.Append($"function {library}.{func.Name}(");

				foreach (var parameter in func.Method.GetParameters())
				{
					if (parameter.Position > 0)
					{
						sb.Append(", ");
					}
					sb.Append(IsParams(parameter) ? "..." : parameter.Name);
				}

				sb.AppendLine(") end");
				sb.AppendLine();
			}
			File.WriteAllText(filePath, sb.ToString().ReplaceLineEndings());
		}
	}

	private static string FormatMarkdown(string value, string prefix = "---")
	{
		// prefix every line
		value = Regex.Replace(value, "^", prefix, RegexOptions.Multiline);
		// replace {{wiki markup}} with `markdown`
		value = Regex.Replace(value, "{{(.+?)}}", "`$1`");
		// replace wiki image markup with markdown
		value = Regex.Replace(value, @"\[(?<url>.+?)\|alt=(?<alt>.+?)\]", "![${alt}](${url})");
		return value;
	}

	private static string FormatValue(object value) => value switch
	{
		string str => $"\"{str}\"",
		true => "true",
		false => "false",
		null => "nil",
		_ => value.ToString(),
	};

	private static string GetLuaType(ParameterInfo parameter)
	{
		if (GetHardcodedType(parameter) is string hardcodedType)
		{
			return hardcodedType;
		}

		if (parameter.GetCustomAttribute<LuaColorParamAttribute>() is not null)
		{
			return "color"; // see Preamble
		}

		if (parameter.ParameterType.IsArray && IsParams(parameter))
		{
			// no [] array modifier for varargs
			return GetLuaType(parameter.ParameterType.GetElementType());
		}

		return GetLuaType(parameter.ParameterType);
	}

	private static string GetLuaType(Type type)
	{
		// try this twice, before and after extracting the array/nullable type
		if (TypeConversions.TryGetValue(type, out string luaType))
		{
			return luaType;
		}

		if (type.IsArray)
		{
			return GetLuaType(type.GetElementType()) + "[]";
		}

		if (IsNullable(type))
		{
			type = type.GetGenericArguments()[0];
		}

		if (TypeConversions.TryGetValue(type, out luaType))
		{
			return luaType;
		}

		throw new NotSupportedException($"Unknown type {type.FullName} used in API. Generator must be updated to handle this.");
	}

	private static bool IsNullable(Type type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);

	private static bool IsParams(ParameterInfo parameter) => parameter.GetCustomAttribute<ParamArrayAttribute>() is not null;
}
