using System;

using BizHawk.Bizware.BizwareGL;

namespace BizHawk.Client.EmuHawk
{
	public static class IGLExtensions
	{
		public static IGuiRenderer CreateRenderer(this IGL gl) => gl switch
		{
			IGL_GdiPlus _ => (IGuiRenderer) new GDIPlusGuiRenderer(gl),
			IGL_SlimDX9 _ => new GuiRenderer(gl),
			IGL_TK _ => new GuiRenderer(gl),
			_ => throw new NotSupportedException()
		};
	}
}
