/***********************************************************************
Copyright 2016 Stefan Hausotte

Licensed under the Apache License, Version 2.0 (the "License");
you may not use this file except in compliance with the License.
You may obtain a copy of the License at

    http://www.apache.org/licenses/LICENSE-2.0

Unless required by applicable law or agreed to in writing, software
distributed under the License is distributed on an "AS IS" BASIS,
WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
See the License for the specific language governing permissions and
limitations under the License.

*************************************************************************/

using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace PeNet.ImpHash
{
    /// <summary>
    ///     Mandiant’s imphash convention requires the following:
    ///     Resolving ordinals to function names when they appear.
    ///     Converting both DLL names and function names to all lowercase.
    ///     Removing the file extensions from imported module names.
    ///     Building and storing the lowercased strings in an ordered list.
    ///     Generating the MD5 hash of the ordered list.
    ///     oleaut32, ws2_32 and wsock32 can resolve ordinals to functions names.
    ///     The implementation is equal to the python module "pefile" 1.2.10-139
    ///     https://code.google.com/p/pefile/
    /// </summary>
    public class ImportHash
    {
        /// <summary>
        ///     Create an import hash object from the imported functions of a
        ///     PE file.
        /// </summary>
        /// <param name="importedFunctions"></param>
        public ImportHash(ICollection<ImportFunction> importedFunctions)
        {
            ImpHash = ComputeImpHash(importedFunctions);
        }

        /// <summary>
        ///     The import hash of the PE file as a string.
        /// </summary>
        public string ImpHash { get; private set; }


        private string ComputeImpHash(ICollection<ImportFunction> importedFunctions)
        {
            if (importedFunctions == null || importedFunctions.Count == 0)
                return null;

            var list = new List<string>();
            foreach (var impFunc in importedFunctions)
            {
                var tmp = FormatLibraryName(impFunc.DLL);
                tmp += FormatFunctionName(impFunc);

                list.Add(tmp);
            }

            // Concatenate all imports to one string separated by ','.
            var imports = string.Join(",", list);

            var md5 = MD5.Create();
            var inputBytes = Encoding.ASCII.GetBytes(imports);
            var hash = md5.ComputeHash(inputBytes);
            var sb = new StringBuilder();
            foreach (var t in hash)
            {
                sb.Append(t.ToString("x2"));
            }
            return sb.ToString();
        }

        private string FormatLibraryName(string libraryName)
        {
            var exts = new List<string> {"ocx", "sys", "dll"};
            var parts = libraryName.ToLower().Split('.');
            var libName = "";

            if (parts.Length > 1 && exts.Contains(parts[parts.Length - 1]))
            {
                for (var i = 0; i < parts.Length - 1; i++)
                {
                    libName += parts[i];
                    libName += ".";
                }
            }
            else
            {
                foreach (var p in parts)
                {
                    libName += p;
                    libName += ".";
                }
            }

            return libName;
        }

        private string FormatFunctionName(ImportFunction impFunc)
        {
            var tmp = "";
            if (impFunc.Name == null) // Import by ordinal
            {
                if (impFunc.DLL.ToLower() == "oleaut32.dll")
                {
                    tmp += OrdinalSymbolMapping.Lookup(OrdinalSymbolMapping.Modul.oleaut32, impFunc.Hint);
                }
                else if (impFunc.DLL.ToLower() == "ws2_32.dll")
                {
                    tmp += OrdinalSymbolMapping.Lookup(OrdinalSymbolMapping.Modul.ws2_32, impFunc.Hint);
                }
                else if (impFunc.DLL.ToLower() == "wsock32.dll")
                {
                    tmp += OrdinalSymbolMapping.Lookup(OrdinalSymbolMapping.Modul.wsock32, impFunc.Hint);
                }
                else // cannot resolve ordinal to a function name
                {
                    tmp += "ord";
                    tmp += impFunc.Hint.ToString();
                }
            }
            else // Import by name
            {
                tmp += impFunc.Name;
            }

            return tmp.ToLower();
        }
    }
}