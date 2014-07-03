using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;

namespace BizHawk.Common.ReflectionExtensions
{
	/// <summary>
	/// Reflection based helper methods
	/// </summary>
	public static class ReflectionExtensions
	{
		/// <summary>
		/// Gets the description attribute from an object
		/// </summary>
		public static string GetDescription(this object obj)
		{
			Type type = obj.GetType();

			var memInfo = type.GetMember(obj.ToString());

			if (memInfo != null && memInfo.Length > 0)
			{
				object[] attrs = memInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);

				if (attrs != null && attrs.Length > 0)
				{
					return ((DescriptionAttribute)attrs[0]).Description;
				}
			}

			return obj.ToString();
		}

		/// <summary>
		/// Gets the description attribute from a type
		/// </summary>
		/// <returns></returns>
		public static string Description(this Type type)
		{
			var descriptions = (DescriptionAttribute[])
			type.GetCustomAttributes(typeof(DescriptionAttribute), false);

			if (descriptions.Length == 0)
			{
				return string.Empty;
			}

			return descriptions[0].Description;
		}

		/// <summary>
		/// Takes an object and determines if it has methodName as a public method
		/// </summary>
		/// <returns>Returns whether or not the obj both contains the method name and the method is public</returns>
		public static bool HasExposedMethod(this object obj, string methodName)
		{
			var method = obj.GetType().GetMethod(methodName);

			if (method != null)
			{
				return method.IsPublic;
			}

			return false;
		}

		/// <summary>
		/// Takes an object and invokes the method
		/// The method must exist and be public
		/// </summary>
		/// <returns>The return value of the method, as an object.  
		/// If the method returns void, the return value is null
		/// If the method does not exist or is not public, it returns null
		/// </returns>
		public static object InvokeMethod(this object obj, string methodName, object[] args)
		{
			var method = obj.GetType().GetMethod(methodName);
			if (method != null && method.IsPublic)
			{
				return method.Invoke(obj, args);
			}

			return null;
		}

		public static bool HasPublicProperty(this object obj, string propertyName)
		{
			var property = obj.GetType().GetProperty(propertyName);

			if (property != null)
			{
				return property.CanRead;
			}

			return false;
		}

		public static object GetPropertyValue(this object obj, string propertyName)
		{
			var property = obj.GetType().GetProperty(propertyName);
			if (property != null && property.CanRead)
			{
				return property.GetValue(obj, null);
			}

			return null;
		}
	}
}
