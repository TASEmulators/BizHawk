#nullable disable

using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace BizHawk.Common.ReflectionExtensions
{
	/// <summary>
	/// Reflection based helper methods
	/// </summary>
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
		/// Gets the description attribute from an object
		/// </summary>
		public static string GetDescription(this object obj)
		{
			var type = obj.GetType();

			var memInfo = type.GetMember(obj.ToString());

			if (memInfo.Length > 0)
			{
				var attrs = memInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);

				if (attrs.Length > 0)
				{
					return ((DescriptionAttribute)attrs[0]).Description;
				}
			}

			return obj.ToString();
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

		/// <summary>
		/// Gets the description attribute from a type
		/// </summary>
		public static string Description(this Type type)
		{
			var descriptions = (DescriptionAttribute[])
			type.GetCustomAttributes(typeof(DescriptionAttribute), false);

			if (descriptions.Length == 0)
			{
				return "";
			}

			return descriptions[0].Description;
		}

		/// <summary>
		/// Gets an enum from a description attribute
		/// </summary>
		/// <param name="description">The description attribute value</param>
		/// <typeparam name="T">The type of the enum</typeparam>
		/// <returns>An enum value with the given description attribute, if no suitable description is found then a default value of the enum is returned</returns>
		/// <exception cref="InvalidOperationException"><typeparamref name="T"/> does not inherit <see cref="Enum"/></exception>
		/// <remarks>implementation from https://stackoverflow.com/a/4367868/7467292</remarks>
		public static T GetEnumFromDescription<T>(this string description)
		{
			var type = typeof(T);
			if (!type.IsEnum)
			{
				throw new InvalidOperationException();
			}

			foreach (var field in type.GetFields())
			{
				var attribute = Attribute.GetCustomAttribute(field,
					typeof(DescriptionAttribute)) as DescriptionAttribute;
				if (attribute != null)
				{
					if (attribute.Description == description)
					{
						return (T)field.GetValue(null);
					}
				}
				else
				{
					if (field.Name == description)
					{
						return (T)field.GetValue(null);
					}
				}
			}

			return default(T);
		}

		/// <summary>
		/// Takes an enum Type and generates a list of strings from the description attributes
		/// </summary>
		public static IEnumerable<string> GetEnumDescriptions(this Type type)
		{
			var vals = Enum.GetValues(type);

			foreach (var v in vals)
			{
				yield return v.GetDescription();
			}
		}

		public static T GetAttribute<T>(this object o)
		{
			return (T)o.GetType().GetCustomAttributes(typeof(T), false)[0];
		}
	}
}
