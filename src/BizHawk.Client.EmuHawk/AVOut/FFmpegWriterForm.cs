using System.Linq;
using System.Windows.Forms;
using BizHawk.Client.Common;

namespace BizHawk.Client.EmuHawk
{
	/// <summary>
	/// configures the FFmpegWriter
	/// </summary>
	public partial class FFmpegWriterForm : Form
	{
		/// <summary>
		/// stores a single format preset
		/// </summary>
		public class FormatPreset : IDisposable
		{
			/// <summary>
			/// Gets the name for the listbox
			/// </summary>
			public string Name { get; }

			/// <summary>
			/// Gets the long human readable description
			/// </summary>
			public string Desc { get; }

			/// <summary>
			/// Gets the actual portion of the ffmpeg commandline
			/// </summary>
			public string Commandline { get; set; }

			/// <summary>
			/// Gets a value indicating whether or not it can be edited
			/// </summary>
			public bool Custom { get; }

			/// <summary>
			/// Gets the default file extension
			/// </summary>
			public string Extension { get; set; }

			/// <summary>
			/// get a list of canned presets
			/// </summary>
			public static FormatPreset[] GetPresets(string customCommand)
			{
				return new[]
				{
					new FormatPreset("AVI Lossless UT Video", "Lossless UT video and uncompressed audio in an AVI container. Compatible with AVISource(), if UT Video decoder is installed. Fast, but low compression.",
						"-c:a pcm_s16le -c:v utvideo -pred median -pix_fmt gbrp -f avi", false, "avi"),
					new FormatPreset("AVI Lossless FFV1", "Lossless FFV1 video and uncompressed audio in an AVI container. Compatible with AVISource(), if ffmpeg based decoder is installed. Slow, but high compression.",
						"-c:a pcm_s16le -c:v ffv1 -pix_fmt bgr0 -level 1 -g 1 -coder 1 -context 1 -f avi", false, "avi"),
					new FormatPreset("AVI Lossless AVC", "Lossless AVC video and uncompressed audio in an AVI container. High speed and compression, compatible with AVISource() if x264vfw or ffmpeg based decoder is installed. Seeking may be unstable.",
						"-c:a pcm_s16le -c:v libx264rgb -qp 0 -preset ultrafast -g 10 -pix_fmt rgb24 -f avi", false, "avi"),
					new FormatPreset("AVI Uncompressed", "Uncompressed video and audio in an AVI container. Very large, don't use!",
						"-c:a pcm_s16le -c:v rawvideo -f avi", false, "avi"),
					new FormatPreset("Matroska Lossless", "Lossless AVC video and uncompressed audio in a Matroska container.",
						"-c:a pcm_s16le -c:v libx264rgb -qp 0 -pix_fmt rgb24 -f matroska", false, "mkv"),
					new FormatPreset("Matroska", "AVC video and Vorbis audio in a Matroska container.",
						"-c:a libvorbis -c:v libx264 -f matroska", false, "mkv"),
					new FormatPreset("MP4", "AVC video and AAC audio in an MP4 container.",
						"-c:a aac -c:v libx264 -f mp4", false, "mp4"),
					new FormatPreset("WebM", "VP8 video and Vorbis audio in a WebM container.",
						"-c:a libvorbis -c:v libvpx -auto-alt-ref 0 -f webm", false, "webm"),
					new FormatPreset("Ogg", "Theora video and Vorbis audio in an Ogg container.",
						"-c:a libvorbis -c:v libtheora -f ogg", false, "ogg"),
					new FormatPreset("Xvid", "Xvid video and MP3 audio in an AVI container.",
						"-c:a libmp3lame -c:v libxvid -f avi", false, "avi"),
					new FormatPreset("QuickTime", "AVC video and AAC audio in a QuickTime container.",
						"-c:a aac -c:v libx264 -f mov", false, "mov"),
					new FormatPreset("FLV", "AVC video and AAC audio in a Flash Video container.",
						"-c:a aac -c:v libx264 -f flv", false, "flv"),
					new FormatPreset("[Custom]", "Write your own ffmpeg command. For advanced users only.",
						customCommand, true, "foobar")
				};
			}

			/// <summary>
			/// get the default format preset (from config files)
			/// </summary>
			public static FormatPreset GetDefaultPreset(Config config)
			{
				FormatPreset[] fps = GetPresets(config.FFmpegCustomCommand);

				foreach (var fp in fps)
				{
					if (fp.ToString() == config.FFmpegFormat)
					{
						if (fp.Custom)
						{
							return fp;
						}
					}
				}

				// default to xvid?
				return fps[1];
			}

			public override string ToString()
			{
				return Name;
			}

			public void Dispose()
			{
			}

			public void DeduceFormat(string commandline)
			{
				var splitCommandLine = commandline.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
				for (int index = 0; index < splitCommandLine.Length - 1; index++)
				{
					if (splitCommandLine[index] == "-f")
					{
						Extension = splitCommandLine[index + 1];
						break;
					}
				}

				// are there other formats that don't match their file extensions?
				if (Extension == "matroska")
				{
					Extension = "mkv";
				}
			}

			private FormatPreset(string name, string desc, string commandline, bool custom, string ext)
			{
				Name = name;
				Desc = desc;
				Commandline = commandline;
				Custom = custom;

				DeduceFormat(Commandline);
			}
		}

		private FFmpegWriterForm()
		{
			InitializeComponent();
		}

		private void ListBox1_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (listBox1.SelectedIndex != -1)
			{
				var f = (FormatPreset)listBox1.SelectedItem;
				label5.Text = $"Extension: {f.Extension}";
				label3.Text = f.Desc;
				textBox1.Text = f.Commandline;
			}
		}

		/// <summary>
		/// return a FormatPreset corresponding to the user's choice
		/// </summary>
		public static FormatPreset DoFFmpegWriterDlg(IWin32Window owner, Config config)
		{
			FFmpegWriterForm dlg = new FFmpegWriterForm();
			dlg.listBox1.Items.AddRange(FormatPreset.GetPresets(config.FFmpegCustomCommand).Cast<object>().ToArray());

			int i = dlg.listBox1.FindStringExact(config.FFmpegFormat);
			if (i != ListBox.NoMatches)
			{
				dlg.listBox1.SelectedIndex = i;
			}

			DialogResult result = dlg.ShowDialog(owner);

			FormatPreset ret;
			if (result != DialogResult.OK || dlg.listBox1.SelectedIndex == -1)
			{
				ret = null;
			}
			else
			{
				ret = (FormatPreset)dlg.listBox1.SelectedItem;
				config.FFmpegFormat = ret.ToString();
				if (ret.Custom)
				{
					ret.Commandline =
						config.FFmpegCustomCommand =
						dlg.textBox1.Text;

					ret.DeduceFormat(ret.Commandline);
				}
			}

			dlg.Dispose();
			return ret;
		}
	}
}
