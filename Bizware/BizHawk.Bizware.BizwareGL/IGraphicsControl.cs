using System;
using System.Windows.Forms;

namespace BizHawk.Bizware.BizwareGL
{
	public interface IGraphicsControl : IDisposable
	{
		/// <summary>
		/// Sets whether presentation operations on this control will vsync
		/// </summary>
		void SetVsync(bool state);

		/// <summary>
		/// Swaps the buffers for this control
		/// </summary>
		void SwapBuffers();

		/// <summary>
		/// Makes this control current for rendering operations.
		/// Note that at this time, the window size shouldnt change until End() or else something bad might happen
		/// Please be aware that this might change the rendering context, meaning that some things you set without calling Begin/End might not be affected
		/// </summary>
		void Begin();

		/// <summary>
		/// Ends rendering on the specified control.
		/// NOTE: DO NOT EXPECT TO SEE BEGIN/END CALLED IN PAIRS, STRICTLY.
		/// this is more about GL context management than anything else.
		/// See GLManager for details.
		/// In particular, dont expect to have End() called before doing certain things. Maybe use SwapBuffers instead
		/// </summary>
		void End();
	}


}