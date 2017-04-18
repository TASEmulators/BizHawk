using System;
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
			public string Defaultext { get; }

			/// <summary>
			/// get a list of canned presets
			/// </summary>
			/// <returns></returns>
			public static FormatPreset[] GetPresets()
			{
				return new[]
				{
					new FormatPreset("Uncompressed AVI", "AVI file with uncompressed audio and video.  Very large.", "-c:a pcm_s16le -c:v rawvideo -f avi", false, "avi"),
					new FormatPreset("Xvid", "AVI file with xvid video and mp3 audio.", "-c:a libmp3lame -c:v libxvid -f avi", false, "avi"),
					//new FormatPreset("Lossless Compressed AVI", "AVI file with zlib video and uncompressed audio.", "-c:a pcm_s16le -c:v zlib -f avi", false, "avi"),
					new FormatPreset("FLV", "avc+aac in flash container.", "-c:a libvo_aacenc -c:v libx264 -f flv", false, "flv"),
					new FormatPreset("Matroska Lossless", "MKV file with lossless video and audio", "-c:a pcm_s16le -c:v libx264rgb -crf 0 -f matroska", false, "mkv"),
					new FormatPreset("Matroska", "MKV file with h264 + vorbis", "-c:a libvorbis -c:v libx264 -f matroska", false, "mkv"),
					new FormatPreset("QuickTime", "MOV file with avc+aac", "-c:a libvo_aacenc -c:v libx264 -f mov", false, "mov"),
					new FormatPreset("Ogg", "Theora + Vorbis in OGG", "-c:a libvorbis -c:v libtheora -f ogg", false, "ogg"),
					new FormatPreset("WebM", "Vp8 + Vorbis in WebM", "-c:a libvorbis -c:v libvpx -f webm", false, "webm"),
					new FormatPreset("mp4", "ISO mp4 with AVC+AAC", "-c:a libvo_aacenc -c:v libx264 -f mp4", false, "mp4"),
					new FormatPreset("[Custom]", "Write your own ffmpeg command.  For advanced users only", "-c:a foo -c:v bar -f baz", true, "foobar"),
				};
			}

			/// <summary>
			/// get the default format preset (from config files)
			/// </summary>
			public static FormatPreset GetDefaultPreset()
			{
				FormatPreset[] fps = GetPresets();

				foreach (var fp in fps)
				{
					if (fp.ToString() == Global.Config.FFmpegFormat)
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

			private FormatPreset(string name, string desc, string commandline, bool custom, string defaultext)
			{
				Name = name;
				Desc = desc;
				Custom = custom;

				Commandline = Custom
					? Global.Config.FFmpegCustomCommand
					: commandline;

				Defaultext = defaultext;
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

				label5.Text = "Extension: " + f.Defaultext;
				label3.Text = f.Desc;
				textBox1.Text = f.Commandline;
				textBox1.ReadOnly = !f.Custom;
			}
		}

		/// <summary>
		/// return a FormatPreset corresponding to the user's choice
		/// </summary>
		public static FormatPreset DoFFmpegWriterDlg(IWin32Window owner)
		{
			FFmpegWriterForm dlg = new FFmpegWriterForm();
			dlg.listBox1.Items.AddRange(FormatPreset.GetPresets());

			int i = dlg.listBox1.FindStringExact(Global.Config.FFmpegFormat);
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
				Global.Config.FFmpegFormat = ret.ToString();
				if (ret.Custom)
				{
					ret.Commandline = dlg.textBox1.Text;
					Global.Config.FFmpegCustomCommand = dlg.textBox1.Text;
				}
			}

			dlg.Dispose();
			return ret;
		}
	}
}
