using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.InteropServices;

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
		/// Gets an enum from a description attribute
		/// </summary>
		/// <typeparam name="T">The type of the enum</typeparam>
		/// <param name="description">The description attribute value</param>
		/// <returns>An enum value with the given description attribute, if no suitable description is found then a default value of the enum is returned</returns>
		/// <remarks>http://stackoverflow.com/questions/4367723/get-enum-from-description-attribute</remarks>
		public static T GetEnumFromDescription<T>(this string description)
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

		/// <summary>
		/// Takes an enum Type and generates a list of strings from the description attributes
		/// </summary>
		/// <returns></returns>
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

		/// <summary>
		/// where the fields begin relative to the address an object references points to
		/// </summary>
		public static IntPtr ManagedFieldStart { get { return _managedfieldstart; } }

		[StructLayout(LayoutKind.Explicit)]
		private class Junkus { [FieldOffset(0)]public IntPtr s; }

		static IntPtr _managedfieldstart = GetManagedOffset(typeof(Junkus).GetField("s"));

		/// <summary>
		/// the address of a field relative to the address an object reference of that type points to.  this function is very expensive to call.
		/// </summary>
		/// <param name="field"></param>
		/// <returns></returns>
		public static IntPtr GetManagedOffset(this FieldInfo field)
		{
			Type type = field.DeclaringType;

			var dyn = new System.Reflection.Emit.DynamicMethod(
				"xxz0", typeof(IntPtr), new Type[] { typeof(object) }, typeof(ReflectionExtensions).Module, true);
			var il = dyn.GetILGenerator();

			var pin = il.DeclareLocal(type, true);
			var baseaddr = il.DeclareLocal(typeof(IntPtr));

			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Stloc, pin); // force cast object to type (invalid), and pin

			il.Emit(OpCodes.Ldloc, pin); // base address of reference (points to typeinfo)
			il.Emit(OpCodes.Conv_I); // convert object ref to intptr (invalid)
			il.Emit(OpCodes.Stloc, baseaddr);

			il.Emit(OpCodes.Ldloc, pin);
			il.Emit(OpCodes.Ldflda, field); // address of desired field
			il.Emit(OpCodes.Conv_I); // convert field& to intptr (invalid)
			il.Emit(OpCodes.Ldloc, baseaddr);
			il.Emit(OpCodes.Sub);
			il.Emit(OpCodes.Ret);

			return (IntPtr)dyn.Invoke(null, new object[] { new object() });
		}

		public static bool ThrowsError(this MethodInfo info)
		{
			var il = info.GetMethodBody().GetILAsByteArray();
			return (il[il.Length - 1] == 0x7A);
		}

		public static bool IsEmpty(this MethodInfo info)
		{
			var il = info.GetMethodBody().GetILAsByteArray();
			return (il.Length == 1 && il[0] == 0x2A);
		}
	}
}
