using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

using BizHawk.Common.BufferExtensions;
using BizHawk.Client.Common;

using BizHawk.Emulation.Cores.PCEngine;
using BizHawk.Emulation.Common.Components;
using BizHawk.Emulation.Common;

using ICSharpCode.SharpZipLib.Zip;

namespace BizHawk.Client.EmuHawk
{
	public partial class PCESoundDebugger : Form, IToolFormAutoConfig
	{
		[RequiredService]
		private PCEngine _pce { get; set; }

		public PCESoundDebugger()
		{
			InitializeComponent();

			SetStyle(ControlStyles.AllPaintingInWmPaint, true);
			SetStyle(ControlStyles.UserPaint, true);
			SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
		}

		byte[] waveformTemp = new byte[32 * 2];

		protected override void OnShown(EventArgs e)
		{
			for (int i = 0; i < lvChEn.Items.Count; i++)
				lvChEn.Items[i].Checked = true;
			base.OnShown(e);
		}

		public void NewUpdate(ToolFormUpdateType type) { }

		public void UpdateValues()
		{
			foreach (var entry in PSGEntries)
			{
				entry.wasactive = entry.active;
				entry.active = false;
			}

			bool sync = false;
			lvPsgWaveforms.BeginUpdate();
			lvChannels.BeginUpdate();

			for (int i = 0; i < 6; i++)
			{
				var ch = _pce.PSG.Channels[i];

				//these conditions mean a sample isnt playing
				if (!ch.Enabled)
				{
					lvChannels.Items[i].SubItems[1].Text = "-";
					lvChannels.Items[i].SubItems[2].Text = "-";
					lvChannels.Items[i].SubItems[3].Text = "(disabled)";
					goto DEAD;
				}
				if (ch.DDA)
				{
					lvChannels.Items[i].SubItems[1].Text = "-";
					lvChannels.Items[i].SubItems[2].Text = "-";
					lvChannels.Items[i].SubItems[3].Text = "(DDA)";
					goto DEAD;
				}
				lvChannels.Items[i].SubItems[1].Text = ch.Volume.ToString();
				lvChannels.Items[i].SubItems[2].Text = ch.Frequency.ToString();
				if (ch.NoiseChannel)
				{
					lvChannels.Items[i].SubItems[3].Text = "(noise)";
					goto DEAD;
				}

				if (ch.Volume == 0) goto DEAD;

				lvChannels.Items[i].SubItems[3].Text = "-";

				//ok, a sample is playing. copy out the waveform
				short[] waveform = (short[])ch.Wave.Clone();
				//hash it
				var ms = new MemoryStream(waveformTemp);
				var bw = new BinaryWriter(ms);
				foreach (var s in waveform)
					bw.Write(s);
				bw.Flush();
				string md5 = waveformTemp.HashMD5();

				if (!PSGEntryTable.ContainsKey(md5))
				{
					var entry = new PSGEntry()
					{
						hash = md5,
						name = md5,
						waveform = waveform,
						active = true,
						hitcount = 1,
						index = PSGEntries.Count
					};
					PSGEntries.Add(entry);
					PSGEntryTable[md5] = entry;
					sync = true;
					LastSamples[i] = entry;
				}
				else
				{
					PSGEntry entry = PSGEntryTable[md5];
					entry.active = true;

					//are we playing the same sample as before?
					if (LastSamples[i] == entry) { }
					else
					//if (!entry.wasactive)
					{
						LastSamples[i] = entry;
						entry.hitcount++;
						if (entry.index < lvPsgWaveforms.Items.Count)
							lvPsgWaveforms.Items[entry.index].SubItems[1].Text = entry.hitcount.ToString();
						else
							sync = true;
					}
				}

				lvChannels.Items[i].SubItems[3].Text = PSGEntryTable[md5].name;

				continue;

			DEAD:
				LastSamples[i] = null;
			}

			if (sync)
				SyncLists();
			lvPsgWaveforms.EndUpdate();
			lvChannels.EndUpdate();
		}

		public void FastUpdate()
		{
			// Todo
		}

		class PSGEntry
		{
			public int index;
			public bool active, wasactive;
			public int hitcount;
			public string hash;
			public string name;
			public short[] waveform;
		}

		PSGEntry[] LastSamples = new PSGEntry[8];
		List<PSGEntry> PSGEntries = new List<PSGEntry>();
		Dictionary<string, PSGEntry> PSGEntryTable = new Dictionary<string, PSGEntry>();

		public void Restart()
		{
		}

		public bool AskSaveChanges()
		{
			return true;
		}

		public bool UpdateBefore
		{
			get { return false; }
		}


		//32*16 samples, 16bit, mono, 8khz (but we'll change the sample rate)
		static readonly byte[] emptyWav = new byte[] {
			0x52, 0x49, 0x46, 0x46, 0x24, 0x04, 0x00, 0x00, 0x57, 0x41, 0x56, 0x45, 0x66, 0x6D, 0x74, 0x20, 
			0x10, 0x00, 0x00, 0x00, 0x01, 0x00, 0x01, 0x00, 0xE0, 0x2E, 0x00, 0x00, 0xC0, 0x5D, 0x00, 0x00, 
			0x02, 0x00, 0x10, 0x00, 0x64, 0x61, 0x74, 0x61, 0x00, 0x04, 0x00, 0x00, 
		};


		private void btnExport_Click(object sender, EventArgs e)
		{
			string tmpf = Path.GetTempFileName() + ".zip";
			using (var stream = new FileStream(tmpf, FileMode.Create, FileAccess.Write, FileShare.Read))
			{
				var zip = new ZipOutputStream(stream)
				{
					IsStreamOwner = false,
					UseZip64 = UseZip64.Off
				};

				foreach (var entry in PSGEntries)
				{
					var ze = new ZipEntry(entry.name + ".wav") { CompressionMethod = CompressionMethod.Deflated };
					zip.PutNextEntry(ze);
					var ms = new MemoryStream();
					var bw = new BinaryWriter(ms);
					bw.Write(emptyWav, 0, emptyWav.Length);
					ms.Position = 0x18; //samplerate and avgbytespersecond
					bw.Write(20000);
					bw.Write(20000 * 2);
					bw.Flush();
					ms.Position = 0x2C;
					for (int i = 0; i < 32; i++)
						for (int j = 0; j < 16; j++)
							bw.Write(entry.waveform[i]);
					bw.Flush();
					var buf = ms.GetBuffer();
					zip.Write(buf, 0, (int)ms.Length);
					zip.Flush();
					zip.CloseEntry();
				}
				zip.Close();
				stream.Flush();
			}
			System.Diagnostics.Process.Start(tmpf);
		}

		class ZipDataSource : IStaticDataSource
		{
			public ZipDataSource(byte[] data) { this.data = data; }
			byte[] data;
			public Stream GetSource() { return new MemoryStream(data); }
		}

		private void btnReset_Click(object sender, EventArgs e)
		{
			PSGEntryTable.Clear();
			PSGEntries.Clear();
			for (int i = 0; i < 8; i++) LastSamples[i] = null;
			SyncLists();
		}

		void SyncLists()
		{
			lvPsgWaveforms.Items.Clear();
			foreach (var entry in PSGEntries)
			{
				var lvi = new ListViewItem(entry.name);
				lvi.SubItems.Add(entry.hitcount.ToString());
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

		private void lvPsgWaveforms_ItemActivate(object sender, EventArgs e)
		{

		}

		private void lvPsgWaveforms_AfterLabelEdit(object sender, LabelEditEventArgs e)
		{
			var entry = PSGEntries[e.Item];
			entry.name = e.Label;
		}

		private void lvChEn_ItemSelectionChanged(object sender, ListViewItemSelectionChangedEventArgs e)
		{

		}

		private void lvChEn_ItemChecked(object sender, ItemCheckedEventArgs e)
		{
			for (int i = 0; i < 6; i++)
				_pce.PSG.UserMute[i] = !lvChEn.Items[i].Checked;
		}
	}
}
