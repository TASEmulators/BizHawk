namespace BizHawk
{
    public interface IVideoProvider
    {
        int[] GetVideoBuffer();

        int BufferWidth { get; }
        int BufferHeight { get; }
        int BackgroundColor { get; }
    }
}
