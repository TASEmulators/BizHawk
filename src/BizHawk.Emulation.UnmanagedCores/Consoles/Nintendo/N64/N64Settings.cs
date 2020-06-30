namespace BizHawk.Emulation.Cores.Nintendo.N64
{
	public class N64Settings
	{
		public int VideoSizeX = 320;
		public int VideoSizeY = 240;

		public bool UseMupenStyleLag { get; set; }

		public N64Settings Clone()
		{
			return new N64Settings
			{
				VideoSizeX = VideoSizeX,
				VideoSizeY = VideoSizeY,

				UseMupenStyleLag = UseMupenStyleLag
			};
		}
	}
}
