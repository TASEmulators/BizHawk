using System;
using System.Xml;
using System.Xml.XPath;
using System.Xml.Linq;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using BizHawk.Client.Common;
using BizHawk.Emulation.Cores.Nintendo.NES;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public partial class NESMusicRipper : Form, IToolFormAutoConfig
	{
		[RequiredService]
		private NES nes { get; set; }

		public NESMusicRipper()
		{
			InitializeComponent();
			SyncContents();
		}

		public bool AskSaveChanges() { return true; }
		public bool UpdateBefore { get { return true; } }

		public void Restart()
		{
		}

		public void NewUpdate(ToolFormUpdateType type) { }

		public void UpdateValues()
		{
		}

		public void FastUpdate()
		{
			// Do nothing
		}

		bool IsRunning;

		//http://www.phy.mtu.edu/~suits/notefreqs.html
		//begins at C0. ends at B8
		static readonly float[] freqtbl = new[] {0,
			16.35f,17.32f,18.35f,19.45f,20.6f,21.83f,23.12f,24.5f,25.96f,27.5f,29.14f,30.87f,32.7f,34.65f,36.71f,38.89f,41.2f,43.65f,46.25f,49f,51.91f,55f,58.27f,61.74f,65.41f,69.3f,73.42f,77.78f,82.41f,87.31f,92.5f,98f,103.83f,110f,116.54f,123.47f,130.81f,138.59f,146.83f,155.56f,164.81f,174.61f,185f,196f,207.65f,220f,233.08f,246.94f,261.63f,277.18f,293.66f,311.13f,329.63f,349.23f,369.99f,392f,415.3f,440f,466.16f,493.88f,523.25f,554.37f,587.33f,622.25f,659.25f,698.46f,739.99f,783.99f,830.61f,880f,932.33f,987.77f,1046.5f,1108.73f,1174.66f,1244.51f,1318.51f,1396.91f,1479.98f,1567.98f,1661.22f,1760f,1864.66f,1975.53f,2093f,2217.46f,2349.32f,2489.02f,2637.02f,2793.83f,2959.96f,3135.96f,3322.44f,3520f,3729.31f,3951.07f,4186.01f,4434.92f,4698.63f,4978.03f,5274.04f,5587.65f,5919.91f,6271.93f,6644.88f,7040f,7458.62f,7902.13f,
			1000000
		};

		static readonly string[] noteNames = new[] { "C-", "C#", "D-", "D#", "E-", "F-", "F#", "G-", "G#", "A-", "A#", "B-" };

		string NameForNote(int note)
		{
			int tone = note % 12;
			int octave = note / 12;
			return string.Format("{0}{1}", noteNames[tone], octave);
		}

		//this isnt thoroughly debugged but it seems to work OK
		//pitch bends are massively broken anyway
		int FindNearestNote(float freq)
		{
			for (int i = 1; i < freqtbl.Length; i++)
			{
				float a = freqtbl[i - 1];
				float b = freqtbl[i];
				float c = freqtbl[i + 1];
				float min = (a + b) / 2;
				float max = (b + c) / 2;
				if (freq >= min && freq <= max)
					return i - 1;
			}
			return 95; //I guess?
		}

		struct PulseState
		{
			public bool en;
			public byte vol, type;
			public int note;
		}

		struct TriangleState
		{
			public bool en;
			public int note;
		}

		struct NoiseState
		{
			public bool en;
			public byte vol;
			public int note;
		}

		class ApuState
		{
			public PulseState pulse0, pulse1;
			public TriangleState triangle;
			public NoiseState noise;
		}

		class Stupid : ICSharpCode.SharpZipLib.Zip.IStaticDataSource
		{
			public Stream stream;
			public Stream GetSource() { return stream; }
		}

		private void btnExport_Click(object sender, EventArgs e)
		{
			//acquire target
			var sfd = new SaveFileDialog();
			sfd.Filter = "XRNS (*.xrns)|*.xrns";
			if (sfd.ShowDialog() != System.Windows.Forms.DialogResult.OK)
				return;

			//configuration:
			var outPath = sfd.FileName;
			string templatePath = Path.Combine(Path.GetDirectoryName(outPath), "template.xrns");
			int configuredPatternLength = int.Parse(txtPatternLength.Text);


			//load template
			MemoryStream msSongXml = new MemoryStream();
			var zfTemplate = new ICSharpCode.SharpZipLib.Zip.ZipFile(templatePath);
			{
				int zfSongXmlIndex = zfTemplate.FindEntry("Song.xml", true);
				using (var zis = zfTemplate.GetInputStream(zfTemplate.GetEntry("Song.xml")))
				{
					byte[] buffer = new byte[4096];     // 4K is optimum
					ICSharpCode.SharpZipLib.Core.StreamUtils.Copy(zis, msSongXml, buffer);
				}
			}
			XElement templateRoot = XElement.Parse(System.Text.Encoding.UTF8.GetString(msSongXml.ToArray()));

			//get the pattern pool, and whack the child nodes
			var xPatterns = templateRoot.XPathSelectElement("//Patterns");
			var xPatternPool = xPatterns.Parent;
			xPatterns.Remove();

			var writer = new StringWriter();
			writer.WriteLine("<Patterns>");


			int pulse0_lastNote = -1;
			int pulse0_lastType = -1;
			int pulse1_lastNote = -1;
			int pulse1_lastType = -1;
			int tri_lastNote = -1;
			int noise_lastNote = -1;

			int patternCount = 0;
			int time = 0;
			while (time < Log.Count)
			{
				patternCount++;

				//begin writing pattern: open the tracks list
				writer.WriteLine("<Pattern>");
				writer.WriteLine("<NumberOfLines>{0}</NumberOfLines>", configuredPatternLength);
				writer.WriteLine("<Tracks>");

				//write the pulse tracks
				for (int TRACK = 0; TRACK < 2; TRACK++)
				{
					writer.WriteLine("<PatternTrack type=\"PatternTrack\">");
					writer.WriteLine("<Lines>");

					int lastNote = TRACK == 0 ? pulse0_lastNote : pulse1_lastNote;
					int lastType = TRACK == 0 ? pulse0_lastType : pulse1_lastType;
					for (int i = 0; i < configuredPatternLength; i++)
					{
						int patLine = i;

						int index = i + time;
						if (index >= Log.Count) continue;

						var rec = Log[index];

						PulseState pulse = new PulseState();
						if (TRACK == 0) pulse = rec.pulse0;
						if (TRACK == 1) pulse = rec.pulse1;

						//transform quieted notes to dead notes
						//blech its buggy, im tired
						//if (pulse.vol == 0)
						//  pulse.en = false;

						bool keyoff = false, keyon = false;
						if (lastNote != -1 && !pulse.en)
						{
							lastNote = -1;
							lastType = -1;
							keyoff = true;
						}
						else if (lastNote != pulse.note && pulse.en)
							keyon = true;

						if (lastType != pulse.type && pulse.note != -1)
							keyon = true;

						if (pulse.en)
						{
							lastNote = pulse.note;
							lastType = pulse.type;
						}

						writer.WriteLine("<Line index=\"{0}\">", patLine);
						writer.WriteLine("<NoteColumns>");
						writer.WriteLine("<NoteColumn>");
						if (keyon)
						{
							writer.WriteLine("<Note>{0}</Note>", NameForNote(pulse.note));
							writer.WriteLine("<Instrument>{0:X2}</Instrument>", pulse.type);
						}
						else if (keyoff) writer.WriteLine("<Note>OFF</Note>");

						if(lastNote != -1)
							writer.WriteLine("<Volume>{0:X2}</Volume>", pulse.vol * 8);

						writer.WriteLine("</NoteColumn>");
						writer.WriteLine("</NoteColumns>");
						writer.WriteLine("</Line>");
					}

					//close PatternTrack
					writer.WriteLine("</Lines>");
					writer.WriteLine("</PatternTrack>");

					if (TRACK == 0)
					{
						pulse0_lastNote = lastNote;
						pulse0_lastType = lastType;
					}
					else
					{
						pulse1_lastNote = lastNote;
						pulse1_lastType = lastType;
					}

				} //pulse tracks loop

				//triangle track generation
				{
					writer.WriteLine("<PatternTrack type=\"PatternTrack\">");
					writer.WriteLine("<Lines>");

					for (int i = 0; i < configuredPatternLength; i++)
					{
						int patLine = i;

						int index = i + time;
						if (index >= Log.Count) continue;

						var rec = Log[index];

						TriangleState tri = rec.triangle;

						{
							bool keyoff = false, keyon = false;
							if (tri_lastNote != -1 && !tri.en)
							{
								tri_lastNote = -1;
								keyoff = true;
							}
							else if (tri_lastNote != tri.note && tri.en)
								keyon = true;

							if(tri.en)
								tri_lastNote = tri.note;

							writer.WriteLine("<Line index=\"{0}\">", patLine);
							writer.WriteLine("<NoteColumns>");
							writer.WriteLine("<NoteColumn>");
							if (keyon)
							{
								writer.WriteLine("<Note>{0}</Note>", NameForNote(tri.note));
								writer.WriteLine("<Instrument>08</Instrument>");
							}
							else if (keyoff) writer.WriteLine("<Note>OFF</Note>");

							//no need for tons of these
							//if(keyon) writer.WriteLine("<Volume>80</Volume>");

							writer.WriteLine("</NoteColumn>");
							writer.WriteLine("</NoteColumns>");
							writer.WriteLine("</Line>");
						}
					}

					//close PatternTrack
					writer.WriteLine("</Lines>");
					writer.WriteLine("</PatternTrack>");
				}

				//noise track generation
				{
					writer.WriteLine("<PatternTrack type=\"PatternTrack\">");
					writer.WriteLine("<Lines>");

					for (int i = 0; i < configuredPatternLength; i++)
					{
						int patLine = i;

						int index = i + time;
						if (index >= Log.Count) continue;

						var rec = Log[index];

						NoiseState noise = rec.noise;

						//transform quieted notes to dead notes
						//blech its buggy, im tired
						//if (noise.vol == 0)
						//  noise.en = false;

						{
							bool keyoff = false, keyon = false;
							if (noise_lastNote != -1 && !noise.en)
							{
								noise_lastNote = -1;
								keyoff = true;
							}
							else if (noise_lastNote != noise.note && noise.en)
								keyon = true;

							if (noise.en)
								noise_lastNote = noise.note;

							writer.WriteLine("<Line index=\"{0}\">", patLine);
							writer.WriteLine("<NoteColumns>");
							writer.WriteLine("<NoteColumn>");
							if (keyon)
							{
								writer.WriteLine("<Note>{0}</Note>", NameForNote(noise.note));
								writer.WriteLine("<Instrument>04</Instrument>");
							}
							else if (keyoff) writer.WriteLine("<Note>OFF</Note>");

							if (noise_lastNote != -1)
								writer.WriteLine("<Volume>{0:X2}</Volume>", noise.vol * 8);

							writer.WriteLine("</NoteColumn>");
							writer.WriteLine("</NoteColumns>");
							writer.WriteLine("</Line>");
						}
					}

					//close PatternTrack
					writer.WriteLine("</Lines>");
					writer.WriteLine("</PatternTrack>");
				} //noise track generation

				//write empty track for now for pcm
				for (int TRACK = 4; TRACK < 5; TRACK++)
				{
					writer.WriteLine("<PatternTrack type=\"PatternTrack\">");
					writer.WriteLine("<Lines>");
					writer.WriteLine("</Lines>");
					writer.WriteLine("</PatternTrack>");
				}

				//we definitely need a dummy master track now
				writer.WriteLine("<PatternMasterTrack type=\"PatternMasterTrack\">");
				writer.WriteLine("</PatternMasterTrack>");

				//close tracks
				writer.WriteLine("</Tracks>");

				//close pattern
				writer.WriteLine("</Pattern>");

				time += configuredPatternLength;

			} //main pattern loop

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

			var msOutXml = new MemoryStream();
			templateRoot.Save(msOutXml);
			msOutXml.Flush();
			msOutXml.Position = 0;
			var zfOutput = new ICSharpCode.SharpZipLib.Zip.ZipFile(outPath);
			zfOutput.BeginUpdate();
			zfOutput.Add(new Stupid { stream = msOutXml }, "Song.xml");
			zfOutput.CommitUpdate();
			zfOutput.Close();

			//for easier debugging, write patterndata XML
			//DUMP_TO_DISK(msOutXml.ToArray())
		}


		List<ApuState> Log = new List<ApuState>();

		void DebugCallback()
		{
			//fpulse = fCPU/(16*(t+1)) (where fCPU is 1.789773 MHz for NTSC, 1.662607 MHz for PAL, and 1.773448 MHz for Dendy)
	    //ftriangle = fCPU/(32*(tval + 1))

			var apu = nes.apu;
			
			//evaluate the pitches
			int pulse0_period = apu.pulse[0].timer_reload_value;
			float pulse0_freq = 1789773.0f / (16.0f * (pulse0_period + 1));
			int pulse0_note = FindNearestNote(pulse0_freq);

			int pulse1_period = apu.pulse[1].timer_reload_value;
			float pulse1_freq = 1789773.0f / (16.0f * (pulse1_period + 1));
			int pulse1_note = FindNearestNote(pulse1_freq);

			int tri_period = apu.triangle.Debug_PeriodValue;
			float tri_freq = 1789773.0f / (32.0f * (tri_period + 1));
			int tri_note = FindNearestNote(tri_freq);

			//uncertain
			int noise_period = apu.noise.Debug_Period;
			float noise_freq = 1789773.0f / (16.0f * (noise_period + 1));
			int noise_note = FindNearestNote(noise_freq);

			//create the record
			ApuState rec = new ApuState();
			rec.pulse0.en = !apu.pulse[0].Debug_IsSilenced;
			rec.pulse0.vol = (byte)apu.pulse[0].Debug_Volume;
			rec.pulse0.note = pulse0_note;
			rec.pulse0.type = (byte)apu.pulse[0].Debug_DutyType;
			rec.pulse1.en = !apu.pulse[1].Debug_IsSilenced;
			rec.pulse1.vol = (byte)apu.pulse[1].Debug_Volume;
			rec.pulse1.note = pulse1_note;
			rec.pulse1.type = (byte)apu.pulse[1].Debug_DutyType;
			rec.triangle.en = !apu.triangle.Debug_IsSilenced;
			rec.triangle.note = tri_note;
			rec.noise.en = !apu.noise.Debug_IsSilenced;
			rec.noise.vol = (byte)apu.noise.Debug_Volume;
			rec.noise.note = noise_note;

			Log.Add(rec);

			SyncContents();
		}

		void SyncContents()
		{
			lblContents.Text = string.Format("{0} Rows", Log.Count);
		}

		private void btnControl_Click(object sender, EventArgs e)
		{
			if(IsRunning)
			{
				SyncContents();
				nes.apu.DebugCallback = null;
				nes.apu.DebugCallbackDivider = 0;
				IsRunning = false;
				btnControl.Text = "Start";
			}
			else
			{
				Log.Clear();
				nes.apu.DebugCallback = DebugCallback;
				nes.apu.DebugCallbackDivider = int.Parse(txtDivider.Text);
				IsRunning = true;
				btnControl.Text = "Stop";
			}
		}

		private void ExitMenuItem_Click(object sender, EventArgs e)
		{
			Close();
		}

		private void NESMusicRipper_FormClosed(object sender, FormClosedEventArgs e)
		{
			var apu = nes.apu;
			apu.DebugCallbackDivider = 0;
			apu.DebugCallbackTimer = 0;
			apu.DebugCallback = null;
		}

		private void NESMusicRipper_Load(object sender, EventArgs e)
		{

		}
	}
}
