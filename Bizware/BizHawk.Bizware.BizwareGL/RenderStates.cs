using System;
using System.Collections.Generic;

namespace BizHawk.Bizware.BizwareGL
{
	public interface IBlendState { }

	/// <summary>
	/// An IBlendState token that just caches all the args needed to create a blend state
	/// </summary>
	public class CacheBlendState : IBlendState
	{
		public bool Enabled;
		public global::OpenTK.Graphics.OpenGL.BlendingFactorSrc colorSource;
		public global::OpenTK.Graphics.OpenGL.BlendEquationMode colorEquation;
		public global::OpenTK.Graphics.OpenGL.BlendingFactorDest colorDest;
		public global::OpenTK.Graphics.OpenGL.BlendingFactorSrc alphaSource;
		public global::OpenTK.Graphics.OpenGL.BlendEquationMode alphaEquation;
		public global::OpenTK.Graphics.OpenGL.BlendingFactorDest alphaDest;

		public CacheBlendState(bool enabled,
			global::OpenTK.Graphics.OpenGL.BlendingFactorSrc colorSource,
			global::OpenTK.Graphics.OpenGL.BlendEquationMode colorEquation,
			global::OpenTK.Graphics.OpenGL.BlendingFactorDest colorDest,
			global::OpenTK.Graphics.OpenGL.BlendingFactorSrc alphaSource,
			global::OpenTK.Graphics.OpenGL.BlendEquationMode alphaEquation,
			global::OpenTK.Graphics.OpenGL.BlendingFactorDest alphaDest)
		{
			this.Enabled = enabled;
			this.colorSource = (global::OpenTK.Graphics.OpenGL.BlendingFactorSrc)colorSource;
			this.colorEquation = (global::OpenTK.Graphics.OpenGL.BlendEquationMode)colorEquation;
			this.colorDest = (global::OpenTK.Graphics.OpenGL.BlendingFactorDest)colorDest;
			this.alphaSource = (global::OpenTK.Graphics.OpenGL.BlendingFactorSrc)alphaSource;
			this.alphaEquation = (global::OpenTK.Graphics.OpenGL.BlendEquationMode)alphaEquation;
			this.alphaDest = (global::OpenTK.Graphics.OpenGL.BlendingFactorDest)alphaDest;
		}
	}
}