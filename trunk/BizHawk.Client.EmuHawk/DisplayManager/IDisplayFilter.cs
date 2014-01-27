using System.Drawing;
using BizHawk.Bizware.BizwareGL;

namespace BizHawk.Client.EmuHawk
{

	public interface IDisplayFilter
	{
		/// <summary>
		/// describes how this filter will respond to an input format
		/// </summary>
		DisplayFilterAnalysisReport Analyze(Size sourceSize);

		/// <summary>
		/// runs the filter
		/// </summary>
		BitmapBuffer Execute(BitmapBuffer surface);
	}

}