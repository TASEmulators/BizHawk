// ===========================================================================
// BizHawkRafaelia - Teste de Mesa (Logical Algorithm Testing) Module
// ===========================================================================
// 
// FORK PARENT: BizHawk by TASEmulators (https://github.com/TASEmulators/BizHawk)
// FORK MAINTAINER: Rafael Melo Reis (https://github.com/rafaelmeloreisnovo/BizHawkRafaelia)
// 
// Purpose: Comprehensive logical algorithm validation and testing framework
// Implements: Teste de Mesa methodology for bug detection and mitigation
// ZIPRAF_OMEGA: ψχρΔΣΩ operational loop compliance
// ===========================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

namespace BizHawk.Rafaelia.Core
{
	/// <summary>
	/// Implements comprehensive "Teste de Mesa" (desk testing) methodology
	/// for logical algorithm validation and bug detection.
	/// </summary>
	public static class TesteDeMesaValidator
	{
		private static readonly List<ValidationResult> _validationResults = new();
		private static int _testCounter = 0;

		/// <summary>
		/// Validates array bounds before access (prevents IndexOutOfRangeException)
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool ValidateArrayBounds<T>(T[] array, int index, string context = "")
		{
			_testCounter++;
			
			if (array == null)
			{
				LogValidation(false, ValidationCategory.NullReference, 
					$"Array is null in context: {context}", index);
				return false;
			}

			if (index < 0 || index >= array.Length)
			{
				LogValidation(false, ValidationCategory.ArrayBounds, 
					$"Index {index} out of bounds [0, {array.Length}) in context: {context}", index);
				return false;
			}

			return true;
		}

		/// <summary>
		/// Validates division operations (prevents DivideByZeroException)
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool ValidateDivision(double divisor, string context = "")
		{
			_testCounter++;

			if (System.Math.Abs(divisor) < double.Epsilon)
			{
				LogValidation(false, ValidationCategory.DivisionByZero,
					$"Division by zero in context: {context}", (int)divisor);
				return false;
			}

			if (double.IsNaN(divisor) || double.IsInfinity(divisor))
			{
				LogValidation(false, ValidationCategory.InvalidValue,
					$"Invalid divisor (NaN/Infinity) in context: {context}", (int)divisor);
				return false;
			}

			return true;
		}

		/// <summary>
		/// Validates integer division operations
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool ValidateDivision(int divisor, string context = "")
		{
			_testCounter++;

			if (divisor == 0)
			{
				LogValidation(false, ValidationCategory.DivisionByZero,
					$"Division by zero in context: {context}", divisor);
				return false;
			}

			return true;
		}

		/// <summary>
		/// Validates null references (prevents NullReferenceException)
		/// </summary>
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool ValidateNotNull<T>(T? obj, string context = "") where T : class
		{
			_testCounter++;

			if (obj == null)
			{
				LogValidation(false, ValidationCategory.NullReference,
					$"Null reference in context: {context}", 0);
				return false;
			}

			return true;
		}

		/// <summary>
		/// Validates arithmetic overflow potential
		/// </summary>
		public static bool ValidateOverflow(long value, long operand, ArithmeticOperation operation, string context = "")
		{
			_testCounter++;

			try
			{
				checked
				{
					_ = operation switch
					{
						ArithmeticOperation.Add => value + operand,
						ArithmeticOperation.Subtract => value - operand,
						ArithmeticOperation.Multiply => value * operand,
						_ => value
					};
				}
				return true;
			}
			catch (OverflowException)
			{
				LogValidation(false, ValidationCategory.ArithmeticOverflow,
					$"Overflow in {operation}: {value} op {operand} in context: {context}", (int)value);
				return false;
			}
		}

		/// <summary>
		/// Validates pointer operations for safety
		/// </summary>
		public static unsafe bool ValidatePointer(void* ptr, string context = "")
		{
			_testCounter++;

			if (ptr == null)
			{
				LogValidation(false, ValidationCategory.NullPointer,
					$"Null pointer in context: {context}", 0);
				return false;
			}

			return true;
		}

		/// <summary>
		/// Validates collection iteration state
		/// </summary>
		public static bool ValidateCollectionState<T>(IEnumerable<T> collection, string context = "")
		{
			_testCounter++;

			if (collection == null)
			{
				LogValidation(false, ValidationCategory.NullReference,
					$"Collection is null in context: {context}", 0);
				return false;
			}

			// Check if collection was modified during iteration
			try
			{
				var _ = collection.Count();
				return true;
			}
			catch (InvalidOperationException)
			{
				LogValidation(false, ValidationCategory.CollectionModified,
					$"Collection modified during iteration in context: {context}", 0);
				return false;
			}
		}

		/// <summary>
		/// Validates memory allocation size
		/// </summary>
		public static bool ValidateAllocationSize(long size, long maxSize, string context = "")
		{
			_testCounter++;

			if (size < 0)
			{
				LogValidation(false, ValidationCategory.InvalidValue,
					$"Negative allocation size {size} in context: {context}", (int)size);
				return false;
			}

			if (size > maxSize)
			{
				LogValidation(false, ValidationCategory.MemoryExceeded,
					$"Allocation size {size} exceeds maximum {maxSize} in context: {context}", (int)size);
				return false;
			}

			return true;
		}

		/// <summary>
		/// Validates thread-safe operation
		/// </summary>
		public static bool ValidateThreadSafety(object lockObject, string context = "")
		{
			_testCounter++;

			if (lockObject == null)
			{
				LogValidation(false, ValidationCategory.NullReference,
					$"Lock object is null in context: {context}", 0);
				return false;
			}

			if (!System.Threading.Monitor.IsEntered(lockObject))
			{
				LogValidation(false, ValidationCategory.ThreadSafety,
					$"Lock not acquired in context: {context}", 0);
				return false;
			}

			return true;
		}

		/// <summary>
		/// Validates state machine transition
		/// </summary>
		public static bool ValidateStateTransition<TState>(TState currentState, TState targetState, 
			HashSet<(TState, TState)> allowedTransitions, string context = "") where TState : struct
		{
			_testCounter++;

			if (allowedTransitions == null || !allowedTransitions.Contains((currentState, targetState)))
			{
				LogValidation(false, ValidationCategory.InvalidState,
					$"Invalid state transition from {currentState} to {targetState} in context: {context}", 0);
				return false;
			}

			return true;
		}

		/// <summary>
		/// Validates lag/latency threshold
		/// </summary>
		public static bool ValidateLatency(long elapsedMs, long thresholdMs, string context = "")
		{
			_testCounter++;

			if (elapsedMs > thresholdMs)
			{
				LogValidation(false, ValidationCategory.PerformanceLag,
					$"Operation exceeded latency threshold: {elapsedMs}ms > {thresholdMs}ms in context: {context}", 
					(int)elapsedMs);
				return false;
			}

			return true;
		}

		/// <summary>
		/// Measures and validates operation timing
		/// </summary>
		public static bool ValidateOperationTiming(Action operation, long maxMilliseconds, string context = "")
		{
			_testCounter++;
			var sw = Stopwatch.StartNew();

			try
			{
				operation();
				sw.Stop();

				return ValidateLatency(sw.ElapsedMilliseconds, maxMilliseconds, context);
			}
			catch (Exception ex)
			{
				sw.Stop();
				LogValidation(false, ValidationCategory.Exception,
					$"Exception during timed operation in context: {context} - {ex.Message}", 
					(int)sw.ElapsedMilliseconds);
				return false;
			}
		}

		/// <summary>
		/// Logs validation result
		/// </summary>
		private static void LogValidation(bool passed, ValidationCategory category, string message, int value)
		{
			var result = new ValidationResult
			{
				TestId = _testCounter,
				Passed = passed,
				Category = category,
				Message = message,
				Value = value,
				Timestamp = DateTime.UtcNow
			};

			_validationResults.Add(result);

			// In debug mode, write to debug output
			#if DEBUG
			if (!passed)
			{
				Debug.WriteLine($"[TESTE DE MESA] FAILED #{result.TestId}: {category} - {message}");
			}
			#endif
		}

		/// <summary>
		/// Gets validation summary report
		/// </summary>
		public static ValidationSummary GetValidationSummary()
		{
			var grouped = _validationResults.GroupBy(r => r.Category)
				.Select(g => new
				{
					Category = g.Key,
					Total = g.Count(),
					Failed = g.Count(r => !r.Passed)
				})
				.ToList();

			return new ValidationSummary
			{
				TotalTests = _testCounter,
				TotalFailures = _validationResults.Count(r => !r.Passed),
				FailuresByCategory = grouped.ToDictionary(x => x.Category, x => x.Failed),
				Results = _validationResults.ToList()
			};
		}

		/// <summary>
		/// Clears validation history
		/// </summary>
		public static void ClearValidationHistory()
		{
			_validationResults.Clear();
			_testCounter = 0;
		}

		/// <summary>
		/// Validation result record
		/// </summary>
		public struct ValidationResult
		{
			public int TestId { get; set; }
			public bool Passed { get; set; }
			public ValidationCategory Category { get; set; }
			public string Message { get; set; }
			public int Value { get; set; }
			public DateTime Timestamp { get; set; }
		}

		/// <summary>
		/// Validation summary
		/// </summary>
		public struct ValidationSummary
		{
			public int TotalTests { get; set; }
			public int TotalFailures { get; set; }
			public Dictionary<ValidationCategory, int> FailuresByCategory { get; set; }
			public List<ValidationResult> Results { get; set; }
		}

		/// <summary>
		/// Validation categories for bug classification
		/// </summary>
		public enum ValidationCategory
		{
			NullReference,
			ArrayBounds,
			DivisionByZero,
			InvalidValue,
			ArithmeticOverflow,
			NullPointer,
			CollectionModified,
			MemoryExceeded,
			ThreadSafety,
			InvalidState,
			PerformanceLag,
			Exception
		}

		/// <summary>
		/// Arithmetic operations for overflow validation
		/// </summary>
		public enum ArithmeticOperation
		{
			Add,
			Subtract,
			Multiply
		}
	}
}
