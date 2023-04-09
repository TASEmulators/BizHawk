using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using FFmpeg.AutoGen;

namespace SharpAudio.Codec.FFmpeg
{
    internal class FFmpegDecoder : Decoder
    {
        private const int fsStreamSize = 8192;
        private readonly int _DESIRED_CHANNEL_COUNT = 2;
        private readonly AVSampleFormat _DESIRED_SAMPLE_FORMAT = AVSampleFormat.AV_SAMPLE_FMT_S16;
        private readonly int _DESIRED_SAMPLE_RATE = 44_100;
        private readonly byte[] ffmpegFSBuf = new byte[fsStreamSize];
        private readonly int sampleByteSize;
        private readonly Stream targetStream;
        private volatile bool _isDecoderFinished;
        private bool _isFinished;
        private CircularBuffer _slidestream;
        private bool anchorNewPos;
        private avio_alloc_context_read_packet avioRead;
        private avio_alloc_context_seek avioSeek;
        private TimeSpan curPos;
        private volatile bool doSeek;
        private FFmpegPointers ff;
        private TimeSpan seekTimeTarget;
        private int stream_index;
        private byte[] tempSampleBuf;
        private volatile bool _isDisposed;
        private Thread _decoderThread;

        static FFmpegDecoder()
        {
            DoLibraryRuntimePathDetection();
        }

        private static void DoLibraryRuntimePathDetection()
        {
            // For common case native binaries located in specific for OS+Architecture folder:
            // - runtimes/
            // - - win7-x86/
            // - - - native/*.dll
            // - - osx-x86/
            // - - - native/*.dll
            // But when we pack application with MSIX or self-contained for specific architecture, it has another structure:
            // - runtime/*.dll

            string runtimeId = null;

            // Just use the system-wide ffmpeg libraries when on linux.
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                runtimeId = RuntimeInformation.OSArchitecture switch
                {
                    Architecture.X64 => "win7-x64",
                    Architecture.X86 => "win7-x86",
                    _ => runtimeId
                };
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                runtimeId = RuntimeInformation.OSArchitecture switch
                {
                    Architecture.X64 => "osx-x64",
                    Architecture.X86 => "osx-x86",
                    _ => runtimeId
                };

            var curPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);
            var specificRuntimeFolder = Path.Combine(curPath, "runtimes", runtimeId, "native");
            if (Directory.Exists(specificRuntimeFolder))
            {
                ffmpeg.RootPath = specificRuntimeFolder;
            }
            else
            {
                var singleRuntimeFolder = Path.Combine(curPath, "runtime");
                if (Directory.Exists(singleRuntimeFolder))
                {
                    ffmpeg.RootPath = singleRuntimeFolder;
                }
            }
        }

        public FFmpegDecoder(Stream src)
        {
            targetStream = src;
            sampleByteSize = _DESIRED_SAMPLE_RATE * _DESIRED_CHANNEL_COUNT * sizeof(ushort);

            FFmpeg_Initialize();
        }

        public override bool IsFinished => _isFinished;

        public override TimeSpan Position => curPos;

        public override bool HasPosition { get; } = true;

        public override TimeSpan Duration => base.Duration;

        private unsafe int Read(void* opaque, byte* targetBuffer, int targetBufferLength)
        {
            if (_isDisposed)
                return ffmpeg.AVERROR_EOF;

            try
            {
                var readCount = targetStream.Read(ffmpegFSBuf, 0, ffmpegFSBuf.Length);

                if (readCount > 0)
                    Marshal.Copy(ffmpegFSBuf, 0, (IntPtr) targetBuffer, readCount);
                else
                    return ffmpeg.AVERROR_EOF; // fixes Invalid return value 0 for stream protocol. related problem: https://trac.mplayerhq.hu/ticket/2335

                return readCount;
            }
            catch (Exception)
            {
                return ffmpeg.AVERROR_EOF;
            }
        }

        private unsafe long Seek(void* opaque, long offset, int whence)
        {
            SeekOrigin origin;

            switch (whence)
            {
                case ffmpeg.AVSEEK_SIZE:
                    return targetStream.Length;
                case 0:
                case 1:
                case 2:
                    origin = (SeekOrigin) whence;
                    break;
                default:
                    return ffmpeg.AVERROR_EOF;
            }

            targetStream.Seek(offset, origin);
            return targetStream.Position;
        }

        private unsafe void FFmpeg_Initialize()
        {
            var inputBuffer = (byte*) ffmpeg.av_malloc(fsStreamSize);

            avioRead = Read;
            avioSeek = Seek;

            ff.ioContext = ffmpeg.avio_alloc_context(inputBuffer, fsStreamSize, 0, null, avioRead, null, avioSeek);

            if ((int) ff.ioContext == 0) throw new FormatException("FFMPEG: Unable to allocate IO stream context.");

            ff.format_context = ffmpeg.avformat_alloc_context();
            ff.format_context->pb = ff.ioContext;
            ff.format_context->flags |= ffmpeg.AVFMT_FLAG_CUSTOM_IO | ffmpeg.AVFMT_FLAG_GENPTS |
                                        ffmpeg.AVFMT_FLAG_DISCARD_CORRUPT;

            fixed (AVFormatContext** fmt2 = &ff.format_context)
            {
                if (ffmpeg.avformat_open_input(fmt2, "", null, null) != 0)
                    throw new FormatException("FFMPEG: Could not open media stream.");
            }

            if (ffmpeg.avformat_find_stream_info(ff.format_context, null) < 0)
                throw new FormatException("FFMPEG: Could not retrieve stream info from IO stream");

            // Find the index of the first audio stream
            stream_index = -1;
            for (var i = 0; i < ff.format_context->nb_streams; i++)
                if (ff.format_context->streams[i]->codec->codec_type == AVMediaType.AVMEDIA_TYPE_AUDIO)
                {
                    stream_index = i;
                    break;
                }

            if (stream_index == -1)
                throw new FormatException("FFMPEG: Could not retrieve audio stream from IO stream.");

            ff.av_stream = ff.format_context->streams[stream_index];
            ff.av_codec = ff.av_stream->codec;

            if (ffmpeg.avcodec_open2(ff.av_codec, ffmpeg.avcodec_find_decoder(ff.av_codec->codec_id), null) < 0)
                throw new FormatException("FFMPEG: Failed to open decoder for stream #{stream_index} in IO stream.");

            // Fixes SWR @ 0x2192200] Input channel count and layout are unset error.
            if (ff.av_codec->channel_layout == 0)
                ff.av_codec->channel_layout = (ulong) ffmpeg.av_get_default_channel_layout(ff.av_codec->channels);

            // ff.av_codec->request_channel_layout = (ulong)ffmpeg.av_get_default_channel_layout(ff.av_codec->channels);
            // ff.av_codec->request_sample_fmt = _DESIRED_SAMPLE_FORMAT;

            SetAudioFormat();

            ff.swr_context = ffmpeg.swr_alloc_set_opts(null,
                ffmpeg.av_get_default_channel_layout(_DESIRED_CHANNEL_COUNT),
                _DESIRED_SAMPLE_FORMAT,
                _DESIRED_SAMPLE_RATE,
                (long) ff.av_codec->channel_layout,
                ff.av_codec->sample_fmt,
                ff.av_codec->sample_rate,
                0,
                null);

            ffmpeg.swr_init(ff.swr_context);

            if (ffmpeg.swr_is_initialized(ff.swr_context) == 0)
                throw new FormatException("FFMPEG: Resampler has not been properly initialized");

            ff.av_packet = ffmpeg.av_packet_alloc();
            ff.av_src_frame = ffmpeg.av_frame_alloc();

            tempSampleBuf = new byte[_audioFormat.SampleRate * _audioFormat.Channels * 5];
            _slidestream = new CircularBuffer(tempSampleBuf.Length);

            _decoderThread = new Thread(MainLoop);
            _decoderThread.Start();

        }

        private unsafe void SetAudioFormat()
        {
            _audioFormat.SampleRate = _DESIRED_SAMPLE_RATE;
            _audioFormat.Channels = _DESIRED_CHANNEL_COUNT;
            _audioFormat.BitsPerSample = 16;
            _numSamples = (int) (ff.format_context->duration / (float) ffmpeg.AV_TIME_BASE * _DESIRED_SAMPLE_RATE *
                                 _DESIRED_CHANNEL_COUNT);
        }

        public void MainLoop()
        {
            var frameFinished = 0;
            var count = 0;

            while (!_isDecoderFinished)
            {
                if (_isDisposed)
                    break;

                Thread.Sleep(1);

                if (_slidestream.Length > sampleByteSize)
                    continue;

                unsafe
                {
                    if (doSeek)
                    {
                        var seek = (long) (seekTimeTarget.TotalSeconds / ffmpeg.av_q2d(ff.av_stream->time_base));
                        ffmpeg.av_seek_frame(ff.format_context, stream_index, seek, ffmpeg.AVSEEK_FLAG_BACKWARD);
                        ffmpeg.avcodec_flush_buffers(ff.av_stream->codec);
                        ff.av_packet = ffmpeg.av_packet_alloc();
                        doSeek = false;
                        seekTimeTarget = TimeSpan.Zero;
                        _slidestream.Clear();
                        anchorNewPos = true;
                    }

                    if (ffmpeg.av_read_frame(ff.format_context, ff.av_packet) >= 0)
                    {
                        if (ff.av_packet->stream_index == stream_index)
                        {
#pragma warning disable
                            var res = ffmpeg.avcodec_decode_audio4(ff.av_stream->codec, ff.av_src_frame, &frameFinished,
                                ff.av_packet);
#pragma warning restore

                            if (res == 0)
                                continue;

                            if (ff.av_src_frame->pts == ffmpeg.AV_NOPTS_VALUE) continue;

                            if (anchorNewPos)
                            {
                                double pts = ff.av_src_frame->pts;
                                pts *= ff.av_stream->time_base.num / (double) ff.av_stream->time_base.den;
                                curPos = TimeSpan.FromSeconds(pts);
                                anchorNewPos = false;
                            }

                            if (frameFinished > 0)
                            {
                                ProcessAudioFrame(ref tempSampleBuf, ref count);
                                _slidestream.Write(tempSampleBuf, 0, count);
                            }
                        }
                    }
                    else
                    {
                        _isDecoderFinished = true;
                    }
                }
            }
        }

        public override long GetSamples(int samples, ref byte[] data)
        {
            data = new byte[samples];
            var res = _slidestream.Read(data, 0, samples);

            if ((res == 0) & _isDecoderFinished)
            {
                _isFinished = true;
                return -1;
            }

            if (res > 0)
            {
                var x = res / (double) sampleByteSize;
                curPos += TimeSpan.FromSeconds(x);

                if (data.Length != res)
                    data = data[0..res];

                return res;
            }


            return 0;
        }

        private unsafe void ProcessAudioFrame(ref byte[] data, ref int count)
        {
            ff.av_dst_frame = ffmpeg.av_frame_alloc();
            ff.av_dst_frame->sample_rate = _DESIRED_SAMPLE_RATE;
            ff.av_dst_frame->format = (int) _DESIRED_SAMPLE_FORMAT;
            ff.av_dst_frame->channels = _DESIRED_CHANNEL_COUNT;
            ff.av_dst_frame->channel_layout = (ulong) ffmpeg.av_get_default_channel_layout(ff.av_dst_frame->channels);

            ffmpeg.swr_convert_frame(ff.swr_context, ff.av_dst_frame, ff.av_src_frame);

            var bufferSize = ffmpeg.av_samples_get_buffer_size(null,
                ff.av_dst_frame->channels,
                ff.av_dst_frame->nb_samples,
                (AVSampleFormat) ff.av_dst_frame->format,
                1);

            if (bufferSize <= 0) throw new Exception($"ffmpeg returned an invalid buffer size {bufferSize}");

            count = bufferSize;

            fixed (byte* h = &data[0])
            {
                Buffer.MemoryCopy(ff.av_dst_frame->data[0], h, bufferSize, bufferSize);
            }

            fixed (AVFrame** x = &ff.av_dst_frame)
            {
                ffmpeg.av_frame_free(x);
            }
        }

        public override bool TrySeek(TimeSpan time)
        {
            if (!doSeek & targetStream.CanSeek)
            {
                doSeek = true;
                seekTimeTarget = time;
                return true;
            }

            return false;
        }

        public override void Dispose()
        {
            if (targetStream.CanSeek)
                targetStream.Seek(0, SeekOrigin.Begin);

            _isDisposed = true;
            _decoderThread.Join();
        }
    }
}
