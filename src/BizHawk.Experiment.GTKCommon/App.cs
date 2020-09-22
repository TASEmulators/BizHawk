using Xamarin.Forms;

namespace BizHawk.Experiment.GTKClient
{
	public sealed class App : Application
	{
		public App()
		{
			MainPage = new ContentPage
			{
				Content = new Label
				{
					HorizontalOptions = LayoutOptions.CenterAndExpand,
					Text = "Hello, world!",
					VerticalOptions = LayoutOptions.CenterAndExpand
				}
			};
		}
	}
}
