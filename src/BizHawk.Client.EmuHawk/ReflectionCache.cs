using System;
using System.IO;
using System.Linq;
using System.Reflection;

using BizHawk.Common;

namespace BizHawk.Client.EmuHawk
{
	public static class ReflectionCache
	{
		private static readonly Lazy<Type[]> _types = new Lazy<Type[]>(() => Asm.GetTypesWithoutLoadErrors().ToArray());

		private static readonly Assembly Asm = typeof(ReflectionCache).Assembly;

		public static Type[] Types => _types.Value;

		public static Stream EmbeddedResourceStream(string embedPath) => Asm.GetManifestResourceStream($"BizHawk.Client.EmuHawk.{embedPath}");
	}
}
