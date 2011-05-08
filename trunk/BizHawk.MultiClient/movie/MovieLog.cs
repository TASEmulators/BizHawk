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
        //TODO: Insert(int frame) not useful for convenctional tasing but TAStudio will want it

        List<string> MovieRecords = new List<string>();
        
        public MovieLog()
        {
            
        }

        public void Clear()
        {
            MovieRecords.Clear();
        }

        public int GetMovieLength()
        {
            return MovieRecords.Count;
        }

        public void AddFrame(string frame)
        {
            MovieRecords.Add(frame);
        }

        public void Truncate(int frame)
        {
            //TODO
        }

        public string GetFrame(int frameCount) //Frame count is 0 based here, should it be?
        {
            if (frameCount >= 0)
            {
                if (frameCount < MovieRecords.Count)
                    return MovieRecords[frameCount];
                else
                    return "";
            }
            else
                return "";  //TODO: throw an exception?
        }
    }
}
