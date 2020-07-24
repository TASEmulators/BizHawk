#nullable disable

using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace BizHawk.Common.ReflectionExtensions
{
	public static class ReflectionExtensions
	{
		public static IEnumerable<PropertyInfo> GetPropertiesWithAttrib(this Type type, Type attributeType)
		{
			return type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
				.Where(p => p.GetCustomAttributes(attributeType, false).Length > 0);
		}

		public static IEnumerable<MethodInfo> GetMethodsWithAttrib(this Type type, Type attributeType)
		{
			return type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
				.Where(p => p.GetCustomAttributes(attributeType, false).Length > 0);
		}

		/// <summary>
		/// Returns the DisplayName attribute value if it exists, else the name of the class
		/// </summary>
		public static string DisplayName(this Type type)
		{
			var displayName = (DisplayNameAttribute)type
				.GetCustomAttributes(typeof(DisplayNameAttribute), false)
				.FirstOrDefault();

			if (displayName != null)
			{
				return displayName.DisplayName;
			}

			return type.Name;
		}
	}
}
