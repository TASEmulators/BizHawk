using System.Collections.Generic;
using System.Globalization;
using System.IO;

using BizHawk.Common.NumberExtensions;
using BizHawk.Common.StringExtensions;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class BasicMovieInfo : IBasicMovieInfo
	{
		private string _filename;
		private bool IsPal => Header[HeaderKeys.Pal] == "1";

		protected readonly Bk2Header Header = new();

		public BasicMovieInfo(string filename)
		{
			if (string.IsNullOrWhiteSpace(filename))
			{
				throw filename is null
					? new ArgumentNullException(paramName: nameof(filename))
					: new ArgumentException(message: "path cannot be blank", paramName: nameof(filename));
			}

			Filename = filename;
		}

		public string Name { get; private set; }

		public string Filename
		{
			get => _filename;
			set
			{
				_filename = value;
				Name = Path.GetFileName(Filename);
			}
		}

		public virtual int FrameCount { get; private set; }

		public TimeSpan TimeLength
		{
			get
			{
				double numSeconds;

				if (Header.TryGetValue(HeaderKeys.CycleCount, out var numCyclesStr) && Header.TryGetValue(HeaderKeys.ClockRate, out var clockRateStr))
				{
					var numCycles = ulong.Parse(numCyclesStr);
					var clockRate = double.Parse(clockRateStr, CultureInfo.InvariantCulture);
					numSeconds = numCycles / clockRate;
				}
				else
				{
					var numFrames = (ulong)FrameCount;
					numSeconds = numFrames / FrameRate;
				}

				return TimeSpan.FromSeconds(numSeconds);
			}
		}

		public double FrameRate
		{
			get
			{
				if (SystemID == VSystemID.Raw.Arcade && Header.TryGetValue(HeaderKeys.VsyncAttoseconds, out var vsyncAttoStr))
				{
					const decimal attosInSec = 1_000_000_000_000_000_000.0M;
					var m = attosInSec;
					m /= ulong.Parse(vsyncAttoStr);
					return m.ConvertToF64();
				}

				return PlatformFrameRates.GetFrameRate(SystemID, IsPal);
			}
		}

		public SubtitleList Subtitles { get; } = new();
		public IList<string> Comments { get; } = new List<string>();

		public virtual string GameName
		{
			get => Header[HeaderKeys.GameName];
			set => Header[HeaderKeys.GameName] = value;
		}

		public virtual string SystemID
		{
			get => Header[HeaderKeys.Platform];
			set => Header[HeaderKeys.Platform] = value;
		}

		public virtual ulong Rerecords
		{
			get => Header.TryGetValue(HeaderKeys.Rerecords, out var rerecords) ? ulong.Parse(rerecords) : 0;
			set => Header[HeaderKeys.Rerecords] = value.ToString();
		}

		public virtual string Hash
		{
			get => Header[HeaderKeys.Sha1].ToUpperInvariant();
			set => Header[HeaderKeys.Sha1] = value;
		}

		public virtual string Author
		{
			get => Header[HeaderKeys.Author];
			set => Header[HeaderKeys.Author] = value;
		}

		public virtual string Core
		{
			get => Header[HeaderKeys.Core];
			set => Header[HeaderKeys.Core] = value;
		}

		public virtual string BoardName
		{
			get => Header[HeaderKeys.BoardName];
			set => Header[HeaderKeys.BoardName] = value;
		}

		public virtual string EmulatorVersion
		{
			get => Header[HeaderKeys.EmulatorVersion];
			set => Header[HeaderKeys.EmulatorVersion] = value;
		}

		public virtual string OriginalEmulatorVersion
		{
			get => Header[HeaderKeys.OriginalEmulatorVersion];
			set => Header[HeaderKeys.OriginalEmulatorVersion] = value;
		}

		public virtual string FirmwareHash
		{
			get => Header[HeaderKeys.FirmwareSha1].ToUpperInvariant();
			set => Header[HeaderKeys.FirmwareSha1] = value;
		}

		public IDictionary<string, string> HeaderEntries => Header;

		public bool Load()
		{
			if (!File.Exists(Filename))
			{
				return false;
			}

			try
			{
				using var bl = ZipStateLoader.LoadAndDetect(Filename, true);
				if (bl is null) return false;
				ClearBeforeLoad();
				LoadFields(bl);
				if (FrameCount == 0)
				{
					// only iterate the input log if it hasn't been loaded already
					LoadFramecount(bl);
				}

				return true;
			}
			catch (InvalidDataException e) when (e.StackTrace.Contains("ZipArchive.ReadEndOfCentralDirectory"))
			{
				throw new Exception("Archive appears to be corrupt. Make a backup, then try to repair it with e.g. 7-Zip.", e);
			}
		}

		protected virtual void ClearBeforeLoad()
		{
			Header.Clear();
			Subtitles.Clear();
			Comments.Clear();
		}

		protected virtual void LoadFields(ZipStateLoader bl)
		{
			bl.GetLump(BinaryStateLump.Movieheader, abort: true, tr =>
			{
				while (tr.ReadLine() is string line)
				{
					if (!string.IsNullOrWhiteSpace(line))
					{
						var pair = line.Split(new[] { ' ' }, 2, StringSplitOptions.RemoveEmptyEntries);

						if (pair.Length > 1)
						{
							if (!Header.ContainsKey(pair[0]))
							{
								Header.Add(pair[0], pair[1]);
							}
						}
					}
				}
			});

			bl.GetLump(BinaryStateLump.Comments, abort: false, tr =>
			{
				while (tr.ReadLine() is string line)
				{
					if (!string.IsNullOrWhiteSpace(line))
					{
						Comments.Add(line);
					}
				}
			});

			bl.GetLump(BinaryStateLump.Subtitles, abort: false, tr =>
			{
				while (tr.ReadLine() is string line)
				{
					if (!string.IsNullOrWhiteSpace(line))
					{
						Subtitles.AddFromString(line);
					}
				}

				Subtitles.Sort();
			});
		}

		private void LoadFramecount(ZipStateLoader bl)
		{
			bl.GetLump(BinaryStateLump.Input, abort: true, tr =>
			{
				// just skim through the input log and count input lines
				// FIXME: this is potentially expensive and shouldn't be necessary for something as simple as frame count
				while (tr.ReadLine() is string line) if (line.StartsWith('|')) FrameCount++;
			});
		}
	}
}
