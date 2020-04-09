using System.ComponentModel;
using System.Windows.Forms;

namespace BizHawk.WinForms.Controls
{
	/// <summary>Static members are dummies, referenced only in docs in order to centralise them.</summary>
	internal static class Docs
	{
		/// <summary>Inherits <see cref="System.Windows.Forms.Button"/>.</summary>
		/// <seealso cref="TypeNamePrefix">Naming convention for control types</seealso>
		/// <seealso cref="LocSzButtonEx"/>
		/// <seealso cref="SzButtonEx"/>
		public const bool Button = true;

		/// <summary>Inherits <see cref="CheckBox"/>/<see cref="RadioButton"/>.</summary>
		/// <seealso cref="TypeNamePrefix">Naming convention for control types</seealso>
		/// <seealso cref="CheckBoxEx"/>
		/// <seealso cref="RadioButtonEx"/>
		/// <seealso cref="SzRadioButtonEx"/>
		public const bool CheckBoxOrRadioButton = true;

		/// <summary>Inherits <see cref="System.Windows.Forms.GroupBox"/>.</summary>
		/// <seealso cref="TypeNamePrefix">Naming convention for control types</seealso>
		/// <seealso cref="LocSzGroupBoxEx"/>
		/// <seealso cref="SzGroupBoxEx"/>
		public const bool GroupBox = true;

		/// <summary>Inherits <see cref="Label"/>/<see cref="LinkLabel"/>.</summary>
		/// <seealso cref="TypeNamePrefix">Naming convention for control types</seealso>
		/// <seealso cref="LabelEx"/>
		/// <seealso cref="LocLabelEx"/>
		/// <seealso cref="LocSzLabelEx"/>
		/// <seealso cref="SzLabelEx"/>
		/// <seealso cref="LocLinkLabelEx"/>
		public const bool LabelOrLinkLabel = true;

		/// <summary>Inherits <see cref="NumericUpDown"/>. Only <c>Sz*</c> variants are available.</summary>
		/// <seealso cref="TypeNamePrefix">Naming convention for control types</seealso>
		/// <seealso cref="SzNUDEx"/>
		public const bool NUD = true;

		/// <summary>
		/// Inherits <see cref="FlowLayoutPanel"/>.
		/// <see cref="FlowLayoutPanel.WrapContents"/> is locked to <see langword="true"/> and <see cref="FlowLayoutPanel.Margin"/> is locked to <see cref="Padding.Empty"/>.
		/// <see cref="FlowLayoutPanel.FlowDirection"/> is locked to <see cref="FlowDirection.LeftToRight"/>/<see cref="FlowDirection.TopDown"/> for rows/columns, respectively.
		/// </summary>
		/// <seealso cref="TypeNamePrefix">Naming convention for control types</seealso>
		/// <seealso cref="SzColumnsToRightFLP"/>
		/// <seealso cref="SzRowsToBottomFLP"/>
		public const bool RowsOrColsFLP = true;

		/// <summary>
		/// Inherits <see cref="FlowLayoutPanel"/>.
		/// <see cref="FlowLayoutPanel.WrapContents"/> is locked to <see langword="false"/> and <see cref="FlowLayoutPanel.Margin"/> is locked to <see cref="Padding.Empty"/>.
		/// <see cref="FlowLayoutPanel.FlowDirection"/> is locked to <see cref="FlowDirection.LeftToRight"/>/<see cref="FlowDirection.TopDown"/> for a single row/column, respectively.
		/// </summary>
		/// <seealso cref="TypeNamePrefix">Naming convention for control types</seealso>
		/// <seealso cref="SingleColumnFLP"/>
		/// <seealso cref="LocSingleColumnFLP"/>
		/// <seealso cref="LocSzSingleColumnFLP"/>
		/// <seealso cref="SingleRowFLP"/>
		/// <seealso cref="LocSingleRowFLP"/>
		/// <seealso cref="LocSzSingleRowFLP"/>
		public const bool SingleRowOrColFLP = true;

		/// <summary>Inherits <see cref="System.Windows.Forms.TabPage"/>.</summary>
		/// <seealso cref="TypeNamePrefix">Naming convention for control types</seealso>
		/// <seealso cref="TabPageEx"/>
		public const bool TabPage = true;

		/// <summary>Inherits <see cref="System.Windows.Forms.TextBox"/>. Only <c>Sz*</c> variants are available.</summary>
		/// <seealso cref="TypeNamePrefix">Naming convention for control types</seealso>
		/// <seealso cref="SzTextBoxEx"/>
		public const bool TextBox = true;

		/// <remarks>
		/// This project has some naming conventions in regards to type names.
		/// <list type="bullet">
		/// <item><description><c>Loc*</c> are positionable; instances should set <see cref="Control.Location"/>. The intention is for controls without <c>Loc</c> to be used in <see cref="FlowLayoutPanel">FLPs</see>.</description></item>
		/// <item><description><c>Sz*</c> are resizable; instances should set <see cref="Control.Size"/>. <see cref="Control.AutoSize"/> is always set for you, with or without this prefix.</description></item>
		/// <item><description>These combine as expected. A type name without any prefix is the most restrictive, having many properties pre-set (which is a double-edged sword as they can't be changed).</description></item>
		/// </list>
		/// In addition, properties are hidden in the Designer (with <see cref="BrowsableAttribute"/>) when they are read-only and their value is clear from the type name.
		/// </remarks>
		public const bool TypeNamePrefix = true;
	}
}
