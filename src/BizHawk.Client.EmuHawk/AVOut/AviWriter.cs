using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

using BizHawk.Client.Common;
using BizHawk.Common;
using BizHawk.Emulation.Common;

// some helpful p/invoke from http://www.codeproject.com/KB/audio-video/Motion_Detection.aspx?msg=1142967
namespace BizHawk.Client.EmuHawk
{
	[VideoWriter("vfwavi", "AVI writer", 
		"Uses the Microsoft AVIFIL32 system to write .avi files.  Audio is uncompressed; Video can be compressed with any installed VCM codec.  Splits on 2G and resolution change.")]
	internal class AviWriter : IVideoWriter
	{
		private CodecToken _currVideoCodecToken = null;
		private AviWriterSegment _currSegment;
		private IEnumerator<string> _nameProvider;

		public void SetFrame(int frame)
		{
		}

		private bool IsOpen => _nameProvider != null;

		public void Dispose()
		{
			_currSegment?.Dispose();
		}

		/// <summary>sets the codec token to be used for video compression</summary>
		/// <exception cref="ArgumentException"><paramref name="token"/> does not inherit <see cref="AviWriter.CodecToken"/></exception>
		public void SetVideoCodecToken(IDisposable token)
		{
			if (token is CodecToken)
			{
				_currVideoCodecToken = (CodecToken)token;
			}
			else
			{
				throw new ArgumentException($"{nameof(AviWriter)} only takes its own {nameof(CodecToken)}s!");
			}
		}

		public static IEnumerator<string> CreateBasicNameProvider(string template)
		{
			string dir = Path.GetDirectoryName(template);
			string baseName = Path.GetFileNameWithoutExtension(template);
			string ext = Path.GetExtension(template);
			yield return template;
			int counter = 1;
			for (;;)
			{
				yield return Path.Combine(dir, $"{baseName}_{counter}{ext}");
				counter++;
			}
		}

		/// <summary>
		/// opens an avi file for recording with names based on the supplied template.
		/// set a video codec token first.
		/// </summary>
		public void OpenFile(string baseName)
		{
			OpenFile(CreateBasicNameProvider(baseName));
		}

		// thread communication
		// synchronized queue with custom messages
		// it seems like there are 99999 ways to do everything in C#, so i'm sure this is not the best
		System.Collections.Concurrent.BlockingCollection<object> threadQ;
		System.Threading.Thread workerT;

		private void threadproc()
		{
			try
			{
				while (true)
				{
					object o = threadQ.Take();
					if (o is IVideoProvider)
					{
						AddFrameEx((IVideoProvider)o);
					}
					else if (o is short[])
					{
						AddSamplesEx((short[])o);
					}
					else
					{
						// anything else is assumed to be quit time
						return;
					}
				}
			}
			catch (Exception e)
			{
				MessageBox.Show($"AVIFIL32 Thread died:\n\n{e}");
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
				_vb = (int[])c.GetVideoBuffer().Clone();
				BufferWidth = c.BufferWidth;
				BufferHeight = c.BufferHeight;
				BackgroundColor = c.BackgroundColor;
				VirtualWidth = c.VirtualWidth;
				VirtualHeight = c.VirtualHeight;
				VsyncNumerator = c.VsyncNumerator;
				VsyncDenominator = c.VsyncDenominator;
			}

			public int[] GetVideoBuffer()
			{
				return _vb;
			}
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

			threadQ = new System.Collections.Concurrent.BlockingCollection<Object>(30);
			workerT = new System.Threading.Thread(new System.Threading.ThreadStart(threadproc));
			workerT.Start();
		}

		public void CloseFile()
		{
			threadQ.Add(new object()); // acts as stop message
			workerT.Join();
			_currSegment?.Dispose();
			_currSegment = null;
		}

		/// <exception cref="Exception">worker thrread died</exception>
		public void AddFrame(IVideoProvider source)
		{
			while (!threadQ.TryAdd(new VideoCopy(source), 1000))
			{
				if (!workerT.IsAlive)
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

			_currSegment.AddFrame(source);
		}

		/// <exception cref="Exception">worker thrread died</exception>
		public void AddSamples(short[] samples)
		{
			// as MainForm.cs is written now, samples is all ours (nothing else will use it for anything)
			// but that's a bad assumption to make and could change in the future, so copy it since we're passing to another thread
			while (!threadQ.TryAdd((short[])samples.Clone(), 1000))
			{
				if (!workerT.IsAlive)
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

			_currSegment.AddSamples(samples);
		}

		private void ConsiderLengthSegment()
		{
			if (_currSegment == null)
			{
				return;
			}

			long len = _currSegment.GetLengthApproximation();
			const long segment_length_limit = 2 * 1000 * 1000 * 1000; // 2GB

			// const long segment_length_limit = 10 * 1000 * 1000; //for testing
			if (len > segment_length_limit)
			{
				Segment();
			}
		}

		private void StartRecording()
		{
			// i guess theres nothing to do here
		}

		private void Segment()
		{
			if (!IsOpen)
			{
				return;
			}

			if (_currSegment == null)
			{
				StartRecording();
			}
			else
			{
				_currSegment.Dispose();
			}

			_currSegment = new AviWriterSegment();
			_nameProvider.MoveNext();
			_currSegment.OpenFile(_nameProvider.Current, parameters, _currVideoCodecToken);
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
		public IDisposable AcquireVideoCodecToken(IWin32Window hwnd)
		{
			var tempParams = new Parameters
			{
				height = 256,
				width = 256,
				fps = 60,
				fps_scale = 1,
				a_bits = 16,
				a_samplerate = 44100,
				a_channels = 2
			};
			var temp = new AviWriterSegment();
			string tempfile = Path.GetTempFileName();
			File.Delete(tempfile);
			tempfile = Path.ChangeExtension(tempfile, "avi");
			temp.OpenFile(tempfile, tempParams, null);
			CodecToken token = (CodecToken)temp.AcquireVideoCodecToken(hwnd.Handle, _currVideoCodecToken);
			temp.CloseFile();
			File.Delete(tempfile);
			return token;
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
				int bytes = 0;
				if (a_bits == 16) bytes = 2;
				else if (a_bits == 8) bytes = 1;
				else throw new InvalidOperationException($"only 8/16 bits audio are supported by {nameof(AviWriter)} and you chose: {a_bits}");
				if (a_channels == 1) { }
				else if (a_channels == 2) { }
				else throw new InvalidOperationException($"only 1/2 channels audio are supported by {nameof(AviWriter)} and you chose: {a_channels}");
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

		private readonly Parameters parameters = new Parameters();


		/// <summary>
		/// set basic movie timing parameters for the avi file. must be set before the file is opened.
		/// </summary>
		public void SetMovieParameters(int fpsNum, int fpsDen)
		{
			bool change = false;

			change |= fpsNum != parameters.fps;
			parameters.fps = fpsNum;

			change |= parameters.fps_scale != fpsDen;
			parameters.fps_scale = fpsDen;

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
			bool change = false;

			change |= parameters.width != width;
			parameters.width = width;

			change |= parameters.height != height;
			parameters.height = height;

			if (change)
			{
				Segment();
			}
		}

		/// <summary>
		/// set basic audio parameters for the avi file. must be set before the file isopened.
		/// </summary>
		public void SetAudioParameters(int sampleRate, int channels, int bits)
		{
			bool change = false;

			change |= parameters.a_samplerate != sampleRate;
			parameters.a_samplerate = sampleRate;

			change |= parameters.a_channels != channels;
			parameters.a_channels = channels;

			change |= parameters.a_bits != bits;
			parameters.a_bits = bits;

			change |= parameters.has_audio != true;
			parameters.has_audio = true;

			if (change)
			{
				Segment();
			}
		}

		public class CodecToken : IDisposable
		{
			public void Dispose() { }
			private CodecToken() { }
			private AVIWriterImports.AVICOMPRESSOPTIONS comprOptions;
			public string codec;
			public byte[] Format = new byte[0];
			public byte[] Parms = new byte[0];

			private static string decode_mmioFOURCC(int code)
			{
				char[] chs = new char[4];

				for (int i = 0; i < 4; i++)
				{
					chs[i] = (char)(byte)((code >> (i << 3)) & 0xFF);
					if (!char.IsLetterOrDigit(chs[i]))
						chs[i] = ' ';
				}
				return new string(chs);
			}

			public static CodecToken CreateFromAVICOMPRESSOPTIONS(ref AVIWriterImports.AVICOMPRESSOPTIONS opts)
			{
				var ret = new CodecToken
				{
					comprOptions = opts,
					codec = decode_mmioFOURCC(opts.fccHandler),
					Format = new byte[opts.cbFormat],
					Parms = new byte[opts.cbParms]
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
#if false // test: increase stability by never freeing anything, ever
				if (opts.lpParms != IntPtr.Zero) Win32Imports.HeapFree(Win32Imports.GetProcessHeap(), 0, opts.lpParms);
				if (opts.lpFormat != IntPtr.Zero) Win32Imports.HeapFree(Win32Imports.GetProcessHeap(), 0, opts.lpFormat);
#endif
				opts.lpParms = IntPtr.Zero;
				opts.lpFormat = IntPtr.Zero;
			}

			public void AllocateToAVICOMPRESSOPTIONS(ref AVIWriterImports.AVICOMPRESSOPTIONS opts)
			{
				opts = comprOptions;
				if (opts.cbParms != 0)
				{
					opts.lpParms = Win32Imports.HeapAlloc(Win32Imports.GetProcessHeap(), 0, opts.cbParms);
					Marshal.Copy(Parms, 0, opts.lpParms, opts.cbParms);
				}
				if (opts.cbFormat != 0)
				{
					opts.lpFormat = Win32Imports.HeapAlloc(Win32Imports.GetProcessHeap(), 0, opts.cbFormat);
					Marshal.Copy(Format, 0, opts.lpFormat, opts.cbFormat);
				}
			}
		
			private byte[] SerializeToByteArray()
			{
				var m = new MemoryStream();
				var b = new BinaryWriter(m);

				b.Write(comprOptions.fccType);
				b.Write(comprOptions.fccHandler);
				b.Write(comprOptions.dwKeyFrameEvery);
				b.Write(comprOptions.dwQuality);
				b.Write(comprOptions.dwBytesPerSecond);
				b.Write(comprOptions.dwFlags);
				//b.Write(comprOptions.lpFormat);
				b.Write(comprOptions.cbFormat);
				//b.Write(comprOptions.lpParms);
				b.Write(comprOptions.cbParms);
				b.Write(comprOptions.dwInterleaveEvery);
				b.Write(Format);
				b.Write(Parms);
				b.Close();
				return m.ToArray();
			}

			private static CodecToken DeSerializeFromByteArray(byte[] data)
			{
				var m = new MemoryStream(data, false);
				var b = new BinaryReader(m);

				AVIWriterImports.AVICOMPRESSOPTIONS comprOptions = new AVIWriterImports.AVICOMPRESSOPTIONS();

				byte[] Format;
				byte[] Parms;

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

					Format = b.ReadBytes(comprOptions.cbFormat);
					Parms = b.ReadBytes(comprOptions.cbParms);
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

				var ret = new CodecToken
				{
					comprOptions = comprOptions,
					Format = Format,
					Parms = Parms,
					codec = decode_mmioFOURCC(comprOptions.fccHandler)
				};
				return ret;
			}

			public string Serialize()
			{
				return Convert.ToBase64String(SerializeToByteArray());
			}

			public static CodecToken DeSerialize(string s)
			{
				return DeSerializeFromByteArray(Convert.FromBase64String(s));
			}
		}

		/// <summary>
		/// set metadata parameters; should be called before opening file
		/// NYI
		/// </summary>
		public void SetMetaData(string gameName, string authors, UInt64 lengthMS, UInt64 rerecords)
		{
		}

		unsafe class AviWriterSegment : IDisposable
		{
			static AviWriterSegment()
			{
				AVIWriterImports.AVIFileInit();
			}

			public AviWriterSegment()
			{
			}

			public void Dispose()
			{
				CloseFile();
			}

			private CodecToken currVideoCodecToken = null;
			private bool IsOpen;
			private IntPtr pAviFile, pAviRawVideoStream, pAviRawAudioStream, pAviCompressedVideoStream;
			private IntPtr pGlobalBuf;
			private int pGlobalBuf_size;

			// are we sending 32 bit RGB to avi or 24?
			private bool bit32 = false;

			/// <summary>
			/// there is just ony global buf. this gets it and makes sure its big enough. don't get all re-entrant on it!
			/// </summary>
			private IntPtr GetStaticGlobalBuf(int amount)
			{
				if (amount > pGlobalBuf_size)
				{
					if (pGlobalBuf != IntPtr.Zero)
					{
						Marshal.FreeHGlobal(pGlobalBuf);
					}

					pGlobalBuf_size = amount;
					pGlobalBuf = Marshal.AllocHGlobal(pGlobalBuf_size);
				}

				return pGlobalBuf;
			}


			private class OutputStatus
			{
				public int video_frames;
				public int video_bytes;
				public int audio_bytes;
				public int audio_samples;
				public int audio_buffered_shorts;
				public const int AUDIO_SEGMENT_SIZE = 44100 * 2;
				public short[] BufferedShorts = new short[AUDIO_SEGMENT_SIZE];
			}

			private OutputStatus outStatus;

			public long GetLengthApproximation()
			{
				return outStatus.video_bytes + outStatus.audio_bytes;
			}

			private static bool FAILED(int hr) => hr < 0;

			static unsafe int AVISaveOptions(IntPtr stream, ref AVIWriterImports.AVICOMPRESSOPTIONS opts, IntPtr owner)
			{
				fixed (AVIWriterImports.AVICOMPRESSOPTIONS* _popts = &opts)
				{
					IntPtr* pStream = &stream;
					AVIWriterImports.AVICOMPRESSOPTIONS* popts = _popts;
					AVIWriterImports.AVICOMPRESSOPTIONS** ppopts = &popts;
					return AVIWriterImports.AVISaveOptions(owner, 0, 1, (void*)pStream, (void*)ppopts);
				}
			}

			Parameters parameters;

			/// <exception cref="InvalidOperationException">unmanaged call failed</exception>
			public void OpenFile(string destPath, Parameters parameters, CodecToken videoCodecToken)
			{
				static int mmioFOURCC(string str) => (
					((int)(byte)(str[0]))
					| ((int)(byte)(str[1]) << 8)
					| ((int)(byte)(str[2]) << 16)
					| ((int)(byte)(str[3]) << 24)
				);

				this.parameters = parameters;
				this.currVideoCodecToken = videoCodecToken;

				// TODO - try creating the file once first before we let vfw botch it up?

				// open the avi output file handle
				if (File.Exists(destPath))
				{
					File.Delete(destPath);
				}

				if (FAILED(AVIWriterImports.AVIFileOpenW(ref pAviFile, destPath, AVIWriterImports.OpenFileStyle.OF_CREATE | AVIWriterImports.OpenFileStyle.OF_WRITE, 0)))
				{
					throw new InvalidOperationException($"Couldnt open dest path for avi file: {destPath}");
				}

				// initialize the video stream
				AVIWriterImports.AVISTREAMINFOW vidstream_header = new AVIWriterImports.AVISTREAMINFOW();
				AVIWriterImports.BITMAPINFOHEADER bmih = new AVIWriterImports.BITMAPINFOHEADER();
				parameters.PopulateBITMAPINFOHEADER24(ref bmih);
				vidstream_header.fccType = mmioFOURCC("vids");
				vidstream_header.dwRate = parameters.fps;
				vidstream_header.dwScale = parameters.fps_scale;
				vidstream_header.dwSuggestedBufferSize = (int)bmih.biSizeImage;
				if (FAILED(AVIWriterImports.AVIFileCreateStreamW(pAviFile, out pAviRawVideoStream, ref vidstream_header)))
				{
					CloseFile();
					throw new InvalidOperationException("Failed opening raw video stream. Not sure how this could happen");
				}

				// initialize audio stream
				AVIWriterImports.AVISTREAMINFOW audstream_header = new AVIWriterImports.AVISTREAMINFOW();
				AVIWriterImports.WAVEFORMATEX wfex = new AVIWriterImports.WAVEFORMATEX();
				parameters.PopulateWAVEFORMATEX(ref wfex);
				audstream_header.fccType = mmioFOURCC("auds");
				audstream_header.dwQuality = -1;
				audstream_header.dwScale = wfex.nBlockAlign;
				audstream_header.dwRate = (int)wfex.nAvgBytesPerSec;
				audstream_header.dwSampleSize = wfex.nBlockAlign;
				audstream_header.dwInitialFrames = 1; // ??? optimal value?
				if (FAILED(AVIWriterImports.AVIFileCreateStreamW(pAviFile, out pAviRawAudioStream, ref audstream_header)))
				{
					CloseFile();
					throw new InvalidOperationException("Failed opening raw audio stream. Not sure how this could happen");
				}

				outStatus = new OutputStatus();
				IsOpen = true;
			}


			/// <summary>acquires a video codec configuration from the user</summary>
			/// <exception cref="InvalidOperationException">no file open (need to call <see cref="OpenFile"/>)</exception>
			public IDisposable AcquireVideoCodecToken(IntPtr hwnd, CodecToken lastCodecToken)
			{
				if (!IsOpen)
				{
					throw new InvalidOperationException("File must be opened before acquiring a codec token (or else the stream formats wouldnt be known)");
				}

				if (lastCodecToken != null)
				{
					currVideoCodecToken = lastCodecToken;
				}

				// encoder params
				AVIWriterImports.AVICOMPRESSOPTIONS comprOptions = new AVIWriterImports.AVICOMPRESSOPTIONS();
				currVideoCodecToken?.AllocateToAVICOMPRESSOPTIONS(ref comprOptions);

				bool result = AVISaveOptions(pAviRawVideoStream, ref comprOptions, hwnd) != 0;
				CodecToken ret = CodecToken.CreateFromAVICOMPRESSOPTIONS(ref comprOptions);

				// so, AVISaveOptions may have changed some of the pointers
				// if it changed the pointers, did it it free the old ones? we don't know
				// let's assume it frees them. if we're wrong, we leak. if we assume otherwise and we're wrong, we may crash.
				// so that means any pointers that come in here are either
				// 1. ones we allocated a minute ago
				// 2. ones VFW allocated
				// guess what? doesn't matter. We'll free them all ourselves.
				CodecToken.DeallocateAVICOMPRESSOPTIONS(ref comprOptions);

				if (result)
				{
					// save to config and return it
					GlobalWin.Config.AviCodecToken = ret.Serialize();
					return ret;
				}

				return null;
			}

			/// <summary>begin recording</summary>
			/// <exception cref="InvalidOperationException">no video codec token set (need to call <see cref="OpenFile"/>), or unmanaged call failed</exception>
			public void OpenStreams()
			{
				if (currVideoCodecToken == null)
				{
					throw new InvalidOperationException("set a video codec token before opening the streams!");
				}

				// open compressed video stream
				AVIWriterImports.AVICOMPRESSOPTIONS opts = new AVIWriterImports.AVICOMPRESSOPTIONS();
				currVideoCodecToken.AllocateToAVICOMPRESSOPTIONS(ref opts);
				bool failed = FAILED(AVIWriterImports.AVIMakeCompressedStream(out pAviCompressedVideoStream, pAviRawVideoStream, ref opts, IntPtr.Zero));
				CodecToken.DeallocateAVICOMPRESSOPTIONS(ref opts);
				
				if (failed)
				{
					CloseStreams();
					throw new InvalidOperationException("Failed making compressed video stream");
				}

				// set the compressed video stream input format
				AVIWriterImports.BITMAPINFOHEADER bmih = new AVIWriterImports.BITMAPINFOHEADER();
				if (bit32)
				{
					parameters.PopulateBITMAPINFOHEADER32(ref bmih);
				}
				else
				{
					parameters.PopulateBITMAPINFOHEADER24(ref bmih);
				}

				if (FAILED(AVIWriterImports.AVIStreamSetFormat(pAviCompressedVideoStream, 0, ref bmih, Marshal.SizeOf(bmih))))
				{
					bit32 = true; // we'll try again
					CloseStreams();
					throw new InvalidOperationException("Failed setting compressed video stream input format");
				}

				// set audio stream input format
				AVIWriterImports.WAVEFORMATEX wfex = new AVIWriterImports.WAVEFORMATEX();
				parameters.PopulateWAVEFORMATEX(ref wfex);
				if (FAILED(AVIWriterImports.AVIStreamSetFormat(pAviRawAudioStream, 0, ref wfex, Marshal.SizeOf(wfex))))
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
				if (pAviRawAudioStream != IntPtr.Zero)
				{
					AVIWriterImports.AVIStreamRelease(pAviRawAudioStream);
					pAviRawAudioStream = IntPtr.Zero;
				}

				if (pAviRawVideoStream != IntPtr.Zero)
				{
					AVIWriterImports.AVIStreamRelease(pAviRawVideoStream);
					pAviRawVideoStream = IntPtr.Zero;
				}

				if (pAviFile != IntPtr.Zero)
				{
					AVIWriterImports.AVIFileRelease(pAviFile);
					pAviFile = IntPtr.Zero;
				}

				if (pGlobalBuf != IntPtr.Zero)
				{
					Marshal.FreeHGlobal(pGlobalBuf);
					pGlobalBuf = IntPtr.Zero;
					pGlobalBuf_size = 0;
				}
			}

			/// <summary>
			/// end recording
			/// </summary>
			public void CloseStreams()
			{
				if (pAviRawAudioStream != IntPtr.Zero)
				{
					FlushBufferedAudio();
				}

				if (pAviCompressedVideoStream != IntPtr.Zero)
				{
					AVIWriterImports.AVIStreamRelease(pAviCompressedVideoStream);
					pAviCompressedVideoStream = IntPtr.Zero;
				}
			}

			// todo - why couldnt this take an ISoundProvider? it could do the timekeeping as well.. hmm
			public unsafe void AddSamples(short[] samples)
			{
				int todo = samples.Length;
				int idx = 0;
				while (todo > 0)
				{
					int remain = OutputStatus.AUDIO_SEGMENT_SIZE - outStatus.audio_buffered_shorts;
					int chunk = Math.Min(remain, todo);
					for (int i = 0; i < chunk; i++)
					{
						outStatus.BufferedShorts[outStatus.audio_buffered_shorts++] = samples[idx++];
					}
					todo -= chunk;

					if (outStatus.audio_buffered_shorts == OutputStatus.AUDIO_SEGMENT_SIZE)
					{
						FlushBufferedAudio();
					}
				}
			}

			unsafe void FlushBufferedAudio()
			{
				int todo = outStatus.audio_buffered_shorts;
				int todo_realsamples = todo / 2;
				IntPtr buf = GetStaticGlobalBuf(todo * 2);

				short* sptr = (short*)buf.ToPointer();
				for (int i = 0; i < todo; i++)
				{
					sptr[i] = outStatus.BufferedShorts[i];
				}

				// (TODO - inefficient- build directly in a buffer)
				AVIWriterImports.AVIStreamWrite(pAviRawAudioStream, outStatus.audio_samples, todo_realsamples, buf, todo_realsamples * 4, 0, IntPtr.Zero, out var bytes_written);
				outStatus.audio_samples += todo_realsamples;
				outStatus.audio_bytes += bytes_written;
				outStatus.audio_buffered_shorts = 0;
			}

			/// <exception cref="InvalidOperationException">attempted frame resize during encoding</exception>
			public unsafe void AddFrame(IVideoProvider source)
			{
				const int AVIIF_KEYFRAME = 0x00000010;

				if (parameters.width != source.BufferWidth
					|| parameters.height != source.BufferHeight)
					throw new InvalidOperationException("video buffer changed between start and now");

				int pitch_add = parameters.pitch_add;

				int todo = parameters.pitch * parameters.height;
				int w = source.BufferWidth;
				int h = source.BufferHeight;

				if (!bit32)
				{
					IntPtr buf = GetStaticGlobalBuf(todo);

					// TODO - would using a byte* be faster?
					int[] buffer = source.GetVideoBuffer();
					fixed (int* buffer_ptr = &buffer[0])
					{
						byte* bytes_ptr = (byte*)buf.ToPointer();
						{
							byte* bp = bytes_ptr;

							for (int idx = w * h - w, y = 0; y < h; y++)
							{
								for (int x = 0; x < w; x++, idx++)
								{
									int r = (buffer[idx] >> 0) & 0xFF;
									int g = (buffer[idx] >> 8) & 0xFF;
									int b = (buffer[idx] >> 16) & 0xFF;
									*bp++ = (byte)r;
									*bp++ = (byte)g;
									*bp++ = (byte)b;
								}
								idx -= w * 2;
								bp += pitch_add;
							}

							int ret = AVIWriterImports.AVIStreamWrite(pAviCompressedVideoStream, outStatus.video_frames, 1, new IntPtr(bytes_ptr), todo, AVIIF_KEYFRAME, IntPtr.Zero, out var bytes_written);
							outStatus.video_bytes += bytes_written;
							outStatus.video_frames++;
						}
					}
				}
				else // 32 bit
				{
					IntPtr buf = GetStaticGlobalBuf(todo * 4);
					int[] buffer = source.GetVideoBuffer();
					fixed (int* buffer_ptr = &buffer[0])
					{
						byte* bytes_ptr = (byte*)buf.ToPointer();
						{
							byte* bp = bytes_ptr;

							for (int idx = w * h - w, y = 0; y < h; y++)
							{
								for (int x = 0; x < w; x++, idx++)
								{
									int r = (buffer[idx] >> 0) & 0xFF;
									int g = (buffer[idx] >> 8) & 0xFF;
									int b = (buffer[idx] >> 16) & 0xFF;
									*bp++ = (byte)r;
									*bp++ = (byte)g;
									*bp++ = (byte)b;
									*bp++ = 0;
								}
								idx -= w * 2;
							}

							int ret = AVIWriterImports.AVIStreamWrite(pAviCompressedVideoStream, outStatus.video_frames, 1, new IntPtr(bytes_ptr), todo * 3, AVIIF_KEYFRAME, IntPtr.Zero, out var bytes_written);
							outStatus.video_bytes += bytes_written;
							outStatus.video_frames++;
						}
					}
				}
			}
		}

		/// <exception cref="Exception">no default codec token in config</exception>
		public void SetDefaultVideoCodecToken()
		{
			CodecToken ct = CodecToken.DeSerialize(GlobalWin.Config.AviCodecToken);
			if (ct == null)
			{
				throw new Exception($"No default {nameof(GlobalWin.Config.AviCodecToken)} in config!");
			}

			_currVideoCodecToken = ct;
		}

		public string DesiredExtension()
		{
			return "avi";
		}

		public bool UsesAudio => parameters.has_audio;

		public bool UsesVideo => true;

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
	}
}
