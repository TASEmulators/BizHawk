using System;
using BizHawk.Bizware.BizwareGL;
using BizHawk.Bizware.BizwareGL.Drivers.GdiPlus;
using BizHawk.Bizware.BizwareGL.Drivers.OpenTK;
using BizHawk.Bizware.BizwareGL.Drivers.SlimDX;
using BizHawk.Bizware.BizwareGL.Drivers.Vulkan;

namespace BizHawk.Client.Common
{
	public static class Extensions
	{
		public static IGuiRenderer CreateRenderer(this IGL gl)
		{
			if (gl is IGL_Vulkan || gl is IGL_TK || gl is IGL_SlimDX9)
			{
				return new GuiRenderer(gl);
			}

			if (gl is IGL_GdiPlus)
			{
				return new GDIPlusGuiRenderer((IGL_GdiPlus)gl);
			}

			throw new NotSupportedException();
		}
	}
}
