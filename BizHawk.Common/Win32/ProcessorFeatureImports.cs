#nullable disable

using System.Runtime.InteropServices;

namespace BizHawk.Common
{
	/// <remarks>used by commented-out code in LibretroApi ctor, don't delete</remarks>
	public static class ProcessorFeatureImports
	{
		[DllImport("kernel32.dll")]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool IsProcessorFeaturePresent(ProcessorFeature processorFeature);

		public enum ProcessorFeature : uint
		{
			/// <summary>The MMX instruction set is available</summary>
			InstructionsMMXAvailable = 3,
			/// <summary>The SSE instruction set is available</summary>
			InstructionsXMMIAvailable = 6,
			/// <summary>The SSE2 instruction set is available</summary>
			InstructionsXMMI64Available = 10,
			/// <summary>The SSE3 instruction set is available. (This feature is not supported until Windows Vista)</summary>
			InstructionsSSE3Available = 13
		}
	}
}
