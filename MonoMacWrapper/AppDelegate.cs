using System;
using MonoMac.CoreFoundation;
using MonoMac.Foundation;
using MonoMac.AppKit;
using BizHawk.Client.EmuHawk;
using System.Windows.Forms;
using System.Reflection;
using BizHawk.Common;
using BizHawk.Client.Common;

namespace MonoMacWrapper
{
	[MonoMac.Foundation.Register("AppDelegate")]
	public class AppDelegate : NSApplicationDelegate
	{
		private System.Collections.Generic.Dictionary<ToolStripMenuItem, MenuItemAdapter> _menuLookup;
		private NSTimer _masterTimer;
		private MainForm _mainWinForm;
		private Action _queuedAction;
		public AppDelegate(){}

		public override void FinishedLaunching(NSObject notification)
		{
			NSApplication.SharedApplication.BeginInvokeOnMainThread(()=>
			{
				StartApplication();
			});
		}

		public override void WillTerminate (NSNotification notification)
		{
			//Doesn't seem to be called anymore, so I override the quit option myself.
			_mainWinForm.Close();
		}

		public override void DidResignActive (NSNotification notification)
		{
			GlobalWin.IsApplicationActive = false;
			//Note: These events that are supposed to notify us when entering or leaving the background sometimes
			//don't fire because the run loop is hogging most of the time on the main thread which is supposed to fire them.
			//Unfortunately, the value I could pull it from manually, NSApplication.SharedApplication.Active, is not updated either.
			//So this works about 80% of the time, but sometimes it does not. Work-around is to click away and click back again.
		}

		public override void DidBecomeActive (NSNotification notification)
		{
			GlobalWin.IsApplicationActive = true;
		}

		private void StartApplication()
		{
			NSUrl[] urls = NSFileManager.DefaultManager.GetUrls(NSSearchPathDirectory.ApplicationSupportDirectory, NSSearchPathDomain.User);
			if (urls.Length > 0) 
			{
				string bizhawkSupport = System.IO.Path.Combine(urls[0].Path, "BizHawk/");
				if(!System.IO.Directory.Exists(bizhawkSupport)) 
				{
					System.IO.Directory.CreateDirectory(bizhawkSupport);
				}
				string iniPath = System.IO.Path.Combine(bizhawkSupport, "config.ini");
				BizHawk.Client.Common.PathManager.DefaultIniPath = iniPath;
			}

			BizHawk.Client.EmuHawk.HawkDialogFactory.OpenDialogClass = typeof(MacOpenFileDialog);
			BizHawk.Client.EmuHawk.HawkDialogFactory.SaveDialogClass = typeof(MacSaveFileDialog);
			BizHawk.Client.EmuHawk.HawkDialogFactory.FolderBrowserClass = typeof(MacFolderBrowserDialog);
			Global.Config = ConfigService.Load<Config>(PathManager.DefaultIniPath);
			GlobalWin.IGL_GL = new BizHawk.Bizware.BizwareGL.Drivers.OpenTK.IGL_TK(2,0,false);

			//setup the GL context manager, needed for coping with multiple opengl cores vs opengl display method
			GLManager.CreateInstance(GlobalWin.IGL_GL);
			GlobalWin.GLManager = GLManager.Instance;
			GlobalWin.GL = GlobalWin.IGL_GL;

			BizHawk.Common.HawkFile.ArchiveHandlerFactory = new SevenZipSharpArchiveHandler();
			try
			{
				_mainWinForm = new BizHawk.Client.EmuHawk.MainForm(new string[0]);
				var title = _mainWinForm.Text;
				_mainWinForm.Show();
				DoMenuExtraction();
				var appMenuItems = NSApplication.SharedApplication.MainMenu.ItemAt(0).Submenu.ItemArray();
				appMenuItems[appMenuItems.Length-1].Action = new MonoMac.ObjCRuntime.Selector("OnAppQuit");

				_mainWinForm.MainMenuStrip.Visible = false; //Hide the real one, since it's been extracted
				_mainWinForm.Text = title;
				//Timer assumes 60hz display. Note that timers are not very accurate, and I should really be doing this a different way.
				//Can't use CVDisplayLink, because that just notifies us of when we need to push video to the display.
				//Both macOS and WinForms need to share the run loop, and this solution produces the most responsive compromise allowing
				//them both to coexist. There seem to be frames occasionally dropped that the WinForms UI doesn't know about because
				//I can set Frameskip to 0 with VSync enabled and the game execution is still full speed.
				_masterTimer = NSTimer.CreateRepeatingTimer(1.0/60.0, MacRunLoop);
				NSRunLoop.Current.AddTimer(_masterTimer, NSRunLoopMode.Common);
			}
			catch (Exception e) 
			{
				NSAlert nsa = new NSAlert();
				nsa.MessageText = e.ToString();
				nsa.RunModal();
			}
		}

		private void MacRunLoop(){
			bool runLoopVal = true;
			for (int i = 0; i < 1; i++)
			{
				runLoopVal &= _mainWinForm.RunLoopCore();
				if(!runLoopVal) break;
			}
			if (runLoopVal) {
				if (_queuedAction != null) {
					_queuedAction.Invoke (); //Needs to happen in the same context as the RunLoop, otherwise we'll get weird behavior.
					_queuedAction = null;
					RefreshAllMenus();
				}
			} else {
				_masterTimer.Invalidate();
				NSApplication.SharedApplication.Terminate(this);
			}
		}
				
		private void DoMenuExtraction()
		{
			_menuLookup = new System.Collections.Generic.Dictionary<ToolStripMenuItem, MenuItemAdapter>();
			ExtractMenus(_mainWinForm.MainMenuStrip);
		}
		
		private void ExtractMenus(System.Windows.Forms.MenuStrip menus)
		{
			for(int i=0; i<menus.Items.Count; i++)
			{
				ToolStripMenuItem item = menus.Items[i] as ToolStripMenuItem;
				MenuItemAdapter menuOption = new MenuItemAdapter(item);
				NSMenu dropDown = new NSMenu(CleanMenuString(item.Text));
				menuOption.Submenu = dropDown;
				NSApplication.SharedApplication.MainMenu.AddItem(menuOption);
				_menuLookup.Add(item, menuOption);
				menuOption.Hidden = !item.Visible;
				item.VisibleChanged += HandleItemVisibleChanged;
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
					menuItem.CheckedChanged += HandleMenuItemCheckedChanged;
					menuItem.EnabledChanged += HandleMenuItemEnabledChanged;
					translated.Action = new MonoMac.ObjCRuntime.Selector("HandleMenu:");
					translated.State = menuItem.Checked ? NSCellStateValue.On : NSCellStateValue.Off;
					if(menuItem.Image != null) translated.Image = ImageToCocoa(menuItem.Image);
					destMenu.AddItem(translated);
					_menuLookup.Add(menuItem, translated);
					if(menuItem.DropDownItems.Count > 0)
					{
						NSMenu dropDown = new NSMenu(CleanMenuString(item.Text));
						translated.Submenu = dropDown;
						ExecuteDropDownOpened(menuItem);
						ExtractSubmenu(menuItem.DropDownItems, dropDown, false);
					}
				}
				else if(item is ToolStripSeparator)
				{
					destMenu.AddItem(NSMenuItem.SeparatorItem);
				}
			}
		}
		
		private void ExecuteDropDownOpened(ToolStripMenuItem item)
		{
			var dropDownOpeningKey = typeof(ToolStripDropDownItem).GetField("DropDownOpenedEvent", BindingFlags.Static | BindingFlags.NonPublic);
			var eventProp = typeof(ToolStripDropDownItem).GetProperty("Events", BindingFlags.Instance | BindingFlags.NonPublic);
			if (eventProp != null && dropDownOpeningKey != null)
			{
				var dropDownOpeningValue = dropDownOpeningKey.GetValue(item);
				var eventList = eventProp.GetValue(item, null) as System.ComponentModel.EventHandlerList;
				if(eventList != null)
				{
					Delegate ddd = eventList[dropDownOpeningValue];
					try{
						if(ddd!=null) ddd.DynamicInvoke(null, EventArgs.Empty);
					}
					catch(Exception ex){
						//throw ex;
					}
				}
	        }
		}
		
		private void HandleItemVisibleChanged(object sender, EventArgs e)
		{
			if(sender is ToolStripMenuItem && _menuLookup.ContainsKey((ToolStripMenuItem)sender))
			{
				MenuItemAdapter translated = _menuLookup[(ToolStripMenuItem)sender];
				translated.Hidden = !translated.Hidden; 
				//Can't actually look at Visible property because the entire menubar is hidden.
				//Since the event only gets called when Visible is changed, we can assume it got flipped.
				if(((ToolStripMenuItem)sender).Text.Equals("&NES")){
					//Hack to rebuild menu contents due to changing FDS sub-menu.
					//At some point, I might want to figure out a better way to do this.
					RemoveMenuItems(translated);
					ExtractSubmenu(translated.HostMenu.DropDownItems, translated.Submenu, false);
				}
			}
		}

		private void RemoveMenuItems(MenuItemAdapter menu)
		{
			if(menu.HasSubmenu)
			{
				for(int i=menu.Submenu.Count-1; i>=0; i--)
				{
					MenuItemAdapter item = menu.Submenu.ItemAt(i) as MenuItemAdapter;
					if(item != null) //It will be null if it's a separator
					{
						RemoveMenuItems(item);
						if(_menuLookup.ContainsKey(item.HostMenu))
						{
							_menuLookup.Remove(item.HostMenu);
						}
						item.HostMenu.CheckedChanged -= HandleMenuItemCheckedChanged;
						item.HostMenu.EnabledChanged -= HandleMenuItemEnabledChanged;
					}
					menu.Submenu.RemoveItemAt(i);
				}
			}
		}

		private void HandleMenuItemEnabledChanged(object sender, EventArgs e)
		{
			if(sender is ToolStripMenuItem && _menuLookup.ContainsKey((ToolStripMenuItem)sender))
			{
				MenuItemAdapter translated = _menuLookup[(ToolStripMenuItem)sender];
				translated.Enabled = translated.HostMenu.Enabled;
			}
		}

		private void HandleMenuItemCheckedChanged(object sender, EventArgs e)
		{
			if(sender is ToolStripMenuItem && _menuLookup.ContainsKey((ToolStripMenuItem)sender))
			{
				MenuItemAdapter translated = _menuLookup[(ToolStripMenuItem)sender];
				translated.State = translated.HostMenu.Checked ? NSCellStateValue.On : NSCellStateValue.Off;
			}
		}

		private void RefreshAllMenus(){
			
			for (int i = 0; i < _mainWinForm.MainMenuStrip.Items.Count; i++)
			{
				ToolStripMenuItem item = _mainWinForm.MainMenuStrip.Items[i] as ToolStripMenuItem;
				MenuItemAdapter mia = _menuLookup[item];
				if (mia != null)
				{
					RemoveMenuItems(mia);
					ExtractSubmenu(mia.HostMenu.DropDownItems, mia.Submenu, i==0);
				}
			}
		}
		
		private static NSImage ImageToCocoa(System.Drawing.Image input)
		{
			System.IO.MemoryStream ms = new System.IO.MemoryStream();
			input.Save(ms,System.Drawing.Imaging.ImageFormat.Png);
			ms.Position = 0;
			NSImage img = NSImage.FromStream(ms);
			img.Size = new System.Drawing.SizeF(16f, 16f); //Some of BizHawk's menu icons are larger, even though WinForms only does 16x16.
			return img;
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
		
		[Export("HandleMenu:")]
		private void HandleMenu(MenuItemAdapter item)
		{
			_queuedAction = new Action(item.HostMenu.PerformClick);
		}

		[Export("OnAppQuit")]
		private void OnQuit ()
		{
			_mainWinForm.Close();
		}
	}
}