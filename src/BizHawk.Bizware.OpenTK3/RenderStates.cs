using BizHawk.Bizware.BizwareGL;

namespace BizHawk.Bizware.OpenTK3
{
	/// <summary>
	/// An IBlendState token that just caches all the args needed to create a blend state
	/// </summary>
	public class CacheBlendState : IBlendState
	{
		public readonly bool Enabled;

		public readonly BlendingFactorSrc colorSource;

		public readonly BlendEquationMode colorEquation;

		public readonly BlendingFactorDest colorDest;

		public readonly BlendingFactorSrc alphaSource;

		public readonly BlendEquationMode alphaEquation;

		public readonly BlendingFactorDest alphaDest;

		public CacheBlendState(
			bool enabled,
			BlendingFactorSrc colorSource,
			BlendEquationMode colorEquation,
			BlendingFactorDest colorDest,
			BlendingFactorSrc alphaSource,
			BlendEquationMode alphaEquation,
			BlendingFactorDest alphaDest)
		{
			this.Enabled = enabled;
			this.colorSource = colorSource;
			this.colorEquation = colorEquation;
			this.colorDest = colorDest;
			this.alphaSource = alphaSource;
			this.alphaEquation = alphaEquation;
			this.alphaDest = alphaDest;
		}
	}
}