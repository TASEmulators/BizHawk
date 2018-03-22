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
using BizHawk.Client.Common;
using BizHawk.Emulation.Common.IEmulatorExtensions;
using System.Windows.Forms;
using System.IO;

namespace BizHawk.Client.EmuHawk
{

	public class Communication
	{

		public class HttpCommunication
		{
			private static HttpClient client = new HttpClient();
			private string PostUrl = "http://localhost:9876/post/";
			private string GetUrl = "http://localhost:9876/index";
			public bool initialized = false;
			private ScreenShot screenShot = new ScreenShot();
			public int timeout = 0;
			public int default_timeout = 500;
			
			public void SetTimeout(int _timeout)
			{
				//timeout = _timeout.TotalMilliseconds;
				if (timeout == 0 && _timeout == 0)
				{
					timeout = default_timeout;
				}
				if (_timeout != 0)
				{
					client.Timeout = new TimeSpan(0, 0, 0, _timeout / 1000, _timeout % 1000);
					timeout = _timeout;
				}
				
			}
			public void SetPostUrl(string url)
			{
				PostUrl = url;
			}
			public void SetGetUrl(string url)
			{
				GetUrl = url;
			}

			public string GetGetUrl()
			{
				return GetUrl;
			}
			public string GetPostUrl()
			{
				return PostUrl;
			}

			public async Task<string> Get(string url)
			{
				client.DefaultRequestHeaders.ConnectionClose = false;
				HttpResponseMessage response = await client.GetAsync(url).ConfigureAwait(false);
				if (response.IsSuccessStatusCode) {
					return await response.Content.ReadAsStringAsync();
				}
				else
				{
					return null;
				}
			}

			public async Task<string> Post(string url, FormUrlEncodedContent content)
			{
				client.DefaultRequestHeaders.ConnectionClose = true;
				HttpResponseMessage response = null;
				try
				{
					response = await client.PostAsync(url, content).ConfigureAwait(false);

				}
				catch (Exception e)
				{
					MessageBox.Show(e.ToString());
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
				Task<String> getResponse = Get(GetUrl);
				return getResponse.Result;
			}

			public string SendScreenshot(string url, string parameter)
			{
				int trials = 5;
				var values = new Dictionary<string, string>
				{
					{parameter, screenShot.GetScreenShotAsString()},
				};
				FormUrlEncodedContent content = new FormUrlEncodedContent(values);

				Task<string> postResponse = null;
				while (postResponse == null && trials > 0) 
				{
					postResponse = Post(PostUrl, content);
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
					{"payload", payload},
				};
				FormUrlEncodedContent content = new FormUrlEncodedContent(values);
				return Post(url, content).Result;
			}

			public string ExecPost(string payload)
			{
				var values = new Dictionary<string, string>
				{
					{"payload", payload},
				};
				FormUrlEncodedContent content = new FormUrlEncodedContent(values);
				return Post(PostUrl, content).Result;
			}
		}
		public class SocketServer
		{

			public string ip = "192.168.178.21";
			public int port = 9999;
			public Decoder decoder = Encoding.UTF8.GetDecoder();
			public Socket soc = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			public IPAddress ipAdd;
			public IPEndPoint remoteEP;
			public IVideoProvider currentVideoProvider = null;
			public bool connected = false;
			public bool initialized = false;
			public int retries = 10;
			public bool success = false; //indicates whether the last command was executed succesfully

			public void Initialize(IVideoProvider _currentVideoProvider)
			{
				currentVideoProvider = _currentVideoProvider;
				SetIp(ip, port);
				initialized = true;
				
			}
			public void Connect()
			{
				if (!initialized)
				{
					Initialize(currentVideoProvider);
				}
				remoteEP = new IPEndPoint(ipAdd, port);
				soc = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				soc.Connect(remoteEP);
				connected = true;
				soc.ReceiveTimeout = 5;
				
			}
			public void SetIp(string ip_)
			{
				ip = ip_;
				ipAdd = System.Net.IPAddress.Parse(ip);
				remoteEP = new IPEndPoint(ipAdd, port);
			}
			public void SetIp(string ip_, int port_)
			{
				ip = ip_;
				port = port_;
				ipAdd = System.Net.IPAddress.Parse(ip);
				remoteEP = new IPEndPoint(ipAdd, port);
				
			}
			public void SetTimeout(int timeout)
			{
				soc.ReceiveTimeout = timeout;
			}
			public void SocketConnected()
			{
				bool part1 = soc.Poll(1000, SelectMode.SelectRead);
				bool part2 = (soc.Available == 0);
				if (part1 && part2)
					connected = false;
				else
					connected = true;
			}
			public int SendString(string SendString)
			{
				
				int sentBytes = SendBytes(Encoding.ASCII.GetBytes(SendString));
				success = sentBytes > 0;
				return sentBytes;
			}
			public int SendBytes(byte[] SendBytes)
			{
				int sentBytes = 0;
				try
				{
					sentBytes = soc.Send(SendBytes);
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
				if (!connected)
				{
					Connect();
				}
				ScreenShot screenShot = new ScreenShot();
				using (BitmapBuffer bb = screenShot.MakeScreenShotImage())
				{
					using (var img = bb.ToSysdrawingBitmap())
					{
						byte[] bmpBytes = screenShot.ImageToByte(img);
						int sentBytes = 0;
						int tries = 0;
						while (sentBytes <= 0 && tries < retries)
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
						success = (tries < retries);
					}
				}
				String resp = "";
				if (!success)
				{
					resp = "Screenshot could not be sent";
				} else
				{
					resp = "Screenshot was sent";
				}
				if (waitingTime == 0)
				{
					return resp;
				}
				resp = "";
				resp = ReceiveMessage();
				if (resp == "")
				{
					resp = "Failed to get a response";
				}
				return resp;
			}
			public string ReceiveMessage()
			{
				if (!connected)
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
						receivedLength = soc.Receive(receivedBytes, receivedBytes.Length, 0);
						resp += Encoding.ASCII.GetString(receivedBytes);
					} catch
					{
						receivedLength = 0;
					}
				}
				
				return resp;
			}
			public bool Successful()
			{
				return success;
			}
		}

		public class MemoryMappedFiles
		{
			public string filename_main = "BizhawkTemp_main";
			public Dictionary<string, MemoryMappedFile> mmf_files = new Dictionary<string, MemoryMappedFile>();
			public int index = 0;
			public bool initialized = false;
			public int main_size = 10 ^ 5;
			ScreenShot screenShot = new ScreenShot();

			public void SetFilename(string filename)
			{
				filename_main = filename;
			}
			public string GetFilename()
			{
				return filename_main;
			}

			public int ScreenShotToFile()
			{
				ScreenShot screenShot = new ScreenShot();
				var bb = screenShot.MakeScreenShotImage();
				var img = bb.ToSysdrawingBitmap();
				byte[] bmpBytes = screenShot.ImageToByte(img);
				return WriteToFile(@filename_main, bmpBytes);
			}

			public int WriteToFile(string filename, byte[] outputBytes)
			{
				MemoryMappedFile mmf_file;
				int bytesWritten = -1;
				if (mmf_files.TryGetValue(filename, out mmf_file) == false)
				{
					mmf_file = MemoryMappedFile.CreateOrOpen(filename, outputBytes.Length);
					mmf_files[filename] = mmf_file;
				}
				try
				{
					using (MemoryMappedViewAccessor accessor = mmf_file.CreateViewAccessor(0, outputBytes.Length, MemoryMappedFileAccess.Write))
					{
						accessor.WriteArray<byte>(0, outputBytes, 0, outputBytes.Length);
						bytesWritten = outputBytes.Length;
					}
				}
				catch (UnauthorizedAccessException)
				{
					try
					{
						mmf_file.Dispose();
					}
					catch (Exception)
					{

					}

					mmf_file = MemoryMappedFile.CreateOrOpen(filename, outputBytes.Length);
					mmf_files[filename] = mmf_file;
					using (MemoryMappedViewAccessor accessor = mmf_file.CreateViewAccessor(0, outputBytes.Length, MemoryMappedFileAccess.Write))
					{
						accessor.WriteArray<byte>(0, outputBytes, 0, outputBytes.Length);
						bytesWritten = outputBytes.Length;
					}
				}
				return bytesWritten;
			}

			public string ReadFromFile(string filename, int expectedSize)
			{
				MemoryMappedFile mmf_file = mmf_file = MemoryMappedFile.OpenExisting(@filename);
				using (MemoryMappedViewAccessor viewAccessor = mmf_file.CreateViewAccessor())
				{
					byte[] bytes = new byte[expectedSize];
					viewAccessor.ReadArray(0, bytes, 0, bytes.Length);
					string text = Encoding.UTF8.GetString(bytes);
					return text;
				}
			}

		}

		class ScreenShot
		//makes all functionalities for providing screenshots available
		{
			private IVideoProvider currentVideoProvider = null;
			private ImageConverter converter = new ImageConverter();
			public BitmapBuffer MakeScreenShotImage()
			{
				if (currentVideoProvider == null)
				{
					currentVideoProvider = Global.Emulator.AsVideoProviderOrDefault();
				}
				return GlobalWin.DisplayManager.RenderVideoProvider(currentVideoProvider);
			}
			public byte[] ImageToByte(Image img)
			{
				return (byte[])converter.ConvertTo(img, typeof(byte[]));
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

