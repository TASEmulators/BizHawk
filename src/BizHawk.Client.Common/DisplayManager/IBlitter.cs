using System.Drawing;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// This is an old abstracted rendering class that the OSD system is using to get its work done.
	/// We should probably just use a GuiRenderer (it was designed to do that) although wrapping it with
	/// more information for OSDRendering could be helpful I suppose
	/// </summary>
	public interface IBlitter
	{
		void DrawString(string s, Color color, float x, float y);

		SizeF MeasureString(string s);

		Rectangle ClipBounds { get; }

		public float Scale { get; }
	}
}
