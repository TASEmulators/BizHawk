#nullable enable

using BizHawk.Bizware.Graphics;

namespace BizHawk.Client.Common
{
	public class Savestate
	{
		public required byte[] coreData;
		public BitmapBuffer? screenshot;
	}
}
