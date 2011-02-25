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

        public void AddFrame(string frame)
        {
            MovieRecords.Add(frame); //Validate the format? Or shoudl the Movie class be resonible for formatting?
        }

        public string GetFrame(int frameCount) //Frame count is 0 based here, should it be?
        {
            if (frameCount >= 0)
                return MovieRecords[frameCount];
            else
                return "";  //TODO: throw an exception?
        }
    }
}
