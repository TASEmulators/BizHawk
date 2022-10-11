namespace BizHawk.Analyzers;

using System;
using System.Collections.Generic;

public static class Extensions
{
	public static T? FirstOrNull<T>(this IEnumerable<T> list, Func<T, bool> predicate)
		where T : struct
	{
		foreach (var e in list) if (predicate(e)) return e;
		return null;
	}

	public static string RemovePrefix(this string str, string prefix)
		=> str.StartsWith(prefix) ? str.Substring(prefix.Length, str.Length - prefix.Length) : str;
}
