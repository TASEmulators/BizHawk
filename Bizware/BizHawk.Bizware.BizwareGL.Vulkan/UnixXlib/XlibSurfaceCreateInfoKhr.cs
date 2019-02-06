namespace Vulkan.UnixXlib
{
	public struct XlibSurfaceCreateInfoKhr
	{
		public StructureType sType;
		public unsafe void* pNext;
		public XlibSurfaceCreateFlagsKHR flags;
		public Display* dpy;
		public Window window;
	}
}