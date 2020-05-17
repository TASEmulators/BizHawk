using System;

namespace BizHawk.Common.StringExtensions
{
	public static class SubstringExtensions
	{
		/// <returns><paramref name="str"/> with the first char removed, or the original <paramref name="str"/> if the first char of <paramref name="str"/> is not <paramref name="prefix"/></returns>
		public static string RemovePrefix(this string str, char prefix) => str.RemovePrefix(prefix, notFoundValue: str);

		/// <returns><paramref name="str"/> with the first char removed, or <paramref name="notFoundValue"/> if the first char of <paramref name="str"/> is not <paramref name="prefix"/></returns>
		public static string RemovePrefix(this string str, char prefix, string notFoundValue) => str.Length != 0 && str[0] == prefix ? str.Substring(1, str.Length - 1) : notFoundValue;

		/// <returns><paramref name="str"/> with the leading substring <paramref name="prefix"/> removed, or the original <paramref name="str"/> if <paramref name="str"/> does not start with <paramref name="prefix"/></returns>
		public static string RemovePrefix(this string str, string prefix) => str.RemovePrefix(prefix, notFoundValue: str);

		/// <returns><paramref name="str"/> with the leading substring <paramref name="prefix"/> removed, or <paramref name="notFoundValue"/> if <paramref name="str"/> does not start with <paramref name="prefix"/></returns>
		public static string RemovePrefix(this string str, string prefix, string notFoundValue) => str.StartsWith(prefix) ? str.Substring(prefix.Length, str.Length - prefix.Length) : notFoundValue;

		/// <returns><paramref name="str"/> with the first char removed, or <see cref="String.Empty">string.Empty</see> if the first char of <paramref name="str"/> is not <paramref name="prefix"/></returns>
		public static string RemovePrefixOrEmpty(this string str, char prefix) => str.RemovePrefix(prefix, notFoundValue: str);

		/// <returns><paramref name="str"/> with the leading substring <paramref name="prefix"/> removed, or <see cref="String.Empty">string.Empty</see> if <paramref name="str"/> does not start with <paramref name="prefix"/></returns>
		public static string RemovePrefixOrEmpty(this string str, string prefix) => str.RemovePrefix(prefix, notFoundValue: str);

		/// <returns><paramref name="str"/> with the first char removed, or <<see langword="null"/> if the first char of <paramref name="str"/> is not <paramref name="prefix"/></returns>
		public static string? RemovePrefixOrNull(this string str, char prefix) => str.Length != 0 && str[0] == prefix ? str.Substring(1, str.Length - 1) : null;

		/// <returns><paramref name="str"/> with the leading substring <paramref name="prefix"/> removed, or <see langword="null"/> if <paramref name="str"/> does not start with <paramref name="prefix"/></returns>
		public static string? RemovePrefixOrNull(this string str, string prefix) => str.StartsWith(prefix) ? str.Substring(prefix.Length, str.Length - prefix.Length) : null;

		/// <returns><paramref name="str"/> with the last char removed, or the original <paramref name="str"/> if the last char of <paramref name="str"/> is not <paramref name="suffix"/></returns>
		public static string RemoveSuffix(this string str, char suffix) => str.RemoveSuffix(suffix, notFoundValue: str);

		/// <returns><paramref name="str"/> with the last char removed, or <paramref name="notFoundValue"/> if the last char of <paramref name="str"/> is not <paramref name="suffix"/></returns>
		public static string RemoveSuffix(this string str, char suffix, string notFoundValue) => str.Length != 0 && str[str.Length - 1] == suffix ? str.Substring(0, str.Length - 1) : notFoundValue;

		/// <returns><paramref name="str"/> with the trailing substring <paramref name="suffix"/> removed, or the original <paramref name="str"/> if <paramref name="str"/> does not end with <paramref name="suffix"/></returns>
		public static string RemoveSuffix(this string str, string suffix) => str.RemoveSuffix(suffix, notFoundValue: str);

		/// <returns><paramref name="str"/> with the trailing substring <paramref name="suffix"/> removed, or <paramref name="notFoundValue"/> if <paramref name="str"/> does not end with <paramref name="suffix"/></returns>
		public static string RemoveSuffix(this string str, string suffix, string notFoundValue) => str.EndsWith(suffix) ? str.Substring(0, str.Length - suffix.Length) : notFoundValue;

		/// <returns><paramref name="str"/> with the last char removed, or <see cref="String.Empty">string.Empty</see> if the last char of <paramref name="str"/> is not <paramref name="suffix"/></returns>
		public static string RemoveSuffixOrEmpty(this string str, char suffix) => str.RemoveSuffix(suffix, notFoundValue: str);

		/// <returns><paramref name="str"/> with the trailing substring <paramref name="suffix"/> removed, or <see cref="String.Empty">string.Empty</see> if <paramref name="str"/> does not end with <paramref name="suffix"/></returns>
		public static string RemoveSuffixOrEmpty(this string str, string suffix) => str.RemoveSuffix(suffix, notFoundValue: str);

		/// <returns><paramref name="str"/> with the last char removed, or <<see langword="null"/> if the last char of <paramref name="str"/> is not <paramref name="suffix"/></returns>
		public static string? RemoveSuffixOrNull(this string str, char suffix) => str.Length != 0 && str[str.Length - 1] == suffix ? str.Substring(0, str.Length - 1) : null;

		/// <returns><paramref name="str"/> with the trailing substring <paramref name="suffix"/> removed, or <see langword="null"/> if <paramref name="str"/> does not end with <paramref name="suffix"/></returns>
		public static string? RemoveSuffixOrNull(this string str, string suffix) => str.EndsWith(suffix) ? str.Substring(0, str.Length - suffix.Length) : null;

		/// <returns>the substring of <paramref name="str"/> after the first occurrence of <paramref name="delimiter"/>, or the original <paramref name="str"/> if not found</returns>
		public static string SubstringAfter(this string str, char delimiter) => str.SubstringAfter(delimiter, notFoundValue: str);

		/// <returns>the substring of <paramref name="str"/> after the first occurrence of <paramref name="delimiter"/>, or <paramref name="notFoundValue"/> if not found</returns>
		public static string SubstringAfter(this string str, char delimiter, string notFoundValue)
		{
			var index = str.IndexOf(delimiter);
			return index < 0 ? notFoundValue : str.Substring(index + 1, str.Length - index - 1);
		}

		/// <returns>the substring of <paramref name="str"/> after the first occurrence of <paramref name="delimiter"/>, or the original <paramref name="str"/> if not found</returns>
		public static string SubstringAfter(this string str, string delimiter) => str.SubstringAfter(delimiter, notFoundValue: str);

		/// <returns>the substring of <paramref name="str"/> after the first occurrence of <paramref name="delimiter"/>, or <paramref name="notFoundValue"/> if not found</returns>
		public static string SubstringAfter(this string str, string delimiter, string notFoundValue)
		{
			var index = str.IndexOf(delimiter);
			return index < 0 ? notFoundValue : str.Substring(index + delimiter.Length, str.Length - index - delimiter.Length);
		}

		/// <returns>the substring of <paramref name="str"/> after the last occurrence of <paramref name="delimiter"/>, or the original <paramref name="str"/> if not found</returns>
		public static string SubstringAfterLast(this string str, char delimiter) => str.SubstringAfterLast(delimiter, notFoundValue: str);

		/// <returns>the substring of <paramref name="str"/> after the last occurrence of <paramref name="delimiter"/>, or <paramref name="notFoundValue"/> if not found</returns>
		public static string SubstringAfterLast(this string str, char delimiter, string notFoundValue)
		{
			var index = str.LastIndexOf(delimiter);
			return index < 0 ? notFoundValue : str.Substring(index + 1, str.Length - index - 1);
		}

		/// <returns>the substring of <paramref name="str"/> after the last occurrence of <paramref name="delimiter"/>, or the original <paramref name="str"/> if not found</returns>
		public static string SubstringAfterLast(this string str, string delimiter) => str.SubstringAfterLast(delimiter, notFoundValue: str);

		/// <returns>the substring of <paramref name="str"/> after the last occurrence of <paramref name="delimiter"/>, or <paramref name="notFoundValue"/> if not found</returns>
		public static string SubstringAfterLast(this string str, string delimiter, string notFoundValue)
		{
			var index = str.LastIndexOf(delimiter);
			return index < 0 ? notFoundValue : str.Substring(index + delimiter.Length, str.Length - index - delimiter.Length);
		}

		/// <returns>the substring of <paramref name="str"/> after the last occurrence of <paramref name="delimiter"/>, or <see cref="String.Empty">string.Empty</see> if not found</returns>
		public static string SubstringAfterLastOrEmpty(this string str, char delimiter) => str.SubstringAfterLast(delimiter, notFoundValue: str);

		/// <returns>the substring of <paramref name="str"/> after the last occurrence of <paramref name="delimiter"/>, or <see cref="String.Empty">string.Empty</see> if not found</returns>
		public static string SubstringAfterLastOrEmpty(this string str, string delimiter) => str.SubstringAfterLast(delimiter, notFoundValue: str);

		/// <returns>the substring of <paramref name="str"/> after the last occurrence of <paramref name="delimiter"/>, or <see langword="null"/> if not found</returns>
		public static string? SubstringAfterLastOrNull(this string str, char delimiter)
		{
			var index = str.LastIndexOf(delimiter);
			return index < 0 ? null : str.Substring(index + 1, str.Length - index - 1);
		}

		/// <returns>the substring of <paramref name="str"/> after the last occurrence of <paramref name="delimiter"/>, or <see langword="null"/> if not found</returns>
		public static string? SubstringAfterLastOrNull(this string str, string delimiter)
		{
			var index = str.LastIndexOf(delimiter);
			return index < 0 ? null : str.Substring(index + delimiter.Length, str.Length - index - delimiter.Length);
		}

		/// <returns>the substring of <paramref name="str"/> after the first occurrence of <paramref name="delimiter"/>, or <see cref="String.Empty">string.Empty</see> if not found</returns>
		public static string SubstringAfterOrEmpty(this string str, char delimiter) => str.SubstringAfter(delimiter, notFoundValue: str);

		/// <returns>the substring of <paramref name="str"/> after the first occurrence of <paramref name="delimiter"/>, or <see cref="String.Empty">string.Empty</see> if not found</returns>
		public static string SubstringAfterOrEmpty(this string str, string delimiter) => str.SubstringAfter(delimiter, notFoundValue: str);

		/// <returns>the substring of <paramref name="str"/> after the first occurrence of <paramref name="delimiter"/>, or <see langword="null"/> if not found</returns>
		public static string? SubstringAfterOrNull(this string str, char delimiter)
		{
			var index = str.IndexOf(delimiter);
			return index < 0 ? null : str.Substring(index + 1, str.Length - index - 1);
		}

		/// <returns>the substring of <paramref name="str"/> after the first occurrence of <paramref name="delimiter"/>, or <see langword="null"/> if not found</returns>
		public static string? SubstringAfterOrNull(this string str, string delimiter)
		{
			var index = str.IndexOf(delimiter);
			return index < 0 ? null : str.Substring(index + delimiter.Length, str.Length - index - delimiter.Length);
		}

		/// <returns>the substring of <paramref name="str"/> before the first occurrence of <paramref name="delimiter"/>, or the original <paramref name="str"/> if not found</returns>
		public static string SubstringBefore(this string str, char delimiter) => str.SubstringBefore(delimiter, notFoundValue: str);

		/// <returns>the substring of <paramref name="str"/> before the first occurrence of <paramref name="delimiter"/>, or <paramref name="notFoundValue"/> if not found</returns>
		public static string SubstringBefore(this string str, char delimiter, string notFoundValue)
		{
			var index = str.IndexOf(delimiter);
			return index < 0 ? notFoundValue : str.Substring(0, index);
		}

		/// <returns>the substring of <paramref name="str"/> before the first occurrence of <paramref name="delimiter"/>, or the original <paramref name="str"/> if not found</returns>
		public static string SubstringBefore(this string str, string delimiter) => str.SubstringBefore(delimiter, notFoundValue: str);

		/// <returns>the substring of <paramref name="str"/> before the first occurrence of <paramref name="delimiter"/>, or <paramref name="notFoundValue"/> if not found</returns>
		public static string SubstringBefore(this string str, string delimiter, string notFoundValue)
		{
			var index = str.IndexOf(delimiter);
			return index < 0 ? notFoundValue : str.Substring(0, index);
		}

		/// <returns>the substring of <paramref name="str"/> before the last occurrence of <paramref name="delimiter"/>, or the original <paramref name="str"/> if not found</returns>
		public static string SubstringBeforeLast(this string str, char delimiter) => str.SubstringBeforeLast(delimiter, notFoundValue: str);

		/// <returns>the substring of <paramref name="str"/> before the last occurrence of <paramref name="delimiter"/>, or <paramref name="notFoundValue"/> if not found</returns>
		public static string SubstringBeforeLast(this string str, char delimiter, string notFoundValue)
		{
			var index = str.LastIndexOf(delimiter);
			return index < 0 ? notFoundValue : str.Substring(0, index);
		}

		/// <returns>the substring of <paramref name="str"/> before the last occurrence of <paramref name="delimiter"/>, or the original <paramref name="str"/> if not found</returns>
		public static string SubstringBeforeLast(this string str, string delimiter) => str.SubstringBeforeLast(delimiter, notFoundValue: str);

		/// <returns>the substring of <paramref name="str"/> before the last occurrence of <paramref name="delimiter"/>, or <paramref name="notFoundValue"/> if not found</returns>
		public static string SubstringBeforeLast(this string str, string delimiter, string notFoundValue)
		{
			var index = str.LastIndexOf(delimiter);
			return index < 0 ? notFoundValue : str.Substring(0, index);
		}

		/// <returns>the substring of <paramref name="str"/> before the last occurrence of <paramref name="delimiter"/>, or <see cref="String.Empty">string.Empty</see> if not found</returns>
		public static string SubstringBeforeLastOrEmpty(this string str, char delimiter) => str.SubstringBeforeLast(delimiter, notFoundValue: str);

		/// <returns>the substring of <paramref name="str"/> before the last occurrence of <paramref name="delimiter"/>, or <see cref="String.Empty">string.Empty</see> if not found</returns>
		public static string SubstringBeforeLastOrEmpty(this string str, string delimiter) => str.SubstringBeforeLast(delimiter, notFoundValue: str);

		/// <returns>the substring of <paramref name="str"/> before the last occurrence of <paramref name="delimiter"/>, or <see langword="null"/> if not found</returns>
		public static string? SubstringBeforeLastOrNull(this string str, char delimiter)
		{
			var index = str.LastIndexOf(delimiter);
			return index < 0 ? null : str.Substring(0, index);
		}

		/// <returns>the substring of <paramref name="str"/> before the last occurrence of <paramref name="delimiter"/>, or <see langword="null"/> if not found</returns>
		public static string? SubstringBeforeLastOrNull(this string str, string delimiter)
		{
			var index = str.LastIndexOf(delimiter);
			return index < 0 ? null : str.Substring(0, index);
		}

		/// <returns>the substring of <paramref name="str"/> before the first occurrence of <paramref name="delimiter"/>, or <see cref="String.Empty">string.Empty</see> if not found</returns>
		public static string SubstringBeforeOrEmpty(this string str, char delimiter) => str.SubstringBefore(delimiter, notFoundValue: str);

		/// <returns>the substring of <paramref name="str"/> before the first occurrence of <paramref name="delimiter"/>, or <see cref="String.Empty">string.Empty</see> if not found</returns>
		public static string SubstringBeforeOrEmpty(this string str, string delimiter) => str.SubstringBefore(delimiter, notFoundValue: str);

		/// <returns>the substring of <paramref name="str"/> before the first occurrence of <paramref name="delimiter"/>, or <see langword="null"/> if not found</returns>
		public static string? SubstringBeforeOrNull(this string str, char delimiter)
		{
			var index = str.IndexOf(delimiter);
			return index < 0 ? null : str.Substring(0, index);
		}

		/// <returns>the substring of <paramref name="str"/> before the first occurrence of <paramref name="delimiter"/>, or <see langword="null"/> if not found</returns>
		public static string? SubstringBeforeOrNull(this string str, string delimiter)
		{
			var index = str.IndexOf(delimiter);
			return index < 0 ? null : str.Substring(0, index);
		}
	}
}
