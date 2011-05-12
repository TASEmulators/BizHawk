using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace BizHawk.MultiClient
{
    public enum MOVIEMODE { INACTIVE, PLAY, RECORD, FINISHED };
    public class Movie
    {
        private MovieHeader Header = new MovieHeader();
        private MovieLog Log = new MovieLog();

        private bool IsText = true;
        private string Filename;

        private MOVIEMODE MovieMode = new MOVIEMODE();

        public int lastLog;
        public int rerecordCount;

        //TODO:
        //Author field, needs to be passed in by a record or play dialog

        public Movie(string filename, MOVIEMODE m)
        {
            Filename = filename;    //TODO: Validate that file is writable
            MovieMode = m;
            lastLog = 0;
            rerecordCount = 0;
        }

        public string GetFilePath() 
        {
            return Filename;
        }

        public string GetSysID()
        {
            return Header.GetHeaderLine(MovieHeader.PLATFORM);
        }

        public string GetGameName()
        {
            return Header.GetHeaderLine(MovieHeader.GAMENAME);
        }

        public void StopMovie()
        {
            if (MovieMode == MOVIEMODE.RECORD)
                WriteMovie();
            MovieMode = MOVIEMODE.INACTIVE;            
        }

        public void StartNewRecording()
        {
            MovieMode = MOVIEMODE.RECORD;
            Log.Clear();
            Header = new MovieHeader("BizHawk v1.0.0", MovieHeader.MovieVersion, Global.Emulator.SystemId, Global.Game.Name, "", 0);
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
                Log.AddFrame(Global.ActiveController.GetControllersAsMnemonic());
        }

        public string GetInputFrame(int frame)
        {
            lastLog = frame;
            if (frame < Log.GetMovieLength())
                return Log.GetFrame(frame);
            else
                return "";
        }

        //Movie editing tools may like to have something like this
        public void AddMovieRecord(string record)
        {
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
            if (Filename.Length == 0) return;   //Nothing to write
            int length = Log.GetMovieLength();
            
            using (StreamWriter sw = new StreamWriter(Filename))
            {          
                foreach (KeyValuePair<string, string> kvp in Header.GetHeaderInfo())
                {
                    sw.WriteLine(kvp.Key + " " + kvp.Value);
                }

                
                for (int x = 0; x < length; x++)
                {
                    sw.WriteLine(Log.GetFrame(x));
                }
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
                        Log.AddFrame(str);
                    }
                    else
                    {
                        Header.Comments.Add(str);
                    }
                }
            }

            return true;
            
        }

        public bool PreLoadText()
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
                        break;
                    }
                    else
                    {
                        Header.Comments.Add(str);
                    }

                }
                sr.Close();
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

        public void DumpLogIntoSavestateText(TextWriter writer)
        {
            writer.WriteLine("[Input]");
            for (int x = 0; x < Log.Length(); x++)
                writer.WriteLine(Log.GetFrame(x));
            writer.WriteLine("[/Input]");
        }

        public void LoadLogFromSavestateText(TextReader reader)
        {
            Log.Clear();
            while (true)
            {
                string line = reader.ReadLine();
                if (line.Trim() == "") continue;
                if (line == "[Input]") continue;
                if (line == "[/Input]") break;
                if (line[0] == '|')
                    Log.AddFrame(line);
            }
        }
    }
}
