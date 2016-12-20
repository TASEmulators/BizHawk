using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;

using BizHawk.Common;

namespace BizHawk.Emulation.Common
{
	[CoreAttributes("NullHawk", "", false, true)]
	[ServiceNotApplicable(typeof(IStatable), typeof(ISaveRam), typeof(IDriveLight), typeof(ICodeDataLogger), typeof(IMemoryDomains),
		typeof(IDebuggable), typeof(IDisassemblable), typeof(IInputPollable), typeof(IRegionable), typeof(ITraceable))]
	public class NullEmulator : IEmulator, IVideoProvider, ISoundProvider, ISettable<NullEmulator.NullEmulatorSettings, object>
	{
		public NullEmulator(CoreComm comm, object settings)
		{
			SyncMode = SyncSoundMode.Sync;
			ServiceProvider = new BasicServiceProvider(this);
			CoreComm = comm;
			_settings = (NullEmulatorSettings)settings ?? new NullEmulatorSettings();

			var d = DateTime.Now;
			_xmas = d.Month == 12 && d.Day >= 17 && d.Day <= 27;
			if (_xmas)
			{
				_pleg = new Pleg();
			}
		}

		#region IEmulator

		public IEmulatorServiceProvider ServiceProvider { get; private set; }

		public ControllerDefinition ControllerDefinition
		{
			get { return NullController.Instance.Definition; }
		}

		public IController Controller { get; set; }

		public void FrameAdvance(bool render, bool rendersound)
		{
			if (render == false) return;
			if (!_settings.SnowyDisplay)
			{
				if (_frameBufferClear) return;
				_frameBufferClear = true;
				Array.Clear(FrameBuffer, 0, 256 * 192);
				return;
			}

			_frameBufferClear = false;
			if (_xmas)
			{
				for (int i = 0; i < 256 * 192; i++)
				{
					byte b = (byte)Rand.Next();
					FrameBuffer[i] = Colors.ARGB(b, (byte)(255 - b), 0, 255);
				}
			}
			else
			{
				for (int i = 0; i < 256 * 192; i++)
				{
					FrameBuffer[i] = Colors.Luminosity((byte)Rand.Next());
				}
			}

			Frame++;
		}

		public int Frame { get; set; }

		public string SystemId { get { return "NULL"; } }

		public bool DeterministicEmulation { get { return true; } }

		public void ResetCounters()
		{
			Frame = 0;
		}

		public string BoardName { get { return null; } }

		public CoreComm CoreComm { get; private set; }

		public void Dispose() { }

		#endregion

		#region IVideoProvider

		public int[] GetVideoBuffer()
		{
			return FrameBuffer;
		}

		public int VirtualWidth
		{
			get { return 256; }
		}

		public int VirtualHeight
		{
			get { return 192; }
		}

		public int BufferWidth
		{
			get { return 256; }
		}

		public int BufferHeight
		{
			get { return 192; }
		}

		public int BackgroundColor
		{
			get { return 0; }
		}

		#endregion

		#region ISoundProvider

		public void GetSamplesSync(out short[] samples, out int nsamp)
		{
			if (SyncMode != SyncSoundMode.Sync)
			{
				throw new InvalidOperationException("Attempt to call a Sync method in async mode");
			}

			nsamp = 735;
			samples = SampleBuffer;
			if (!_settings.SnowyDisplay)
			{
				return;
			}

			if (_xmas)
			{
				_pleg.Generate(samples);
			}
		}

		public void DiscardSamples()
		{
		}

		public void GetSamplesAsync(short[] samples)
		{
			if (SyncMode != SyncSoundMode.Async)
			{
				throw new InvalidOperationException("Attempt to call an Async method in sync mode");
			}

			if (!_settings.SnowyDisplay)
			{
				return;
			}

			if (_xmas)
			{
				_pleg.Generate(samples);
			}
		}

		public bool CanProvideAsync
		{
			get { return true; }
		}

		public SyncSoundMode SyncMode { get; private set; }

		public void SetSyncMode(SyncSoundMode mode)
		{
			SyncMode = mode;
		}

		#endregion

		#region ISettable

		public NullEmulatorSettings GetSettings()
		{
			return _settings.Clone();
		}

		public object GetSyncSettings()
		{
			return null;
		}

		public bool PutSettings(NullEmulatorSettings o)
		{
			_settings = o;
			return false;
		}

		public bool PutSyncSettings(object o)
		{
			return false;
		}

		#endregion

		private readonly int[] FrameBuffer = new int[256 * 192];
		private readonly short[] SampleBuffer = new short[735 * 2];
		private readonly Random Rand = new Random();

		private bool _frameBufferClear = true;

		private bool _xmas;
		private Pleg _pleg;

		private NullEmulatorSettings _settings;

		public class NullEmulatorSettings
		{
			[DefaultValue(false)]
			public bool SnowyDisplay { get; set; }

			public NullEmulatorSettings()
			{
				SettingsUtil.SetDefaultValues(this);
			}

			public NullEmulatorSettings Clone()
			{
				return (NullEmulatorSettings)MemberwiseClone();
			}
		}
	}

	#region super tone generator

	internal class Bell
	{
		// ms ima adpcm
		private const string dataz = "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";

		private static readonly int[] AdaptationTable = { 230, 230, 230, 230, 307, 409, 512, 614, 768, 614, 512, 409, 307, 230, 230, 230 };
		private static readonly int[] AdaptCoeff1 = { 256, 512, 0, 192, 240, 460, 392 };
		private static readonly int[] AdaptCoeff2 = { 0, -256, 0, 64, 0, -208, -232 };

		private byte[] data;

		public Bell()
		{
			var gz = new System.IO.Compression.GZipStream(
				new MemoryStream(Convert.FromBase64String(dataz), false),
				System.IO.Compression.CompressionMode.Decompress);
			var ms = new MemoryStream();
			gz.CopyTo(ms);
			data = ms.ToArray();
			for (int i = 0; i < 3800; i++) // compenstate for sample start point
			{
				Next();
			}
		}

		private int blockpredictor;
		private int sample1;
		private int sample2;
		private int delta;

		private int idx = 0;
		private bool top = false;

		private int samplectr = 0;
		private const int sampleloop = 15360;

		public short Next()
		{
			int ret;

			if ((idx & 0x3ff) == 0) // start block
			{
				blockpredictor = data[idx] % 7;
				delta = (short)(data[idx + 1] | data[idx + 2] << 8);
				sample1 = (short)(data[idx + 3] | data[idx + 4] << 8);
				sample2 = (short)(data[idx + 5] | data[idx + 6] << 8);

				ret = sample2;
				idx++;
			}
			else if ((idx & 0x3ff) == 1)
			{
				ret = sample1;
				top = true;
				idx += 6;
			}
			else
			{
				int nibble = data[idx];
				if (top)
					nibble >>= 4;
				else
					idx++;
				top ^= true;
				nibble <<= 28;
				nibble >>= 28;

				int predictor = sample1 * AdaptCoeff1[blockpredictor] + sample2 * AdaptCoeff2[blockpredictor];
				predictor >>= 8;
				predictor += nibble * delta;
				if (predictor >= 32767) predictor = 32767;
				if (predictor <= -32768) predictor = -32768;
				ret = predictor;
				sample2 = sample1;
				sample1 = predictor;
				delta = AdaptationTable[nibble & 15] * delta;
				delta >>= 8;
				if (delta < 16) delta = 16;
			}

			samplectr++;
			if (samplectr == sampleloop)
			{
				samplectr = 0;
				idx = 0;
			}

			return (short)ret;
		}
	}

	internal class Pleg
	{
		private const string data = "H4sICI/2sVICAG91dDMudHh0AOxazdLbIAy8d6bvgkFImFsvufb936Yt3YyKvjBY5UvS6XDSxOZndyULy9H3ylLD1y8/baxHs/Lb5rNG2IT7zVKq9Msmrmf7Tb/st3qcP4ff7rdhb7itw04eXrVzsYWOTuXTt7yzl/OXvYHtDWwN+0cQi0IcqzJnxtchy9lDbo5rVODAAJvbdXWk1PiQooBiMBQPnxcOnYbhfkoCSgGUMmLxbgsoCSgdoCSgFEApwxZQArZ0uryWTp227DUBxVzDpbXLNUhlAVIGJELsZ6hb+kzACdePGqFqxPiE8QnjEualCcUZtb+mRKAUP0tlfyxHQAiIZUEsJ6gZYVXtTlVOiGWBmhk29qoS+zIQ6zQvJZ3rUHFtSwm9I++q5WJUS1At90mNAywhA/CqausZIPaPG/Jtgwhq6ug3qU5GdZMRMg+OmNR7IxfjjQwbDLXD5Q09Yta9QcfqKQfkz4Aw3fptrP0xNVfsCVu++j1S55KPJem01Yi2Bw/R27N2yxfj9znNI9TnESo1dikyT7J68aledNqi6vO1yjUI5RkQplu/mTWRf8u7LVTzZeXaaBRNeUxDTozimi8HRhuNqM/XJZOoiK5IeLJFOF5bEV3XSBGxeHiwjDSbaTXRBkhmuBUBU83T9IiK/wEPUmQOf3RIZxqxI2YVEQfDy7C3VZzJuWTqDuTkDzmW9PUT49KfXHIAlzD0s+qk6CJWx2ptFdzt9mqWsuYF6KT6aBoRAmWGK3MPMfEIkoHg2JIRPfajC39U1/K2TCeQ3SrqHi4V+YSK8VUq2hJoriKDd3So+NJYtBTUnvV4jaqq1omtCVYGsdi9RVmIyDdzqJoPNLdZ6O0q5MhzKh8LUAIFGQSIraFFA8VSg0QOagAJ+5xY1xpaBrGel2I9j2Nd63Kiv8u7tBDb5Mu7xaiYH6uovAcq0ttV5KIxvq6iMxb/HxV7CmpLPV6i6vhrGZdRHp5Us/SEPEwmD5eaXQEzycN5kIfZ5GHjDS7LediftAaxH/DN0r5riPWOLXld3xiI/unqWhgqnbCHieGzU8v9/YJK2wWrSqxHA0404bv+7yjpy1G7HwGBFAoiOIJw9PsABHVVHhBc+G8UJyAAYwv1lJASaZZAiPFbzCN6Pq7zKPq+pUWdtuy7oo9qp2YCNe59xGwe0RmWco1CWaDAfeKUA95KfXmA6+qlWKOpwieUZlTW/0NNSqH9DoAcAfmosUuYx2d5wf+MpP4ZYYbqAdBpoP5x73ExrRFHXwuKpSa+Z0R0mo+aFqsygKRrj9SerYqrZu1V3CRuqRbougPdId0qxLlfR6Psgam9PBxhT+wd+71zcKmeg05bVBWQboBkIF7Zq8xWxdXJ2iuZfILTSuil/SxIqSxDu+bX+RHOYjIxwUZTQIgeKoOuQ2Ac993tbsTdjbi7EXc34u5G3N2IuxtxdyPubsTdjbi7EXc34u5G3N2IuxtxdyPubsTdjbi7EXc34o927dAGAACEgeB27D8SEoVBleRmqGg+ORqRRqQRaUQakUakEWlEGjG1rmlEGpFGpBFpRBqRRqQRaUQakUakEWlEGpFGpBFpRBqRRqQRaUQakUakEWlEGpFGpBFpRBqRRqQRaUQakUb86OhoRBqRRqQRk+qaRqQRaUQakUakEWlEGpFGpBFvGnFXiHMetSzUwqZz46p5AAA=";
		private readonly List<SinMan> SinMen = new List<SinMan>();
		private readonly List<string> Lines = new List<string>();
		private readonly Bell Bell = new Bell();

		private int LineIDX = 0;
		private int deadtime = 0;

		public Pleg()
		{
			var gz = new System.IO.Compression.GZipStream(
				new MemoryStream(Convert.FromBase64String(data), false),
				System.IO.Compression.CompressionMode.Decompress);
			var tr = new StreamReader(gz);
			string line;
			while ((line = tr.ReadLine()) != null)
			{
				Lines.Add(line);
			}
		}

		private void Off(int c, int n)
		{
			foreach (var s in SinMen)
			{
				if (s.c == c && s.n == n && !s.fading)
				{
					s.fading = true;
				}
			}
		}

		private void On(int c, int n)
		{
			if (c == 9)
			{
				return;
			}

			var s = new SinMan(1500, n);
			s.c = c;
			s.n = n;
			SinMen.Add(s);
		}

		private short Next()
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

		private string FetchNext()
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
					n += Bell.Next();
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

	internal class SinMan
	{
		public int c;
		public int n;

		double theta;
		double freq;
		double amp;

		public bool fading = false;

		public bool Done
		{
			get { return amp < 2.0; }
		}

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
