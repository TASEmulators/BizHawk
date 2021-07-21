using System;

using BizHawk.Bizware.BizwareGL;
using BizHawk.Bizware.DirectX;
using BizHawk.Bizware.OpenTK3;
using BizHawk.Client.Common;

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

		public static EDispMethod DispMethodEnum(this IGL gl) => gl switch
		{
			IGL_GdiPlus => EDispMethod.GdiPlus,
			IGL_SlimDX9 => EDispMethod.SlimDX9,
			IGL_TK => EDispMethod.OpenGL,
			_ => throw new ArgumentException("unknown GL impl", nameof(gl))
		};
	}
}
