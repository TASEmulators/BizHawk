using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace BizHawk.Tests
{
	public static class CustomAssertions
	{
		public static void IsInstanceOfType<T>(this Assert assertObj, object? o)
			=> Assert.IsInstanceOfType(o, typeof(T));
	}
}
