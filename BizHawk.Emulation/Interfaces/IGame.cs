using System.Collections.Generic;

namespace BizHawk
{
    public interface IGame
    {
        byte[] GetRomData();
        IList<string> GetOptions();
        string Name { get; }
    }
}