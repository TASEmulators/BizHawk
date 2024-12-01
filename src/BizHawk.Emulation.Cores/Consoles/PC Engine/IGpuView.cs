using System.Runtime.InteropServices;
using BizHawk.Emulation.Common;

namespace BizHawk.Emulation.Cores.PCEngine
{
	public interface IPceGpuView : IEmulatorService
	{
		bool IsSgx { get; }
		void GetGpuData(int vdc, Action<PceGpuData> callback);
	}
	[StructLayout(LayoutKind.Sequential)]
	public unsafe class PceGpuData
	{
		public int BatWidth;
		public int BatHeight;
		public int* PaletteCache;
		public byte* BackgroundCache;
		public byte* SpriteCache;
		public ushort* Vram;
	}
}
