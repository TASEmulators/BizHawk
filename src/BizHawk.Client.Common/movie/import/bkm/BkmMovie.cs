using System;
using System.Collections.Generic;
using System.IO;

namespace BizHawk.Client.Common
{
	internal class BkmMovie
	{
		private readonly List<string> _log = new List<string>();
		private int? _loopOffset;

		public string PreferredExtension => "bkm";

		public BkmHeader Header { get; } = new BkmHeader();
		public string Filename { get; set; } = "";
		public bool Loaded { get; private set; }
		
		public int InputLogLength => _log.Count;

		public double FrameCount
		{
			get
			{
				if (_loopOffset.HasValue)
				{
					return double.PositiveInfinity;
				}
				
				return Loaded ? _log.Count : 0;
			}
		}

		public BkmControllerAdapter GetInputState(int frame)
		{
			if (frame < FrameCount && frame >= 0)
			{
				int getFrame;

				if (_loopOffset.HasValue)
				{
					if (frame < _log.Count)
					{
						getFrame = frame;
					}
					else
					{
						getFrame = ((frame - _loopOffset.Value) % (_log.Count - _loopOffset.Value)) + _loopOffset.Value;
					}
				}
				else
				{
					getFrame = frame;
				}

				var adapter = new BkmControllerAdapter
				{
					Definition = Global.MovieSession.MovieController.Definition
				};
				adapter.SetControllersAsMnemonic(_log[getFrame]);
				return adapter;
			}

			return null;
		}

		public IDictionary<string, string> HeaderEntries => Header;

		public SubtitleList Subtitles => Header.Subtitles;

		public IList<string> Comments => Header.Comments;

		public string SyncSettingsJson
		{
			get => Header[HeaderKeys.SyncSettings];
			set => Header[HeaderKeys.SyncSettings] = value;
		}

		public string TextSavestate { get; set; }
		public byte[] BinarySavestate { get; set; }

		public bool Load()
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
					if (line == "")
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
			{
				BinarySavestate = Convert.FromBase64String(Header.SavestateBinaryBase64Blob);
			}

			Loaded = true;
			return true;
		}
	}
}