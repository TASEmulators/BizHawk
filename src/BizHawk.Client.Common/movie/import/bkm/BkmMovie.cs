using System.Collections.Generic;
using System.IO;

using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	internal class BkmMovie
	{
		private readonly List<string> _log = new List<string>();
		public BkmHeader Header { get; } = new BkmHeader();
		public string Filename { get; set; } = "";
		public bool Loaded { get; private set; }
		public int InputLogLength => Loaded ? _log.Count : 0;

		public BkmControllerAdapter GetInputState(int frame, ControllerDefinition definition, string sytemId)
		{
			if (frame < InputLogLength && frame >= 0)
			{
				var adapter = new BkmControllerAdapter(definition, sytemId);
				adapter.SetControllersAsMnemonic(_log[frame]);
				return adapter;
			}

			return null;
		}

		public SubtitleList Subtitles => Header.Subtitles;

		public IList<string> Comments => Header.Comments;

		public string SyncSettingsJson
		{
			get => Header[HeaderKeys.SyncSettings];
			set => Header[HeaderKeys.SyncSettings] = value;
		}

		public byte[] BinarySavestate { get; set; }

		public bool Load()
		{
			var file = new FileInfo(Filename);

			if (!file.Exists)
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
					if (line == "")
					{
					}
					else if (Header.ParseLineFromFile(line))
					{
					}
					else if (line.StartsWith('|'))
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
			{
				BinarySavestate = Convert.FromBase64String(Header.SavestateBinaryBase64Blob);
			}

			Loaded = true;
			return true;
		}
	}
}
