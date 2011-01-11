namespace BizHawk
{
    public interface IGame
    {
        byte[] GetRomData();
        string[] GetOptions();
        string Name { get; }
    }
}