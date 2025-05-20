#if AVI_SUPPORT
#pragma warning disable SA1129
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

using BizHawk.Client.Common;
using BizHawk.Common;
using BizHawk.Common.PathExtensions;
using BizHawk.Emulation.Common;

using Windows.Win32;

// some helpful p/invoke from http://www.codeproject.com/KB/audio-video/Motion_Detection.aspx?msg=1142967
namespace BizHawk.Client.EmuHawk
{
	[VideoWriter("vfwavi", "AVI writer",
		"Uses the Microsoft AVIFIL32 system to write .avi files.  Audio is uncompressed; Video can be compressed with any installed VCM codec.  Splits on 2G and resolution change.")]
	internal class AviWriter : IVideoWriter
	{
		private CodecToken _currVideoCodecToken;
		private AviWriterSegment _currSegment;

		private readonly IDialogParent _dialogParent;

		private IEnumerator<string> _nameProvider;

		public AviWriter(IDialogParent dialogParent) => _dialogParent = dialogParent;

		public void SetFrame(int frame)
		{
		}

		private bool IsOpen => _nameProvider != null;

		public void Dispose()
			=> _currSegment?.Dispose();

		/// <summary>sets the codec token to be used for video compression</summary>
		/// <exception cref="ArgumentException"><paramref name="token"/> does not inherit <see cref="CodecToken"/></exception>
		public void SetVideoCodecToken(IDisposable token)
		{
			if (token is CodecToken cToken)
			{
				_currVideoCodecToken = cToken;
			}
			else
			{
				throw new ArgumentException(message: $"{nameof(AviWriter)} only takes its own {nameof(CodecToken)}s!", paramName: nameof(token));
			}
		}

		public static IEnumerator<string> CreateBasicNameProvider(string template)
		{
			var (dir, baseName, ext) = template.SplitPathToDirFileAndExt();
			yield return template;
			var counter = 1;
			while (counter < int.MaxValue)
			{
				yield return Path.Combine(dir, $"{baseName}_{counter}{ext}");
				counter++;
			}

			yield return Path.Combine(dir, $"{baseName}_{counter}{ext}");

			throw new InvalidOperationException("Reached maximum names");
		}

		/// <summary>
		/// opens an avi file for recording with names based on the supplied template.
		/// set a video codec token first.
		/// </summary>
		public void OpenFile(string baseName)
			=> OpenFile(CreateBasicNameProvider(baseName));

		// thread communication
		// synchronized queue with custom messages
		// it seems like there are 99999 ways to do everything in C#, so i'm sure this is not the best
		private BlockingCollection<object> _threadQ;
		private Thread _workerT;

		private void ThreadProc()
		{
			try
			{
				while (true)
				{
					var o = _threadQ.Take();
					switch (o)
					{
						case IVideoProvider provider:
							AddFrameEx(provider);
							break;
						case short[] arr:
							AddSamplesEx(arr);
							break;
						default:
							// anything else is assumed to be quit time
							return;
					}
				}
			}
			catch (Exception e)
			{
				_dialogParent.DialogController.ShowMessageBox($"AVIFIL32 Thread died:\n\n{e}");
			}
		}

		// we can't pass the IVideoProvider we get to another thread, because it doesn't actually keep a local copy of its data,
		// instead grabbing it from the emu as needed.  this causes frame loss/dupping as a race condition
		// instead we pass this
		private class VideoCopy : IVideoProvider
		{
			private readonly int[] _vb;
			public int VirtualWidth { get; }
			public int VirtualHeight { get; }
			public int BufferWidth { get; }
			public int BufferHeight { get; }
			public int BackgroundColor { get; }
			public int VsyncNumerator { get; }
			public int VsyncDenominator { get; }

			public VideoCopy(IVideoProvider c)
			{
				_vb = c.GetVideoBufferCopy();
				BufferWidth = c.BufferWidth;
				BufferHeight = c.BufferHeight;
				BackgroundColor = c.BackgroundColor;
				VirtualWidth = c.VirtualWidth;
				VirtualHeight = c.VirtualHeight;
				VsyncNumerator = c.VsyncNumerator;
				VsyncDenominator = c.VsyncDenominator;
			}

			public int[] GetVideoBuffer()
				=> _vb;
		}

		/// <summary>opens an avi file for recording, with <paramref name="nameProvider"/> being used to name files</summary>
		/// <exception cref="InvalidOperationException">no video codec token set</exception>
		public void OpenFile(IEnumerator<string> nameProvider)
		{
			_nameProvider = nameProvider;
			if (_currVideoCodecToken == null)
			{
				throw new InvalidOperationException("Tried to start recording an AVI with no video codec token set");
			}

			_threadQ = new(30);
			_workerT = new(ThreadProc) { IsBackground = true };
			_workerT.Start();
		}

		public void CloseFile()
		{
			_threadQ.Add(new()); // acts as stop message
			_workerT.Join();
			_currSegment?.Dispose();
			_currSegment = null;
		}

		/// <exception cref="Exception">worker thrread died</exception>
		public void AddFrame(IVideoProvider source)
		{
			while (!_threadQ.TryAdd(new VideoCopy(source), 1000))
			{
				if (!_workerT.IsAlive)
				{
					throw new Exception("AVI Worker thread died!");
				}
			}
		}

		private void AddFrameEx(IVideoProvider source)
		{
			SetVideoParameters(source.BufferWidth, source.BufferHeight);
			ConsiderLengthSegment();
			if (_currSegment == null)
			{
				Segment();
			}

			_currSegment!.AddFrame(source);
		}

		/// <exception cref="Exception">worker thrread died</exception>
		public void AddSamples(short[] samples)
		{
			// as MainForm.cs is written now, samples is all ours (nothing else will use it for anything)
			// but that's a bad assumption to make and could change in the future, so copy it since we're passing to another thread
			while (!_threadQ.TryAdd((short[])samples.Clone(), 1000))
			{
				if (!_workerT.IsAlive)
				{
					throw new Exception("AVI Worker thread died!");
				}
			}
		}

		private void AddSamplesEx(short[] samples)
		{
			ConsiderLengthSegment();
			if (_currSegment == null)
			{
				Segment();
			}

			_currSegment!.AddSamples(samples);
		}

		private void ConsiderLengthSegment()
		{
			if (_currSegment == null)
			{
				return;
			}

			var len = _currSegment.GetLengthApproximation();
			const long segment_length_limit = 2 * 1000 * 1000 * 1000; // 2GB

			// const long segment_length_limit = 10 * 1000 * 1000; //for testing
			if (len > segment_length_limit)
			{
				Segment();
			}
		}

		private void Segment()
		{
			if (!IsOpen)
			{
				return;
			}

			_currSegment?.Dispose();
			_currSegment = new();

			_nameProvider.MoveNext();
			_currSegment.OpenFile(_nameProvider.Current, _parameters, _currVideoCodecToken);

			try
			{
				_currSegment.OpenStreams();
			}
			catch // will automatically try again with 32 bit
			{
				_currSegment.OpenStreams();
			}
		}

		/// <summary>
		/// Acquires a video codec configuration from the user. you may save it for future use, but you must dispose of it when you're done with it.
		/// returns null if the user canceled the dialog
		/// </summary>
		public IDisposable AcquireVideoCodecToken(Config config)
		{
			var tempParams = new Parameters
			{
				height = 256,
				width = 256,
				fps = 60,
				fps_scale = 1,
				a_bits = 16,
				a_samplerate = 44100,
				a_channels = 2,
			};

			var tempSegment = new AviWriterSegment();
			var tempFile = Path.GetTempFileName();
			File.Delete(tempFile);
			tempFile = Path.ChangeExtension(tempFile, "avi");
			tempSegment.OpenFile(tempFile, tempParams, null);

			try
			{
				var ret = tempSegment.AcquireVideoCodecToken(_dialogParent.AsWinFormsHandle().Handle, _currVideoCodecToken);
				var token = (CodecToken)ret;
				config.AviCodecToken = token?.Serialize();
				return token;
			}
			finally
			{
				tempSegment.CloseFile();
				File.Delete(tempFile);
			}
		}

		private class Parameters
		{
			public int width, height;
			public int pitch; //in bytes
			public int pitch_add;
			public void PopulateBITMAPINFOHEADER24(ref AVIWriterImports.BITMAPINFOHEADER bmih)
			{
				bmih.Init();
				bmih.biPlanes = 1;
				bmih.biBitCount = 24;
				bmih.biHeight = height;

				// pad up width so that we end up with multiple of 4 bytes
				pitch = width * 3;
				pitch = (pitch + 3) & ~3;
				pitch_add = pitch - (width * 3);
				bmih.biWidth = width;
				bmih.biSizeImage = (uint)(pitch * height);
			}

			public void PopulateBITMAPINFOHEADER32(ref AVIWriterImports.BITMAPINFOHEADER bmih)
			{
				bmih.Init();
				bmih.biPlanes = 1;
				bmih.biBitCount = 32;
				pitch = width * 4;
				bmih.biHeight = height;
				bmih.biWidth = width;
				bmih.biSizeImage = (uint)(pitch * height);
			}

			public bool has_audio;
			public int a_samplerate, a_channels, a_bits;

			/// <exception cref="InvalidOperationException"><see cref="a_bits"/> is not <c>8</c> or <c>16</c>, or <see cref="a_channels"/> is not in range <c>1..2</c></exception>
			public void PopulateWAVEFORMATEX(ref AVIWriterImports.WAVEFORMATEX wfex)
			{
				const int WAVE_FORMAT_PCM = 1;
				var bytes = a_bits switch
				{
					16 => 2,
					8 => 1,
					_ => throw new InvalidOperationException($"only 8/16 bits audio are supported by {nameof(AviWriter)} and you chose: {a_bits}"),
				};

				if (a_channels is not (1 or 2))
				{
					throw new InvalidOperationException($"only 1/2 channels audio are supported by {nameof(AviWriter)} and you chose: {a_channels}");
				}

				wfex.Init();
				wfex.nBlockAlign = (ushort)(bytes * a_channels);
				wfex.nChannels = (ushort)a_channels;
				wfex.wBitsPerSample = (ushort)a_bits;
				wfex.wFormatTag = WAVE_FORMAT_PCM;
				wfex.nSamplesPerSec = (uint)a_samplerate;
				wfex.nAvgBytesPerSec = (uint)(wfex.nBlockAlign * a_samplerate);
			}

			public int fps, fps_scale;
		}

		private readonly Parameters _parameters = new Parameters();


		/// <summary>
		/// set basic movie timing parameters for the avi file. must be set before the file is opened.
		/// </summary>
		public void SetMovieParameters(int fpsNum, int fpsDen)
		{
			bool change = false;

			change |= fpsNum != _parameters.fps;
			_parameters.fps = fpsNum;

			change |= _parameters.fps_scale != fpsDen;
			_parameters.fps_scale = fpsDen;

			if (change)
			{
				Segment();
			}
		}

		/// <summary>
		/// set basic video parameters for the avi file. must be set before the file is opened.
		/// </summary>
		public void SetVideoParameters(int width, int height)
		{
			var change = width != _parameters.width || height != _parameters.height;
			if (!change) return;
			_parameters.width = width;
			_parameters.height = height;
			Segment();
		}

		/// <summary>
		/// set basic audio parameters for the avi file. must be set before the file isopened.
		/// </summary>
		public void SetAudioParameters(int sampleRate, int channels, int bits)
		{
			var change = !_parameters.has_audio || sampleRate != _parameters.a_samplerate
				|| channels != _parameters.a_channels || bits != _parameters.a_bits;
			if (!change) return;
			_parameters.a_samplerate = sampleRate;
			_parameters.a_channels = channels;
			_parameters.a_bits = bits;
			_parameters.has_audio = true;
			Segment();
		}

		public class CodecToken : IDisposable
		{
			public void Dispose()
			{
			}

			private CodecToken()
			{
			}

			private AVIWriterImports.AVICOMPRESSOPTIONS _comprOptions;
			public string codec;
			public byte[] Format = Array.Empty<byte>();
			public byte[] Parms = Array.Empty<byte>();

			private static unsafe string Decode_mmioFOURCC(int code)
			{
				var chs = stackalloc char[4];

				for (var i = 0; i < 4; i++)
				{
					chs[i] = (char)(byte)((code >> (i << 3)) & 0xFF);
					if (!char.IsLetterOrDigit(chs[i]))
						chs[i] = ' ';
				}

				return new(chs, 0, 4);
			}

			public static CodecToken CreateFromAVICOMPRESSOPTIONS(ref AVIWriterImports.AVICOMPRESSOPTIONS opts)
			{
				var ret = new CodecToken
				{
					_comprOptions = opts,
					codec = Decode_mmioFOURCC(opts.fccHandler),
					Format = new byte[opts.cbFormat],
					Parms = new byte[opts.cbParms],
				};

				if (opts.lpFormat != IntPtr.Zero)
				{
					Marshal.Copy(opts.lpFormat, ret.Format, 0, opts.cbFormat);
				}

				if (opts.lpParms != IntPtr.Zero)
				{
					Marshal.Copy(opts.lpParms, ret.Parms, 0, opts.cbParms);
				}

				return ret;
			}

			public static void DeallocateAVICOMPRESSOPTIONS(ref AVIWriterImports.AVICOMPRESSOPTIONS opts)
			{
#endif
#if false // test: increase stability by never freeing anything, ever
				if (opts.lpParms != IntPtr.Zero)
				{
					HeapFree(GetProcessHeap_SafeHandle(), 0, opts.lpParms);
				}

				if (opts.lpFormat != IntPtr.Zero)
				{
					HeapFree(GetProcessHeap_SafeHandle(), 0, opts.lpFormat);
				}
#endif
#if AVI_SUPPORT
				opts.lpParms = IntPtr.Zero;
				opts.lpFormat = IntPtr.Zero;
			}

			public void AllocateToAVICOMPRESSOPTIONS(out AVIWriterImports.AVICOMPRESSOPTIONS opts)
			{
				if (_comprOptions.cbParms != 0)
				{
					_comprOptions.lpParms = Win32Imports.HeapAlloc(_comprOptions.cbParms);
					Marshal.Copy(Parms, 0, _comprOptions.lpParms, _comprOptions.cbParms);
				}

				if (_comprOptions.cbFormat != 0)
				{
					_comprOptions.lpFormat = Win32Imports.HeapAlloc(_comprOptions.cbFormat);
					Marshal.Copy(Format, 0, _comprOptions.lpFormat, _comprOptions.cbFormat);
				}

				opts = _comprOptions;
			}

			private byte[] SerializeToByteArray()
			{
				var m = new MemoryStream();
				var b = new BinaryWriter(m);

				b.Write(_comprOptions.fccType);
				b.Write(_comprOptions.fccHandler);
				b.Write(_comprOptions.dwKeyFrameEvery);
				b.Write(_comprOptions.dwQuality);
				b.Write(_comprOptions.dwBytesPerSecond);
				b.Write(_comprOptions.dwFlags);
				//b.Write(comprOptions.lpFormat);
				b.Write(_comprOptions.cbFormat);
				//b.Write(comprOptions.lpParms);
				b.Write(_comprOptions.cbParms);
				b.Write(_comprOptions.dwInterleaveEvery);
				b.Write(Format);
				b.Write(Parms);
				b.Close();
				return m.ToArray();
			}

			private static CodecToken DeSerializeFromByteArray(byte[] data)
			{
				var m = new MemoryStream(data, false);
				var b = new BinaryReader(m);

				var comprOptions = default(AVIWriterImports.AVICOMPRESSOPTIONS);

				byte[] format;
				byte[] parms;

				try
				{

					comprOptions.fccType = b.ReadInt32();
					comprOptions.fccHandler = b.ReadInt32();
					comprOptions.dwKeyFrameEvery = b.ReadInt32();
					comprOptions.dwQuality = b.ReadInt32();
					comprOptions.dwBytesPerSecond = b.ReadInt32();
					comprOptions.dwFlags = b.ReadInt32();
					//comprOptions.lpFormat = b.ReadInt32();
					comprOptions.cbFormat = b.ReadInt32();
					//comprOptions.lpParms = b.ReadInt32();
					comprOptions.cbParms = b.ReadInt32();
					comprOptions.dwInterleaveEvery = b.ReadInt32();

					format = b.ReadBytes(comprOptions.cbFormat);
					parms = b.ReadBytes(comprOptions.cbParms);
				}
				catch (IOException)
				{
					// ran off end of array most likely
					return null;
				}
				finally
				{
					b.Close();
				}

				return new()
				{
					_comprOptions = comprOptions,
					Format = format,
					Parms = parms,
					codec = Decode_mmioFOURCC(comprOptions.fccHandler),
				};
			}

			public string Serialize()
				=> Convert.ToBase64String(SerializeToByteArray());

			public static CodecToken DeSerialize(string s)
				=> DeSerializeFromByteArray(Convert.FromBase64String(s));
		}

		/// <summary>
		/// set metadata parameters; should be called before opening file
		/// NYI
		/// </summary>
		public void SetMetaData(string gameName, string authors, ulong lengthMS, ulong rerecords)
		{
		}

		private class AviWriterSegment : IDisposable
		{
			static AviWriterSegment()
			{
				AVIWriterImports.AVIFileInit();
			}

			public void Dispose()
				=> CloseFile();

			private CodecToken _currVideoCodecToken;
			private bool _isOpen;
			private IntPtr _pAviFile, _pAviRawVideoStream, _pAviRawAudioStream, _pAviCompressedVideoStream;
			private IntPtr _pGlobalBuf;
			private int _pGlobalBuffSize;

			// are we sending 32 bit RGB to avi or 24?
			private bool _bit32;

			/// <summary>
			/// there is just ony global buf. this gets it and makes sure its big enough. don't get all re-entrant on it!
			/// </summary>
			private IntPtr GetStaticGlobalBuf(int amount)
			{
				if (amount > _pGlobalBuffSize)
				{
					if (_pGlobalBuf != IntPtr.Zero)
					{
						Marshal.FreeHGlobal(_pGlobalBuf);
					}

					_pGlobalBuffSize = amount;
					_pGlobalBuf = Marshal.AllocHGlobal(_pGlobalBuffSize);
				}

				return _pGlobalBuf;
			}

			private class OutputStatus
			{
				public int video_frames;
				public int video_bytes;
				public int audio_bytes;
				public int audio_samples;
				public int audio_buffered_shorts;
				public const int AUDIO_SEGMENT_SIZE = 44100 * 2;
				public readonly short[] BufferedShorts = new short[AUDIO_SEGMENT_SIZE];
			}

			private OutputStatus _outStatus;

			public long GetLengthApproximation()
				=> _outStatus.video_bytes + _outStatus.audio_bytes;

			private static unsafe int AVISaveOptions(IntPtr stream, ref AVIWriterImports.AVICOMPRESSOPTIONS opts, IntPtr owner)
			{
				fixed (AVIWriterImports.AVICOMPRESSOPTIONS* _popts = &opts)
				{
					var pStream = &stream;
					var popts = _popts;
					var ppopts = &popts;
					return AVIWriterImports.AVISaveOptions(owner, 0, 1, pStream, ppopts);
				}
			}

			private Parameters _parameters;

			/// <exception cref="InvalidOperationException">unmanaged call failed</exception>
			public void OpenFile(string destPath, Parameters parameters, CodecToken videoCodecToken)
			{
				static int mmioFOURCC(string str) => (
					(byte)str[0] |
					((byte)str[1] << 8) |
					((byte)str[2] << 16) |
					((byte)str[3] << 24)
				);

				this._parameters = parameters;
				this._currVideoCodecToken = videoCodecToken;

				// TODO - try creating the file once first before we let vfw botch it up?

				// open the avi output file handle
				if (File.Exists(destPath))
				{
					File.Delete(destPath);
				}

				var hr = AVIWriterImports.AVIFileOpenW(ref _pAviFile, destPath,
					AVIWriterImports.OpenFileStyle.OF_CREATE | AVIWriterImports.OpenFileStyle.OF_WRITE, 0);
				var hrEx = Marshal.GetExceptionForHR(hr);
				if (hrEx != null)
				{
					throw new InvalidOperationException($"Couldnt open dest path for avi file: {destPath}", hrEx);
				}

				// initialize the video stream
				var vidstream_header = default(AVIWriterImports.AVISTREAMINFOW);
				var bmih = default(AVIWriterImports.BITMAPINFOHEADER);
				parameters.PopulateBITMAPINFOHEADER24(ref bmih);
				vidstream_header.fccType = mmioFOURCC("vids");
				vidstream_header.dwRate = parameters.fps;
				vidstream_header.dwScale = parameters.fps_scale;
				vidstream_header.dwSuggestedBufferSize = (int)bmih.biSizeImage;

				hr = AVIWriterImports.AVIFileCreateStreamW(_pAviFile, out _pAviRawVideoStream, ref vidstream_header);
				hrEx = Marshal.GetExceptionForHR(hr);
				if (hrEx != null)
				{
					CloseFile();
					throw new InvalidOperationException("Failed opening raw video stream. Not sure how this could happen", hrEx);
				}

				// initialize audio stream
				var audstream_header = default(AVIWriterImports.AVISTREAMINFOW);
				var wfex = default(AVIWriterImports.WAVEFORMATEX);
				parameters.PopulateWAVEFORMATEX(ref wfex);
				audstream_header.fccType = mmioFOURCC("auds");
				audstream_header.dwQuality = -1;
				audstream_header.dwScale = wfex.nBlockAlign;
				audstream_header.dwRate = (int)wfex.nAvgBytesPerSec;
				audstream_header.dwSampleSize = wfex.nBlockAlign;
				audstream_header.dwInitialFrames = 1; // ??? optimal value?

				hr = AVIWriterImports.AVIFileCreateStreamW(_pAviFile, out _pAviRawAudioStream, ref audstream_header);
				hrEx = Marshal.GetExceptionForHR(hr);
				if (hrEx != null)
				{
					CloseFile();
					throw new InvalidOperationException("Failed opening raw audio stream. Not sure how this could happen", hrEx);
				}

				_outStatus = new();
				_isOpen = true;
			}


			/// <summary>acquires a video codec configuration from the user</summary>
			/// <exception cref="InvalidOperationException">no file open (need to call <see cref="OpenFile"/>)</exception>
			public IDisposable AcquireVideoCodecToken(IntPtr hwnd, CodecToken lastCodecToken)
			{
				if (!_isOpen)
				{
					throw new InvalidOperationException("File must be opened before acquiring a codec token (or else the stream formats wouldnt be known)");
				}

				if (lastCodecToken != null)
				{
					_currVideoCodecToken = lastCodecToken;
				}

				// encoder params
				var comprOptions = default(AVIWriterImports.AVICOMPRESSOPTIONS);
				_currVideoCodecToken?.AllocateToAVICOMPRESSOPTIONS(out comprOptions);

				var result = AVISaveOptions(_pAviRawVideoStream, ref comprOptions, hwnd) != 0;
				var ret = CodecToken.CreateFromAVICOMPRESSOPTIONS(ref comprOptions);

				// so, AVISaveOptions may have changed some of the pointers
				// if it changed the pointers, did it it free the old ones? we don't know
				// let's assume it frees them. if we're wrong, we leak. if we assume otherwise and we're wrong, we may crash.
				// so that means any pointers that come in here are either
				// 1. ones we allocated a minute ago
				// 2. ones VFW allocated
				// guess what? doesn't matter. We'll free them all ourselves.
				CodecToken.DeallocateAVICOMPRESSOPTIONS(ref comprOptions);

				return result ? ret : null;
			}

			/// <summary>begin recording</summary>
			/// <exception cref="InvalidOperationException">no video codec token set (need to call <see cref="OpenFile"/>), or unmanaged call failed</exception>
			public void OpenStreams()
			{
				if (_currVideoCodecToken == null)
				{
					throw new InvalidOperationException("set a video codec token before opening the streams!");
				}

				// open compressed video stream
				_currVideoCodecToken.AllocateToAVICOMPRESSOPTIONS(out var opts);

				var hr = AVIWriterImports.AVIMakeCompressedStream(out _pAviCompressedVideoStream, _pAviRawVideoStream, ref opts, IntPtr.Zero);
				var hrEx = Marshal.GetExceptionForHR(hr);
				CodecToken.DeallocateAVICOMPRESSOPTIONS(ref opts);
				if (hrEx != null)
				{
					CloseStreams();
					throw new InvalidOperationException("Failed making compressed video stream", hrEx);
				}

				// set the compressed video stream input format
				var bmih = default(AVIWriterImports.BITMAPINFOHEADER);
				if (_bit32)
				{
					_parameters.PopulateBITMAPINFOHEADER32(ref bmih);
				}
				else
				{
					_parameters.PopulateBITMAPINFOHEADER24(ref bmih);
				}

				hr = AVIWriterImports.AVIStreamSetFormat(_pAviCompressedVideoStream, 0, ref bmih, Marshal.SizeOf(bmih));
				hrEx = Marshal.GetExceptionForHR(hr);
				if (hrEx != null)
				{
					_bit32 = true; // we'll try again
					CloseStreams();
					throw new InvalidOperationException("Failed setting compressed video stream input format", hrEx);
				}

				// set audio stream input format
				var wfex = default(AVIWriterImports.WAVEFORMATEX);
				_parameters.PopulateWAVEFORMATEX(ref wfex);

				hr = AVIWriterImports.AVIStreamSetFormat(_pAviRawAudioStream, 0, ref wfex, Marshal.SizeOf(wfex));
				hrEx = Marshal.GetExceptionForHR(hr);
				if (hrEx != null)
				{
					CloseStreams();
					throw new InvalidOperationException("Failed setting raw audio stream input format");
				}
			}

			/// <summary>
			/// wrap up the AVI writing
			/// </summary>
			public void CloseFile()
			{
				CloseStreams();
				if (_pAviRawAudioStream != IntPtr.Zero)
				{
					_ = AVIWriterImports.AVIStreamRelease(_pAviRawAudioStream);
					_pAviRawAudioStream = IntPtr.Zero;
				}

				if (_pAviRawVideoStream != IntPtr.Zero)
				{
					_ = AVIWriterImports.AVIStreamRelease(_pAviRawVideoStream);
					_pAviRawVideoStream = IntPtr.Zero;
				}

				if (_pAviFile != IntPtr.Zero)
				{
					_ = AVIWriterImports.AVIFileRelease(_pAviFile);
					_pAviFile = IntPtr.Zero;
				}

				if (_pGlobalBuf != IntPtr.Zero)
				{
					Marshal.FreeHGlobal(_pGlobalBuf);
					_pGlobalBuf = IntPtr.Zero;
					_pGlobalBuffSize = 0;
				}
			}

			/// <summary>
			/// end recording
			/// </summary>
			private void CloseStreams()
			{
				if (_pAviRawAudioStream != IntPtr.Zero)
				{
					FlushBufferedAudio();
				}

				if (_pAviCompressedVideoStream != IntPtr.Zero)
				{
					_ = AVIWriterImports.AVIStreamRelease(_pAviCompressedVideoStream);
					_pAviCompressedVideoStream = IntPtr.Zero;
				}
			}

			// todo - why couldnt this take an ISoundProvider? it could do the timekeeping as well.. hmm
			public void AddSamples(IReadOnlyList<short> samples)
			{
				var todo = samples.Count;
				var idx = 0;
				while (todo > 0)
				{
					var remain = OutputStatus.AUDIO_SEGMENT_SIZE - _outStatus.audio_buffered_shorts;
					var chunk = Math.Min(remain, todo);

					for (var i = 0; i < chunk; i++)
					{
						_outStatus.BufferedShorts[_outStatus.audio_buffered_shorts++] = samples[idx++];
					}

					todo -= chunk;

					if (_outStatus.audio_buffered_shorts == OutputStatus.AUDIO_SEGMENT_SIZE)
					{
						FlushBufferedAudio();
					}
				}
			}

			private unsafe void FlushBufferedAudio()
			{
				var todo = _outStatus.audio_buffered_shorts;
				var todo_realsamples = todo / 2;
				var buf = GetStaticGlobalBuf(todo * 2);

				var sptr = (short*)buf.ToPointer();
				for (var i = 0; i < todo; i++)
				{
					sptr[i] = _outStatus.BufferedShorts[i];
				}

				// (TODO - inefficient- build directly in a buffer)
				_ = AVIWriterImports.AVIStreamWrite(_pAviRawAudioStream, _outStatus.audio_samples,
					todo_realsamples, buf, todo_realsamples * 4, 0, IntPtr.Zero, out var bytes_written);
				_outStatus.audio_samples += todo_realsamples;
				_outStatus.audio_bytes += bytes_written;
				_outStatus.audio_buffered_shorts = 0;
			}

			/// <exception cref="InvalidOperationException">attempted frame resize during encoding</exception>
			public unsafe void AddFrame(IVideoProvider source)
			{
				const int AVIIF_KEYFRAME = 0x00000010;

				if (_parameters.width != source.BufferWidth
					|| _parameters.height != source.BufferHeight)
					throw new InvalidOperationException("video buffer changed between start and now");

				var pitch_add = _parameters.pitch_add;

				var todo = _parameters.pitch * _parameters.height;
				var w = source.BufferWidth;
				var h = source.BufferHeight;

				if (!_bit32)
				{
					var buf = GetStaticGlobalBuf(todo);

					// TODO - would using a byte* be faster?
					var buffer = source.GetVideoBuffer();
					fixed (int* buffer_ptr = buffer)
					{
						var bytes_ptr = (byte*)buf.ToPointer();
						{
							var bp = bytes_ptr;

							for (int idx = w * h - w, y = 0; y < h; y++)
							{
								for (var x = 0; x < w; x++, idx++)
								{
									var r = (buffer_ptr[idx] >> 0) & 0xFF;
									var g = (buffer_ptr[idx] >> 8) & 0xFF;
									var b = (buffer_ptr[idx] >> 16) & 0xFF;
									*bp++ = (byte)r;
									*bp++ = (byte)g;
									*bp++ = (byte)b;
								}

								idx -= w * 2;
								bp += pitch_add;
							}

							_ = AVIWriterImports.AVIStreamWrite(_pAviCompressedVideoStream, _outStatus.video_frames,
								1, new(bytes_ptr), todo, AVIIF_KEYFRAME, IntPtr.Zero, out var bytes_written);
							_outStatus.video_bytes += bytes_written;
							_outStatus.video_frames++;
						}
					}
				}
				else // 32 bit
				{
					var buf = GetStaticGlobalBuf(todo * 4);
					var buffer = source.GetVideoBuffer();
					fixed (int* buffer_ptr = buffer)
					{
						var bytes_ptr = (byte*)buf.ToPointer();
						{
							var bp = bytes_ptr;

							for (int idx = w * h - w, y = 0; y < h; y++)
							{
								for (var x = 0; x < w; x++, idx++)
								{
									var r = (buffer_ptr[idx] >> 0) & 0xFF;
									var g = (buffer_ptr[idx] >> 8) & 0xFF;
									var b = (buffer_ptr[idx] >> 16) & 0xFF;
									*bp++ = (byte)r;
									*bp++ = (byte)g;
									*bp++ = (byte)b;
									*bp++ = 0;
								}

								idx -= w * 2;
							}

							_ = AVIWriterImports.AVIStreamWrite(_pAviCompressedVideoStream, _outStatus.video_frames,
								1, new(bytes_ptr), todo * 3, AVIIF_KEYFRAME, IntPtr.Zero, out var bytes_written);
							_outStatus.video_bytes += bytes_written;
							_outStatus.video_frames++;
						}
					}
				}
			}
		}

		/// <exception cref="Exception">no default codec token in config</exception>
		public void SetDefaultVideoCodecToken(Config config)
		{
			var ct = CodecToken.DeSerialize(config.AviCodecToken);
			_currVideoCodecToken = ct ?? throw new Exception($"No default {nameof(config.AviCodecToken)} in config!");
		}

		public string DesiredExtension()
			=> "avi";

		public bool UsesAudio => _parameters.has_audio;

		public bool UsesVideo => true;

#endif
#if false // API has changed
		private static void TestAVI()
		{
			AviWriter aw = new AviWriter();
			aw.SetVideoParameters(256, 256);
			aw.SetMovieParameters(60, 1);
			aw.OpenFile("d:\\bizhawk.avi");
			CreateHandle();
			var token = aw.AcquireVideoCodecToken(Handle);
			aw.SetVideoCodecToken(token);
			aw.OpenStreams();

			for (int i = 0; i < 100; i++)
			{
				TestVideoProvider video = new TestVideoProvider();
				Bitmap bmp = new Bitmap(256, 256, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
				using (Graphics g = Graphics.FromImage(bmp))
				{
					g.Clear(Color.Red);
					using (Font f = new Font(FontFamily.GenericMonospace, 10))
						g.DrawString(i.ToString(), f, Brushes.Black, 0, 0);
				}
//				bmp.Save($"c:\\dump\\{i}.bmp", ImageFormat.Bmp);
				for (int y = 0, idx = 0; y < 256; y++)
					for (int x = 0; x < 256; x++)
						video.buffer[idx++] = bmp.GetPixel(x, y).ToArgb();
				aw.AddFrame(video);
			}
			aw.CloseStreams();
			aw.CloseFile();
		}
#endif
#if AVI_SUPPORT
	}
}
#endif
