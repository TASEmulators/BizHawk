using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.MultiClient
{
    /// <summary>
    /// Represents the controller key presses of a movie
    /// </summary>
    class MovieLog
    {
        List<string> MovieRecords = new List<string>();
        
        public MovieLog()
        {
            
        }

        public int GetMovieLength()
        {
            return MovieRecords.Count;
        }
    }
}
