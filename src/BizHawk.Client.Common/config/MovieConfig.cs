namespace BizHawk.Client.Common
{
	public class MovieConfig
	{
		public MovieEndAction MovieEndAction { get; set; } = MovieEndAction.Finish;
		public bool EnableBackupMovies { get; set; } = true;
		public bool MoviesOnDisk { get; set; }
		public int MovieCompressionLevel { get; set; } = 2;
		public bool VBAStyleMovieLoadState { get; set; }
		public bool MoviePlaybackPokeMode { get; set; }
	}
}
