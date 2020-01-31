using System;
using System.Runtime.CompilerServices;

namespace BizHawk.Experiment.AutoGenConfig
{
	internal static class ScopingExtensions
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static T Also<T>(this T receiver, Action<T> action) where T : notnull
		{
			action(receiver);
			return receiver;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void Let<T>(this T receiver, Action<T> func) where T : notnull => func(receiver);

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static TReturn Let<TRec, TReturn>(this TRec receiver, Func<TRec, TReturn> func) where TRec : notnull => func(receiver);
	}
}
