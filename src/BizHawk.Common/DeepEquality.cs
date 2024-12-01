using System.Collections.Generic;
using System.Reflection;

using BizHawk.Common.StringExtensions;

namespace BizHawk.Common
{
	/// <summary>Annotated fields will not be used by <see cref="DeepEquality"/> for comparison.</summary>
	[AttributeUsage(AttributeTargets.Field)]
	public sealed class DeepEqualsIgnoreAttribute : Attribute
	{
	}

	public static class DeepEquality
	{
		/// <summary>
		/// return true if an array type is not 0-based
		/// </summary>
		private static bool IsNonZeroBasedArray(Type t)
		{
			if (!t.IsArray)
			{
				throw new InvalidOperationException();
			}

			// is there a better way to do this?  i couldn't find any documentation...
			return t.ToString().ContainsOrdinal('*');
		}

		/// <summary>
		/// return all instance fields of a type
		/// </summary>
		public static IEnumerable<FieldInfo> GetAllFields(Type? t)
		{
			while (t != null)
			{
				// GetFields() will not return inherited private fields, so walk the inheritance hierachy
				foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly))
				{
					if (f.GetCustomAttributes(typeof(DeepEqualsIgnoreAttribute), true).Length == 0)
					{
						yield return f;
					}
				}

				t = t.BaseType;
			}
		}

		/// <summary>
		/// test if two arrays are equal in contents; arrays should have same type
		/// </summary>
		private static bool ArrayEquals<T>(T[] o1, T[] o2)
		{
			if (o1.Length != o2.Length)
			{
				return false;
			}

			for (int i = 0; i < o1.Length; i++)
			{
				if (!DeepEquals(o1[i], o2[i]))
				{
					return false;
				}
			}

			return true;
		}

		private static readonly MethodInfo ArrayEqualsGeneric = typeof(DeepEquality).GetMethod(nameof(ArrayEquals), BindingFlags.NonPublic | BindingFlags.Static)!;

		/// <summary>test if two objects <paramref name="o1"/> and <paramref name="o2"/> are equal, field-by-field (with deep inspection of each field)</summary>
		/// <exception cref="InvalidOperationException"><paramref name="o1"/> is an array with rank > 1 or is a non-zero-indexed array</exception>
		public static bool DeepEquals(object? o1, object? o2)
		{
			if (o1 == o2)
			{
				// reference equal, so nothing else to be done
				return true;
			}
			if (o1 == null || o2 == null) return false; // not equal (above) and one is null

			Type t1 = o1.GetType();
			Type t2 = o2.GetType();
			if (t1 != t2)
			{
				return false;
			}
			if (t1.IsArray)
			{
				// it's not too hard to support thesse if needed
				if (t1.GetArrayRank() > 1 || IsNonZeroBasedArray(t1))
				{
					throw new InvalidOperationException("Complex arrays not supported");
				}

				// this is actually pretty fast; it allows using fast ldelem and stelem opcodes on
				// arbitrary array types without emitting custom IL
				var method = ArrayEqualsGeneric.MakeGenericMethod(new Type[] { t1.GetElementType() });
				return (bool)method.Invoke(null, new[] { o1, o2 });
			}

			if (t1.IsPrimitive)
			{
				return o1.Equals(o2);
			}
			
			foreach (var field in GetAllFields(t1))
			{
				if (!DeepEquals(field.GetValue(o1), field.GetValue(o2)))
				{
					return false;
				}
			}

			return true;
		}
	}
}
