using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace BizHawk.MultiClient
{
    class Movie
    {
        private MovieHeader Header = new MovieHeader();
        private MovieLog Log = new MovieLog();

        private bool IsText = true;
        private string Filename;

        public Movie(string filename)
        {
            Filename = filename;    //TODO: Validate that file is writable
            Log.AddFrame("|........|0|");
        }

        public void AddMovieRecord()
        {
            //TODO: validate input
            //Format into string acceptable by MovieLog
        }

        public void WriteMovie()
        {
            if (IsText)
                WriteText();
            else
                WriteBinary();
        }

        private void WriteText()
        {
            var file = new FileInfo(Filename);
            
            int length = Log.GetMovieLength();
            string str = "";
            
            using (StreamWriter sw = new StreamWriter(Filename))
            {          
                foreach (KeyValuePair<string, string> kvp in Header.GetHeaderInfo())
                {
                    str += kvp.Key + " " + kvp.Value + "\n";
                }

                
                for (int x = 0; x < length; x++)
                {
                    str += Log.GetFrame(x) + "\n";
                }
                sw.WriteLine(str);
            }
        }

        private void WriteBinary()
        {

        }

        private bool LoadText()
        {
            var file = new FileInfo(Filename);
            using (StreamReader sr = file.OpenText())
            {
                string str = "";

                while ((str = sr.ReadLine()) != null)
                {
                    if (str.Contains(MovieHeader.EMULATIONVERSION))
                    {

                    }
                    else if (str.Contains(MovieHeader.MOVIEVERSION))
                    {

                    }
                    else if (str.Contains(MovieHeader.PLATFORM))
                    {

                    }
                    else if (str.Contains(MovieHeader.GAMENAME))
                    {

                    }
                    else if (str[0] == '|')
                    {

                    }
                    else
                    {
                        //Something has gone wrong here!
                    }
                }
            }

            return true;
            
        }

        private bool LoadBinary()
        {
            return true;
        }

        public bool LoadMovie()
        {
            var file = new FileInfo(Filename);
            if (file.Exists == false) return false; //TODO: methods like writemovie will fail, some internal flag needs to prevent this
            //TODO: must determine if file is text or binary
            return LoadText();
        }

        public int GetMovieLength()
        {
            return Log.GetMovieLength();
        }
    }
}
