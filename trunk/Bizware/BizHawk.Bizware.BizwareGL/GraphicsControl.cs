using System;
using swf = System.Windows.Forms;

namespace BizHawk.Bizware.BizwareGL
{
	/// <summary>
	/// Represents
	/// </summary>
	public abstract class GraphicsControl : IDisposable
	{
		/// <summary>
		/// Gets the control that this interface is wrapping
		/// </summary>
		public abstract swf.Control Control { get; }

		public static implicit operator swf.Control(GraphicsControl ctrl) { return ctrl.Control; }
		
		/// <summary>
		/// Sets whether presentation operations on this control will vsync
		/// </summary>
		public abstract void SetVsync(bool state);

		/// <summary>
		/// Swaps the buffers for this control
		/// </summary>
		public abstract void SwapBuffers();

		/// <summary>
		/// Makes this control current for rendering operations.
		/// Note that at this time, the window size shouldnt change until End() or else something bad might happen
		/// Please be aware that this might change the rendering context, meaning that some things you set without calling BeginControl/EndControl might not be affected
		/// </summary>
		public abstract void Begin();

		/// <summary>
		/// Ends rendering on the specified control.
		/// </summary>
		public abstract void End();

		public abstract void Dispose();
	}
}