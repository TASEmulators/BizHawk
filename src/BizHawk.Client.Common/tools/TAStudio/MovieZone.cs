using System.Linq;
using System.IO;

using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	public class MovieZone
	{
		private readonly string[] _log;
		private string _inputKey;

		/// <summary>
		/// The macro's controller, which might not have the same definition as the movie controller.
		/// </summary>
		private IMovieController _controller;

		/// <summary>
		/// A controller who's definition matches the movie controller.
		/// </summary>
		private readonly IMovieController _targetController;

		/// <summary>
		/// The controller definition for the movie the macro is being applied to.
		/// </summary>
		private readonly ControllerDefinition _movieDefinition;

		public MovieZone(IMovie movie, int start, int length)
			: this(movie)
		{
			_inputKey = CleanInputKey(Bk2LogEntryGenerator.GenerateLogKey(_movieDefinition));
			_log = new string[length];

			// Get a IController that only contains buttons in key.
			InitController();

			string movieKey = CleanInputKey(Bk2LogEntryGenerator.GenerateLogKey(_controller.Definition));
			if (_inputKey == movieKey)
			{
				for (int i = 0; i < length; i++)
				{
					_log[i] = movie.GetInputLogEntry(i + start);
				}
			}
			else
			{
				for (int i = 0; i < length; i++)
				{
					_controller.SetFrom(movie.GetInputState(i + start));
					_log[i] = Bk2LogEntryGenerator.GenerateLogEntry(_controller);
				}
			}
		}

		private MovieZone(IMovie movie)
		{
			_movieDefinition = movie.Session.MovieController.Definition;
			_targetController = new Bk2Controller(_movieDefinition);
			_targetController.SetFrom(_targetController); // Reference and create all buttons
		}

		private void InitController()
		{
			string[] keys = _inputKey.Split('|');
			ControllerDefinition d = new(_movieDefinition.Name);
			foreach (var k in keys)
			{
				if (_movieDefinition.BoolButtons.Contains(k))
				{
					d.BoolButtons.Add(k);
				}
				else
				{
					d.Axes.Add(k, _movieDefinition.Axes[k]);
				}
			}

			_controller = new Bk2Controller(d.MakeImmutable());
		}

		public string Name { get; set; }
		public int Length => _log.Length;

		public bool Replace { get; set; } = true;
		public bool Overlay { get; set; }

		public string InputKey
		{
			get => _inputKey;
			set { _inputKey = value; ReSetLog(); }
		}

		private void ReSetLog()
		{
			IMovieController oldController = _controller;
			InitController();

			// Reset all buttons in targetController (it may still have buttons that aren't being set here set true)
			_targetController.SetFromMnemonic(Bk2LogEntryGenerator.EmptyEntry(_targetController));
			for (int i = 0; i < Length; i++)
			{
				oldController.SetFromMnemonic(_log[i]);
				LatchFromSourceButtons(_targetController, oldController);
				_controller.SetFrom(_targetController);
				_log[i] = Bk2LogEntryGenerator.GenerateLogEntry(_controller);
			}
		}

		public void PlaceZone(IMovie movie, int start)
		{
			ITasMovie/*?*/ tasMovie = movie as ITasMovie;
			tasMovie?.ChangeLog.BeginNewBatch($"Place Macro at {start}");

			if (start > movie.InputLogLength)
			{
				// Cannot place a frame here. Find a nice way around this.
				return;
			}

			if (!Replace)
			{
				// Can't be done with a regular movie.
				tasMovie?.InsertEmptyFrame(start, Length);
			}

			if (Overlay)
			{
				// Overlay the frames.
				for (int i = 0; i < Length; i++)
				{
					_controller.SetFromMnemonic(_log[i]);
					LatchFromSourceButtons(_targetController, _controller);
					ORLatchFromSource(_targetController, movie.GetInputState(i + start));
					movie.PokeFrame(i + start, _targetController);
				}
			}
			else
			{
				// Copy over the frame.
				for (int i = 0; i < Length; i++)
				{
					_controller.SetFromMnemonic(_log[i]);
					LatchFromSourceButtons(_targetController, _controller);
					movie.PokeFrame(i + start, _targetController);
				}
			}

			tasMovie?.ChangeLog.EndBatch();
		}

		public FileWriteResult Save(string fileName)
		{
			// Save the controller definition/LogKey
			// Save the controller name and player count. (Only for the user.)
			// Save whether or not the macro should use overlay input, and/or replace

			return FileWriter.Write(fileName, (fs) =>
			{
				using var writer = new StreamWriter(fs);
				writer.WriteLine(InputKey);
				writer.WriteLine(_movieDefinition.Name);
				writer.WriteLine(_movieDefinition.PlayerCount.ToString());
				writer.WriteLine($"{Overlay},{Replace}");

				foreach (string line in _log)
				{
					writer.WriteLine(line);
				}
			});
		}

		public MovieZone(string fileName, IDialogController dialogController, IMovie movie)
			: this(movie)
		{
			if (!File.Exists(fileName))
			{
				return;
			}

			string[] readText = File.ReadAllLines(fileName);

			// If the LogKey contains buttons/controls not accepted by the emulator,
			// tell the user and display the macro's controller name and player count
			_inputKey = readText[0];
			string key = CleanInputKey(Bk2LogEntryGenerator.GenerateLogKey(_movieDefinition));
			string[] emuKeys = key.Split('|');
			string[] macroKeys = _inputKey.Split('|');
			foreach (var macro in macroKeys)
			{
				if (!emuKeys.Contains(macro))
				{
					dialogController.ShowMessageBox($"The selected macro is not compatible with the current emulator core.\nMacro controller: {readText[1]}\nMacro player count: {readText[2]}", "Error");
					return;
				}
			}

			// Settings
			string[] settings = readText[3].Split(',');
			Overlay = Convert.ToBoolean(settings[0]);
			Replace = Convert.ToBoolean(settings[1]);

			_log = new string[readText.Length - 4];
			readText.ToList().CopyTo(4, _log, 0, _log.Length);

			Name = Path.GetFileNameWithoutExtension(fileName);

			// Get a IController that only contains buttons in key.
			InitController();
		}

		private string CleanInputKey(string rawKey)
		{
			string key = rawKey.Replace("#", ""); // Movies separate players with #, but that character has no meaning for us.
			key = key.Substring(startIndex: 0, length: key.Length - 1); // drop last |, so we don't have an empty button when we split
			return key;
		}

		private void LatchFromSourceButtons(IMovieController latching, IController source)
		{
			foreach (string button in source.Definition.BoolButtons)
			{
				latching.SetBool(button, source.IsPressed(button));
			}

			foreach (string name in source.Definition.Axes.Keys)
			{
				latching.SetAxis(name, source.AxisValue(name));
			}
		}

		private void ORLatchFromSource(IMovieController latching, IController source)
		{
			foreach (string button in latching.Definition.BoolButtons)
			{
				latching.SetBool(button, latching.IsPressed(button) | source.IsPressed(button));
			}

			foreach (string name in latching.Definition.Axes.Keys)
			{
				var axisValue = source.AxisValue(name);
				if (axisValue == source.Definition.Axes[name].Neutral)
				{
					latching.SetAxis(name, axisValue);
				}
			}
		}
	}
}
