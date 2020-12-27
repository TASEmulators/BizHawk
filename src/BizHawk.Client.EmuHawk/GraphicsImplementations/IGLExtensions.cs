using System;

using BizHawk.Bizware.BizwareGL;
using BizHawk.Bizware.DirectX;

namespace BizHawk.Client.EmuHawk
{
	public static class IGLExtensions
	{
		public static IGuiRenderer CreateRenderer(this IGL gl) => gl switch
		{
			IGL_GdiPlus => new GDIPlusGuiRenderer(gl),
			IGL_SlimDX9 => new GuiRenderer(gl),
			IGL_TK => new GuiRenderer(gl),
			_ => throw new NotSupportedException()
		};
	}
}
