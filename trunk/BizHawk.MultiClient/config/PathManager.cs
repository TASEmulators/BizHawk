using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Reflection;

namespace BizHawk.MultiClient
{
    public static class PathManager
    {
        public static string GetExePathAbsolute()
        {
            string p = Path.GetDirectoryName(Assembly.GetExecutingAssembly().GetName().CodeBase);
            if (p.Substring(0, 6) == "file:\\")
                p = p.Remove(0, 6);
            string z = p;
            return p;
        }

        public static string GetBasePathAbsolute()
        {
            //Gets absolute base as derived from EXE

            if (Global.Config.BasePath.Length < 1) //If empty, then EXE path
                return GetExePathAbsolute();

            if (Global.Config.BasePath.Length >= 5 && 
                Global.Config.BasePath.Substring(0, 5) == "%exe%")
                    return GetExePathAbsolute();
            if (Global.Config.BasePath[0] == '.')
            {
                if (Global.Config.BasePath.Length == 1)
                    return GetExePathAbsolute();
                else
                {
                    if (Global.Config.BasePath.Length == 2 &&
                        Global.Config.BasePath == ".\\")
                        return GetExePathAbsolute();
                    else
                    {
                        string tmp = Global.Config.BasePath;
                        tmp = tmp.Remove(0, 1);
                        tmp = tmp.Insert(0, GetExePathAbsolute());
                        return tmp;
                    }
                }
            }

            if (Global.Config.BasePath.Substring(0, 2) == "..")
                return RemoveParents(Global.Config.BasePath, GetExePathAbsolute());

            //In case of error, return EXE path
            return GetExePathAbsolute();
        }

        public static string GetPlatformBase(string system)
        {
            switch (system)
            {
                case "NES":
                    return Global.Config.BaseNES;
                case "SG":
                    return Global.Config.BaseSG;
                case "GG":
                    return Global.Config.BaseGG;
                case "SMS":
                    return Global.Config.BaseSMS;
                case "SGX":
                case "PCE":
                    return Global.Config.BasePCE;
                case "TI83":
                    return Global.Config.BaseTI83;
                case "GEN":
                    return Global.Config.BaseGenesis;
                case "GB":
                    return Global.Config.BaseGameboy;
                default:
                    return "";
            }
        }

        public static string MakeAbsolutePath(string path, string system)
        {
            //This function translates relative path and special identifiers in absolute paths

            if (path.Length < 1)
                return GetBasePathAbsolute();

            if (path == "%recent%")
            {
                return Environment.SpecialFolder.Recent.ToString();
            }

            if (path.Length >= 5 && path.Substring(0, 5) == "%exe%")
            {
                if (path.Length == 5)
                    return GetExePathAbsolute();
                else
                {
                    string tmp = path.Remove(0, 5);
                    tmp = tmp.Insert(0, GetExePathAbsolute());
                    return tmp;
                }
            }

            if (path[0] == '.')
            {
                if (system.Length > 0)
                {
                    
                    path = path.Remove(0, 1);
                    path = path.Insert(0, GetPlatformBase(system));
                }
                if (path.Length == 1)
                    return GetBasePathAbsolute();
                else
                {
                    if (path[0] == '.')
                    {
                        path = path.Remove(0, 1);
                        path = path.Insert(0, GetBasePathAbsolute());
                    }
                    
                    return path;
                }
            }

            //If begins wtih .. do alorithm to determine how many ..\.. combos and deal with accordingly, return drive letter only if too many ..

            if ((path[0] > 'A' && path[0] < 'Z') || (path[0] > 'a' && path[0] < 'z'))
            {
                //C:\
                if (path.Length > 2 && path[1] == ':' && path[2] == '\\')
                    return path;
                else
                {
                    //file:\ is an acceptable path as well, and what FileBrowserDialog returns
                    if (path.Length >= 6 && path.Substring(0, 6) == "file:\\")
                        return path;
                    else
                        return GetExePathAbsolute(); //bad path
                }
            }

            //all pad paths default to EXE
            return GetExePathAbsolute();
        }

        public static string RemoveParents(string path, string workingpath)
        {
            //determines number of parents, then removes directories from working path, return absolute path result
            //Ex: "..\..\Bob\", "C:\Projects\Emulators\Bizhawk" will return "C:\Projects\Bob\" 
            int x = NumParentDirectories(path);
            if (x > 0)
            {
                int y = HowMany(path, "..\\");
                int z = HowMany(workingpath, "\\");
                if (y >= z)
                {
                    //Return drive letter only, working path must be absolute?
                }
                return "";
            }
            else return path;
        }

        public static int NumParentDirectories(string path)
        {
            //determine the number of parent directories in path and return result
            int x = HowMany(path, '\\');
            if (x > 0)
            {
                return HowMany(path, "..\\");
            }
            return 0;
        }

        public static int HowMany(string str, string s)
        {
            int count = 0;
            for (int x = 0; x < (str.Length - s.Length); x++)
            {
                if (str.Substring(x, s.Length) == s)
                    count++;
            }
            return count;
        }

        public static int HowMany(string str, char c)
        {
            int count = 0;
            for (int x = 0; x < str.Length; x++)
            {
                if (str[x] == c)
                    count++;
            }
            return count;
        }

        public static bool IsRecent(string path)
        {
            if (path == "%recent%")
                return true;
            else
                return false;
        }
    }
}
