using System;
using System.Runtime.InteropServices;

using Vulkan;

namespace Vulkan.UnixXlib
{
	public class NativeMethods
	{
		[DllImport("libvulkan.so.1")]
		public static extern unsafe Result CreateXlibSurfaceKHR(IntPtr instance, XlibSurfaceCreateInfoKhr* pCreateInfo, Vulkan.Interop.AllocationCallbacks* pAllocator, UInt64* pSurface);
	}
}