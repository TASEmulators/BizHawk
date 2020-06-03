namespace BizHawk.Client.Common
{
	public interface IMovieConfig
	{
		public MovieEndAction MovieEndAction { get; }
		public bool EnableBackupMovies { get; }
		public bool MoviesOnDisk { get; }
		public int MovieCompressionLevel { get; }
		public bool VBAStyleMovieLoadState { get; }
		public bool MoviePlaybackPokeMode { get; }
	}

	public class MovieConfig : IMovieConfig
	{
		public MovieEndAction MovieEndAction { get; set; } = MovieEndAction.Finish;
		public bool EnableBackupMovies { get; set; } = true;
		public bool MoviesOnDisk { get; set; }
		public int MovieCompressionLevel { get; set; } = 2;
		public bool VBAStyleMovieLoadState { get; set; }
		public bool MoviePlaybackPokeMode { get; set; }
	}
}
