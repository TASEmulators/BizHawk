using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Windows.Forms;
using System.Xml.XPath;
using System.Xml.Linq;

using BizHawk.Client.Common;
using BizHawk.Common;
using BizHawk.Emulation.Cores.Nintendo.NES;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class NESMusicRipper : ToolFormBase, IToolFormAutoConfig
	{
		private static readonly FilesystemFilterSet RenoiseFilesFSFilterSet = new(new FilesystemFilter("Renoise Song Files", new[] { "xrns" }))
		{
			AppendAllFilesEntry = false,
		};

		public static Icon ToolIcon
			=> Properties.Resources.NesControllerIcon;

		[RequiredService]
		private NES Nes { get; set; }

		protected override string WindowTitleStatic => "Music Ripper";

		public NESMusicRipper()
		{
			InitializeComponent();
			Icon = ToolIcon;
			SyncContents();
		}

		private bool _isRunning;

		// http://www.phy.mtu.edu/~suits/notefreqs.html
		// begins at C0. ends at B8
		private static readonly float[] FreqTable =
		{
			0, 16.35f,17.32f,18.35f,19.45f,20.6f,21.83f,23.12f,24.5f,25.96f,27.5f,29.14f,30.87f,32.7f,34.65f,36.71f,38.89f,41.2f,43.65f,46.25f,49f,51.91f,55f,58.27f,61.74f,65.41f,69.3f,73.42f,77.78f,82.41f,87.31f,92.5f,98f,103.83f,110f,116.54f,123.47f,130.81f,138.59f,146.83f,155.56f,164.81f,174.61f,185f,196f,207.65f,220f,233.08f,246.94f,261.63f,277.18f,293.66f,311.13f,329.63f,349.23f,369.99f,392f,415.3f,440f,466.16f,493.88f,523.25f,554.37f,587.33f,622.25f,659.25f,698.46f,739.99f,783.99f,830.61f,880f,932.33f,987.77f,1046.5f,1108.73f,1174.66f,1244.51f,1318.51f,1396.91f,1479.98f,1567.98f,1661.22f,1760f,1864.66f,1975.53f,2093f,2217.46f,2349.32f,2489.02f,2637.02f,2793.83f,2959.96f,3135.96f,3322.44f,3520f,3729.31f,3951.07f,4186.01f,4434.92f,4698.63f,4978.03f,5274.04f,5587.65f,5919.91f,6271.93f,6644.88f,7040f,7458.62f,7902.13f, 1000000
		};

		private static readonly string[] NoteNames = { "C-", "C#", "D-", "D#", "E-", "F-", "F#", "G-", "G#", "A-", "A#", "B-" };

		private string NameForNote(int note)
		{
			int tone = note % 12;
			int octave = note / 12;
			return NoteNames[tone] + octave;
		}

		// this isn't thoroughly debugged but it seems to work OK
		// pitch bends are massively broken anyway
		private int FindNearestNote(float freq)
		{
			for (int i = 1; i < FreqTable.Length; i++)
			{
				float a = FreqTable[i - 1];
				float b = FreqTable[i];
				float c = FreqTable[i + 1];
				var range = ((a + b) / 2).RangeTo((b + c) / 2);
				if (range.Contains(freq))
				{
					return i - 1;
				}
			}

			return 95; // I guess?
		}

		private struct PulseState
		{
			public bool En;
			public byte Vol;
			public byte Type;
			public int Note;
		}

		private struct TriangleState
		{
			public bool En;
			public int Note;
		}

		private struct NoiseState
		{
			public bool En;
			public byte Vol;
			public int Note;
		}

		private class ApuState
		{
			public PulseState Pulse0;
			public PulseState Pulse1;
			public TriangleState Triangle;
			public NoiseState Noise;
		}

		private void Export_Click(object sender, EventArgs e)
		{
			//acquire target
			var outPath = this.ShowFileSaveDialog(
				filter: RenoiseFilesFSFilterSet,
				initDir: Config!.PathEntries.ToolsAbsolutePath());
			if (outPath is null) return;

			// configuration:
			string templatePath = Path.Combine(Path.GetDirectoryName(outPath) ?? "", "template.xrns");
			int configuredPatternLength = int.Parse(txtPatternLength.Text);


			// load template
			XElement templateRoot;
			using (var zfTemplate = new ZipArchive(new FileStream(templatePath, FileMode.Open, FileAccess.Read), ZipArchiveMode.Read))
			{
				var entry = zfTemplate.Entries.Single(entry => entry.FullName == "Song.xml");
				using var stream = entry.Open();
				templateRoot = XElement.Load(stream);
			}

			//get the pattern pool, and whack the child nodes
			var xPatterns = templateRoot.XPathSelectElement("//Patterns");
			var xPatternPool = xPatterns.Parent;
			xPatterns.Remove();

			var writer = new StringWriter();
			writer.WriteLine("<Patterns>");


			int pulse0LastNote = -1;
			int pulse0LastType = -1;
			int pulse1LastNote = -1;
			int pulse1LastType = -1;
			int triLastNote = -1;
			int noiseLastNote = -1;

			int patternCount = 0;
			int time = 0;
			while (time < _log.Count)
			{
				patternCount++;

				//begin writing pattern: open the tracks list
				writer.WriteLine("<Pattern>");
				writer.WriteLine("<NumberOfLines>{0}</NumberOfLines>", configuredPatternLength);
				writer.WriteLine("<Tracks>");

				//write the pulse tracks
				for (int track = 0; track < 2; track++)
				{
					writer.WriteLine("<PatternTrack type=\"PatternTrack\">");
					writer.WriteLine("<Lines>");

					int lastNote = track == 0 ? pulse0LastNote : pulse1LastNote;
					int lastType = track == 0 ? pulse0LastType : pulse1LastType;
					for (int i = 0; i < configuredPatternLength; i++)
					{
						int patLine = i;

						int index = i + time;
						if (index >= _log.Count) continue;

						var rec = _log[index];

						var pulse = track switch
						{
							0 => rec.Pulse0,
							1 => rec.Pulse1,
							_ => default
						};

						// transform quieted notes to dead notes
						// blech its buggy, im tired
						////if (pulse.vol == 0)
						////  pulse.en = false;

						bool keyOff = false, keyOn = false;
						if (lastNote != -1 && !pulse.En)
						{
							lastNote = -1;
							lastType = -1;
							keyOff = true;
						}
						else if (lastNote != pulse.Note && pulse.En)
						{
							keyOn = true;
						}

						if (lastType != pulse.Type && pulse.Note != -1)
						{
							keyOn = true;
						}

						if (pulse.En)
						{
							lastNote = pulse.Note;
							lastType = pulse.Type;
						}

						writer.WriteLine("<Line index=\"{0}\">", patLine);
						writer.WriteLine("<NoteColumns>");
						writer.WriteLine("<NoteColumn>");
						if (keyOn)
						{
							writer.WriteLine("<Note>{0}</Note>", NameForNote(pulse.Note));
							writer.WriteLine("<Instrument>{0:X2}</Instrument>", pulse.Type);
						}
						else if (keyOff)
						{
							writer.WriteLine("<Note>OFF</Note>");
						}

						if (lastNote != -1)
						{
							writer.WriteLine("<Volume>{0:X2}</Volume>", pulse.Vol * 8);
						}

						writer.WriteLine("</NoteColumn>");
						writer.WriteLine("</NoteColumns>");
						writer.WriteLine("</Line>");
					}

					// close PatternTrack
					writer.WriteLine("</Lines>");
					writer.WriteLine("</PatternTrack>");

					if (track == 0)
					{
						pulse0LastNote = lastNote;
						pulse0LastType = lastType;
					}
					else
					{
						pulse1LastNote = lastNote;
						pulse1LastType = lastType;
					}

				} // pulse tracks loop

				// triangle track generation
				{
					writer.WriteLine("<PatternTrack type=\"PatternTrack\">");
					writer.WriteLine("<Lines>");

					for (int i = 0; i < configuredPatternLength; i++)
					{
						int patLine = i;

						int index = i + time;
						if (index >= _log.Count) continue;

						var rec = _log[index];

						TriangleState tri = rec.Triangle;

						{
							bool keyOff = false, keyOn = false;
							if (triLastNote != -1 && !tri.En)
							{
								triLastNote = -1;
								keyOff = true;
							}
							else if (triLastNote != tri.Note && tri.En)
								keyOn = true;

							if(tri.En)
								triLastNote = tri.Note;

							writer.WriteLine("<Line index=\"{0}\">", patLine);
							writer.WriteLine("<NoteColumns>");
							writer.WriteLine("<NoteColumn>");
							if (keyOn)
							{
								writer.WriteLine("<Note>{0}</Note>", NameForNote(tri.Note));
								writer.WriteLine("<Instrument>08</Instrument>");
							}
							else if (keyOff)
							{
								writer.WriteLine("<Note>OFF</Note>");
							}

							// no need for tons of these
							////if(keyon) writer.WriteLine("<Volume>80</Volume>");

							writer.WriteLine("</NoteColumn>");
							writer.WriteLine("</NoteColumns>");
							writer.WriteLine("</Line>");
						}
					}

					// close PatternTrack
					writer.WriteLine("</Lines>");
					writer.WriteLine("</PatternTrack>");
				}

				// noise track generation
				{
					writer.WriteLine("<PatternTrack type=\"PatternTrack\">");
					writer.WriteLine("<Lines>");

					for (int i = 0; i < configuredPatternLength; i++)
					{
						int patLine = i;

						int index = i + time;
						if (index >= _log.Count) continue;

						var rec = _log[index];

						NoiseState noise = rec.Noise;

						// transform quieted notes to dead notes
						// blech its buggy, im tired
						////if (noise.vol == 0)
						//// noise.en = false;

						{
							bool keyOff = false, keyOn = false;
							if (noiseLastNote != -1 && !noise.En)
							{
								noiseLastNote = -1;
								keyOff = true;
							}
							else if (noiseLastNote != noise.Note && noise.En)
							{
								keyOn = true;
							}

							if (noise.En)
							{
								noiseLastNote = noise.Note;
							}

							writer.WriteLine("<Line index=\"{0}\">", patLine);
							writer.WriteLine("<NoteColumns>");
							writer.WriteLine("<NoteColumn>");
							if (keyOn)
							{
								writer.WriteLine("<Note>{0}</Note>", NameForNote(noise.Note));
								writer.WriteLine("<Instrument>04</Instrument>");
							}
							else if (keyOff) writer.WriteLine("<Note>OFF</Note>");

							if (noiseLastNote != -1)
								writer.WriteLine("<Volume>{0:X2}</Volume>", noise.Vol * 8);

							writer.WriteLine("</NoteColumn>");
							writer.WriteLine("</NoteColumns>");
							writer.WriteLine("</Line>");
						}
					}

					// close PatternTrack
					writer.WriteLine("</Lines>");
					writer.WriteLine("</PatternTrack>");
				} // noise track generation

				// write empty track for now for pcm
				for (int track = 4; track < 5; track++)
				{
					writer.WriteLine("<PatternTrack type=\"PatternTrack\">");
					writer.WriteLine("<Lines>");
					writer.WriteLine("</Lines>");
					writer.WriteLine("</PatternTrack>");
				}

				// we definitely need a dummy master track now
				writer.WriteLine("<PatternMasterTrack type=\"PatternMasterTrack\">");
				writer.WriteLine("</PatternMasterTrack>");

				// close tracks
				writer.WriteLine("</Tracks>");

				// close pattern
				writer.WriteLine("</Pattern>");

				time += configuredPatternLength;

			} // main pattern loop

			writer.WriteLine("</Patterns>");
			writer.Flush();

			var xNewPatternList = XElement.Parse(writer.ToString());
			xPatternPool.Add(xNewPatternList);

			//write pattern sequence
			writer = new StringWriter();
			writer.WriteLine("<SequenceEntries>");
			for (int i = 0; i < patternCount; i++)
			{
				writer.WriteLine("<SequenceEntry>");
				writer.WriteLine("<IsSectionStart>false</IsSectionStart>");
				writer.WriteLine("<Pattern>{0}</Pattern>", i);
				writer.WriteLine("</SequenceEntry>");
			}
			writer.WriteLine("</SequenceEntries>");

			var xPatternSequence = templateRoot.XPathSelectElement("//PatternSequence");
			xPatternSequence.XPathSelectElement("SequenceEntries").Remove();
			xPatternSequence.Add(XElement.Parse(writer.ToString()));

			//copy template file to target
			File.Delete(outPath);
			File.Copy(templatePath, outPath);

			using var zfOutput = new ZipArchive(new FileStream(outPath, FileMode.Create, FileAccess.Write), ZipArchiveMode.Create);
			using (var stream = zfOutput.CreateEntry("Song.xml").Open())
			{
				templateRoot.Save(stream);
			}
		}

		private readonly List<ApuState> _log = new List<ApuState>();

		private void DebugCallback()
		{
			////fpulse = fCPU/(16*(t+1)) (where fCPU is 1.789773 MHz for NTSC, 1.662607 MHz for PAL, and 1.773448 MHz for Dendy)
			////ftriangle = fCPU/(32*(tval + 1))

			var apu = Nes.apu;
			
			// evaluate the pitches
			int pulse0Period = apu.pulse[0].timer_reload_value;
			float pulse0Freq = 1789773.0f / (16.0f * (pulse0Period + 1));
			int pulse0Note = FindNearestNote(pulse0Freq);

			int pulse1Period = apu.pulse[1].timer_reload_value;
			float pulse1Freq = 1789773.0f / (16.0f * (pulse1Period + 1));
			int pulse1Note = FindNearestNote(pulse1Freq);

			int triPeriod = apu.triangle.Debug_PeriodValue;
			float triFreq = 1789773.0f / (32.0f * (triPeriod + 1));
			int triNote = FindNearestNote(triFreq);

			//uncertain
			int noisePeriod = apu.noise.Debug_Period;
			float noiseFreq = 1789773.0f / (16.0f * (noisePeriod + 1));
			int noiseNote = FindNearestNote(noiseFreq);

			// create the record
			var rec = new ApuState
			{
				Pulse0 =
				{
					En = !apu.pulse[0].Debug_IsSilenced,
					Vol = (byte) apu.pulse[0].Debug_Volume,
					Note = pulse0Note,
					Type = (byte) apu.pulse[0].Debug_DutyType
				},
				Pulse1 =
				{
					En = !apu.pulse[1].Debug_IsSilenced,
					Vol = (byte) apu.pulse[1].Debug_Volume,
					Note = pulse1Note,
					Type = (byte) apu.pulse[1].Debug_DutyType
				},
				Triangle =
				{
					En = !apu.triangle.Debug_IsSilenced,
					Note = triNote
				},
				Noise =
				{
					En = !apu.noise.Debug_IsSilenced,
					Vol = (byte) apu.noise.Debug_Volume,
					Note = noiseNote
				}
			};

			_log.Add(rec);
			SyncContents();
		}

		private void SyncContents()
		{
			lblContents.Text = $"{_log.Count} Rows";
		}

		private void BtnControl_Click(object sender, EventArgs e)
		{
			if(_isRunning)
			{
				SyncContents();
				Nes.apu.DebugCallback = null;
				Nes.apu.DebugCallbackDivider = 0;
				_isRunning = false;
				btnControl.Text = "Start";
			}
			else
			{
				_log.Clear();
				Nes.apu.DebugCallback = DebugCallback;
				Nes.apu.DebugCallbackDivider = int.Parse(txtDivider.Text);
				_isRunning = true;
				btnControl.Text = "Stop";
			}
		}

		private void NESMusicRipper_FormClosed(object sender, FormClosedEventArgs e)
		{
			var apu = Nes.apu;
			apu.DebugCallbackDivider = 0;
			apu.DebugCallbackTimer = 0;
			apu.DebugCallback = null;
		}
	}
}
