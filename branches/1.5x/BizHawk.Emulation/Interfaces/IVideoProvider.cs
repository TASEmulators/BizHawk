namespace BizHawk
{
    public interface IVideoProvider
    {
        int[] GetVideoBuffer();

        int VirtualWidth { get; } // Used for controlling aspect ratio. Just return BufferWidth if you dont know what to do with this.

        int BufferWidth { get; }
        int BufferHeight { get; }
        int BackgroundColor { get; }
    }
}
