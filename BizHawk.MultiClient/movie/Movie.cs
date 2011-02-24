using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BizHawk.MultiClient
{
    class Movie
    {
        private MovieHeader Header = new MovieHeader();
        private MovieLog Log = new MovieLog();

        public Movie()
        {

        }

        public void AddMovieRecord()
        {

        }

        public void WriteMovie()
        {

        }

        public int GetMovieLength()
        {
            return Log.GetMovieLength();
        }
    }
}
