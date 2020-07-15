using System;
using System.Drawing;
using System.Windows.Forms;

namespace BizHawk.WinForms.BuilderDSL
{
	public static partial class BuilderDSL
	{
		public static void AnchorAddBottom(this IBuilderTakesAnchor builder) => builder.AddFlagToAnchor(AnchorStyles.Bottom);

		public static void AnchorAddLeft(this IBuilderTakesAnchor builder) => builder.AddFlagToAnchor(AnchorStyles.Left);

		public static void AnchorAddLeftInLTR<TBuilder>(this TBuilder builder)
			where TBuilder : IControlBuilderBase<IFinalizedBuilder<object>>, IBuilderTakesAnchor
		{
			if (builder.Context.IsLTR) builder.AnchorAddLeft();
			else builder.AnchorAddRight();
		}

		public static void AnchorAddRight(this IBuilderTakesAnchor builder) => builder.AddFlagToAnchor(AnchorStyles.Right);

		public static void AnchorAddRightInLTR<TBuilder>(this TBuilder builder)
			where TBuilder : IControlBuilderBase<IFinalizedBuilder<object>>, IBuilderTakesAnchor
		{
			if (builder.Context.IsLTR) builder.AnchorAddRight();
			else builder.AnchorAddLeft();
		}

		public static void AnchorAddTop(this IBuilderTakesAnchor builder) => builder.AddFlagToAnchor(AnchorStyles.Top);

		public static void AnchorAll(this IBuilderTakesAnchor builder) => builder.SetAnchor(AnchorStyles.Left | AnchorStyles.Top | AnchorStyles.Right | AnchorStyles.Bottom);

		public static void AnchorBottom(this IBuilderTakesAnchor builder) => builder.SetAnchor(AnchorStyles.Bottom);

		public static void AnchorBottomLeft(this IBuilderTakesAnchor builder) => builder.SetAnchor(AnchorStyles.Left | AnchorStyles.Bottom);

		public static void AnchorBottomLeftInLTR<TBuilder>(this TBuilder builder)
			where TBuilder : IControlBuilderBase<IFinalizedBuilder<object>>, IBuilderTakesAnchor
		{
			if (builder.Context.IsLTR) builder.AnchorBottomLeft();
			else builder.AnchorBottomRight();
		}

		public static void AnchorBottomRight(this IBuilderTakesAnchor builder) => builder.SetAnchor(AnchorStyles.Right | AnchorStyles.Bottom);

		public static void AnchorBottomRightInLTR<TBuilder>(this TBuilder builder)
			where TBuilder : IControlBuilderBase<IFinalizedBuilder<object>>, IBuilderTakesAnchor
		{
			if (builder.Context.IsLTR) builder.AnchorBottomRight();
			else builder.AnchorBottomLeft();
		}

		public static void AnchorLeft(this IBuilderTakesAnchor builder) => builder.SetAnchor(AnchorStyles.Left);

		public static void AnchorLeftInLTR<TBuilder>(this TBuilder builder)
			where TBuilder : IControlBuilderBase<IFinalizedBuilder<object>>, IBuilderTakesAnchor
		{
			if (builder.Context.IsLTR) builder.AnchorLeft();
			else builder.AnchorRight();
		}

		public static void AnchorRight(this IBuilderTakesAnchor builder) => builder.SetAnchor(AnchorStyles.Right);

		public static void AnchorRightInLTR<TBuilder>(this TBuilder builder)
			where TBuilder : IControlBuilderBase<IFinalizedBuilder<object>>, IBuilderTakesAnchor
		{
			if (builder.Context.IsLTR) builder.AnchorRight();
			else builder.AnchorLeft();
		}

		public static void AnchorTop(this IBuilderTakesAnchor builder) => builder.SetAnchor(AnchorStyles.Top);

		public static void AnchorTopLeft(this IBuilderTakesAnchor builder) => builder.SetAnchor(AnchorStyles.Left | AnchorStyles.Top);

		public static void AnchorTopLeftInLTR<TBuilder>(this TBuilder builder)
			where TBuilder : IControlBuilderBase<IFinalizedBuilder<object>>, IBuilderTakesAnchor
		{
			if (builder.Context.IsLTR) builder.AnchorTopLeft();
			else builder.AnchorTopRight();
		}

		public static void AnchorTopRight(this IBuilderTakesAnchor builder) => builder.SetAnchor(AnchorStyles.Top | AnchorStyles.Right);

		public static void AnchorTopRightInLTR<TBuilder>(this TBuilder builder)
			where TBuilder : IControlBuilderBase<IFinalizedBuilder<object>>, IBuilderTakesAnchor
		{
			if (builder.Context.IsLTR) builder.AnchorTopRight();
			else builder.AnchorTopLeft();
		}

		public static void AutoPos(this IBuilderTakesPosition builder) => builder.UnsetPosition();

		public static void Check(this IBuilderCanBeChecked builder) => builder.SetInitialValue(true);

		public static void CheckIf(this IBuilderCanBeChecked builder, bool b)
		{
			if (b) builder.Check();
		}

		public static void CheckIfNot(this IBuilderCanBeChecked builder, bool b)
		{
			if (!b) builder.Check();
		}

		public static void DisableIf(this IBuilderCanBeDisabled builder, bool b)
		{
			if (b) builder.Disable();
		}

		public static void DisableIfNot(this IBuilderCanBeDisabled builder, bool b)
		{
			if (!b) builder.Disable();
		}

		public static void FixedSize(this IBuilderTakesSize builder, int width, int height) => builder.FixedSize(new Size(width, height));

		public static void InnerPadding(this IBuilderTakesPadding builder, int all) => builder.InnerPadding(new Padding(all));

		public static void InnerPadding(this IBuilderTakesPadding builder, int left, int top, int right, int bottom) => builder.InnerPadding(new Padding(left, top, right, bottom));

		public static void InnerPaddingInLTR<TBuilder>(this TBuilder builder, int left, int top, int right, int bottom)
			where TBuilder : IControlBuilderBase<IFinalizedBuilder<object>>, IBuilderTakesPadding
			=> builder.InnerPadding(builder.Context.IsLTR ? new Padding(left, top, right, bottom) : new Padding(left: right, top: top, right: left, bottom: bottom));

		public static void OuterPadding(this IBuilderTakesPadding builder, int all) => builder.OuterPadding(new Padding(all));

		public static void OuterPadding(this IBuilderTakesPadding builder, int left, int top, int right, int bottom) => builder.OuterPadding(new Padding(left, top, right, bottom));

		public static void OuterPaddingInLTR<TBuilder>(this TBuilder builder, int left, int top, int right, int bottom)
			where TBuilder : IControlBuilderBase<IFinalizedBuilder<object>>, IBuilderTakesPadding
			=> builder.OuterPadding(builder.Context.IsLTR ? new Padding(left, top, right, bottom) : new Padding(left: right, top: top, right: left, bottom: bottom));

		public static void Position(this IBuilderTakesPosition builder, int x, int y) => builder.Position(new Point(x, y));

		public static void SubToCheckedChanged<TBuilder, TControl>(this TBuilder builder, Action<TControl> subscriber)
			where TBuilder : IControlBuilderBase<IFinalizedBuilder<TControl>>, IBuilderPublishesCheckedChanged
			where TControl : class
			=> builder.SubToCheckedChanged((sender, e) => subscriber((TControl) sender));

		public static void SubToClick<TBuilder, TControl>(this TBuilder builder, Action<TControl> subscriber)
			where TBuilder : IControlBuilderBase<IFinalizedBuilder<TControl>>, IBuilderPublishesClick
			where TControl : class
			=> builder.SubToClick((sender, e) => subscriber((TControl) sender));

		public static void SubToScroll<TBuilder, TControl>(this TBuilder builder, Action<TControl> subscriber)
			where TBuilder : IControlBuilderBase<IFinalizedBuilder<TControl>>, IBuilderPublishesScroll
			where TControl : class
			=> builder.SubToScroll((sender, e) => subscriber((TControl) sender));

		public static void SubToValueChanged<TBuilder, TControl>(this TBuilder builder, Action<TControl> subscriber)
			where TBuilder : IControlBuilderBase<IFinalizedBuilder<TControl>>, IBuilderPublishesValueChanged
			where TControl : class
			=> builder.SubToValueChanged((sender, e) => subscriber((TControl) sender));
	}
}
