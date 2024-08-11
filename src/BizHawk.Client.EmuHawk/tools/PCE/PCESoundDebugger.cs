using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Common;
using BizHawk.Emulation.Cores.PCEngine;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	[SpecializedTool("Sound Debugger")]
	public partial class PCESoundDebugger : ToolFormBase, IToolFormAutoConfig
	{
		public static Icon ToolIcon
			=> Properties.Resources.BugIcon;

		[RequiredService]
		private PCEngine PCE { get; set; }

		protected override string WindowTitleStatic => "Sound Debugger";

		public PCESoundDebugger()
		{
			InitializeComponent();
			Icon = ToolIcon;

			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
		}

		private readonly byte[] _waveformTemp = new byte[32 * 2];

		protected override void OnShown(EventArgs e)
		{
			for (int i = 0; i < lvChEn.Items.Count; i++)
			{
				lvChEn.Items[i].Checked = true;
			}

			base.OnShown(e);
		}

		protected override void UpdateAfter()
		{
			foreach (var entry in _psgEntries)
			{
				entry.WasActive = entry.Active;
				entry.Active = false;
			}

			bool sync = false;
			lvPsgWaveforms.BeginUpdate();
			lvChannels.BeginUpdate();

			for (int i = 0; i < 6; i++)
			{
				var ch = PCE.PSG.Channels[i];

				// these conditions mean a sample isn't playing
				if (!ch.Enabled)
				{
					lvChannels.Items[i].SubItems[1].Text = "-";
					lvChannels.Items[i].SubItems[2].Text = "-";
					lvChannels.Items[i].SubItems[3].Text = "(disabled)";
					_lastSamples[i] = null;
					continue;
				}
				if (ch.DDA)
				{
					lvChannels.Items[i].SubItems[1].Text = "-";
					lvChannels.Items[i].SubItems[2].Text = "-";
					lvChannels.Items[i].SubItems[3].Text = "(DDA)";
					_lastSamples[i] = null;
					continue;
				}
				lvChannels.Items[i].SubItems[1].Text = ch.Volume.ToString();
				lvChannels.Items[i].SubItems[2].Text = ch.Frequency.ToString();
				if (ch.NoiseChannel)
				{
					lvChannels.Items[i].SubItems[3].Text = "(noise)";
					_lastSamples[i] = null;
					continue;
				}

				if (ch.Volume == 0)
				{
					_lastSamples[i] = null;
					continue;
				}

				lvChannels.Items[i].SubItems[3].Text = "-";

				// ok, a sample is playing. copy out the waveform
				short[] waveform = (short[])ch.Wave.Clone();

				// hash it
				var ms = new MemoryStream(_waveformTemp);
				var bw = new BinaryWriter(ms);
				foreach (var s in waveform)
				{
					bw.Write(s);
				}

				bw.Flush();
				var md5 = MD5Checksum.ComputeDigestHex(_waveformTemp);

				if (!_psgEntryTable.TryGetValue(md5, out var entry))
				{
					entry = new PsgEntry
					{
						Name = md5,
						WaveForm = waveform,
						Active = true,
						HitCount = 1,
						Index = _psgEntries.Count
					};
					_psgEntries.Add(entry);
					_psgEntryTable[md5] = entry;
					sync = true;
					_lastSamples[i] = entry;
				}
				else
				{
					entry.Active = true;

					// are we playing the same sample as before?
					if (_lastSamples[i] != entry)
					{
						_lastSamples[i] = entry;
						entry.HitCount++;
						if (entry.Index < lvPsgWaveforms.Items.Count)
						{
							lvPsgWaveforms.Items[entry.Index].SubItems[1].Text = entry.HitCount.ToString();
						}
						else
						{
							sync = true;
						}
					}
				}

				lvChannels.Items[i].SubItems[3].Text = entry.Name;
			}

			if (sync)
				SyncLists();
			lvPsgWaveforms.EndUpdate();
			lvChannels.EndUpdate();
		}

		private class PsgEntry
		{
			public int Index { get; set; }
			public bool Active { get; set; }
			public bool WasActive { get; set; }
			public int HitCount { get; set; }
			public string Name { get; set; }
			public short[] WaveForm { get; set; }
		}

		private readonly PsgEntry[] _lastSamples = new PsgEntry[8];
		private readonly List<PsgEntry> _psgEntries = new List<PsgEntry>();
		private readonly Dictionary<string, PsgEntry> _psgEntryTable = new Dictionary<string, PsgEntry>();

		// 32*16 samples, 16bit, mono, 8khz (but we'll change the sample rate)
		private static readonly byte[] EmptyWav = {
			0x52, 0x49, 0x46, 0x46, 0x24, 0x04, 0x00, 0x00, 0x57, 0x41, 0x56, 0x45, 0x66, 0x6D, 0x74, 0x20,
			0x10, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0xE0, 0x2E, 0x00, 0x00, 0xC0, 0x5D, 0x00, 0x00,
			0x02, 0x00, 0x10, 0x00, 0x64, 0x61, 0x74, 0x61, 0x00, 0x04, 0x00, 0x00
		};


		private void BtnExport_Click(object sender, EventArgs e)
		{
			string tmpFilename = $"{Path.GetTempFileName()}.zip";
			using (var stream = new FileStream(tmpFilename, FileMode.Create, FileAccess.Write, FileShare.Read))
			{
				using var zip = new ZipArchive(stream, ZipArchiveMode.Create);

				foreach (var entry in _psgEntries)
				{
					var ms = new MemoryStream();
					var bw = new BinaryWriter(ms);
					bw.Write(EmptyWav, 0, EmptyWav.Length);
					ms.Position = 0x18; // samplerate and avgbytespersecond
					bw.Write(20000);
					bw.Write(20000 * 2);
					bw.Flush();
					ms.Position = 0x2C;
					for (int i = 0; i < 32; i++)
					{
						for (int j = 0; j < 16; j++)
						{
							bw.Write(entry.WaveForm[i]);
						}
					}

					bw.Flush();
					var buf = ms.GetBuffer();

					var ze = zip.CreateEntry($"{entry.Name}.wav", CompressionLevel.Fastest);
					using var zipstream = ze.Open();
					zipstream.Write(buf, 0, (int)ms.Length);
				}
			}
			System.Diagnostics.Process.Start(tmpFilename);
		}

		private void BtnReset_Click(object sender, EventArgs e)
		{
			_psgEntryTable.Clear();
			_psgEntries.Clear();
			for (int i = 0; i < 8; i++) _lastSamples[i] = null;
			SyncLists();
		}

		private void SyncLists()
		{
			lvPsgWaveforms.Items.Clear();
			foreach (var entry in _psgEntries)
			{
				var lvi = new ListViewItem(entry.Name);
				lvi.SubItems.Add(entry.HitCount.ToString());
				lvPsgWaveforms.Items.Add(lvi);
			}
		}

		private void lvPsgWaveforms_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.F2 && lvPsgWaveforms.SelectedItems.Count > 0)
			{
				lvPsgWaveforms.SelectedItems[0].BeginEdit();
			}
		}

		private void lvPsgWaveforms_AfterLabelEdit(object sender, LabelEditEventArgs e)
		{
			var entry = _psgEntries[e.Item];
			entry.Name = e.Label;
		}

		private void lvChEn_ItemChecked(object sender, ItemCheckedEventArgs e)
		{
			for (int i = 0; i < 6; i++)
			{
				PCE.PSG.UserMute[i] = !lvChEn.Items[i].Checked;
			}
		}
	}
}
