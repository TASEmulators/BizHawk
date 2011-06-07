using System.Collections.Generic;

namespace BizHawk
{
    public interface IGame
    {
        byte[] GetRomData();
		byte[] GetFileData();
        IList<string> GetOptions();
        
		//only use this for cosmetic purposes
		string Name { get; }

		//use this for path-building purposes
		string FilesystemSafeName { get; }
    }
}