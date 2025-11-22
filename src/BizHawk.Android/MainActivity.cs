using Android.App;
using Android.OS;
using Android.Widget;

namespace BizHawkRafaelia.Android
{
	[Activity(Label = "@string/app_name", MainLauncher = true)]
	public class MainActivity : Activity
	{
		protected override void OnCreate(Bundle? savedInstanceState)
		{
			base.OnCreate(savedInstanceState);
			
			// Create a simple UI for now
			var layout = new LinearLayout(this)
			{
				Orientation = Orientation.Vertical
			};
			
			var textView = new TextView(this)
			{
				Text = "BizHawk Rafaelia\nOptimized ARM64 Build\n\nPerformance Framework Active",
				TextSize = 24,
				Gravity = Android.Views.GravityFlags.Center
			};
			
			layout.AddView(textView);
			SetContentView(layout);
		}
	}
}
