public unsafe static class Util
{
	static readonly char[] HexConvArr = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };
	static System.Runtime.InteropServices.GCHandle HexConvHandle;
	public static char* HexConvPtr;
	static unsafe Util()
	{
		HexConvHandle = System.Runtime.InteropServices.GCHandle.Alloc(HexConvArr, System.Runtime.InteropServices.GCHandleType.Pinned);
		HexConvPtr = (char*)HexConvHandle.AddrOfPinnedObject().ToPointer();
	}
}