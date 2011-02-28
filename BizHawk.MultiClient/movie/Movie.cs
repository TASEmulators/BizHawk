using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace BizHawk.MultiClient
{
    public enum MOVIEMODE { INACTIVE, PLAY, RECORD, FINISHED };
    class Movie
    {
        private MovieHeader Header = new MovieHeader();
        private MovieLog Log = new MovieLog();

        private bool IsText = true;
        private string Filename;

        private MOVIEMODE MovieMode = new MOVIEMODE();

        public Movie(string filename, MOVIEMODE m)
        {
            Filename = filename;    //TODO: Validate that file is writable
            MovieMode = m;
        }

        public void StopMovie()
        {
            MovieMode = MOVIEMODE.INACTIVE;
            WriteMovie();
        }

        public void StartNewRecording()
        {
            MovieMode = MOVIEMODE.RECORD;
            Log.Clear();
        }

        public void StartPlayback()
        {
            MovieMode = MOVIEMODE.PLAY;
            //TODO:...something else should be done here
        }

        public MOVIEMODE GetMovieMode()
        {
            return MovieMode;
        }

        public void GetMnemonic()
        {
            if (MovieMode == MOVIEMODE.RECORD)
                Log.AddFrame(Global.Emulator.GetControllersAsMnemonic());
        }

        public string GetInputFrame(int frame)
        {
            if (frame < Log.GetMovieLength())
                return Log.GetFrame(frame);
            else
                return "";
        }

        //Movie editing tools may like to have something like this
        public void AddMovieRecord(string record)
        {
            //TODO: validate input
            //Format into string acceptable by MovieLog
            Log.AddFrame(record);
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

        private string ParseHeader(string line, string headerName)
        {
            string str;
            int x = line.LastIndexOf(headerName) + headerName.Length;
            str = line.Substring(x + 1, line.Length - x - 1);
            return str;
        }

        private bool LoadText()
        {
            var file = new FileInfo(Filename);
            
            if (file.Exists == false)
                return false;
            else
            {
                Header.Clear();
                Log.Clear();
            }
            
            using (StreamReader sr = file.OpenText())
            {
                string str = "";

                while ((str = sr.ReadLine()) != null)
                {
                    if (str == "")
                    {
                        continue;
                    }
                    else if (str.Contains(MovieHeader.EMULATIONVERSION))
                    {
                        str = ParseHeader(str, MovieHeader.EMULATIONVERSION);
                        Header.AddHeaderLine(MovieHeader.EMULATIONVERSION, str);
                    }
                    else if (str.Contains(MovieHeader.MOVIEVERSION))
                    {
                        str = ParseHeader(str, MovieHeader.MOVIEVERSION);
                        Header.AddHeaderLine(MovieHeader.MOVIEVERSION, str);
                    }
                    else if (str.Contains(MovieHeader.PLATFORM))
                    {
                        str = ParseHeader(str, MovieHeader.PLATFORM);
                        Header.AddHeaderLine(MovieHeader.PLATFORM, str);
                    }
                    else if (str.Contains(MovieHeader.GAMENAME))
                    {
                        str = ParseHeader(str, MovieHeader.GAMENAME);
                        Header.AddHeaderLine(MovieHeader.GAMENAME, str);
                    }
                    else if (str[0] == '|')
                    {
                        Log.AddFrame(str);  //TODO: validate proper formatting
                    }
                    else
                    {
                        //TODO: Something has gone wrong here!
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
