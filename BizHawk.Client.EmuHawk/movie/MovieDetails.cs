using System;
using System.Drawing;

namespace BizHawk.Client.EmuHawk
{
    /// <summary>
    /// Used for the sorting of the moviedetails in PlayMovie.cs
    /// </summary>
   public class MovieDetails
    {
       public String keys { get; set; }
       public String values { get; set; }
       public Color backgroundColor { get; set; }

        public MovieDetails()
        {
            keys = String.Empty;
            values = String.Empty;
            backgroundColor = Color.White;
        }
    }
}
