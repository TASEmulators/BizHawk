using System;
using BizHawk.Bizware.BizwareGL;
using BizHawk.Bizware.BizwareGL.Drivers.SlimDX;

namespace BizHawk.Client.Common
{
	public static class Extensions
	{
		public static IGuiRenderer CreateRenderer(this IGL gl)
		{
			if (gl is IGL_TK)
			{
				return new GuiRenderer(gl);
			}

			if (gl is IGL_SlimDX9)
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
