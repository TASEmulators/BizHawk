using System;
using System.Windows.Forms;

using BizHawk.Common;

using Veldrid;

namespace BizHawk.WinForms.Controls
{
	public static class VeldridHelpers
	{
		public static GraphicsDevice CreateGraphicsDevice(
			IWin32Window window,
			GraphicsBackend preferredBackend,
			GraphicsDeviceOptions options,
			uint width,
			uint height)
		{
			switch (preferredBackend)
			{
				case GraphicsBackend.Vulkan:
					return GraphicsDevice.CreateVulkan(
						options,
						new SwapchainDescription(
							GetSwapchainSource(window),
							width,
							height,
							options.SwapchainDepthFormat,
							options.SyncToVerticalBlank,
							options.SwapchainSrgbFormat));
#if false
				case GraphicsBackend.OpenGL:
					return GraphicsDevice.CreateOpenGL(
						options,
						new(
							gl.openGLContextHandle,
							gl.GetProcAddress,
							context => gl.MakeCurrent(window.Handle, context),
							gl.GetCurrentContext,
							() => gl.MakeCurrent(IntPtr.Zero, IntPtr.Zero),
							gl.DeleteContext,
							() => gl.SwapWindow(window.Handle),
							sync => gl.SetSwapInterval(sync ? 1 : 0)),
						width,
						height);
#endif
				case GraphicsBackend.Direct3D11:
					return GraphicsDevice.CreateD3D11(
						options,
						new SwapchainDescription(
							GetSwapchainSource(window),
							width,
							height,
							options.SwapchainDepthFormat,
							options.SyncToVerticalBlank,
							options.SwapchainSrgbFormat));
				default:
					throw new VeldridException($"Invalid GraphicsBackend: {preferredBackend}");
			}
		}

		public static SwapchainSource GetSwapchainSource(IWin32Window window)
		{
			switch (OSTailoredCode.CurrentOS)
			{
				case OSTailoredCode.DistinctOS.Linux:
#if false
					if (/*isWayland*/) return SwapchainSource.CreateWayland(/*display*/, /*surface*/);
					// else X11
#endif
					var display = XlibImports.XOpenDisplay(null);
					return SwapchainSource.CreateXlib(display, window.Handle);
#if false
				case OSTailoredCode.DistinctOS.macOS:
					return SwapchainSource.CreateNSWindow(/*window*/);
#endif
				case OSTailoredCode.DistinctOS.Windows:
					return SwapchainSource.CreateWin32(window.Handle, window.Handle);
				default:
					throw new NotImplementedException();
			}
		}
	}
}
