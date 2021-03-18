namespace BizHawk.Bizware.BizwareGL
{
	/// <summary>
	/// Represents a piece of 2d art. Not as versatile as a texture.. could have come from an atlas. So it comes with a boatload of constraints
	/// </summary>
	public class Art
	{
		//bleh, didnt mean to have this here, but I need it now
		public Art(Texture2d tex)
		{
			BaseTexture = tex;
			u1 = 1;
			v1 = 1;
			Width = tex.Width;
			Height = tex.Height;
		}

		public Art(ArtManager owner)
		{
			Owner = owner;
		}

		public ArtManager Owner { get; }
		public Texture2d BaseTexture { get; set; }
		
		public float Width, Height;
		public float u0, v0, u1, v1;

		internal void Initialize()
		{
			//TBD
		}
	}
}