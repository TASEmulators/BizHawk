using System;
using System.Collections.Generic;
using System.IO;
using BizHawk.Common;

namespace BizHawk.Emulation.Common
{
	public class NullEmulator : IEmulator, IVideoProvider, ISyncSoundProvider, ISoundProvider
	{
		public string SystemId { get { return "NULL"; } }
		public static readonly ControllerDefinition NullController = new ControllerDefinition { Name = "Null Controller" };

		public string BoardName { get { return null; } }

		private readonly int[] frameBuffer = new int[256 * 192];
		private readonly Random rand = new Random();
		public CoreComm CoreComm { get; private set; }
		public IVideoProvider VideoProvider { get { return this; } }
		public ISoundProvider SoundProvider { get { return this; } }
		public ISyncSoundProvider SyncSoundProvider { get { return this; } }
		public bool StartAsyncSound() { return true; }
		public void EndAsyncSound() { }

		public NullEmulator(CoreComm comm)
		{
			CoreComm = comm;
			var domains = new MemoryDomainList(
				new List<MemoryDomain>
				{
					new MemoryDomain("Main RAM", 1, MemoryDomain.Endian.Little, addr => 0, (a, v) => { })
				});
			memoryDomains = new MemoryDomainList(domains);

			var d = DateTime.Now;
			xmas = d.Month == 12 && d.Day >= 17 && d.Day <= 27;
			if (xmas)
				pleg = new Pleg();
		}
		public void ResetCounters()
		{
			Frame = 0;
			// no lag frames on this stub core
		}

		public void FrameAdvance(bool render, bool rendersound)
		{
			if (render == false) return;
			for (int i = 0; i < 256 * 192; i++)
			{
				byte b = (byte)rand.Next();
				if (xmas)
					frameBuffer[i] = Colors.ARGB(b, (byte)(255 - b), 0, 255);
				else
					frameBuffer[i] = Colors.Luminosity((byte) rand.Next());
			}
		}
		public ControllerDefinition ControllerDefinition { get { return NullController; } }
		public IController Controller { get; set; }

		public int Frame { get; set; }
		public int LagCount { get { return 0; } set { return; } }
		public bool IsLagFrame { get { return false; } }

		public byte[] ReadSaveRam() { return null; }
		public void StoreSaveRam(byte[] data) { }
		public void ClearSaveRam() { }
		public bool DeterministicEmulation { get { return true; } }
		public bool SaveRamModified { get; set; }
		public void SaveStateText(TextWriter writer) { }
		public void LoadStateText(TextReader reader) { }
		public void SaveStateBinary(BinaryWriter writer) { }
		public void LoadStateBinary(BinaryReader reader) { }
		public byte[] SaveStateBinary() { return new byte[1]; }
		public bool BinarySaveStatesPreferred { get { return false; } }
		public int[] GetVideoBuffer() { return frameBuffer; }
		public int VirtualWidth { get { return 256; } }
		public int BufferWidth { get { return 256; } }
		public int BufferHeight { get { return 192; } }
		public int BackgroundColor { get { return 0; } }
		private readonly MemoryDomainList memoryDomains;
		public MemoryDomainList MemoryDomains { get { return memoryDomains; } }
		public void Dispose() { }

		public Dictionary<string, int> GetCpuFlagsAndRegisters()
		{
			return new Dictionary<string, int>();
		}

		bool xmas;
		Pleg pleg;

		short[] sampbuff = new short[735 * 2];

		public void GetSamples(out short[] samples, out int nsamp)
		{
			nsamp = 735;
			samples = sampbuff;
			if (xmas)
				pleg.Generate(samples);
		}

		public void DiscardSamples()
		{
		}

		public void GetSamples(short[] samples)
		{
			if (xmas)
				pleg.Generate(samples);
		}

		public int MaxVolume
		{
			get;
			set;
		}

		public object GetSettings() { return null; }
		public object GetSyncSettings() { return null; }
		public bool PutSettings(object o) { return false; }
		public bool PutSyncSettings(object o) { return false; }
	}

	public class NullSound : ISoundProvider
	{
		public static readonly NullSound SilenceProvider = new NullSound();

		public void GetSamples(short[] samples) { }
		public void DiscardSamples() { }
		public int MaxVolume { get; set; }
	}

	#region super tone generator

	class Pleg
	{
		string data = "H4sICI/2sVICAG91dDMudHh0AOxazdLbIAy8d6bvgkFImFsvufb936Yt3YyKvjBY5UvS6XDSxOZndyULy9H3ylLD1y8/baxHs/Lb5rNG2IT7zVKq9Msmrmf7Tb/st3qcP4ff7rdhb7itw04eXrVzsYWOTuXTt7yzl/OXvYHtDWwN+0cQi0IcqzJnxtchy9lDbo5rVODAAJvbdXWk1PiQooBiMBQPnxcOnYbhfkoCSgGUMmLxbgsoCSgdoCSgFEApwxZQArZ0uryWTp227DUBxVzDpbXLNUhlAVIGJELsZ6hb+kzACdePGqFqxPiE8QnjEualCcUZtb+mRKAUP0tlfyxHQAiIZUEsJ6gZYVXtTlVOiGWBmhk29qoS+zIQ6zQvJZ3rUHFtSwm9I++q5WJUS1At90mNAywhA/CqausZIPaPG/Jtgwhq6ug3qU5GdZMRMg+OmNR7IxfjjQwbDLXD5Q09Yta9QcfqKQfkz4Aw3fptrP0xNVfsCVu++j1S55KPJem01Yi2Bw/R27N2yxfj9znNI9TnESo1dikyT7J68aledNqi6vO1yjUI5RkQplu/mTWRf8u7LVTzZeXaaBRNeUxDTozimi8HRhuNqM/XJZOoiK5IeLJFOF5bEV3XSBGxeHiwjDSbaTXRBkhmuBUBU83T9IiK/wEPUmQOf3RIZxqxI2YVEQfDy7C3VZzJuWTqDuTkDzmW9PUT49KfXHIAlzD0s+qk6CJWx2ptFdzt9mqWsuYF6KT6aBoRAmWGK3MPMfEIkoHg2JIRPfajC39U1/K2TCeQ3SrqHi4V+YSK8VUq2hJoriKDd3So+NJYtBTUnvV4jaqq1omtCVYGsdi9RVmIyDdzqJoPNLdZ6O0q5MhzKh8LUAIFGQSIraFFA8VSg0QOagAJ+5xY1xpaBrGel2I9j2Nd63Kiv8u7tBDb5Mu7xaiYH6uovAcq0ttV5KIxvq6iMxb/HxV7CmpLPV6i6vhrGZdRHp5Us/SEPEwmD5eaXQEzycN5kIfZ5GHjDS7LediftAaxH/DN0r5riPWOLXld3xiI/unqWhgqnbCHieGzU8v9/YJK2wWrSqxHA0404bv+7yjpy1G7HwGBFAoiOIJw9PsABHVVHhBc+G8UJyAAYwv1lJASaZZAiPFbzCN6Pq7zKPq+pUWdtuy7oo9qp2YCNe59xGwe0RmWco1CWaDAfeKUA95KfXmA6+qlWKOpwieUZlTW/0NNSqH9DoAcAfmosUuYx2d5wf+MpP4ZYYbqAdBpoP5x73ExrRFHXwuKpSa+Z0R0mo+aFqsygKRrj9SerYqrZu1V3CRuqRbougPdId0qxLlfR6Psgam9PBxhT+wd+71zcKmeg05bVBWQboBkIF7Zq8xWxdXJ2iuZfILTSuil/SxIqSxDu+bX+RHOYjIxwUZTQIgeKoOuQ2Ac993tbsTdjbi7EXc34u5G3N2IuxtxdyPubsTdjbi7EXc34u5G3N2IuxtxdyPubsTdjbi7EXc34o927dAGAACEgeB27D8SEoVBleRmqGg+ORqRRqQRaUQakUakEWlEGjG1rmlEGpFGpBFpRBqRRqQRaUQakUakEWlEGpFGpBFpRBqRRqQRaUQakUakEWlEGpFGpBFpRBqRRqQRaUQakUb86OhoRBqRRqQRk+qaRqQRaUQakUakEWlEGpFGpBFvGnFXiHMetSzUwqZz46p5AAA=";
		List<string> Lines = new List<string>();
		int LineIDX = 0;

		public Pleg()
		{
			var gz = new System.IO.Compression.GZipStream(
				new MemoryStream(Convert.FromBase64String(data), false),
				System.IO.Compression.CompressionMode.Decompress);
			var tr = new StreamReader(gz);
			string line;
			while ((line = tr.ReadLine()) != null)
				Lines.Add(line);
		}

		List<SinMan> SinMen = new List<SinMan>();

		int deadtime = 0;

		void Off(int c, int n)
		{
			foreach (var s in SinMen)
			{
				if (s.c == c && s.n == n && !s.fading)
					s.fading = true;
			}
		}
		void On(int c, int n)
		{
			if (c == 9)
				return;
			var s = new SinMan(1500, n);
			s.c = c;
			s.n = n;
			SinMen.Add(s);
		}

		short Next()
		{
			int ret = 0;
			for (int i = 0; i < SinMen.Count; i++)
			{
				var s = SinMen[i];
				if (s.Done)
				{
					SinMen.RemoveAt(i);
					i--;
				}
				else
				{
					ret += s.Next();
				}
			}
			if (ret > 32767) ret = 32767;
			if (ret < -32767) ret = -32767;
			return (short)ret;
		}

		string FetchNext()
		{
			string ret = Lines[LineIDX];
			LineIDX++;
			if (LineIDX == Lines.Count)
				LineIDX = 0;
			return ret;
		}

		public void Generate(short[] dest)
		{
			int idx = 0;
			while (idx < dest.Length)
			{
				if (deadtime > 0)
				{
					short n = Next();
					dest[idx++] = n;
					dest[idx++] = n;
					deadtime--;
				}
				else
				{
					string[] s = FetchNext().Split(':');
					char c = s[0][0];
					if (c == 'A')
						deadtime = int.Parse(s[1]) * 40;
					else if (c == 'O')
						On(int.Parse(s[2]), int.Parse(s[1]));
					else if (c == 'F')
						Off(int.Parse(s[2]), int.Parse(s[1]));
				}
			}

		}
	}

	class SinMan
	{
		public int c;
		public int n;

		double theta;
		double freq;
		double amp;

		public bool fading = false;

		public bool Done { get { return amp < 2.0; } }

		static double GetFreq(int note)
		{
			return Math.Pow(2.0, note / 12.0) * 13.0;
		}

		public short Next()
		{
			short result = (short)(Math.Sin(theta) * amp);
			theta += freq * Math.PI / 22050.0;
			if (theta >= Math.PI * 2.0)
				theta -= Math.PI * 2.0;
			if (fading)
				amp *= 0.87;
			return result;
		}

		public SinMan(int amp, int note)
		{
			this.amp = amp;
			this.freq = GetFreq(note);
		}

	}

	#endregion
}
