using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace BizHawk.MultiClient
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
			/// name for listbox
			/// </summary>
			public string name;
			/// <summary>
			/// long human readable description
			/// </summary>
			public string desc;
			/// <summary>
			/// actual portion of ffmpeg commandline
			/// </summary>
			public string commandline;
			public override string ToString()
			{
				return name;
			}
			/// <summary>
			/// can be edited
			/// </summary>
			public bool custom = false;

			/// <summary>
			/// default file extension
			/// </summary>
			public string defaultext;

			FormatPreset(string name, string desc, string commandline, bool custom, string defaultext)
			{
				this.name = name;
				this.desc = desc;
				this.commandline = commandline;
				this.custom = custom;
				this.defaultext = defaultext;
			}

			/// <summary>
			/// get a list of canned presets
			/// </summary>
			/// <returns></returns>
			public static FormatPreset[] GetPresets()
			{
				return new FormatPreset[]
				{
					new FormatPreset("Uncompressed AVI", "AVI file with uncompressed audio and video.  Very large.", "-c:a pcm_s16le -c:v rawvideo -f avi", false, "avi"),
					new FormatPreset("Xvid", "AVI file with xvid video and mp3 audio.", "-c:a libmp3lame -c:v libxvid -f avi", false, "avi"),
					new FormatPreset("Lossless Compressed AVI", "AVI file with zlib video and uncompressed audio.", "-c:a pcm_s16le -c:v zlib -f avi", false, "avi"),
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

			public void Dispose()
			{
			}
		}

		public FFmpegWriterForm()
		{
			InitializeComponent();
		}

		private void listBox1_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (listBox1.SelectedIndex != -1)
			{
				FormatPreset f = (FormatPreset)listBox1.SelectedItem;

				label5.Text = "Extension: " + f.defaultext;
				label3.Text = f.desc;
				textBox1.Text = f.commandline;
				textBox1.ReadOnly = !f.custom;
			}
		}

		/// <summary>
		/// return a formatpreset corresponding to the user's choice
		/// </summary>
		/// <param name="owner"></param>
		/// <returns></returns>
		public static FormatPreset DoFFmpegWriterDlg(IWin32Window owner)
		{
			FFmpegWriterForm dlg = new FFmpegWriterForm();
			dlg.listBox1.Items.AddRange(FormatPreset.GetPresets());

			DialogResult result = dlg.ShowDialog(owner);

			FormatPreset ret;
			if (result != DialogResult.OK || dlg.listBox1.SelectedIndex == -1)
				ret = null;
			else
			{
				ret = (FormatPreset)dlg.listBox1.SelectedItem;
				if (ret.custom)
					ret.commandline = dlg.textBox1.Text;
			}
			dlg.Dispose();
			return ret;
		}
	}
}
