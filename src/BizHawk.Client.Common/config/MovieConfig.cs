namespace BizHawk.Client.Common
{
	public interface IMovieConfig
	{
		MovieEndAction MovieEndAction { get; }
		bool EnableBackupMovies { get; }
		bool MoviesOnDisk { get; }
		int MovieCompressionLevel { get; }
		bool VBAStyleMovieLoadState { get; }
		bool PlaySoundOnMovieEnd { get; set; }
		ZwinderStateManagerSettings DefaultTasStateManagerSettings { get; }
	}

	public class MovieConfig : IMovieConfig
	{
		public MovieEndAction MovieEndAction { get; set; } = MovieEndAction.Pause;
		public bool EnableBackupMovies { get; set; } = true;
		public bool MoviesOnDisk { get; set; }
		public int MovieCompressionLevel { get; set; } = 2;
		public bool VBAStyleMovieLoadState { get; set; }
		public bool PlaySoundOnMovieEnd { get; set; }

		public ZwinderStateManagerSettings DefaultTasStateManagerSettings { get; set; } = new ZwinderStateManagerSettings();
	}
}
