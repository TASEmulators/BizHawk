using Windows.Win32.Foundation;
using Windows.Win32.Media.Multimedia;

namespace Windows.Win32
{
	public static partial class Win32Imports
	{
		/// <seealso cref="AVISaveOptions(HWND, uint, int, IAVIStream[], AVICOMPRESSOPTIONS**)"/>
		public static unsafe nint AVISaveOptions(IAVIStream ppavi, ref AVICOMPRESSOPTIONS opts, HWND owner)
		{
			fixed (AVICOMPRESSOPTIONS* popts = &opts)
			{
				return AVISaveOptions(owner, uiFlags: 0, nStreams: 1, [ ppavi ], &popts);
			}
		}
	}
}
