using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using Vulkan;
using Vulkan.Windows;

using Version = Vulkan.Version;

namespace BizHawk.Bizware.BizwareGL.Drivers.Vulkan
{
	internal class GLControlWrapper_Vulkan : VulkanControl, IGraphicsControl
	{
		public GLControlWrapper_Vulkan(IGL_Vulkan owner): base(new Instance(VkCommonInstInfo))
		{
			PhysDevice = Array.Find(Instance.EnumeratePhysicalDevices(), d => d.GetProperties().DeviceType == PhysicalDeviceType.DiscreteGpu);
			if (PhysDevice == null) throw new InvalidOperationException("no Vulkan-capable device available");
			Instance.EnableDebug(DebugReportCallback);
			Owner = owner;
			Console.WriteLine(Version.ToString(PhysDevice.GetProperties().ApiVersion));
		}

		private static readonly InstanceCreateInfo VkCommonInstInfo = new InstanceCreateInfo
		{
			ApplicationInfo = new ApplicationInfo {
				ApiVersion = Version.Make(1, 0, 0),
				ApplicationName = "EmuHawk Vulkan renderer"
			},
			EnabledExtensionNames = new[] {
				"VK_EXT_debug_report",
				"VK_KHR_surface"
			}
//			EnabledLayerNames = new[] {
//				"VK_LAYER_GOOGLE_threading",
//				"VK_LAYER_GOOGLE_unique_objects",
//				"VK_LAYER_LUNARG_core_validation",
//				"VK_LAYER_LUNARG_device_limits",
//				"VK_LAYER_LUNARG_image",
//				"VK_LAYER_LUNARG_object_tracker",
//				"VK_LAYER_LUNARG_parameter_validation",
//				"VK_LAYER_LUNARG_swapchain"
//			}
		};

		private static Bool32 DebugReportCallback(DebugReportFlagsExt flags, DebugReportObjectTypeExt objectType, ulong objectHandle, IntPtr location, int messageCode, IntPtr layerPrefix, IntPtr message, IntPtr userData)
		{
			Console.WriteLine($"DebugReport layer: {Marshal.PtrToStringAnsi(layerPrefix)} message: {Marshal.PtrToStringAnsi(message)}");
			return false;
		}

		private readonly IGL_Vulkan Owner;
		private readonly PhysicalDevice PhysDevice;
		private readonly SampleVkConsumer VkConsumer = new SampleVkConsumer();

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
//			VkConsumer.Initialize(PhysDevice, Surface);
		}

//		protected override void OnPaint(PaintEventArgs e)
//		{
//			base.OnPaint(e);
//			VkConsumer.DrawFrame();
//		}

		public void SetVsync(bool state)
		{
			//TODO
		}

		public void Begin()
		{
//			OnLoad(EventArgs.Empty);
		}

		public void End()
		{
			Owner.MakeDefaultCurrent();
		}

		public void SwapBuffers()
		{
			//TODO
		}
	}
}
