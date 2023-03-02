namespace BizHawk.Analyzers;

public static class Extensions
{
	public static string RemovePrefix(this string str, string prefix)
		=> str.StartsWith(prefix) ? str.Substring(prefix.Length, str.Length - prefix.Length) : str;

	public static void SwapReferences<T>(ref T a, ref T b)
	{
		ref T c = ref a; // using var results in CS8619 warning?
		a = ref b;
		b = ref c;
	}
}
