using Vulkan;
using Vulkan.Windows;

namespace BizHawk.Bizware.BizwareGL.Drivers.Vulkan
{
	internal class GLControlWrapper_Vulkan : VulkanControl, IGraphicsControl
	{
		public GLControlWrapper_Vulkan(IGL_Vulkan owner): base(new Instance(VkCommonInstInfo))
		{
			Owner = owner;
		}

		private static readonly InstanceCreateInfo VkCommonInstInfo = new InstanceCreateInfo()
		{
			EnabledExtensionNames = new[] { "VK_KHR_surface" },
			ApplicationInfo = new ApplicationInfo()
			{
				ApiVersion = Version.Make(1, 0, 0)
			}
		};

		private readonly IGL_Vulkan Owner;

		public void SetVsync(bool state)
		{
			//TODO
		}

		public void Begin()
		{
			//TODO
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
