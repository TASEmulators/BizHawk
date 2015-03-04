using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace BizHawk.Common
{
	/// <summary>
	/// causes DeepEquality to ignore this field when determining equality
	/// </summary>
	[AttributeUsage(AttributeTargets.Field)]
	public class DeepEqualsIgnoreAttribute : Attribute
	{
	}

	public class DeepEquality
	{
		/// <summary>
		/// return true if an array type is not 0-based
		/// </summary>
		/// <param name="t"></param>
		/// <returns></returns>
		private static bool IsNonZeroBaedArray(Type t)
		{
			if (!t.IsArray)
				throw new InvalidOperationException();
			// is there a better way to do this?  i couldn't find any documentation...
			return t.ToString().Contains('*');
		}

		/// <summary>
		/// return all instance fields of a type
		/// </summary>
		/// <param name="t"></param>
		/// <returns></returns>
		public static IEnumerable<FieldInfo> GetAllFields(Type t)
		{
			while (t != null)
			{
				// GetFields() will not return inherited private fields, so walk the inheritance hierachy
				foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly))
				{
					if (f.GetCustomAttributes(typeof(DeepEqualsIgnoreAttribute), true).Length == 0)
						yield return f;
				}
				t = t.BaseType;
			}
		}

		/// <summary>
		/// test if two arrays are equal in contents; arrays should have same type
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="o1"></param>
		/// <param name="o2"></param>
		/// <returns></returns>
		private static bool ArrayEquals<T>(T[] o1, T[] o2)
		{
			if (o1.Length != o2.Length)
			{
				return false;
			}
			for (int i = 0; i < o1.Length; i++)
			{
				if (!DeepEquals(o1[i], o2[i]))
					return false;
			}
			return true;
		}
		static MethodInfo ArrayEqualsGeneric = typeof(DeepEquality).GetMethod("ArrayEquals", BindingFlags.NonPublic | BindingFlags.Static);

		/// <summary>
		/// test if two objects are equal field by field (with deep inspection of each field)
		/// </summary>
		/// <param name="o1"></param>
		/// <param name="o2"></param>
		/// <returns></returns>
		public static bool DeepEquals(object o1, object o2)
		{
			if (o1 == o2)
			{
				// reference equal, so nothing else to be done
				return true;
			}
			Type t1 = o1.GetType();
			Type t2 = o2.GetType();
			if (t1 != t2)
			{
				return false;
			}
			if (t1.IsArray)
			{
				// it's not too hard to support thesse if needed
				if (t1.GetArrayRank() > 1 || IsNonZeroBaedArray(t1))
					throw new InvalidOperationException("Complex arrays not supported");

				// this is actually pretty fast; it allows using fast ldelem and stelem opcodes on
				// arbitrary array types without emitting custom IL
				var method = ArrayEqualsGeneric.MakeGenericMethod(new Type[] { t1.GetElementType() });
				return (bool)method.Invoke(null, new object[] { o1, o2 });
			}
			else if (t1.IsPrimitive)
			{
				return o1.Equals(o2);
			}
			else
			{
				foreach (var field in GetAllFields(t1))
				{
					if (!DeepEquals(field.GetValue(o1), field.GetValue(o2)))
						return false;
				}
				return true;
			}
		}

	}
}
