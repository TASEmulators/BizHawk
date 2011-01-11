using System;
using System.IO;

namespace BizHawk.MultiClient
{
    public class HawkFile : IDisposable
    {
        private bool zipped;
        public bool Zipped { get { return zipped; } }

        private bool exists;
        public bool Exists { get { return exists; } }
       
        private string extension;
        public string Extension { get { return extension; } }

        public string Directory { get { return Path.GetDirectoryName(rawFileName); } }

        private string rawFileName;
        private string name;
        public string Name { get { return name; } }
        public string FullName { get { return name + "." + extension; } }

        private IDisposable thingToDispose;
        private Stream zippedStream;

        public HawkFile(string path) : this(path,"SMS","PCE","SGX","GG","SG","BIN","SMD","GB","IPS") {}
        
        public HawkFile(string path, params string[] recognizedExtensions)
        {
            var file = new FileInfo(path);

            exists = file.Exists;
            if (file.Exists == false)
                return;

            if (file.Extension.ToLower().In(".zip",".rar",".7z"))
            {
                LoadZipFile(path, recognizedExtensions);
                return;
            }

            zipped = false;
            extension = file.Extension.Substring(1).ToUpperInvariant();
            rawFileName = path;
            name = Path.GetFileNameWithoutExtension(path);
        }

        private void LoadZipFile(string path, string[] recognizedExtensions)
        {
            zipped = true;
            rawFileName = path;

			using (var extractor = new SevenZip.SevenZipExtractor(path))
            {
				thingToDispose = extractor;
                foreach (var e in extractor.ArchiveFileData)
                {
                    extension = Path.GetExtension(e.FileName).Substring(1).ToUpperInvariant();

                    if (extension.In(recognizedExtensions))
                    {
                        // we found our match.
                        name = Path.GetFileNameWithoutExtension(e.FileName);
                        zippedStream = new MemoryStream();
                        //e.Extract(zippedStream);
						extractor.ExtractFile(e.Index,zippedStream);
                        thingToDispose = zippedStream;
                        return;
                    }
                }
                exists = false;
            }
        }
              
        public Stream GetStream()
        {
            if (zipped == false)
            {
                var stream = new FileStream(rawFileName, FileMode.Open, FileAccess.Read);
                thingToDispose = stream;
                return stream;
            }
            
            return zippedStream;
        }

        public void Dispose()
        {
            if (thingToDispose != null)
                thingToDispose.Dispose();
        }
    }
}