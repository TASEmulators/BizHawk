using System.Windows.Forms;

namespace BizHawk.Bizware.Graphics.Controls
{
	public abstract class GraphicsControl : Control
	{
		/// <summary>
		/// Allows the control to tear when out of vsync
		/// Only relevant for D3D11Control currently
		/// </summary>
		public abstract void AllowTearing(bool state);

		/// <summary>
		/// Sets whether presentation operations on this control will vsync
		/// </summary>
		public abstract void SetVsync(bool state);

		/// <summary>
		/// Swaps the buffers for this control.
		/// Be aware, the owner IGL's current render target is undefined after calling this
		/// </summary>
		public abstract void SwapBuffers();

		/// <summary>
		/// Makes this control current for rendering operations.
		/// Note that at this time, the window size shouldn't change until End() or else something bad might happen
		/// Please be aware that this might change the rendering context, meaning that some things you set without calling Begin/End might not be affected
		/// </summary>
		public abstract void Begin();

		/// <summary>
		/// Ends rendering on the specified control.
		/// NOTE: DO NOT EXPECT TO SEE BEGIN/END CALLED IN PAIRS, STRICTLY.
		/// this is more about GL context management than anything else.
		/// In particular, don't expect to have End() called before doing certain things. Maybe use SwapBuffers instead
		/// </summary>
		public abstract void End();
	}
}