using System.Drawing;

namespace BizHawk.Client.Common
{
	public interface IWindowCoordsTransformer
	{
		(int Left, int Top, int Right, int Bottom) ClientExtraPadding { get; set; }

		(int Left, int Top, int Right, int Bottom) GameExtraPadding { get; set; }

		Size GetPanelNativeSize();

		Point TransformPoint(Point p);

		Point UntransformPoint(Point p);
	}
}
