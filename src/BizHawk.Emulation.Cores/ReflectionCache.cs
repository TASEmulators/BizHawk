using System;
using System.IO;
using System.Reflection;

namespace BizHawk.Emulation.Cores
{
	public static class ReflectionCache
	{
		private static readonly Lazy<Type[]> _types = new Lazy<Type[]>(() => Asm.GetTypes());

		public static readonly Assembly Asm = typeof(ReflectionCache).Assembly;

		public static Type[] Types => _types.Value;

		public static Stream EmbeddedResourceStream(string embedPath) => Asm.GetManifestResourceStream($"BizHawk.Emulation.Cores.{embedPath}");
	}
}
