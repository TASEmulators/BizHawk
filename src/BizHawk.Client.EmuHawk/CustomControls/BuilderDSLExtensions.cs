#nullable enable

using System;
using System.Windows.Forms;

using BizHawk.WinForms.BuilderDSL;

namespace BizHawk.Client.EmuHawk.CustomControls
{
	public static class BuilderDSLExtensions
	{
		public static IFinalizedBuilder<Button> DialogCancelButton(this FLPBuilder parentFLP, EventHandler? onClick = null, Blueprint<ButtonBuilder>? blueprint = null)
			=> parentFLP.AddButton(button =>
			{
				button.SetText("&Cancel");
				button.SetDialogResult(DialogResult.Cancel);
				button.FixedSize(75, 23);
				if (onClick != null) button.SubToClick(onClick);
				blueprint?.Invoke(button);
			});

		public static IFinalizedBuilder<Button> DialogOKButton(this FLPBuilder parentFLP, EventHandler? onClick = null, Blueprint<ButtonBuilder>? blueprint = null)
			=> parentFLP.AddButton(button =>
			{
				button.SetText("&OK");
				button.FixedSize(75, 23);
				if (onClick != null) button.SubToClick(onClick);
				blueprint?.Invoke(button);
			});
	}
}
