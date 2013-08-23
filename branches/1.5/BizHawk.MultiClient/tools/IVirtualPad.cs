namespace BizHawk.MultiClient
{
	public interface IVirtualPad
	{
		string GetMnemonic();
		void Clear();
		void SetButtons(string buttons);
	}
}
