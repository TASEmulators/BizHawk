using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel;
using System.Reflection;

namespace BizHawk.Common
{
	public static class EnumHelper
	{
		public static string GetDescription(object en)
		{
			Type type = en.GetType();

			var memInfo = type.GetMember(en.ToString());

			if (memInfo != null && memInfo.Length > 0)
			{
				object[] attrs = memInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);

				if (attrs != null && attrs.Length > 0)
				{
					return ((DescriptionAttribute)attrs[0]).Description;
				}
			}

			return en.ToString();
		}

		/// <summary>
		/// Gets an enum from a description attribute
		/// </summary>
		/// <typeparam name="T">The type of the enum</typeparam>
		/// <param name="description">The description attribute value</param>
		/// <returns>An enum value with the given description attribute, if no suitable description is found then a default value of the enum is returned</returns>
		/// <remarks>http://stackoverflow.com/questions/4367723/get-enum-from-description-attribute</remarks>
		public static T GetValueFromDescription<T>(this string description)
		{
			var type = typeof(T);
			if (!type.IsEnum) throw new InvalidOperationException();
			foreach (var field in type.GetFields())
			{
				var attribute = Attribute.GetCustomAttribute(field,
					typeof(DescriptionAttribute)) as DescriptionAttribute;
				if (attribute != null)
				{
					if (attribute.Description == description)
						return (T)field.GetValue(null);
				}
				else
				{
					if (field.Name == description)
						return (T)field.GetValue(null);
				}
			}

			return default(T);
		}

		public static IEnumerable<string> GetDescriptions<T>()
		{
			var vals = Enum.GetValues(typeof(T));

			foreach (var v in vals)
			{
				yield return GetDescription(v);
			}
		}
	}
}