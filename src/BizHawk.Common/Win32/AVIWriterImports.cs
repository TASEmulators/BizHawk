#nullable disable

#if AVI_SUPPORT

namespace BizHawk.Common
{
	public static class AVIWriterImports
	{
		[Flags]
		public enum OpenFileStyle : uint
		{
			OF_WRITE = 0x00000001,
			OF_CREATE = 0x00001000,
		}
	}
}
#endif
