using System;
using System.Drawing;
using System.Windows.Forms;

using BizHawk.Common;
using BizHawk.WinForms.Controls;

namespace BizHawk.WinForms.BuilderDSL
{
	public interface IBuilderCanBeChecked : IBuilderTakesInitialValue<bool> {}

	public interface IBuilderCanBeDisabled
	{
		void Disable();
	}

	public interface IBuilderPublishesCheckedChanged
	{
		void SubToCheckedChanged(EventHandler subscriber);
	}

	public interface IBuilderPublishesClick
	{
		void SubToClick(EventHandler subscriber);
	}

	public interface IBuilderPublishesScroll
	{
		void SubToScroll(EventHandler subscriber);
	}

	public interface IBuilderPublishesValueChanged
	{
		void SubToValueChanged(EventHandler subscriber);
	}

	public interface IBuilderTakesAnchor
	{
		void AddFlagToAnchor(AnchorStyles flags);

		void SetAnchor(AnchorStyles anchor);

		void UnsetAnchor();
	}

	public interface IBuilderTakesContentText
	{
		void SetText(string text);

		void UnsetText();
	}

	public interface IBuilderTakesDataTag
	{
		void SetDataTag(object dataRef);
	}

	public interface IBuilderTakesInitialValue<in T>
	{
		void SetInitialValue(T initValue);
	}

	public interface IBuilderTakesLabelText
	{
		void LabelText(string labelText);

		void UnsetLabelText();
	}

	public interface IBuilderTakesPadding
	{
		void InnerPadding(Padding padding);

		void OuterPadding(Padding padding);

		void UnsetInnerPadding();

		void UnsetOuterPadding();
	}

	public interface IBuilderTakesPosition
	{
		void Position(Point pos);

		void UnsetPosition();
	}

	public interface IBuilderTakesRBGroupTracker
	{
		void SetTracker(RadioButtonGroupTracker tracker);
	}

	public interface IBuilderTakesRange<in T> where T : unmanaged, IComparable<T>
	{
		void SetValidRange(Range<T> validRange);
	}

	public interface IBuilderTakesSize
	{
		void AutoSize();

		void FixedSize(Size size);

		void UnsetSize();
	}
}
