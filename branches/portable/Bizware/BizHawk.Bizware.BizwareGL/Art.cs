namespace BizHawk.Bizware.BizwareGL
{
	/// <summary>
	/// Represents a piece of 2d art. Not as versatile as a texture.. could have come from an atlas. So it comes with a boatload of constraints
	/// </summary>
	public class Art
	{
		internal Art(ArtManager owner)
		{
			Owner = owner;
		}

		public ArtManager Owner { get; private set; }
		public Texture2d BaseTexture { get; internal set; }
		
		public float Width, Height;
		public float u0, v0, u1, v1;

		internal void Initialize()
		{
			//TBD
		}
	}
}