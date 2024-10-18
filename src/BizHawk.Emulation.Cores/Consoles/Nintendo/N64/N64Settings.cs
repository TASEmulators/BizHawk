namespace BizHawk.Emulation.Cores.Nintendo.N64
{
	public record N64Settings
	{
		public int VideoSizeX = 320;
		public int VideoSizeY = 240;

		public bool UseMupenStyleLag { get; set; }
	}
}
