namespace BizHawk.Client.Common
{
	public interface IComm : IExternalApi
	{
		Communication.HttpCommunication HTTP { get; }

		Communication.MemoryMappedFiles MemoryMappedFiles { get; }

		Communication.SocketServer SocketServer { get; }
	}
}
