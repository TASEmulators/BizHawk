using System;
using System.IO;
using System.Text;

using BizHawk.Common;

namespace BizHawk.Client.Common
{
	public partial class BkmMovie
	{
		private int _preloadFramecount; // Not a a reliable number, used for preloading (when no log has yet been loaded), this is only for quick stat compilation for dialogs such as play movie

		public void SaveAs(string path)
		{
			Filename = path;
			if (!Loaded)
			{
				return;
			}

			var directory_info = new FileInfo(Filename).Directory;
			if (directory_info != null)
			{
				Directory.CreateDirectory(directory_info.FullName);
			}

			Write(Filename);
		}

		public void Save()
		{
			if (!Loaded || string.IsNullOrWhiteSpace(Filename))
			{
				return;
			}

			SaveAs(Filename);
			_changes = false;
		}

		public void SaveBackup()
		{
			if (!Loaded || string.IsNullOrWhiteSpace(Filename))
			{
				return;
			}

			var backupName = Filename;
			backupName = backupName.Insert(Filename.LastIndexOf("."), string.Format(".{0:yyyy-MM-dd HH.mm.ss}", DateTime.Now));
			backupName = Path.Combine(Global.Config.PathEntries["Global", "Movie backups"].Path, Path.GetFileName(backupName));

			var directory_info = new FileInfo(backupName).Directory;
			if (directory_info != null)
			{
				Directory.CreateDirectory(directory_info.FullName);
			}

			Write(backupName);
		}

		public bool Load(bool preload)
		{
			var file = new FileInfo(Filename);

			if (file.Exists == false)
			{
				Loaded = false;
				return false;
			}

			Header.Clear();
			_log.Clear();

			using (var sr = file.OpenText())
			{
				string line;

				while ((line = sr.ReadLine()) != null)
				{
					if (line == string.Empty)
					{
						continue;
					}

					if (line.Contains("LoopOffset"))
					{
						try
						{
							_loopOffset = int.Parse(line.Split(new[] { ' ' }, 2)[1]);
						}
						catch (Exception)
						{
							continue;
						}
					}
					else if (Header.ParseLineFromFile(line))
					{
						continue;
					}
					else if (line.StartsWith("|"))
					{
						_log.Add(line);
					}
					else
					{
						Header.Comments.Add(line);
					}
				}
			}
			if (Header.SavestateBinaryBase64Blob != null)
				BinarySavestate = Convert.FromBase64String(Header.SavestateBinaryBase64Blob);

			Loaded = true;
			_changes = false;
			return true;
		}

		/// <summary>
		/// Load Header information only for displaying file information in dialogs such as play movie
		/// TODO - consider not loading the SavestateBinaryBase64Blob key?
		/// </summary>
		public bool PreLoadHeaderAndLength(HawkFile hawkFile)
		{
			Loaded = false;
			var file = new FileInfo(hawkFile.CanonicalFullPath);

			if (file.Exists == false)
			{
				return false;
			}

			Header.Clear();
			_log.Clear();

			var origStreamPosn = hawkFile.GetStream().Position;
			hawkFile.GetStream().Position = 0; // Reset to start

			// No using block because we're sharing the stream and need to give it back undisposed.
			var sr = new StreamReader(hawkFile.GetStream());

			for (; ; )
			{
				//read to first space (key/value delimeter), or pipe, or EOF
				int first = sr.Read();

				if (first == -1)
				{
					break;
				} // EOF

				if (first == '|') //pipe: begin input log
				{
					//NOTE - this code is a bit convoluted due to its predating the basic outline of the parser which was upgraded in may 2014
					var line = '|' + sr.ReadLine();

					//how many bytes are left, total?
					long remain = sr.BaseStream.Length - sr.BaseStream.Position;

					//try to find out whether we use \r\n or \n
					//but only look for 1K characters.
					bool usesR = false;
					for (int i = 0; i < 1024; i++)
					{
						int c = sr.Read();
						if (c == -1)
							break;
						if (c == '\r')
						{
							usesR = true;
							break;
						}
						if (c == '\n')
							break;
					}

					int lineLen = line.Length + 1; //account for \n
					if (usesR) lineLen++; //account for \r

					_preloadFramecount = (int)(remain / lineLen); //length is remaining bytes / length per line
					_preloadFramecount++; //account for the current line
					break;
				}
				else
				{
					//a header line. finish reading key token, to make sure it isn't one of the FORBIDDEN keys
					var sbLine = new StringBuilder();
					sbLine.Append((char)first);
					for (; ; )
					{
						int c = sr.Read();
						if (c == -1) break;
						if (c == '\n') break;
						if (c == ' ') break;
						sbLine.Append((char)c);
					}

					var line = sbLine.ToString();

					//ignore these suckers, theyre way too big for preloading. seriously, we will get out of memory errors.
					var skip = line == HeaderKeys.SAVESTATEBINARYBASE64BLOB;

					if (skip)
					{
						//skip remainder of the line
						sr.DiscardBufferedData();
						var stream = sr.BaseStream;
						for (; ; )
						{
							int c = stream.ReadByte();
							if (c == -1) break;
							if (c == '\n') break;
						}
						//proceed to next line
						continue;
					}


					var remainder = sr.ReadLine();
					sbLine.Append(' ');
					sbLine.Append(remainder);
					line = sbLine.ToString();

					if (string.IsNullOrWhiteSpace(line) || Header.ParseLineFromFile(line))
					{
						continue;
					}

					Header.Comments.Add(line);
				}
			}

			hawkFile.GetStream().Position = origStreamPosn;

			return true;
		}

		private void Write(string fn)
		{
			if (BinarySavestate != null)
				Header.SavestateBinaryBase64Blob = Convert.ToBase64String(BinarySavestate);
			else
				Header.SavestateBinaryBase64Blob = null;

			using (var fs = new FileStream(fn, FileMode.Create, FileAccess.Write, FileShare.Read))
			{
				using (var sw = new StreamWriter(fs))
				{
					sw.Write(Header.ToString());

					// TODO: clean this up
					if (_loopOffset.HasValue)
					{
						sw.WriteLine("LoopOffset " + _loopOffset);
					}

					foreach (var input in _log)
					{
						sw.WriteLine(input);
					}
				}
			}

			_changes = false;
		}
	}
}
