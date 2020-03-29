using System;
using System.Diagnostics;

namespace BizHawk.Emulation.Common
{
	public unsafe class VideoScreen
	{
		public VideoScreen(int* buffer, int width, int height)
		{
			Buffer = buffer;
			Width = width;
			Height = height;
		}

		public int* Buffer { get; }
		public int Width { get; } 
		public int Height { get; }

		public int Length => Width * Height;
	}

	/// <summary>Provides a way to arrange displays inside a frame buffer.</summary>
	public static class ScreenArranger
	{
		/// <remarks>this is taken as an assumption to allow for simpler algorithms; in the future this may need to be rethought (e.g. for 3DS)</remarks>
		[Conditional("DEBUG")]
		private static void DebugAssertScreenDimensionsMatch(int lengthA, int lengthB)
		{
			if (lengthA != lengthB) throw new ArgumentException();
		}

		[Conditional("DEBUG")]
		private static void DebugAssertPreallocatedBufferSize(int expected, int preallocLength)
		{
			if (preallocLength != expected) throw new Exception();
		}

		public static unsafe int[] UprightStack(VideoScreen forTop, VideoScreen forBottom, int gapLineCount = 0)
		{
			DebugAssertScreenDimensionsMatch(forTop.Width, forBottom.Width);
			var outputWidth = forTop.Width;

			var gapStartOffset = forTop.Length;
			var screen2StartOffset = gapStartOffset + gapLineCount * outputWidth;
			var bufferLength = screen2StartOffset + forBottom.Height * outputWidth;
			var prealloc = new int[bufferLength]; //TODO actually take a `ref int[] prealloc` (or an int* maybe?)
			DebugAssertPreallocatedBufferSize(bufferLength, prealloc.Length);

			for (var i = 0; i < gapStartOffset; i++) prealloc[i] = forTop.Buffer[i]; // copy top screen
			// don't bother writing into the gap
			for (int i = 0, l = forBottom.Length; i < l; i++) prealloc[screen2StartOffset + i] = forBottom.Buffer[i]; // copy bottom screen
			return prealloc;
		}

		/// <summary>Simply populates a buffer with a single screen</summary>
		public static unsafe int[] Copy(VideoScreen screen)
		{
			var bufferLength = screen.Length;
			var prealloc = new int[bufferLength]; //TODO actually take a `ref int[] prealloc` (or an int* maybe?)
			DebugAssertPreallocatedBufferSize(bufferLength, prealloc.Length);

			for (var i = 0; i < bufferLength; i++) prealloc[i] = screen.Buffer[i];
			return prealloc;
		}

		public static unsafe int[] UprightSideBySide(VideoScreen forLeft, VideoScreen forRight, int gapLineCount = 0)
		{
			DebugAssertScreenDimensionsMatch(forLeft.Height, forRight.Height);
			var outputHeight = forLeft.Height;

			var rightOffsetHztl = forLeft.Width + gapLineCount;
			var outputWidth = rightOffsetHztl + forRight.Width;
			var bufferLength = outputHeight * outputWidth; // which = `forLeft.Length + outputHeight * gapLineCount + forRight.Length`
			var prealloc = new int[bufferLength]; //TODO actually take a `ref int[] prealloc` (or an int* maybe?)
			DebugAssertPreallocatedBufferSize(bufferLength, prealloc.Length);

			for (var y = 0; y < outputHeight; y++)
			{
				for (int x = 0, w = forLeft.Width; x < w; x++) prealloc[y * outputWidth + x] = forLeft.Buffer[y * w + x]; // copy this row of the left screen
				// don't bother writing into the gap
				for (int x = 0, w = forRight.Width; x < w; x++) prealloc[y * outputWidth + rightOffsetHztl + x] = forRight.Buffer[y * w + x]; // copy this row of the right screen
			}
			return prealloc;
		}

		public static unsafe int[] Rotate90Stack(VideoScreen forLeft, VideoScreen forRight, int gapLineCount = 0)
		{
			DebugAssertScreenDimensionsMatch(forLeft.Width, forRight.Width);
			var outputHeight = forLeft.Width;

			var rightOffsetHztl = forLeft.Height + gapLineCount;
			var outputWidth = rightOffsetHztl + forRight.Height;
			var bufferLength = outputHeight * outputWidth; // which = `forLeft.Length + outputHeight * gapLineCount + forRight.Length`
			var prealloc = new int[bufferLength]; //TODO actually take a `ref int[] prealloc` (or an int* maybe?)
			DebugAssertPreallocatedBufferSize(bufferLength, prealloc.Length);

			for (var y = 0; y < outputHeight; y++)
			{
				for (int x = 0, w = forLeft.Height; x < w; x++) prealloc[y * outputWidth + x] = forLeft.Buffer[(x + 1) * outputHeight - y]; // copy and rotate this column of the top screen to the left of the output
				// don't bother writing into the gap
				for (int x = 0, w = forRight.Height; x < w; x++) prealloc[y * outputWidth + rightOffsetHztl + x] = forRight.Buffer[(x + 1) * outputHeight - y]; // copy and rotate this column of the bottom screen to the right of the output
			}
			return prealloc;
		}

		public static unsafe int[] Rotate270Stack(VideoScreen forLeft, VideoScreen forRight, int gapLineCount = 0)
		{
			DebugAssertScreenDimensionsMatch(forLeft.Width, forRight.Width);
			var outputHeight = forLeft.Width;

			var rightOffsetHztl = forLeft.Height + gapLineCount;
			var outputWidth = rightOffsetHztl + forRight.Height;
			var bufferLength = outputHeight * outputWidth; // which = `forLeft.Length + outputHeight * gapLineCount + forRight.Length`
			var prealloc = new int[bufferLength]; //TODO actually take a `ref int[] prealloc` (or an int* maybe?)
			DebugAssertPreallocatedBufferSize(bufferLength, prealloc.Length);

			for (var y = 0; y < outputHeight; y++)
			{
				for (int x = 0, w = forLeft.Height; x < w; x++) prealloc[y * outputWidth + x] = forLeft.Buffer[(w - x) * outputHeight + y]; // copy and rotate this column of the bottom screen to the left of the output
				// don't bother writing into the gap
				for (int x = 0, w = forRight.Height; x < w; x++) prealloc[y * outputWidth + rightOffsetHztl + x] = forRight.Buffer[(w - x) * outputHeight + y]; // copy and rotate this column of the top screen to the right of the output
			}
			return prealloc;
		}
	}
}
