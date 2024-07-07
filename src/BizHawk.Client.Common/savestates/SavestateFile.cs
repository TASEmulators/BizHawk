using System.Collections.Generic;
using System.IO;
using System.Linq;

using BizHawk.Bizware.Graphics;
using BizHawk.Common;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.Common
{
	/// <summary>
	/// Represents an aggregated savestate file that includes core, movie, and other related data
	/// </summary>
	public class SavestateFile
	{
		public static BitmapBuffer/*?*/ GetFrameBufferFrom(string path)
		{
			using var bl = ZipStateLoader.LoadAndDetect(path);
			if (bl is null) return null;
			IVideoProvider/*?*/ vp = null;
			bl.GetLump(BinaryStateLump.Framebuffer, abort: false, br => QuickBmpFile.LoadAuto(br.BaseStream, out vp));
			return vp is null ? null : new(width: vp.BufferWidth, height: vp.BufferHeight, vp.GetVideoBuffer());
		}

		private readonly IEmulator _emulator;
		private readonly IStatable _statable;
		private readonly IVideoProvider _videoProvider;
		private readonly IMovieSession _movieSession;

		private readonly IDictionary<string, object> _userBag;

		public SavestateFile(
			IEmulator emulator,
			IMovieSession movieSession,
			IDictionary<string, object> userBag)
		{
			if (!emulator.HasSavestates())
			{
				throw new InvalidOperationException("The provided core must have savestates");
			}

			_emulator = emulator;
			_statable = emulator.AsStatable();
			if (emulator.HasVideoProvider())
			{
				_videoProvider = emulator.AsVideoProvider();
			}

			_movieSession = movieSession;
			_userBag = userBag;
		}

		public void Create(string filename, SaveStateConfig config)
		{
			// the old method of text savestate save is now gone.
			// a text savestate is just like a binary savestate, but with a different core lump
			using var bs = new ZipStateSaver(filename, config.CompressionLevelNormal);

			using (new SimpleTime("Save Core"))
			{
				if (config.Type == SaveStateType.Text)
				{
					bs.PutLump(BinaryStateLump.CorestateText, tw => _statable.SaveStateText(tw));
				}
				else
				{
					bs.PutLump(BinaryStateLump.Corestate, bw => _statable.SaveStateBinary(bw));
				}
			}

			if (config.SaveScreenshot && _videoProvider != null)
			{
				var buff = _videoProvider.GetVideoBuffer();
				if (buff.Length == 1)
				{
					// is a hacky opengl texture ID. can't handle this now!
					// need to discuss options
					// 1. cores must be able to provide a pixels VideoProvider in addition to a texture ID, on command (not very hard overall but interface changing and work per core)
					// 2. SavestateManager must be setup with a mechanism for resolving texture IDs (even less work, but sloppy)
					// There are additional problems with AVWriting. They depend on VideoProvider providing pixels.
				}
				else
				{
					int outWidth = _videoProvider.BufferWidth;
					int outHeight = _videoProvider.BufferHeight;

					// if buffer is too big, scale down screenshot
					if (!config.NoLowResLargeScreenshots && buff.Length >= config.BigScreenshotSize)
					{
						outWidth /= 2;
						outHeight /= 2;
					}

					using (new SimpleTime("Save Framebuffer"))
					{
						bs.PutLump(BinaryStateLump.Framebuffer, s => QuickBmpFile.Save(_videoProvider, s, outWidth, outHeight));
					}
				}
			}

			if (_movieSession.Movie.IsActive())
			{
				bs.PutLump(BinaryStateLump.Input,
					tw =>
					{
						// TODO: this should not happen and no exception should be thrown here.
						// Just make this noisy for now until the issue is fixed.
						if (_movieSession.Movie.FrameCount < _emulator.Frame)
						{
							throw new InvalidOperationException(
								$"Tried to create a savestate at frame {_emulator.Frame}, but only got a log of length {_movieSession.Movie.FrameCount}!");
						}
						// this never should have been a core's responsibility
						tw.WriteLine("Frame {0}", _emulator.Frame);
						_movieSession.HandleSaveState(tw);
					});
			}

			if (_userBag.Any())
			{
				bs.PutLump(BinaryStateLump.UserData,
					tw =>
					{
						var data = ConfigService.SaveWithType(_userBag);
						tw.WriteLine(data);
					});
			}

			if (_movieSession.Movie.IsActive() && _movieSession.Movie is ITasMovie)
			{
				bs.PutLump(BinaryStateLump.LagLog, tw => ((ITasMovie) _movieSession.Movie).LagLog.Save(tw));
			}
		}

		public bool Load(string path, IDialogParent dialogParent)
		{
			// try to detect binary first
			using var bl = ZipStateLoader.LoadAndDetect(path);
			if (bl is null) return false;
			var succeed = false;

			if (!VersionInfo.DeveloperBuild)
			{
				bl.GetLump(BinaryStateLump.BizVersion, true, tr => succeed = tr.ReadLine() == VersionInfo.GetEmuVersion());
				if (!succeed)
				{
					var result = dialogParent.ModalMessageBox2(
						"This savestate was made with a different version, so it's unlikely to work.\nChoose OK to try loading it anyway.",
						"Savestate version mismatch",
						EMsgBoxIcon.Question,
						useOKCancel: true);
					if (!result) return false;
				}
			}

			// Movie timeline check must happen before the core state is loaded
			if (_movieSession.Movie.IsActive())
			{
				bl.GetLump(BinaryStateLump.Input, true, tr => succeed = _movieSession.CheckSavestateTimeline(tr));
				if (!succeed)
				{
					return false;
				}
			}

			using (new SimpleTime("Load Core"))
			{
				bl.GetCoreState(br => _statable.LoadStateBinary(br), tr => _statable.LoadStateText(tr));
			}

			// We must handle movie input AFTER the core is loaded to properly handle mode changes, and input latching
			if (_movieSession.Movie.IsActive())
			{
				bl.GetLump(BinaryStateLump.Input, true, tr => succeed = _movieSession.HandleLoadState(tr));
				if (!succeed)
				{
					return false;
				}
			}

			if (_videoProvider != null)
			{
				bl.GetLump(BinaryStateLump.Framebuffer, false, br => PopulateFramebuffer(br, _videoProvider));
			}

			string userData = "";
			bl.GetLump(BinaryStateLump.UserData, abort: false, tr =>
			{
				string line;
				while ((line = tr.ReadLine()) != null)
				{
					if (!string.IsNullOrWhiteSpace(line))
					{
						userData = line;
					}
				}
			});

			if (!string.IsNullOrWhiteSpace(userData))
			{
				var bag = (Dictionary<string, object>)ConfigService.LoadWithType(userData);
				_userBag.Clear();
				foreach (var (k, v) in bag) _userBag.Add(k, v);
			}

			if (_movieSession.Movie.IsActive() && _movieSession.Movie is ITasMovie)
			{
				bl.GetLump(BinaryStateLump.LagLog, abort: false, tr => ((ITasMovie) _movieSession.Movie).LagLog.Load(tr));
			}

			return true;
		}

		private static void PopulateFramebuffer(BinaryReader br, IVideoProvider videoProvider)
		{
			try
			{
				using (new SimpleTime("Load Framebuffer"))
				{
					QuickBmpFile.Load(videoProvider, br.BaseStream);
				}
			}
			catch
			{
				var buff = videoProvider.GetVideoBuffer();
				try
				{
					for (int i = 0; i < buff.Length; i++)
					{
						int j = br.ReadInt32();
						buff[i] = j;
					}
				}
				catch (EndOfStreamException)
				{
				}
			}
		}
	}
}
