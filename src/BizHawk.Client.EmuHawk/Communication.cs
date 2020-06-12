using System;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.IO.MemoryMappedFiles;
using BizHawk.Bizware.BizwareGL;
using System.Drawing;
using BizHawk.Emulation.Common;

namespace BizHawk.Client.EmuHawk
{
	public class Communication
	{
		public class HttpCommunication
		{
			private static readonly HttpClient Client = new HttpClient();
			public string PostUrl { get; set; } = null;
			public string GetUrl { get; set; } = null;
			private readonly ScreenShot _screenShot = new ScreenShot();
			public int Timeout { get; set; }
			public int DefaultTimeout { get; set; } = 500;
			
			public void SetTimeout(int timeout)
			{
				if (Timeout == 0 && timeout == 0)
				{
					Timeout = DefaultTimeout;
				}

				if (timeout != 0)
				{
					Client.Timeout = new TimeSpan(0, 0, 0, timeout / 1000, timeout % 1000);
					Timeout = timeout;
				}	
			}

			public async Task<string> Get(string url)
			{
				Client.DefaultRequestHeaders.ConnectionClose = false;
				HttpResponseMessage response = await Client.GetAsync(url).ConfigureAwait(false);
				if (response.IsSuccessStatusCode)
				{
					return await response.Content.ReadAsStringAsync();
				}

				return null;
			}

			public async Task<string> Post(string url, FormUrlEncodedContent content)
			{
				Client.DefaultRequestHeaders.ConnectionClose = true;
				HttpResponseMessage response;
				try
				{
					response = await Client.PostAsync(url, content).ConfigureAwait(false);
				}
				catch (Exception e)
				{
					return e.ToString();
					
				}
				if (!response.IsSuccessStatusCode)
				{
					return null;
				}
				return await response.Content.ReadAsStringAsync();
			}

			public string TestGet()
			{
				Task<string> getResponse = Get(GetUrl);
				return getResponse.Result;
			}

			private string SendScreenshot(string url, string parameter)
			{
				int trials = 5;
				var values = new Dictionary<string, string>
				{
					{ parameter, _screenShot.GetScreenShotAsString() }
				};
				FormUrlEncodedContent content = new FormUrlEncodedContent(values);

				Task<string> postResponse = null;
				while (postResponse == null && trials > 0) 
				{
					postResponse = Post(url, content);
					trials -= 1;
				}
				return postResponse.Result;
			}

			public string SendScreenshot()
			{
				return SendScreenshot(PostUrl, "screenshot");
			}

			public string SendScreenshot(string url)
			{
				return SendScreenshot(url, "screenshot");
			}

			public string ExecGet(string url)
			{
				return Get(url).Result;
			}

			public string ExecGet()
			{
				return Get(GetUrl).Result;
			}

			public string ExecPost(string url, string payload)
			{
				var values = new Dictionary<string, string>
				{
					["payload"] = payload
				};
				FormUrlEncodedContent content = new FormUrlEncodedContent(values);
				return Post(url, content).Result;
			}

			public string ExecPost(string payload)
			{
				var values = new Dictionary<string, string>
				{
					["payload"] = payload
				};
				FormUrlEncodedContent content = new FormUrlEncodedContent(values);
				return Post(PostUrl, content).Result;
			}
		}

		public class SocketServer
		{
			string _ip;
			public string Ip
			{
				get => _ip;
				set
				{
					_ip = value;
					_ipAdd = IPAddress.Parse(_ip);
					Connect();
				}
			}

			int _port;
			public int Port
			{
				get => _port;
				set
				{
					_port = value;
					Connect();
				}
			}

			Socket _soc = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			IPAddress _ipAdd;
			IPEndPoint _remoteEp;
			IVideoProvider _currentVideoProvider;
			public bool Connected { get; set; }
			public bool Initialized { get; set; }
			public int Retries { get; set; } = 10;
			private bool _success; // indicates whether the last command was executed successfully

			public void Initialize()
			{
				if (_currentVideoProvider == null)
				{
					_currentVideoProvider = GlobalWin.Emulator.AsVideoProviderOrDefault();
				}

				Initialized = true;
			}

			public void Connect()
			{
				_remoteEp = new IPEndPoint(_ipAdd, _port);
				_soc = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				_soc.Connect(_remoteEp);
				Connected = true;
				_soc.ReceiveTimeout = 5;
			}

			public void SetIp(string ip, int port)
			{
				_ip = ip;
				_port = port;
				_ipAdd = IPAddress.Parse(_ip);
				Connect();
			}

			public string GetInfo()
			{
				return $"{_ip}:{_port}";
			}

			public void SetTimeout(int timeout)
			{
				_soc.ReceiveTimeout = timeout;
			}

			public void SocketConnected()
			{
				bool part1 = _soc.Poll(1000, SelectMode.SelectRead);
				bool part2 = (_soc.Available == 0);
				Connected = !(part1 && part2);
			}

			public int SendString(string sendString)
			{
				int sentBytes = SendBytes(Encoding.ASCII.GetBytes(sendString));
				_success = sentBytes > 0;
				return sentBytes;
			}

			public int SendBytes(byte[] sendBytes)
			{
				int sentBytes;
				try
				{
					sentBytes = _soc.Send(sendBytes);
				}
				catch
				{
					sentBytes = -1;
				}

				return sentBytes;
			}
			
			public string SendScreenshot()
			{
				return SendScreenshot(0);
			}

			public string SendScreenshot(int waitingTime)
			{
				if (!Initialized)
				{
					Initialize();
				}

				var screenShot = new ScreenShot();
				using (BitmapBuffer bb = screenShot.MakeScreenShotImage())
				{
					using var img = bb.ToSysdrawingBitmap();
					byte[] bmpBytes = screenShot.ImageToByte(img);
					int sentBytes = 0;
					int tries = 0;
					while (sentBytes <= 0 && tries < Retries)
					{
						try
						{
							tries++;
							sentBytes = SendBytes(bmpBytes);
						}
						catch (SocketException)
						{
							Connect();
							sentBytes = 0;
						}
						if (sentBytes == -1)
						{
							Connect();
						}
					}

					_success = tries < Retries;
				}

				var resp = !_success
					? "Screenshot could not be sent"
					: "Screenshot was sent";

				if (waitingTime == 0)
				{
					return resp;
				}

				resp = ReceiveMessage();
				if (resp == "")
				{
					resp = "Failed to get a response";
				}

				return resp;
			}

			public string ReceiveMessage()
			{
				if (!Connected)
				{
					Connect();
				}

				string resp = "";
				byte[] receivedBytes = new byte[256];
				int receivedLength = 1;

				while (receivedLength > 0)
				{
					try
					{
						receivedLength = _soc.Receive(receivedBytes, receivedBytes.Length, 0);
						resp += Encoding.ASCII.GetString(receivedBytes);
					}
					catch
					{
						receivedLength = 0;
					}
				}
				return resp;
			}

			public bool Successful()
			{
				return _success;
			}
		}

		public class MemoryMappedFiles
		{
			private readonly Dictionary<string, MemoryMappedFile> _mmfFiles = new Dictionary<string, MemoryMappedFile>();

			public string Filename { get; set; } = "BizhawkTemp_main";

			public int ScreenShotToFile()
			{
				ScreenShot screenShot = new ScreenShot();
				var bb = screenShot.MakeScreenShotImage();
				var img = bb.ToSysdrawingBitmap();
				byte[] bmpBytes = screenShot.ImageToByte(img);
				return WriteToFile(@Filename, bmpBytes);
			}

			public int WriteToFile(string filename, byte[] outputBytes)
			{
				int bytesWritten = -1;
				if (_mmfFiles.TryGetValue(filename, out var mmfFile) == false)
				{
					mmfFile = MemoryMappedFile.CreateOrOpen(filename, outputBytes.Length);
					_mmfFiles[filename] = mmfFile;
				}
				try
				{
					using MemoryMappedViewAccessor accessor = mmfFile.CreateViewAccessor(0, outputBytes.Length, MemoryMappedFileAccess.Write);
					accessor.WriteArray<byte>(0, outputBytes, 0, outputBytes.Length);
					bytesWritten = outputBytes.Length;
				}
				catch (UnauthorizedAccessException)
				{
					try
					{
						mmfFile.Dispose();
					}
					catch (Exception)
					{
					}

					mmfFile = MemoryMappedFile.CreateOrOpen(filename, outputBytes.Length);
					_mmfFiles[filename] = mmfFile;
					using MemoryMappedViewAccessor accessor = mmfFile.CreateViewAccessor(0, outputBytes.Length, MemoryMappedFileAccess.Write);
					accessor.WriteArray(0, outputBytes, 0, outputBytes.Length);
					bytesWritten = outputBytes.Length;
				}
				return bytesWritten;
			}

			public string ReadFromFile(string filename, int expectedSize)
			{
				MemoryMappedFile mmfFile = MemoryMappedFile.OpenExisting(filename);
				using MemoryMappedViewAccessor viewAccessor = mmfFile.CreateViewAccessor();
				byte[] bytes = new byte[expectedSize];
				viewAccessor.ReadArray(0, bytes, 0, bytes.Length);
				string text = Encoding.UTF8.GetString(bytes);
				return text;
			}

		}

		// makes all functionality for providing screenshots available
		class ScreenShot
		{
			private IVideoProvider _currentVideoProvider;
			private readonly ImageConverter _converter = new ImageConverter();

			public BitmapBuffer MakeScreenShotImage()
			{
				if (_currentVideoProvider == null)
				{
					_currentVideoProvider = GlobalWin.Emulator.AsVideoProviderOrDefault();
				}
				return GlobalWin.DisplayManager.RenderVideoProvider(_currentVideoProvider);
			}

			public byte[] ImageToByte(Image img)
			{
				return (byte[])_converter.ConvertTo(img, typeof(byte[]));
			}

			public string ImageToString(Image img)
			{
				return Convert.ToBase64String(ImageToByte(img));
			}

			public string GetScreenShotAsString()
			{
				BitmapBuffer bb = MakeScreenShotImage();
				byte[] imgBytes = ImageToByte(bb.ToSysdrawingBitmap());
				return Convert.ToBase64String(imgBytes);
			}
		}
	}
}


