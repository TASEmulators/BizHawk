using System.Collections.Generic;

namespace BizHawk
{
    public interface IGame
    {
        byte[] GetRomData();
		byte[] GetFileData();
        IList<string> GetOptions();
        string Name { get; }
    }
}