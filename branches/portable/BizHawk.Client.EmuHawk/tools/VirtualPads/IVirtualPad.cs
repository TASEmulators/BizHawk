namespace BizHawk.Client.EmuHawk
{
	public interface IVirtualPad
	{
		string Controller { get; set; }
		string GetMnemonic();
		void Clear();
		void SetButtons(string buttons);
	}
}
