/*
 * taken from .NET 9 source, MIT-licensed
 * specifically https://github.com/dotnet/runtime/blob/v9.0.0/src/libraries/System.Private.CoreLib/src/System/MemoryExtensions.cs
 * and https://github.com/dotnet/runtime/blob/v9.0.0/src/libraries/System.Private.CoreLib/src/System/String.Manipulation.cs
 * and https://github.com/dotnet/runtime/blob/v9.0.0/src/libraries/System.Private.CoreLib/src/System/Collections/Generic/ValueListBuilder.cs
 */

#if !NET8_0_OR_GREATER
#pragma warning disable RS0030 // `Debug.Assert` w/o message, breaks BizHawk convention
#pragma warning disable SA1514 // "Element documentation header should be preceded by blank line"

using System.Buffers;
using System.Diagnostics;
using System.Runtime.CompilerServices;
#if NETCOREAPP3_0_OR_GREATER
using System.Runtime.Intrinsics;
#endif

namespace BizHawk.Common
{
	public static class MemoryExtensionsBackports
	{
		/// <summary>
		/// Returns a type that allows for enumeration of each element within a split span
		/// using the provided separator character.
		/// </summary>
		/// <typeparam name="T">The type of the elements.</typeparam>
		/// <param name="source">The source span to be enumerated.</param>
		/// <param name="separator">The separator character to be used to split the provided span.</param>
		/// <returns>Returns a <see cref="SpanSplitEnumerator{T}"/>.</returns>
		public static SpanSplitEnumerator<T> Split<T>(this ReadOnlySpan<T> source, T separator) where T : IEquatable<T> =>
			new SpanSplitEnumerator<T>(source, separator);

		/// <summary>
		/// Returns a type that allows for enumeration of each element within a split span
		/// using the provided separator span.
		/// </summary>
		/// <typeparam name="T">The type of the elements.</typeparam>
		/// <param name="source">The source span to be enumerated.</param>
		/// <param name="separator">The separator span to be used to split the provided span.</param>
		/// <returns>Returns a <see cref="SpanSplitEnumerator{T}"/>.</returns>
		public static SpanSplitEnumerator<T> Split<T>(this ReadOnlySpan<T> source, ReadOnlySpan<T> separator) where T : IEquatable<T> =>
			new SpanSplitEnumerator<T>(source, separator, treatAsSingleSeparator: true);

		/// <summary>
		/// Returns a type that allows for enumeration of each element within a split span
		/// using any of the provided elements.
		/// </summary>
		/// <typeparam name="T">The type of the elements.</typeparam>
		/// <param name="source">The source span to be enumerated.</param>
		/// <param name="separators">The separators to be used to split the provided span.</param>
		/// <returns>Returns a <see cref="SpanSplitEnumerator{T}"/>.</returns>
		/// <remarks>
		/// If <typeparamref name="T"/> is <see cref="char"/> and if <paramref name="separators"/> is empty,
		/// all Unicode whitespace characters are used as the separators. This matches the behavior of when
		/// <see cref="string.Split(char[])"/> and related overloads are used with an empty separator array,
		/// or when <see cref="SplitAny(ReadOnlySpan{char}, Span{Range}, ReadOnlySpan{char}, StringSplitOptions)"/>
		/// is used with an empty separator span.
		/// </remarks>
		public static SpanSplitEnumerator<T> SplitAny<T>(this ReadOnlySpan<T> source, /*[UnscopedRef] params*/ ReadOnlySpan<T> separators) where T : IEquatable<T> =>
			new SpanSplitEnumerator<T>(source, separators);

		/// <summary>
		/// Parses the source <see cref="ReadOnlySpan{Char}"/> for the specified <paramref name="separator"/>, populating the <paramref name="destination"/> span
		/// with <see cref="Range"/> instances representing the regions between the separators.
		/// </summary>
		/// <param name="source">The source span to parse.</param>
		/// <param name="destination">The destination span into which the resulting ranges are written.</param>
		/// <param name="separator">A character that delimits the regions in this instance.</param>
		/// <param name="options">A bitwise combination of the enumeration values that specifies whether to trim whitespace and include empty ranges.</param>
		/// <returns>The number of ranges written into <paramref name="destination"/>.</returns>
		/// <remarks>
		/// <para>
		/// Delimiter characters are not included in the elements of the returned array.
		/// </para>
		/// <para>
		/// If the <paramref name="destination"/> span is empty, or if the <paramref name="options"/> specifies <see cref="StringSplitOptions.RemoveEmptyEntries"/> and <paramref name="source"/> is empty,
		/// no ranges are written to the destination.
		/// </para>
		/// <para>
		/// If the span does not contain <paramref name="separator"/>, or if <paramref name="destination"/>'s length is 1, a single range will be output containing the entire <paramref name="source"/>,
		/// subject to the processing implied by <paramref name="options"/>.
		/// </para>
		/// <para>
		/// If there are more regions in <paramref name="source"/> than will fit in <paramref name="destination"/>, the first <paramref name="destination"/> length minus 1 ranges are
		/// stored in <paramref name="destination"/>, and a range for the remainder of <paramref name="source"/> is stored in <paramref name="destination"/>.
		/// </para>
		/// </remarks>
		public static int Split(this ReadOnlySpan<char> source, Span<Range> destination, char separator, StringSplitOptions options = StringSplitOptions.None)
		{
			CheckStringSplitOptions(options);

			return SplitCore(source, destination, stackalloc[] { separator }, default, isAny: true, options);
		}

		/// <summary>
		/// Parses the source <see cref="ReadOnlySpan{Char}"/> for the specified <paramref name="separator"/>, populating the <paramref name="destination"/> span
		/// with <see cref="Range"/> instances representing the regions between the separators.
		/// </summary>
		/// <param name="source">The source span to parse.</param>
		/// <param name="destination">The destination span into which the resulting ranges are written.</param>
		/// <param name="separator">A character that delimits the regions in this instance.</param>
		/// <param name="options">A bitwise combination of the enumeration values that specifies whether to trim whitespace and include empty ranges.</param>
		/// <returns>The number of ranges written into <paramref name="destination"/>.</returns>
		/// <remarks>
		/// <para>
		/// Delimiter characters are not included in the elements of the returned array.
		/// </para>
		/// <para>
		/// If the <paramref name="destination"/> span is empty, or if the <paramref name="options"/> specifies <see cref="StringSplitOptions.RemoveEmptyEntries"/> and <paramref name="source"/> is empty,
		/// no ranges are written to the destination.
		/// </para>
		/// <para>
		/// If the span does not contain <paramref name="separator"/>, or if <paramref name="destination"/>'s length is 1, a single range will be output containing the entire <paramref name="source"/>,
		/// subject to the processing implied by <paramref name="options"/>.
		/// </para>
		/// <para>
		/// If there are more regions in <paramref name="source"/> than will fit in <paramref name="destination"/>, the first <paramref name="destination"/> length minus 1 ranges are
		/// stored in <paramref name="destination"/>, and a range for the remainder of <paramref name="source"/> is stored in <paramref name="destination"/>.
		/// </para>
		/// </remarks>
		public static int Split(this ReadOnlySpan<char> source, Span<Range> destination, ReadOnlySpan<char> separator, StringSplitOptions options = StringSplitOptions.None)
		{
			CheckStringSplitOptions(options);

			// If the separator is an empty string, the whole input is considered the sole range.
			if (separator.IsEmpty)
			{
				if (!destination.IsEmpty)
				{
					int startInclusive = 0, endExclusive = source.Length;

					if (startInclusive != endExclusive || (options & StringSplitOptions.RemoveEmptyEntries) == 0)
					{
						destination[0] = startInclusive..endExclusive;
						return 1;
					}
				}

				return 0;
			}

			return SplitCore(source, destination, separator, default, isAny: false, options);
		}

		/// <summary>
		/// Parses the source <see cref="ReadOnlySpan{Char}"/> for one of the specified <paramref name="separators"/>, populating the <paramref name="destination"/> span
		/// with <see cref="Range"/> instances representing the regions between the separators.
		/// </summary>
		/// <param name="source">The source span to parse.</param>
		/// <param name="destination">The destination span into which the resulting ranges are written.</param>
		/// <param name="separators">Any number of characters that may delimit the regions in this instance. If empty, all Unicode whitespace characters are used as the separators.</param>
		/// <param name="options">A bitwise combination of the enumeration values that specifies whether to trim whitespace and include empty ranges.</param>
		/// <returns>The number of ranges written into <paramref name="destination"/>.</returns>
		/// <remarks>
		/// <para>
		/// Delimiter characters are not included in the elements of the returned array.
		/// </para>
		/// <para>
		/// If the <paramref name="destination"/> span is empty, or if the <paramref name="options"/> specifies <see cref="StringSplitOptions.RemoveEmptyEntries"/> and <paramref name="source"/> is empty,
		/// no ranges are written to the destination.
		/// </para>
		/// <para>
		/// If the span does not contain any of the <paramref name="separators"/>, or if <paramref name="destination"/>'s length is 1, a single range will be output containing the entire <paramref name="source"/>,
		/// subject to the processing implied by <paramref name="options"/>.
		/// </para>
		/// <para>
		/// If there are more regions in <paramref name="source"/> than will fit in <paramref name="destination"/>, the first <paramref name="destination"/> length minus 1 ranges are
		/// stored in <paramref name="destination"/>, and a range for the remainder of <paramref name="source"/> is stored in <paramref name="destination"/>.
		/// </para>
		/// </remarks>
		public static int SplitAny(this ReadOnlySpan<char> source, Span<Range> destination, ReadOnlySpan<char> separators, StringSplitOptions options = StringSplitOptions.None)
		{
			CheckStringSplitOptions(options);

			return SplitCore(source, destination, separators, default, isAny: true, options);
		}

		/// <summary>
		/// Parses the source <see cref="ReadOnlySpan{Char}"/> for one of the specified <paramref name="separators"/>, populating the <paramref name="destination"/> span
		/// with <see cref="Range"/> instances representing the regions between the separators.
		/// </summary>
		/// <param name="source">The source span to parse.</param>
		/// <param name="destination">The destination span into which the resulting ranges are written.</param>
		/// <param name="separators">Any number of strings that may delimit the regions in this instance.  If empty, all Unicode whitespace characters are used as the separators.</param>
		/// <param name="options">A bitwise combination of the enumeration values that specifies whether to trim whitespace and include empty ranges.</param>
		/// <returns>The number of ranges written into <paramref name="destination"/>.</returns>
		/// <remarks>
		/// <para>
		/// Delimiter characters are not included in the elements of the returned array.
		/// </para>
		/// <para>
		/// If the <paramref name="destination"/> span is empty, or if the <paramref name="options"/> specifies <see cref="StringSplitOptions.RemoveEmptyEntries"/> and <paramref name="source"/> is empty,
		/// no ranges are written to the destination.
		/// </para>
		/// <para>
		/// If the span does not contain any of the <paramref name="separators"/>, or if <paramref name="destination"/>'s length is 1, a single range will be output containing the entire <paramref name="source"/>,
		/// subject to the processing implied by <paramref name="options"/>.
		/// </para>
		/// <para>
		/// If there are more regions in <paramref name="source"/> than will fit in <paramref name="destination"/>, the first <paramref name="destination"/> length minus 1 ranges are
		/// stored in <paramref name="destination"/>, and a range for the remainder of <paramref name="source"/> is stored in <paramref name="destination"/>.
		/// </para>
		/// </remarks>
		public static int SplitAny(this ReadOnlySpan<char> source, Span<Range> destination, ReadOnlySpan<string> separators, StringSplitOptions options = StringSplitOptions.None)
		{
			CheckStringSplitOptions(options);

			return SplitCore(source, destination, default, separators!, isAny: true, options);
		}

		/// <summary>Core implementation for all of the Split{Any}AsRanges methods.</summary>
		/// <param name="source">The source span to parse.</param>
		/// <param name="destination">The destination span into which the resulting ranges are written.</param>
		/// <param name="separatorOrSeparators">Either a single separator (one or more characters in length) or multiple individual 1-character separators.</param>
		/// <param name="stringSeparators">Strings to use as separators instead of <paramref name="separatorOrSeparators"/>.</param>
		/// <param name="isAny">true if the separators are a set; false if <paramref name="separatorOrSeparators"/> should be treated as a single separator.</param>
		/// <param name="options">A bitwise combination of the enumeration values that specifies whether to trim whitespace and include empty ranges.</param>
		/// <returns>The number of ranges written into <paramref name="destination"/>.</returns>
		/// <remarks>This implementation matches the various quirks of string.Split.</remarks>
		private static int SplitCore(
			ReadOnlySpan<char> source, Span<Range> destination,
			ReadOnlySpan<char> separatorOrSeparators, ReadOnlySpan<string?> stringSeparators, bool isAny,
			StringSplitOptions options)
		{
			// If the destination is empty, there's nothing to do.
			if (destination.IsEmpty)
			{
				return 0;
			}

			bool keepEmptyEntries = (options & StringSplitOptions.RemoveEmptyEntries) is 0;

			// If the input is empty, then we either return an empty range as the sole range, or if empty entries
			// are to be removed, we return nothing.
			if (source.Length == 0)
			{
				if (keepEmptyEntries)
				{
					destination[0] = default;
					return 1;
				}

				return 0;
			}

			int startInclusive = 0, endExclusive;

			// If the destination has only one slot, then we need to return the whole input, subject to the options.
			if (destination.Length == 1)
			{
				endExclusive = source.Length;
				if (startInclusive != endExclusive || keepEmptyEntries)
				{
					destination[0] = startInclusive..endExclusive;
					return 1;
				}

				return 0;
			}

			const int StackallocIntBufferSizeLimit = 128;
			scoped ValueListBuilder<int> separatorList = new ValueListBuilder<int>(stackalloc int[StackallocIntBufferSizeLimit]);
			scoped ValueListBuilder<int> lengthList = default;

			int separatorLength;
			int rangeCount = 0;
			if (!stringSeparators.IsEmpty)
			{
				lengthList = new ValueListBuilder<int>(stackalloc int[StackallocIntBufferSizeLimit]);
				MakeSeparatorListAny(source, stringSeparators, ref separatorList, ref lengthList);
				separatorLength = -1; // Will be set on each iteration of the loop
			}
			else if (isAny)
			{
				MakeSeparatorListAny(source, separatorOrSeparators, ref separatorList);
				separatorLength = 1;
			}
			else
			{
				MakeSeparatorList(source, separatorOrSeparators, ref separatorList);
				separatorLength = separatorOrSeparators.Length;
			}

			// Try to fill in all but the last slot in the destination.  The last slot is reserved for whatever remains
			// after the last discovered separator. If the options specify that empty entries are to be removed, then we
			// need to skip past all of those here as well, including any that occur at the beginning of the last entry,
			// which is why we enter the loop if remove empty entries is set, even if we've already added enough entries.
			int separatorIndex = 0;
			Span<Range> destinationMinusOne = destination.Slice(0, destination.Length - 1);
			while (separatorIndex < separatorList.Length && (rangeCount < destinationMinusOne.Length || !keepEmptyEntries))
			{
				endExclusive = separatorList[separatorIndex];
				if (separatorIndex < lengthList.Length)
				{
					separatorLength = lengthList[separatorIndex];
				}
				separatorIndex++;

				// Trim off whitespace from the start and end of the range.
				int untrimmedEndEclusive = endExclusive;

				// If the range is not empty or we're not ignoring empty ranges, store it.
				Debug.Assert(startInclusive <= endExclusive);
				if (startInclusive != endExclusive || keepEmptyEntries)
				{
					// If we're not keeping empty entries, we may have entered the loop even if we'd
					// already written enough ranges.  Now that we know this entry isn't empty, we
					// need to validate there's still room remaining.
					if ((uint)rangeCount >= (uint)destinationMinusOne.Length)
					{
						break;
					}

					destinationMinusOne[rangeCount] = startInclusive..endExclusive;
					rangeCount++;
				}

				// Reset to be just past the separator, and loop around to go again.
				startInclusive = untrimmedEndEclusive + separatorLength;
			}

			separatorList.Dispose();
			lengthList.Dispose();

			// Either we found at least destination.Length - 1 ranges or we didn't find any more separators.
			// If we still have a last destination slot available and there's anything left in the source,
			// put a range for the remainder of the source into the destination.
			if ((uint)rangeCount < (uint)destination.Length)
			{
				endExclusive = source.Length;
				if (startInclusive != endExclusive || keepEmptyEntries)
				{
					destination[rangeCount] = startInclusive..endExclusive;
					rangeCount++;
				}
			}

			// Return how many ranges were written.
			return rangeCount;
		}

		/// <summary>Updates the starting and ending markers for a range to exclude whitespace.</summary>
		private static (int StartInclusive, int EndExclusive) TrimSplitEntry(ReadOnlySpan<char> source, int startInclusive, int endExclusive)
		{
			while (startInclusive < endExclusive && char.IsWhiteSpace(source[startInclusive]))
			{
				startInclusive++;
			}

			while (endExclusive > startInclusive && char.IsWhiteSpace(source[endExclusive - 1]))
			{
				endExclusive--;
			}

			return (startInclusive, endExclusive);
		}

		/// <summary>
		/// Enables enumerating each split within a <see cref="ReadOnlySpan{T}"/> that has been divided using one or more separators.
		/// </summary>
		public ref struct SpanSplitEnumerator<T> where T : IEquatable<T>
		{
			/// <summary>The input span being split.</summary>
			private readonly ReadOnlySpan<T> _span;

			/// <summary>A single separator to use when <see cref="_splitMode"/> is <see cref="SpanSplitEnumeratorMode.SingleElement"/>.</summary>
			private readonly T _separator = default!;
			/// <summary>
			/// A separator span to use when <see cref="_splitMode"/> is <see cref="SpanSplitEnumeratorMode.Sequence"/> (in which case
			/// it's treated as a single separator) or <see cref="SpanSplitEnumeratorMode.Any"/> (in which case it's treated as a set of separators).
			/// </summary>
			private readonly ReadOnlySpan<T> _separatorBuffer;

			/// <summary>Mode that dictates how the instance was configured and how its fields should be used in <see cref="MoveNext"/>.</summary>
			private SpanSplitEnumeratorMode _splitMode;
			/// <summary>The inclusive starting index in <see cref="_span"/> of the current range.</summary>
			private int _startCurrent = 0;
			/// <summary>The exclusive ending index in <see cref="_span"/> of the current range.</summary>
			private int _endCurrent = 0;
			/// <summary>The index in <see cref="_span"/> from which the next separator search should start.</summary>
			private int _startNext = 0;

			/// <summary>Gets an enumerator that allows for iteration over the split span.</summary>
			/// <returns>Returns a <see cref="SpanSplitEnumerator{T}"/> that can be used to iterate over the split span.</returns>
			public SpanSplitEnumerator<T> GetEnumerator() => this;

			/// <summary>Gets the current element of the enumeration.</summary>
			/// <returns>Returns a <see cref="Range"/> instance that indicates the bounds of the current element withing the source span.</returns>
			public Range Current => new Range(_startCurrent, _endCurrent);

			/// <summary>Initializes the enumerator for <see cref="SpanSplitEnumeratorMode.Any"/>.</summary>
			internal SpanSplitEnumerator(ReadOnlySpan<T> span, ReadOnlySpan<T> separators)
			{
				_span = span;
				{
					_separatorBuffer = separators;
					_splitMode = SpanSplitEnumeratorMode.Any;
				}
			}

			/// <summary>Initializes the enumerator for <see cref="SpanSplitEnumeratorMode.Sequence"/> (or <see cref="SpanSplitEnumeratorMode.EmptySequence"/> if the separator is empty).</summary>
			/// <remarks><paramref name="treatAsSingleSeparator"/> must be true.</remarks>
			internal SpanSplitEnumerator(ReadOnlySpan<T> span, ReadOnlySpan<T> separator, bool treatAsSingleSeparator)
			{
				Debug.Assert(treatAsSingleSeparator, "Should only ever be called as true; exists to differentiate from separators overload");

				_span = span;
				_separatorBuffer = separator;
				_splitMode = separator.Length == 0 ?
					SpanSplitEnumeratorMode.EmptySequence :
					SpanSplitEnumeratorMode.Sequence;
			}

			/// <summary>Initializes the enumerator for <see cref="SpanSplitEnumeratorMode.SingleElement"/>.</summary>
			internal SpanSplitEnumerator(ReadOnlySpan<T> span, T separator)
			{
				_span = span;
				_separator = separator;
				_splitMode = SpanSplitEnumeratorMode.SingleElement;
			}

			/// <summary>
			/// Advances the enumerator to the next element of the enumeration.
			/// </summary>
			/// <returns><see langword="true"/> if the enumerator was successfully advanced to the next element; <see langword="false"/> if the enumerator has passed the end of the enumeration.</returns>
			public bool MoveNext()
			{
				// Search for the next separator index.
				int separatorIndex, separatorLength;
				switch (_splitMode)
				{
					case SpanSplitEnumeratorMode.None:
						return false;

					case SpanSplitEnumeratorMode.SingleElement:
						separatorIndex = _span.Slice(_startNext).IndexOf(_separator);
						separatorLength = 1;
						break;

					case SpanSplitEnumeratorMode.Any:
						separatorIndex = _span.Slice(_startNext).IndexOfAny(_separatorBuffer);
						separatorLength = 1;
						break;

					case SpanSplitEnumeratorMode.Sequence:
						separatorIndex = _span.Slice(_startNext).IndexOf(_separatorBuffer);
						separatorLength = _separatorBuffer.Length;
						break;

					case SpanSplitEnumeratorMode.EmptySequence:
						separatorIndex = -1;
						separatorLength = 1;
						break;

					default:
						Debug.Assert(false, $"Unknown split mode: {_splitMode}");
						return default;
				}

				_startCurrent = _startNext;
				if (separatorIndex >= 0)
				{
					_endCurrent = _startCurrent + separatorIndex;
					_startNext = _endCurrent + separatorLength;
				}
				else
				{
					_startNext = _endCurrent = _span.Length;

					// Set _splitMode to None so that subsequent MoveNext calls will return false.
					_splitMode = SpanSplitEnumeratorMode.None;
				}

				return true;
			}
		}

		/// <summary>Indicates in which mode <see cref="SpanSplitEnumerator{T}"/> is operating, with regards to how it should interpret its state.</summary>
		private enum SpanSplitEnumeratorMode
		{
			/// <summary>Either a default <see cref="SpanSplitEnumerator{T}"/> was used, or the enumerator has finished enumerating and there's no more work to do.</summary>
			None = 0,

			/// <summary>A single T separator was provided.</summary>
			SingleElement,

			/// <summary>A span of separators was provided, each of which should be treated independently.</summary>
			Any,

			/// <summary>The separator is a span of elements to be treated as a single sequence.</summary>
			Sequence,

			/// <summary>The separator is an empty sequence, such that no splits should be performed.</summary>
			EmptySequence,
		}

		private static void CheckStringSplitOptions(StringSplitOptions options)
		{
			const StringSplitOptions AllValidFlags = StringSplitOptions.RemoveEmptyEntries;

			if ((options & ~AllValidFlags) != 0)
			{
				// at least one invalid flag was set
				// either someone cast a random number to an enum, or this copy of the extensions are being used in modern .NET and the new `TrimEntries` flag was set
				throw new ArgumentException(paramName: nameof(options), message: "Value of flags is invalid.");
			}
		}

		/// <summary>
		/// Uses ValueListBuilder to create list that holds indexes of separators in string.
		/// </summary>
		/// <param name="source">The source to parse.</param>
		/// <param name="separators"><see cref="ReadOnlySpan{T}"/> of separator chars</param>
		/// <param name="sepListBuilder"><see cref="ValueListBuilder{T}"/> to store indexes</param>
		private static void MakeSeparatorListAny(ReadOnlySpan<char> source, ReadOnlySpan<char> separators, ref ValueListBuilder<int> sepListBuilder)
		{
			// Special-case no separators to mean any whitespace is a separator.
			if (separators.Length == 0)
			{
				for (int i = 0; i < source.Length; i++)
				{
					if (char.IsWhiteSpace(source[i]))
					{
						sepListBuilder.Append(i);
					}
				}
			}

			// Special-case the common cases of 1, 2, and 3 separators, with manual comparisons against each separator.
			else if (separators.Length <= 3)
			{
				char sep0, sep1, sep2;
				sep0 = separators[0];
				sep1 = separators.Length > 1 ? separators[1] : sep0;
				sep2 = separators.Length > 2 ? separators[2] : sep1;
#if NETCOREAPP3_0_OR_GREATER
				if (Vector128.IsHardwareAccelerated && source.Length >= Vector128<ushort>.Count * 2)
				{
					MakeSeparatorListVectorized(source, ref sepListBuilder, sep0, sep1, sep2);
					return;
				}
#endif

				for (int i = 0; i < source.Length; i++)
				{
					char c = source[i];
					if (c == sep0 || c == sep1 || c == sep2)
					{
						sepListBuilder.Append(i);
					}
				}
			}

			// Handle > 3 separators with a probabilistic map, ala IndexOfAny.
			// This optimizes for chars being unlikely to match a separator.
			else
			{
#if true // `ProbabilisticMap` is `internal` :(
				for (int i = 0; i < source.Length; i++)
				{
					char c = source[i];
					foreach (var sep in separators) if (sep == c)
					{
						sepListBuilder.Append(i);
					}
				}
#else
				unsafe
				{
					var map = new ProbabilisticMap(separators);
					ref uint charMap = ref Unsafe.As<ProbabilisticMap, uint>(ref map);

					for (int i = 0; i < source.Length; i++)
					{
						if (ProbabilisticMap.Contains(ref charMap, separators, source[i]))
						{
							sepListBuilder.Append(i);
						}
					}
				}
#endif
			}
		}

#if NETCOREAPP3_0_OR_GREATER
		private static void MakeSeparatorListVectorized(ReadOnlySpan<char> sourceSpan, ref ValueListBuilder<int> sepListBuilder, char c, char c2, char c3)
		{
			// Redundant test so we won't prejit remainder of this method
			// on platforms where it is not supported
			if (!Vector128.IsHardwareAccelerated)
			{
				throw new PlatformNotSupportedException();
			}
			Debug.Assert(sourceSpan.Length >= Vector128<ushort>.Count);
			nuint lengthToExamine = (uint)sourceSpan.Length;
			nuint offset = 0;
			ref char source = ref MemoryMarshal.GetReference(sourceSpan);

			if (Vector512.IsHardwareAccelerated && lengthToExamine >= (uint)Vector512<ushort>.Count*2)
			{
				Vector512<ushort> v1 = Vector512.Create((ushort)c);
				Vector512<ushort> v2 = Vector512.Create((ushort)c2);
				Vector512<ushort> v3 = Vector512.Create((ushort)c3);

				do
				{
					Vector512<ushort> vector = Vector512.LoadUnsafe(ref source, offset);
					Vector512<ushort> v1Eq = Vector512.Equals(vector, v1);
					Vector512<ushort> v2Eq = Vector512.Equals(vector, v2);
					Vector512<ushort> v3Eq = Vector512.Equals(vector, v3);
					Vector512<byte> cmp = (v1Eq | v2Eq | v3Eq).AsByte();

					if (cmp != Vector512<byte>.Zero)
					{
						// Skip every other bit
						ulong mask = cmp.ExtractMostSignificantBits() & 0x5555555555555555;
						do
						{
							uint bitPos = (uint)BitOperations.TrailingZeroCount(mask) / sizeof(char);
							sepListBuilder.Append((int)(offset + bitPos));
							mask = BitOperations.ResetLowestSetBit(mask);
						} while (mask != 0);
					}

					offset += (nuint)Vector512<ushort>.Count;
				} while (offset <= lengthToExamine - (nuint)Vector512<ushort>.Count);
			}
			else if (Vector256.IsHardwareAccelerated && lengthToExamine >= (uint)Vector256<ushort>.Count*2)
			{
				Vector256<ushort> v1 = Vector256.Create((ushort)c);
				Vector256<ushort> v2 = Vector256.Create((ushort)c2);
				Vector256<ushort> v3 = Vector256.Create((ushort)c3);

				do
				{
					Vector256<ushort> vector = Vector256.LoadUnsafe(ref source, offset);
					Vector256<ushort> v1Eq = Vector256.Equals(vector, v1);
					Vector256<ushort> v2Eq = Vector256.Equals(vector, v2);
					Vector256<ushort> v3Eq = Vector256.Equals(vector, v3);
					Vector256<byte> cmp = (v1Eq | v2Eq | v3Eq).AsByte();

					if (cmp != Vector256<byte>.Zero)
					{
						// Skip every other bit
						uint mask = cmp.ExtractMostSignificantBits() & 0x55555555;
						do
						{
							uint bitPos = (uint)BitOperations.TrailingZeroCount(mask) / sizeof(char);
							sepListBuilder.Append((int)(offset + bitPos));
							mask = BitOperations.ResetLowestSetBit(mask);
						} while (mask != 0);
					}

					offset += (nuint)Vector256<ushort>.Count;
				} while (offset <= lengthToExamine - (nuint)Vector256<ushort>.Count);
			}
			else if (Vector128.IsHardwareAccelerated)
			{
				Vector128<ushort> v1 = Vector128.Create((ushort)c);
				Vector128<ushort> v2 = Vector128.Create((ushort)c2);
				Vector128<ushort> v3 = Vector128.Create((ushort)c3);

				do
				{
					Vector128<ushort> vector = Vector128.LoadUnsafe(ref source, offset);
					Vector128<ushort> v1Eq = Vector128.Equals(vector, v1);
					Vector128<ushort> v2Eq = Vector128.Equals(vector, v2);
					Vector128<ushort> v3Eq = Vector128.Equals(vector, v3);
					Vector128<byte> cmp = (v1Eq | v2Eq | v3Eq).AsByte();

					if (cmp != Vector128<byte>.Zero)
					{
						// Skip every other bit
						uint mask = cmp.ExtractMostSignificantBits() & 0x5555;
						do
						{
							uint bitPos = (uint)BitOperations.TrailingZeroCount(mask) / sizeof(char);
							sepListBuilder.Append((int)(offset + bitPos));
							mask = BitOperations.ResetLowestSetBit(mask);
						} while (mask != 0);
					}

					offset += (nuint)Vector128<ushort>.Count;
				} while (offset <= lengthToExamine - (nuint)Vector128<ushort>.Count);
			}

			while (offset < lengthToExamine)
			{
				char curr = Unsafe.Add(ref source, offset);
				if (curr == c || curr == c2 || curr == c3)
				{
					sepListBuilder.Append((int)offset);
				}
				offset++;
			}
		}
#endif

		/// <summary>
		/// Uses ValueListBuilder to create list that holds indexes of separators in string.
		/// </summary>
		/// <param name="source">The source to parse.</param>
		/// <param name="separator">separator string</param>
		/// <param name="sepListBuilder"><see cref="ValueListBuilder{T}"/> to store indexes</param>
		private static void MakeSeparatorList(ReadOnlySpan<char> source, ReadOnlySpan<char> separator, ref ValueListBuilder<int> sepListBuilder)
		{
			Debug.Assert(!separator.IsEmpty, "Empty separator");

			int i = 0;
			while (!source.IsEmpty)
			{
				int index = source.IndexOf(separator);
				if (index < 0)
				{
					break;
				}

				i += index;
				sepListBuilder.Append(i);

				i += separator.Length;
				source = source.Slice(index + separator.Length);
			}
		}

		/// <summary>
		/// Uses ValueListBuilder to create list that holds indexes of separators in string and list that holds length of separator strings.
		/// </summary>
		/// <param name="source">The source to parse.</param>
		/// <param name="separators">separator strngs</param>
		/// <param name="sepListBuilder"><see cref="ValueListBuilder{T}"/> for separator indexes</param>
		/// <param name="lengthListBuilder"><see cref="ValueListBuilder{T}"/> for separator length values</param>
		private static void MakeSeparatorListAny(ReadOnlySpan<char> source, ReadOnlySpan<string?> separators, ref ValueListBuilder<int> sepListBuilder, ref ValueListBuilder<int> lengthListBuilder)
		{
			Debug.Assert(!separators.IsEmpty, "Zero separators");

			for (int i = 0; i < source.Length; i++)
			{
				for (int j = 0; j < separators.Length; j++)
				{
					string? separator = separators[j];
					if (string.IsNullOrEmpty(separator))
					{
						continue;
					}
					int currentSepLength = separator!.Length;
					if (source[i] == separator[0] && currentSepLength <= source.Length - i)
					{
						if (currentSepLength == 1 || source.Slice(i, currentSepLength).SequenceEqual(separator.AsSpan()))
						{
							sepListBuilder.Append(i);
							lengthListBuilder.Append(currentSepLength);
							i += currentSepLength - 1;
							break;
						}
					}
				}
			}
		}

		private ref partial struct ValueListBuilder<T>
		{
			private Span<T> _span;
			private T[]? _arrayFromPool;
			private int _pos;

			public ValueListBuilder(Span<T> initialSpan)
			{
				_span = initialSpan;
				_arrayFromPool = null;
				_pos = 0;
			}

			public int Length
			{
				get => _pos;
				set
				{
					Debug.Assert(value >= 0);
					Debug.Assert(value <= _span.Length);
					_pos = value;
				}
			}

			public ref T this[int index]
			{
				get
				{
					Debug.Assert(index < _pos);
					return ref _span[index];
				}
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Append(T item)
			{
				int pos = _pos;

				// Workaround for https://github.com/dotnet/runtime/issues/72004
				Span<T> span = _span;
				if ((uint)pos < (uint)span.Length)
				{
					span[pos] = item;
					_pos = pos + 1;
				}
				else
				{
					AddWithResize(item);
				}
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Append(scoped ReadOnlySpan<T> source)
			{
				int pos = _pos;
				Span<T> span = _span;
				if (source.Length == 1 && (uint)pos < (uint)span.Length)
				{
					span[pos] = source[0];
					_pos = pos + 1;
				}
				else
				{
					AppendMultiChar(source);
				}
			}

			[MethodImpl(MethodImplOptions.NoInlining)]
			private void AppendMultiChar(scoped ReadOnlySpan<T> source)
			{
				if ((uint)(_pos + source.Length) > (uint)_span.Length)
				{
					Grow(_span.Length - _pos + source.Length);
				}

				source.CopyTo(_span.Slice(_pos));
				_pos += source.Length;
			}

			public void Insert(int index, scoped ReadOnlySpan<T> source)
			{
				Debug.Assert(index == 0, "Implementation currently only supports index == 0");

				if ((uint)(_pos + source.Length) > (uint)_span.Length)
				{
					Grow(source.Length);
				}

				_span.Slice(0, _pos).CopyTo(_span.Slice(source.Length));
				source.CopyTo(_span);
				_pos += source.Length;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public Span<T> AppendSpan(int length)
			{
				Debug.Assert(length >= 0);

				int pos = _pos;
				Span<T> span = _span;
#pragma warning disable IDE0004
				if ((ulong)(uint)pos + (ulong)(uint)length <= (ulong)(uint)span.Length) // same guard condition as in Span<T>.Slice on 64-bit
#pragma warning restore IDE0004
				{
					_pos = pos + length;
					return span.Slice(pos, length);
				}
				else
				{
					return AppendSpanWithGrow(length);
				}
			}

			[MethodImpl(MethodImplOptions.NoInlining)]
			private Span<T> AppendSpanWithGrow(int length)
			{
				int pos = _pos;
				Grow(_span.Length - pos + length);
				_pos += length;
				return _span.Slice(pos, length);
			}

			// Hide uncommon path
			[MethodImpl(MethodImplOptions.NoInlining)]
			private void AddWithResize(T item)
			{
				Debug.Assert(_pos == _span.Length);
				int pos = _pos;
				Grow(1);
				_span[pos] = item;
				_pos = pos + 1;
			}

			public ReadOnlySpan<T> AsSpan()
			{
				return _span.Slice(0, _pos);
			}

			public bool TryCopyTo(Span<T> destination, out int itemsWritten)
			{
				if (_span.Slice(0, _pos).TryCopyTo(destination))
				{
					itemsWritten = _pos;
					return true;
				}

				itemsWritten = 0;
				return false;
			}

			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			public void Dispose()
			{
				T[]? toReturn = _arrayFromPool;
				if (toReturn != null)
				{
					_arrayFromPool = null;
					ArrayPool<T>.Shared.Return(toReturn);
				}
			}

			// Note that consuming implementations depend on the list only growing if it's absolutely
			// required.  If the list is already large enough to hold the additional items be added,
			// it must not grow. The list is used in a number of places where the reference is checked
			// and it's expected to match the initial reference provided to the constructor if that
			// span was sufficiently large.
			private void Grow(int additionalCapacityRequired = 1)
			{
				const int ArrayMaxLength = 0x7FFFFFC7; // same as Array.MaxLength

				// Double the size of the span.  If it's currently empty, default to size 4,
				// although it'll be increased in Rent to the pool's minimum bucket size.
				int nextCapacity = Math.Max(_span.Length != 0 ? _span.Length * 2 : 4, _span.Length + additionalCapacityRequired);

				// If the computed doubled capacity exceeds the possible length of an array, then we
				// want to downgrade to either the maximum array length if that's large enough to hold
				// an additional item, or the current length + 1 if it's larger than the max length, in
				// which case it'll result in an OOM when calling Rent below.  In the exceedingly rare
				// case where _span.Length is already int.MaxValue (in which case it couldn't be a managed
				// array), just use that same value again and let it OOM in Rent as well.
				if ((uint)nextCapacity > ArrayMaxLength)
				{
					nextCapacity = Math.Max(Math.Max(_span.Length + 1, ArrayMaxLength), _span.Length);
				}

				T[] array = ArrayPool<T>.Shared.Rent(nextCapacity);
				_span.CopyTo(array);

				T[]? toReturn = _arrayFromPool;
				_span = _arrayFromPool = array;
				if (toReturn != null)
				{
					ArrayPool<T>.Shared.Return(toReturn);
				}
			}
		}
	}
}
#endif
