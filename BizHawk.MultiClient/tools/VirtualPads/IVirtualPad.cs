namespace BizHawk.Client.EmuHawk
{
	public interface IVirtualPad
	{
		string GetMnemonic();
		void Clear();
		void SetButtons(string buttons);
	}
}
