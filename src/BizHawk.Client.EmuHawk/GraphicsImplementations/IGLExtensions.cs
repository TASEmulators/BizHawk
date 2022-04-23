using BizHawk.Bizware.BizwareGL;
using BizHawk.Bizware.OpenTK3;

namespace BizHawk.Client.EmuHawk
{
	public static class IGLExtensions
	{
		public static IGuiRenderer CreateRenderer(this IGL gl)
			=> gl is IGL_GdiPlus gdipImpl
				? new GDIPlusGuiRenderer(gdipImpl)
				: new GuiRenderer(gl); // This implementation doesn't seem to require any OpenGL-specific (or D3D-specific) behaviour; can it be used with IGL_GdiPlus too? If so, is GDIPlusGuiRenderer only kept around because it's faster? --yoshi
	}
}
