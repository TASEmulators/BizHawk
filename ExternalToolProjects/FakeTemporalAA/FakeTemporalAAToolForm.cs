namespace BizHawk.Experiments.FakeTemporalAA;

using System;

using BizHawk.Bizware.Graphics;
using BizHawk.Client.Common;
using BizHawk.Client.EmuHawk;
using BizHawk.Emulation.Common;

[ExternalTool(TOOL_NAME, Description = "Naively blends each frame with the previous")]
public sealed class FakeTemporalAAToolForm: ToolFormBase, IExternalToolForm
{
	private const string TOOL_NAME = "Fake Temporal Anti-aliasing";

	private BitmapBuffer? _bbPrev = null;

	[RequiredService]
	public IVideoProvider? _maybeVideoProvider { get; set; }

	public ApiContainer? _maybeAPIContainer { get; set; }

	private ApiContainer APIs
		=> _maybeAPIContainer!;

	protected override string WindowTitleStatic
		=> TOOL_NAME;

	public FakeTemporalAAToolForm()
		=> _ = _maybeVideoProvider; // used via ToolFormBase.MainForm

	private void ClearDrawingSurface()
		=> _maybeAPIContainer?.Gui?.WithSurface(DisplaySurfaceID.EmuCore, g => g.ClearGraphics(DisplaySurfaceID.EmuCore));

	protected override void Dispose(bool disposing)
	{
		ClearDrawingSurface();
		base.Dispose(disposing);
	}

	public override void Restart()
	{
		_bbPrev = null;
		ClearDrawingSurface();
	}

	public override void UpdateValues(ToolFormUpdateType type)
	{
		const int OPACITY_MASK = 0xFF << 24;
		static void Invert(Span<int> buf)
		{
			for (var i = 0; i < buf.Length; i++) buf[i] = OPACITY_MASK | ~buf[i]; //TODO vectorise?
		}
		static int AverageXRGB(int c1, int c2)
		{
			//TODO can this be improved?
			const int R_MASK = 0xFF0000;
			const int G_MASK = 0xFF00;
			const int B_MASK = 0xFF;
			var r = (((c1 & R_MASK) + (c2 & R_MASK)) / 2) & R_MASK;
			var g = (((c1 & G_MASK) + (c2 & G_MASK)) / 2) & G_MASK;
			var b = (((c1 & B_MASK) + (c2 & B_MASK)) / 2) & B_MASK;
			var c3 = r | g | b;
			c3 &= ~OPACITY_MASK;
			return OPACITY_MASK | c3;
		}
		if (type is not (ToolFormUpdateType.PreFrame or ToolFormUpdateType.FastPreFrame)) return;

		var bbCurrent = MainForm.MakeScreenshotImage();
		var spanCurrent = bbCurrent.AsSpan();
		Invert(spanCurrent);
		if (_bbPrev is null) // initialisation
		{
			_bbPrev = bbCurrent;
			return;
		}

		var spanPrev = _bbPrev.AsSpan(); // was inverted before saving
		for (var i = 0; i < spanCurrent.Length; i++) spanPrev[i] = AverageXRGB(spanPrev[i], spanCurrent[i]);
		Invert(spanPrev);
		APIs.Gui.WithSurface(DisplaySurfaceID.EmuCore, g => g.DrawImage(_bbPrev!.ToSysdrawingBitmap(), 0, 0, cache: false));
		_bbPrev = bbCurrent;
	}
}
