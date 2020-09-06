using System;
using System.Reflection;

namespace BizHawk.Emulation.Cores
{
	public static class ReflectionCache
	{
		private static readonly Lazy<Type[]> _types = new Lazy<Type[]>(() => Asm.GetTypes());

		public static readonly Assembly Asm = typeof(ReflectionCache).Assembly;

		public static Type[] Types => _types.Value;
	}
}
