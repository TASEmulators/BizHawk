using System;
using System.Linq;
using System.Reflection;

using BizHawk.Common;

namespace BizHawk.Client.EmuHawk
{
	public static class ReflectionCache
	{
		private static readonly Lazy<Type[]> _types = new Lazy<Type[]>(() => Asm.GetTypesWithoutLoadErrors().ToArray());

		public static readonly Assembly Asm = typeof(ReflectionCache).Assembly;

		public static Type[] Types => _types.Value;
	}
}
