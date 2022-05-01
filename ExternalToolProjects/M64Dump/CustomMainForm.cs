using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk;
using BizHawk.Emulation.Common;
using BizHawk.WinForms.Controls;

namespace BizHawk.Experiment.M64Dump
{
	[ExternalTool("M64Dump")]
	[ExternalToolApplicability.SingleSystem(CoreSystem.Nintendo64)]
	public class CustomMainForm : ToolFormBase, IExternalToolForm
	{
		public ApiContainer? _apiContainer { get; set; }

		[RequiredService]
		public IInputPollable? _maybeInputPollable { get; set; }

		private ApiContainer APIs => _apiContainer!;

		private IInputPollable InputPollableCore => _maybeInputPollable!;

		private bool _dumping = false;

		private readonly Label _lblReadout;

		private M64Model _m64;

		private string _outputFilename = string.Empty;

		protected override string WindowTitleStatic => "M64 Dumper";

		public CustomMainForm()
		{
			ClientSize = new(320, 64);
			SuspendLayout();
			_lblReadout = new SzLabelEx { Dock = DockStyle.Fill, Text = "initialising" };
			Controls.Add(new LocSzSingleColumnFLP {
				Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
				Controls = { _lblReadout },
				Location = new(4, 4),
				Size = new(ClientSize.Width - 8, ClientSize.Height - 8),
			});
			ResumeLayout();
		}

		private void AbortDump()
			=> _dumping = false;

		private void FinaliseDump()
		{
			_dumping = false;
			File.WriteAllBytes(_outputFilename, _m64.Serialise());
			_lblReadout.Text = $"written {_outputFilename}";
			_outputFilename = string.Empty;
		}

		private void MidMovieWarning()
			=> _lblReadout.Text = "restart movie to dump";

		private void PollCallback(int gamepad)
		{
//			if (_dumping) _m64.Latches.Add();
			// so yeah... Mupen doesn't implement this, and even if it did, the input callback doesn't indicate which gamepad is actually being polled
		}

		private void PrepareToDump()
		{
			_dumping = true;
			_outputFilename = APIs.Movie.Filename().Replace(".bk2", ".m64");
			List<M64Model.GamepadSetupBitfield> players = new();
			for (var i = 1; i <= 4; i++)
			{
				if (APIs.Movie.GetInput(0, i).Count is 0) break;
				players.Add(new() { Connected = true }); //TODO mempack/rumblepak
			}
			_m64 = new(
				author: APIs.Movie.GetHeader()[HeaderKeys.Author],
				framerate: (uint) Math.Round(APIs.Movie.GetFps()),
				players: players,
				rerecordCount: (uint) APIs.Movie.GetRerecordCount(),
				romHeader: APIs.Memory.ReadByteRange(0x0, 0x100, "ROM").ToArray(),
				vblankCount: (uint) APIs.Movie.Length());
//			InputPollableCore.InputCallbacks.Add(PollCallback);
			_lblReadout.Text = $"dumping to {_outputFilename}";
		}

		public override void Restart()
		{
			if (_dumping) AbortDump();
			else if (APIs.Emulation.FrameCount() is 0) PrepareToDump();
			else MidMovieWarning();
		}

		protected override void UpdateAfter()
		{
			if (!_dumping) return;
			if (APIs.Movie.Mode() is "FINISHED") FinaliseDump();
			else UpdateReadout();
		}

		private void UpdateReadout()
			=> _lblReadout.Text = $"dumping to {_outputFilename}; seen {_m64.Latches.Count} latches";
	}
}
