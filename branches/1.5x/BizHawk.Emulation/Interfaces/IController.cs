using System;
using System.Collections.Generic;

namespace BizHawk
{
	// doesn't do what is desired
	// http://connect.microsoft.com/VisualStudio/feedback/details/459307/extension-add-methods-are-not-considered-in-c-collection-initializers
	/*
	public static class UltimateMagic
	{
		public static void Add(this List<ControllerDefinition.FloatRange l, float Min, float Mid, float Max)
		{
			l.Add(new ControllerDefinition.FloatRange(Min, Mid, Max);
		}
	}
	*/

	public class ControllerDefinition
	{
		public struct FloatRange
		{
			public readonly float Min;
			public readonly float Max;
			/// <summary>
			/// default position
			/// </summary>
			public readonly float Mid;
			public FloatRange(float Min, float Mid, float Max)
			{
				this.Min = Min;
				this.Mid = Mid;
				this.Max = Max;
			}
			// for terse construction
			public static implicit operator FloatRange(float[] f)
			{
				if (f.Length != 3)
					throw new ArgumentException();
				return new FloatRange(f[0], f[1], f[2]);
			}
		}

		public string Name;
		public List<string> BoolButtons = new List<string>();
		public List<string> FloatControls = new List<string>();
		public List<FloatRange> FloatRanges = new List<FloatRange>();
		/// <summary>
		/// copy
		/// </summary>
		/// <param name="source"></param>
		public ControllerDefinition(ControllerDefinition source)
		{
			this.Name = source.Name;
			foreach (var s in source.BoolButtons)
				this.BoolButtons.Add(s);
			foreach (var s in source.FloatControls)
				this.FloatControls.Add(s);
		}
		public ControllerDefinition() { }
	}

	public interface IController
	{
		ControllerDefinition Type { get; }

		//TODO - it is obnoxious for this to be here. must be removed.
		bool this[string button] { get; }
		//TODO - this can stay but it needs to be changed to go through the float
		bool IsPressed(string button);

		float GetFloat(string name);

		//TODO - why does this have a frame argument. must be removed.
		void UpdateControls(int frame);
	}
}
