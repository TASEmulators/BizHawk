using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Vulkan.UnixXlib
{
	public class VulkanControl : UserControl
	{
		public Instance Instance;
		public SurfaceKhr Surface;

		public VulkanControl(Instance instance = null) : base()
		{
			if (instance == null)
				CreateDefaultInstance();
			else
				Instance = instance;
		}

#if DEBUG
		protected void CreateDefaultInstance()
		{
			var layerProperties = Commands.EnumerateInstanceLayerProperties();

			var layersToEnable = layerProperties.Any(l => l.LayerName == "VK_LAYER_LUNARG_standard_validation")
				? new[] { "VK_LAYER_LUNARG_standard_validation" }
				: new string [0];

			Instance = new Instance(
				new InstanceCreateInfo
				{
					EnabledExtensionNames = new string[]
						{ "VK_KHR_surface", "VK_KHR_win32_surface", "VK_EXT_debug_report" },
					EnabledLayerNames = layersToEnable,
					ApplicationInfo = new ApplicationInfo
					{
						ApiVersion = Vulkan.Version.Make(1, 0, 0)
					}
				});

			Instance.EnableDebug(DebugCallback);
		}

		private Bool32 DebugCallback(
			DebugReportFlagsExt flags,
			DebugReportObjectTypeExt objectType,
			ulong objectHandle,
			IntPtr location,
			int messageCode,
			IntPtr layerPrefix,
			IntPtr message,
			IntPtr userData)
		{
			Debug.WriteLine($"{flags}: {Marshal.PtrToStringAnsi(message)}");
			return true;
		}

#else
		protected void CreateDefaultInstance ()
		{
			Instance = new Instance (new InstanceCreateInfo () {
				EnabledExtensionNames = new string [] { "VK_KHR_surface", "VK_KHR_win32_surface" },
				ApplicationInfo = new ApplicationInfo () {
					ApiVersion = Vulkan.Version.Make (1, 0, 0)
				}
			});
		}
#endif

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			Surface = Instance.CreateXlibSurfaceKHR(
				new XlibSurfaceCreateInfoKhr
				{
					Hwnd = Handle,
					Hinstance = Process.GetCurrentProcess().Handle
				});
		}
	}
}