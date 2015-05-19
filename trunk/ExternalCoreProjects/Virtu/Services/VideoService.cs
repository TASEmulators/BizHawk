namespace Jellyfish.Virtu.Services
{
	/// <summary>
	/// this isn't really a "service" anymore, just a helper for the video class
	/// </summary>
    public class VideoService
    {
		public VideoService()
		{
			fb = new int[560 * 384];
		}
		public VideoService(int[] fb)
		{
			this.fb = fb;
		}

		[Newtonsoft.Json.JsonIgnore] // client can serialize framebuffer if it wants to
		public int[] fb;

		public void SetPixel(int x, int y, int color)
		{
			int i = 560 * y + x;
			fb[i] = fb[i + 560] = color;
		}
    }
}
