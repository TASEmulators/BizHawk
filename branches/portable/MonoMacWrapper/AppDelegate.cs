using System;
using MonoMac.CoreFoundation;
using MonoMac.Foundation;
using MonoMac.AppKit;
using BizHawk.MultiClient;
using System.Windows.Forms;

namespace MonoMacWrapper
{
	[MonoMac.Foundation.Register("AppDelegate")]
	public class AppDelegate : NSApplicationDelegate
	{
		private System.Threading.Thread _uiRunLoop;
		private MainForm _mainWinForm;
		public AppDelegate(){}

		public override void FinishedLaunching(NSObject notification)
		{
			NSApplication.SharedApplication.BeginInvokeOnMainThread(()=>
            {
				StartApplication();
			});
		}
		
		private void StartApplication()
		{
			BizHawk.HawkUIFactory.OpenDialogClass = typeof(MacOpenFileDialog);
			Global.Config = ConfigService.Load<Config>(PathManager.DefaultIniPath, new Config ());
			try
			{
				_mainWinForm = new BizHawk.MultiClient.MainForm(new string[0]);
				var title = _mainWinForm.Text;
				_mainWinForm.Show();
				DoMenuExtraction();
				_mainWinForm.MainMenuStrip.Visible = false; //Hide the real one, since it's been extracted
				_mainWinForm.Text = title;
			}
			catch (Exception e) 
			{
				NSAlert nsa = new NSAlert();
				nsa.MessageText = e.ToString();
				nsa.RunModal();
			}
			_uiRunLoop = new System.Threading.Thread(KeepThingsGoing);
			_uiRunLoop.Start();
		}
		
		private void KeepThingsGoing()
		{
			while(true)
			{
				NSApplication.SharedApplication.InvokeOnMainThread(()=>
				{
					if(_mainWinForm.Visible && !_mainWinForm.RunLoopBlocked)
						Application.DoEvents();
				});
				System.Threading.Thread.Sleep(0);
			}
		}
				
		private void DoMenuExtraction()
		{
			ExtractMenus(_mainWinForm.MainMenuStrip);
		}
		
		private void ExtractMenus(System.Windows.Forms.MenuStrip menus)
		{
			for(int i=0; i<menus.Items.Count; i++)
			{
				ToolStripMenuItem item = menus.Items[i] as ToolStripMenuItem;
				NSMenuItem menuOption = new NSMenuItem(CleanMenuString(item.Text));
				NSMenu dropDown = new NSMenu(CleanMenuString(item.Text));
				menuOption.Submenu = dropDown;
				NSApplication.SharedApplication.MainMenu.AddItem(menuOption);
				menuOption.Hidden = !item.Visible;
				menuOption.Enabled = item.Enabled;
				ExtractSubmenu(item.DropDownItems, dropDown, i==0); //Skip last 2 options in first menu, redundant exit option
			}
		}
		
		private void ExtractSubmenu(ToolStripItemCollection subItems, NSMenu destMenu, bool fileMenu)
		{
			int max = subItems.Count;
			if(fileMenu) max-=2;
			for(int i=0; i<max; i++)
			{
				ToolStripItem item = subItems[i];
				if(item is ToolStripMenuItem)
				{
					ToolStripMenuItem menuItem = (ToolStripMenuItem)item;
					MenuItemAdapter translated = new MenuItemAdapter(menuItem);
					translated.Action = new MonoMac.ObjCRuntime.Selector("HandleMenu");
					destMenu.AddItem(translated);
					if(menuItem.DropDownItems.Count > 0)
					{
						NSMenu dropDown = new NSMenu(CleanMenuString(item.Text));
						translated.Submenu = dropDown;
						ExtractSubmenu(menuItem.DropDownItems, dropDown, false);
					}
				}
				else if(item is ToolStripSeparator)
				{
					destMenu.AddItem(NSMenuItem.SeparatorItem);
				}
			}
		}
		
		private static string CleanMenuString(string text)
		{
			return text.Replace("&",string.Empty);
		}
		
		private class MenuItemAdapter : NSMenuItem
		{
			public MenuItemAdapter(ToolStripMenuItem host) : base(CleanMenuString(host.Text)) 
			{
				HostMenu = host;
			}
			public ToolStripMenuItem HostMenu { get;set; }
		}
		
		[Export("HandleMenu")]
		private void HandleMenu(MenuItemAdapter item)
		{
			item.HostMenu.PerformClick();
		}
	}
}