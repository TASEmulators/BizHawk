using System;
using System.Buffers;
using System.Runtime.CompilerServices;

namespace BizHawk.Common
{
	/// <summary>
	/// Optimized buffer pool for reducing memory allocations in performance-critical paths.
	/// Uses ArrayPool to minimize GC pressure and improve emulation performance.
	/// </summary>
	public static class OptimizedBufferPool
	{
		/// <summary>
		/// Rents a byte array from the shared pool with a minimum size.
		/// The returned array may be larger than requested.
		/// </summary>
		/// <param name="minimumLength">Minimum size required</param>
		/// <returns>Pooled byte array</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static byte[] RentByteArray(int minimumLength)
		{
			return ArrayPool<byte>.Shared.Rent(minimumLength);
		}

		/// <summary>
		/// Returns a byte array to the shared pool.
		/// Arrays should not be used after being returned.
		/// </summary>
		/// <param name="array">Array to return to pool</param>
		/// <param name="clearArray">Whether to clear the array before returning</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ReturnByteArray(byte[] array, bool clearArray = false)
		{
			ArrayPool<byte>.Shared.Return(array, clearArray);
		}

		/// <summary>
		/// Rents an int array from the shared pool with a minimum size.
		/// The returned array may be larger than requested.
		/// </summary>
		/// <param name="minimumLength">Minimum size required</param>
		/// <returns>Pooled int array</returns>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static int[] RentIntArray(int minimumLength)
		{
			return ArrayPool<int>.Shared.Rent(minimumLength);
		}

		/// <summary>
		/// Returns an int array to the shared pool.
		/// Arrays should not be used after being returned.
		/// </summary>
		/// <param name="array">Array to return to pool</param>
		/// <param name="clearArray">Whether to clear the array before returning</param>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static void ReturnIntArray(int[] array, bool clearArray = false)
		{
			ArrayPool<int>.Shared.Return(array, clearArray);
		}

		/// <summary>
		/// Executes an action with a rented byte buffer, automatically returning it afterwards.
		/// Ensures proper cleanup even if exceptions occur.
		/// </summary>
		/// <param name="minimumLength">Minimum buffer size required</param>
		/// <param name="action">Action to execute with the buffer</param>
		public static void WithByteBuffer(int minimumLength, Action<byte[]> action)
		{
			var buffer = ArrayPool<byte>.Shared.Rent(minimumLength);
			try
			{
				action(buffer);
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(buffer);
			}
		}

		/// <summary>
		/// Executes a function with a rented byte buffer, automatically returning it afterwards.
		/// Ensures proper cleanup even if exceptions occur.
		/// </summary>
		/// <typeparam name="T">Return type</typeparam>
		/// <param name="minimumLength">Minimum buffer size required</param>
		/// <param name="func">Function to execute with the buffer</param>
		/// <returns>Result from the function</returns>
		public static T WithByteBuffer<T>(int minimumLength, Func<byte[], T> func)
		{
			var buffer = ArrayPool<byte>.Shared.Rent(minimumLength);
			try
			{
				return func(buffer);
			}
			finally
			{
				ArrayPool<byte>.Shared.Return(buffer);
			}
		}

		/// <summary>
		/// Executes an action with a rented int buffer, automatically returning it afterwards.
		/// Ensures proper cleanup even if exceptions occur.
		/// </summary>
		/// <param name="minimumLength">Minimum buffer size required</param>
		/// <param name="action">Action to execute with the buffer</param>
		public static void WithIntBuffer(int minimumLength, Action<int[]> action)
		{
			var buffer = ArrayPool<int>.Shared.Rent(minimumLength);
			try
			{
				action(buffer);
			}
			finally
			{
				ArrayPool<int>.Shared.Return(buffer);
			}
		}

		/// <summary>
		/// Executes a function with a rented int buffer, automatically returning it afterwards.
		/// Ensures proper cleanup even if exceptions occur.
		/// </summary>
		/// <typeparam name="T">Return type</typeparam>
		/// <param name="minimumLength">Minimum buffer size required</param>
		/// <param name="func">Function to execute with the buffer</param>
		/// <returns>Result from the function</returns>
		public static T WithIntBuffer<T>(int minimumLength, Func<int[], T> func)
		{
			var buffer = ArrayPool<int>.Shared.Rent(minimumLength);
			try
			{
				return func(buffer);
			}
			finally
			{
				ArrayPool<int>.Shared.Return(buffer);
			}
		}
	}
}
