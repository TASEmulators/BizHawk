using System;

namespace Vulkan.UnixXlib
{
	public class InstanceExtensions
	{
		public static SurfaceKhr CreateXlibSurfaceKHR(this Instance instance, XlibSurfaceCreateInfoKhr pCreateInfo, AllocationCallbacks pAllocator = null)
		{
			Result result;
			SurfaceKhr pSurface;
			unsafe {
				pSurface = new SurfaceKhr();
				fixed (UInt64* ptrpSurface = &pSurface.m) {
					result = NativeMethods.CreateXlibSurfaceKHR(instance.m, pCreateInfo != null ? pCreateInfo.m : (Windows.Interop.Win32SurfaceCreateInfoKhr*)default(IntPtr), pAllocator != null ? pAllocator.m : null, ptrpSurface);
				}
				if (result != Result.Success) throw new ResultException(result);
				return pSurface;
			}
		}
	}
}