using System;
using System.IO;
using System.Reflection;

namespace BizHawk.Tests
{
	public static class EmbeddedData
	{
		private static readonly Assembly Asm = typeof(EmbeddedData).Assembly;

		public static Stream GetStream(string group, string embedPath)
		{
			var fullPath = $"BizHawk.Tests.data.{group}.{embedPath}";
			return Asm.GetManifestResourceStream(fullPath) ?? throw new InvalidOperationException($"Could not find the embedded resource {fullPath}");
		}
	}
}
