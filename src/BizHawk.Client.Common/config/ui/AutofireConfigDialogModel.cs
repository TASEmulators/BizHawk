#nullable enable

namespace BizHawk.Client.Common
{
	public sealed class AutofireConfigDialogModel
	{
		private readonly IAutofireConfigDialogViewAdapter _a;

		private readonly AutofireController _afController;

		private readonly AutoFireStickyXorAdapter _afStickyXORAdapter;

		private readonly Config _config;

		public AutofireConfigDialogModel(
			IAutofireConfigDialogViewAdapter adapter,
			AutofireController afController,
			AutoFireStickyXorAdapter afStickyXORAdapter,
			Config config)
		{
			_a = adapter;
			_afController = afController;
			_afStickyXORAdapter = afStickyXORAdapter;
			_config = config;
		}

		public void BeforeClose(bool shouldSave)
		{
			if (!shouldSave) return;
			_afController.On = _config.AutofireOn = _a.PatternOn;
			_afController.Off = _config.AutofireOff = _a.PatternOff;
			_config.AutofireLagFrames = _a.ConsiderLag;
			_afStickyXORAdapter.SetOnOffPatternFromConfig(_config.AutofireOn, _config.AutofireOff);
		}

		public void BeforeShow()
		{
			_a.ConsiderLag = _config.AutofireLagFrames;
			const int MIN = 1;
			const int MAX = 512;
			_a.PatternOff = _config.AutofireOff switch
			{
				< MIN => MIN,
				> MAX => MAX,
				_ => _config.AutofireOff
			};
			_a.PatternOn = _config.AutofireOn switch
			{
				< MIN => MIN,
				> MAX => MAX,
				_ => _config.AutofireOn
			};
		}
	}
}
