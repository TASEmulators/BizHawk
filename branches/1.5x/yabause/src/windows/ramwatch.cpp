/*  Copyright 2009 Upthorn, Nitsuja, adelikat

    This file is part of Yabause.

    Yabause is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    Yabause is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with Yabause; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301  USA
*/

//RamWatch dialog was copied and adapted from GENS11: http://code.google.com/p/gens-rerecording/
//Authors: Upthorn, Nitsuja, adelikat

#include "resource.h"
#include "ramwatch.h"
#include "ram_search.h"



extern "C" {
#include "../cs2.h"
#include "../memory.h"
#include "./settings/settings.h"
#include "./cpudebug/yuidebug.h"
#include <ctype.h>
}
#include "windows.h"
#include "commctrl.h"

extern "C" {
extern HWND YabWin;
extern HINSTANCE y_hInstance;
}

HWND RamWatchHWnd = NULL;

static char Str_Tmp[1024];
char Rom_Name[64] = "test"; //TODO

static HMENU ramwatchmenu;
static HMENU rwrecentmenu;
static HACCEL RamWatchAccels = NULL;
char rw_recent_files[MAX_RECENT_WATCHES][1024];
char Watch_Dir[1024]="";
const unsigned int RW_MENU_FIRST_RECENT_FILE = 600;
bool RWfileChanged = false;		//Keeps track of whether the current watch file has been changed, if so, ramwatch will prompt to save changes
bool AutoRWLoad = false;			//Keeps track of whether Auto-load is checked
bool RWSaveWindowPos = false;	//Keeps track of whether Save Window position is checked
char currentWatch[1024];
int ramw_x, ramw_y;			//Used to store ramwatch dialog window positions
struct AddressWatcher rswatches[MAX_WATCH_COUNT];
int WatchCount=0;

#define MESSAGEBOXPARENT (RamWatchHWnd ? RamWatchHWnd : YabWin)

int QuickSaveWatches();
int ResetWatches();

unsigned int GetCurrentValue(struct AddressWatcher *watch)
{
	switch (watch->Size)
	{
	case 0x62: return MappedMemoryReadByte(watch->Address);
	case 0x77: return MappedMemoryReadWord(watch->Address);
	case 0x64: return MappedMemoryReadLong(watch->Address);
	}
	return 0;
	//	return ReadValueAtHardwareAddress(watch.Address, watch.Size == 'd' ? 4 : watch.Size == 'w' ? 2 : 1);
}

int IsSameWatch(const struct AddressWatcher *l, const struct AddressWatcher *r)
{
	return ((l->Address == r->Address) && (l->Size == r->Size) && (l->Type == r->Type)/* && (l.WrongEndian == r.WrongEndian)*/);
}

int VerifyWatchNotAlreadyAdded(const struct AddressWatcher *watch)
{
	int j;
	for (j = 0; j < WatchCount; j++)
	{
		if (IsSameWatch(&rswatches[j], watch))
		{
			if(RamWatchHWnd)
				SetForegroundWindow(RamWatchHWnd);
			return 0;
		}
	}
	return 1;
}




int InsertWatch(const struct AddressWatcher *Watch, char *Comment)
{
	int i;
	struct AddressWatcher *NewWatch;

	if(!VerifyWatchNotAlreadyAdded(Watch))
		return 0;

	if(WatchCount >= MAX_WATCH_COUNT)
		return 0;

	i = WatchCount++;

	NewWatch = &rswatches[i];
	//	NewWatch = Watch;
//	if (NewWatch->comment) free(NewWatch->comment);
	NewWatch->comment = (char *) malloc(strlen(Comment)+2);
	NewWatch->CurValue = GetCurrentValue((AddressWatcher*)Watch);;//Watch->CurValue;//GetCurrentValue(NewWatch);
	NewWatch->Address = Watch->Address;
	NewWatch->Size = Watch->Size;
	NewWatch->Type = Watch->Type;

	strcpy(NewWatch->comment, Comment);
	ListView_SetItemCount(GetDlgItem(RamWatchHWnd,IDC_WATCHLIST),WatchCount);
	RWfileChanged=1;

	/*
	NewWatch = rswatches[i];
	NewWatch = Watch;
	//if (NewWatch.comment) free(NewWatch.comment);
	NewWatch->comment = (char *) malloc(strlen(Comment)+2);
	NewWatch->CurValue = GetCurrentValue(NewWatch);
	strcpy(NewWatch->comment, Comment);
	ListView_SetItemCount(GetDlgItem(RamWatchHWnd,IDC_WATCHLIST),WatchCount);
	RWfileChanged=1;
	*/
	return 1;
}

LRESULT CALLBACK PromptWatchNameProc(HWND hDlg, UINT uMsg, WPARAM wParam, LPARAM lParam) //Gets the description of a watched address
{
	RECT r;
	RECT r2;
	int dx1, dy1, dx2, dy2;

	switch(uMsg)
	{
	case WM_INITDIALOG:

		GetWindowRect(YabWin, &r);
		dx1 = (r.right - r.left) / 2;
		dy1 = (r.bottom - r.top) / 2;

		GetWindowRect(hDlg, &r2);
		dx2 = (r2.right - r2.left) / 2;
		dy2 = (r2.bottom - r2.top) / 2;

		//SetWindowPos(hDlg, NULL, max(0, r.left + (dx1 - dx2)), max(0, r.top + (dy1 - dy2)), NULL, NULL, SWP_NOSIZE | SWP_NOZORDER | SWP_SHOWWINDOW);
		SetWindowPos(hDlg, NULL, r.left, r.top, NULL, NULL, SWP_NOSIZE | SWP_NOZORDER | SWP_SHOWWINDOW);
		strcpy(Str_Tmp,"Enter a name for this RAM address.");
		SendDlgItemMessage(hDlg,IDC_PROMPT_TEXT,WM_SETTEXT,0,(LPARAM)_16(Str_Tmp));
		strcpy(Str_Tmp,"");
		SendDlgItemMessage(hDlg,IDC_PROMPT_TEXT2,WM_SETTEXT,0,(LPARAM)_16(Str_Tmp));
		return 1;
		break;

	case WM_COMMAND:
		switch(LOWORD(wParam))
		{
		case IDOK:
			{
				GetDlgItemTextA(hDlg,IDC_PROMPT_EDIT,Str_Tmp,80);
				InsertWatch(&rswatches[WatchCount],Str_Tmp);
				EndDialog(hDlg, 1);
				return 1;
				break;
			}
		case ID_CANCEL:
			EndDialog(hDlg, 0);
			return 0;
			break;
		}
		break;

	case WM_CLOSE:
		EndDialog(hDlg, 0);
		return 0;
		break;
	}

	return 0;
}

int InsertWatchHwnd(const struct AddressWatcher *Watch, HWND parent)
{
	int prevWatchCount;

	if(!VerifyWatchNotAlreadyAdded(Watch))
		return 0;

	if(!parent)
		parent = RamWatchHWnd;
	if(!parent)
		parent = YabWin;

	prevWatchCount = WatchCount;

	rswatches[WatchCount] = *Watch;
	rswatches[WatchCount].CurValue = GetCurrentValue(&rswatches[WatchCount]);
	DialogBox(y_hInstance, MAKEINTRESOURCE(IDD_PROMPT), parent, (DLGPROC) PromptWatchNameProc);

	return WatchCount > prevWatchCount;
}

extern "C" void Update_RAM_Watch()
{
	if (!RamWatchHWnd) return;
	
	HWND lv;
	int top;
	int bottom;
	int start;
	int i;
	// update cached values and detect changes to displayed listview items
	int watchChanged[MAX_WATCH_COUNT] = {0};
	for(i = 0; i < WatchCount; i++)
	{
		unsigned int prevCurValue = rswatches[i].CurValue;
		unsigned int newCurValue = GetCurrentValue(&rswatches[i]);
		if(prevCurValue != newCurValue)
		{
			rswatches[i].CurValue = newCurValue;
			watchChanged[i] = 1;
		}
	}

	// refresh any visible parts of the listview box that changed
	lv = GetDlgItem(RamWatchHWnd,IDC_WATCHLIST);
	top = ListView_GetTopIndex(lv);
	bottom = top + ListView_GetCountPerPage(lv) + 1; // +1 is so we will update a partially-displayed last item
	if(top < 0) top = 0;
	if(bottom > WatchCount) bottom = WatchCount;
	start = -1;
	for(i = top; i <= bottom; i++)
	{
		if(start == -1)
		{
			if(i != bottom && watchChanged[i])
			{
				start = i;
				//somethingChanged = 1;
			}
		}
		else
		{
			if(i == bottom || !watchChanged[i])
			{
				ListView_RedrawItems(lv, start, i-1);
				start = -1;
			}
		}
	}
}

bool AskSave()
{
	//This function asks to save changes if the watch file contents have changed
	//returns 0 only if a save was attempted but failed or was cancelled
	if (RWfileChanged)
	{
		int answer = MessageBox(MESSAGEBOXPARENT, (LPCWSTR)_16("Save Changes?"), (LPCWSTR)_16("Ram Watch"), MB_YESNOCANCEL);
		if(answer == IDYES)
			if(!QuickSaveWatches())
				return false;
		return (answer != IDCANCEL);
	}
	return true;
}


void UpdateRW_RMenu(HMENU menu, unsigned int mitem, unsigned int baseid)
{
	MENUITEMINFO moo;
	int x;

	moo.cbSize = sizeof(moo);
	moo.fMask = MIIM_SUBMENU | MIIM_STATE;

	GetMenuItemInfo(GetSubMenu(ramwatchmenu, 0), mitem, 0, &moo);
	moo.hSubMenu = menu;
	moo.fState = strlen(rw_recent_files[0]) ? MFS_ENABLED : MFS_GRAYED;

	SetMenuItemInfo(GetSubMenu(ramwatchmenu, 0), mitem, 0, &moo);

	// Remove all recent files submenus
	for(x = 0; x < MAX_RECENT_WATCHES; x++)
	{
		RemoveMenu(menu, baseid + x, MF_BYCOMMAND);
	}

	// Recreate the menus
	for(x = MAX_RECENT_WATCHES - 1; x >= 0; x--)
	{  
		char tmp[128 + 5];

		// Skip empty strings
		if(!strlen(rw_recent_files[x]))
		{
			continue;
		}

		moo.cbSize = sizeof(moo);
		moo.fMask = MIIM_DATA | MIIM_ID | MIIM_TYPE;

		// Fill in the menu text.
		if(strlen(rw_recent_files[x]) < 128)
		{
			sprintf(tmp, "&%d. %s", ( x + 1 ) % 10, rw_recent_files[x]);
		}
		else
		{
			sprintf(tmp, "&%d. %s", ( x + 1 ) % 10, rw_recent_files[x] + strlen( rw_recent_files[x] ) - 127);
		}

		// Insert the menu item
		moo.cch = strlen(tmp);
		moo.fType = 0;
		moo.wID = baseid + x;
		moo.dwTypeData = (LPWSTR)_16(tmp);
		InsertMenuItem(menu, 0, 1, &moo);
	}
}

void UpdateRWRecentArray(const char* addString, unsigned int arrayLen, HMENU menu, unsigned int menuItem, unsigned int baseId)
{
	unsigned int x;
	// Try to find out if the filename is already in the recent files list.
	for(x = 0; x < arrayLen; x++)
	{
		if(strlen(rw_recent_files[x]))
		{
			if(!strcmp(rw_recent_files[x], addString))    // Item is already in list.
			{
				// If the filename is in the file list don't add it again.
				// Move it up in the list instead.

				int y;
				char tmp[1024];

				// Save pointer.
				strcpy(tmp,rw_recent_files[x]);

				for(y = x; y; y--)
				{
					// Move items down.
					strcpy(rw_recent_files[y],rw_recent_files[y - 1]);
				}

				// Put item on top.
				strcpy(rw_recent_files[0],tmp);

				// Update the recent files menu
				UpdateRW_RMenu(menu, menuItem, baseId);
			}
		}
	}

	// The filename wasn't found in the list. That means we need to add it.

	// Move the other items down.
	for(x = arrayLen - 1; x; x--)
	{
		strcpy(rw_recent_files[x],rw_recent_files[x - 1]);
	}

	// Add the new item.
	strcpy(rw_recent_files[0], addString);

	// Update the recent files menu
	UpdateRW_RMenu(menu, menuItem, baseId);
	
	return;
}


void RWAddRecentFile(const char *filename)
{
	UpdateRWRecentArray(filename, MAX_RECENT_WATCHES, rwrecentmenu, RAMMENU_FILE_RECENT, RW_MENU_FIRST_RECENT_FILE);
}

void OpenRWRecentFile(int memwRFileNumber)
{
	const char DELIM = '\t';
	struct AddressWatcher Temp;
	char mode;
	int i;
	int WatchAdd;
	FILE *WatchFile;

	char* x;
	int rnum;

	if(!ResetWatches())
		return;

	rnum = memwRFileNumber;
	if ((unsigned int)rnum >= MAX_RECENT_WATCHES)
		return; //just in case



	while(1)
	{
		x = rw_recent_files[rnum];
		if (!*x) 
			return;		//If no recent files exist just return.  Useful for Load last file on startup (or if something goes screwy)

		if (rnum) //Change order of recent files if not most recent
		{
			RWAddRecentFile(x);
			rnum = 0;
		}
		else
		{
			break;
		}
	}

	strcpy(currentWatch,x);
	strcpy(Str_Tmp,currentWatch);

	//loadwatches here
	WatchFile = fopen(Str_Tmp,"rb");
	if (!WatchFile)
	{
		int answer = MessageBox(MESSAGEBOXPARENT,(LPCWSTR)"Error opening file.",(LPCWSTR)"ERROR",MB_OKCANCEL);
		if (answer == IDOK)
		{
			rw_recent_files[rnum][0] = '\0';	//Clear file from list 
			if (rnum)							//Update the ramwatch list
				RWAddRecentFile(rw_recent_files[0]); 
			else
				RWAddRecentFile(rw_recent_files[1]);
		}
		return;
	}
	fgets(Str_Tmp,1024,WatchFile);
	sscanf(Str_Tmp,"%c%*s",&mode);
	/*	if ((mode == '1' && !(SegaCD_Started)) || (mode == '2' && !(_32X_Started)))
	{
	char Device[8];
	strcpy(Device,(mode > '1')?"32X":"SegaCD");
	sprintf(Str_Tmp,"Warning: %s not started. \nWatches for %s addresses will be ignored.",Device,Device);
	MessageBox(MESSAGEBOXPARENT,Str_Tmp,"Possible Device Mismatch",MB_OK);
	}*/

	fgets(Str_Tmp,1024,WatchFile);
	sscanf(Str_Tmp,"%d%*s",&WatchAdd);
	WatchAdd+=WatchCount;
	for (i = WatchCount; i < WatchAdd; i++)
	{
		char *Comment;

		while (i < 0)
			i++;
		do {
			fgets(Str_Tmp,1024,WatchFile);
		} while (Str_Tmp[0] == '\n');
		sscanf(Str_Tmp,"%*05X%*c%08X%*c%c%*c%c%*c%d",&(Temp.Address),&(Temp.Size),&(Temp.Type),&(Temp.WrongEndian));
		Temp.WrongEndian = 0;
		Comment = strrchr(Str_Tmp,DELIM) + 1;
		*strrchr(Comment,'\n') = '\0';
		InsertWatch(&Temp,Comment);
	}

	fclose(WatchFile);
	if (RamWatchHWnd)
		ListView_SetItemCount(GetDlgItem(RamWatchHWnd,IDC_WATCHLIST),WatchCount);
	RWfileChanged=0;
	return;
}

char Gens_Path[64]= "M:\\"; //TODO

TCHAR watchfilename[MAX_PATH] = TEXT("\0");
char watchpath[MAX_PATH] = "\0";

int Change_File_S(char *Dest, char *Dir, char *Titre, char *Filter, char *Ext, HWND hwnd)
{

	WCHAR filter[1024];
	OPENFILENAME ofn;

	CreateFilter(filter, 1024,
		"Watchlist", "*.wch",
		"All files (*.*)", "*.*", NULL);

	SetupOFN(&ofn, OFN_DEFAULTSAVE, hwnd, filter,
		watchfilename, sizeof(watchfilename)/sizeof(TCHAR));
	ofn.lpstrDefExt = (LPCWSTR)_16(Ext);

	if (GetSaveFileName(&ofn)) {

		char text[1024];

		WideCharToMultiByte(CP_ACP, 0, watchfilename, -1, text, sizeof(text), NULL, NULL);
		strcpy(Dest, text);

		return 1;
	}

	return 0;

	/*
	OPENFILENAME ofn;

	SetCurrentDirectory(Gens_Path);

	if (!strcmp(Dest, ""))
	{
	strcpy(Dest, "default.");
	strcat(Dest, Ext);
	}

	memset(&ofn, 0, sizeof(OPENFILENAME));

	ofn.lStructSize = sizeof(OPENFILENAME);
	ofn.hwndOwner = hwnd;
	ofn.hInstance = y_hInstance;
	ofn.lpstrFile = Dest;
	ofn.nMaxFile = 2047;
	ofn.lpstrFilter = Filter;
	ofn.nFilterIndex = 1;
	ofn.lpstrInitialDir = Dir;
	ofn.lpstrTitle = Titre;
	ofn.lpstrDefExt = Ext;
	ofn.Flags = OFN_PATHMUSTEXIST | OFN_HIDEREADONLY;

	if (GetSaveFileName(&ofn)) return 1;

	return 0;
	*/
}


int Save_Watches()
{
	const char DELIM = '\t';
	int i;

	strncpy(Str_Tmp,Rom_Name,512);
	strcat(Str_Tmp,".wch");
	if(Change_File_S(Str_Tmp, Gens_Path, "Save Watches", "GENs Watchlist\0*.wch\0All Files\0*.*\0\0", "wch", RamWatchHWnd))
	{
		FILE *WatchFile;
		WideCharToMultiByte(CP_ACP, 0, (LPCWSTR)Str_Tmp, -1, Str_Tmp, sizeof(Str_Tmp), NULL, NULL);
		WatchFile = fopen(Str_Tmp,"r+b");
		if (!WatchFile) WatchFile = fopen(Str_Tmp,"w+b");
		//		fputc(SegaCD_Started?'1':(_32X_Started?'2':'0'),WatchFile);
		fputc('\n',WatchFile);
		strcpy(currentWatch,Str_Tmp);
		RWAddRecentFile(currentWatch);
		sprintf(Str_Tmp,"%d\n",WatchCount);
		fputs(Str_Tmp,WatchFile);

		for (i = 0; i < WatchCount; i++)
		{
			sprintf(Str_Tmp,"%05X%c%08X%c%c%c%c%c%d%c%s\n",i,DELIM,rswatches[i].Address,DELIM,rswatches[i].Size,DELIM,rswatches[i].Type,DELIM,rswatches[i].WrongEndian,DELIM,rswatches[i].comment);
			fputs(Str_Tmp,WatchFile);
		}

		fclose(WatchFile);
		RWfileChanged=0;
		//TODO: Add to recent list function call here
		return 1;
	}
	return 0;
}

int QuickSaveWatches()
{
	int i;
	const char DELIM = '\t';
	FILE *WatchFile;

	if (RWfileChanged==0) return 1; //If file has not changed, no need to save changes
	if (currentWatch[0] == NULL) //If there is no currently loaded file, run to Save as and then return
	{
		return Save_Watches();
	}

	strcpy(Str_Tmp,currentWatch);
	WatchFile = fopen(Str_Tmp,"r+b");
	if (!WatchFile) WatchFile = fopen(Str_Tmp,"w+b");
	//		fputc(SegaCD_Started?'1':(_32X_Started?'2':'0'),WatchFile);
	fputc('\n',WatchFile);
	sprintf(Str_Tmp,"%d\n",WatchCount);
	fputs(Str_Tmp,WatchFile);

	for (i = 0; i < WatchCount; i++)
	{
		sprintf(Str_Tmp,"%05X%c%08X%c%c%c%c%c%d%c%s\n",i,DELIM,rswatches[i].Address,DELIM,rswatches[i].Size,DELIM,rswatches[i].Type,DELIM,rswatches[i].WrongEndian,DELIM,rswatches[i].comment);
		fputs(Str_Tmp,WatchFile);
	}
	fclose(WatchFile);
	RWfileChanged=0;
	return 1;
}

int Load_Watches(int clear, const char* filename)
{
	const char DELIM = '\t';
	FILE* WatchFile = fopen(filename,"rb");
	struct AddressWatcher Temp;
	char mode;
	int i;
	int WatchAdd;

	if (!WatchFile)
	{
		MessageBox(MESSAGEBOXPARENT,(LPCWSTR)"Error opening file.",(LPCWSTR)"ERROR",MB_OK);
		return 0;
	}
	if(clear)
	{
		if(!ResetWatches())
		{
			fclose(WatchFile);
			return 0;
		}
	}
	strcpy(currentWatch,filename);
	RWAddRecentFile(currentWatch);

	fgets(Str_Tmp,1024,WatchFile);
	sscanf(Str_Tmp,"%c%*s",&mode);
	/*	if ((mode == '1' && !(SegaCD_Started)) || (mode == '2' && !(_32X_Started)))
	{
	char Device[8];
	strcpy(Device,(mode > '1')?"32X":"SegaCD");
	sprintf(Str_Tmp,"Warning: %s not started. \nWatches for %s addresses will be ignored.",Device,Device);
	MessageBox(MESSAGEBOXPARENT,Str_Tmp,"Possible Device Mismatch",MB_OK);
	}*/

	fgets(Str_Tmp,1024,WatchFile);
	sscanf(Str_Tmp,"%d%*s",&WatchAdd);
	WatchAdd+=WatchCount;
	for (i = WatchCount; i < WatchAdd; i++)
	{
		char *Comment;

		while (i < 0)
			i++;
		do {
			fgets(Str_Tmp,1024,WatchFile);
		} while (Str_Tmp[0] == '\n');
		sscanf(Str_Tmp,"%*05X%*c%08X%*c%c%*c%c%*c%d",&(Temp.Address),&(Temp.Size),&(Temp.Type),&(Temp.WrongEndian));
		Temp.WrongEndian = 0;
		Comment = strrchr(Str_Tmp,DELIM) + 1;
		*strrchr(Comment,'\n') = '\0';
		InsertWatch(&Temp,Comment);
	}

	fclose(WatchFile);
	if (RamWatchHWnd)
		ListView_SetItemCount(GetDlgItem(RamWatchHWnd,IDC_WATCHLIST),WatchCount);
	RWfileChanged=0;
	return 1;
}



int Change_File_L(char *Dest, char *Dir, char *Titre, char *Filter, char *Ext, HWND hwnd)
{


	WCHAR filter[1024];
	OPENFILENAME ofn;

	CreateFilter(filter, 1024,
		"Watchlist", "*.wch",
		"All files (*.*)", "*.*", NULL);

	SetupOFN(&ofn, OFN_DEFAULTSAVE, hwnd, filter,
		watchfilename, sizeof(watchfilename)/sizeof(TCHAR));
	ofn.lpstrDefExt = (LPCWSTR)_16(Ext);


	if (GetOpenFileName(&ofn))  {
		char text[1024];

		WideCharToMultiByte(CP_ACP, 0, watchfilename, -1, text, sizeof(text), NULL, NULL);
		strcpy(Dest, text);
		return 1;}

	return 0;

	/*
	OPENFILENAME ofn;

	SetCurrentDirectory(Gens_Path);

	if (!strcmp(Dest, ""))
	{
	strcpy(Dest, "default.");
	strcat(Dest, Ext);
	}

	memset(&ofn, 0, sizeof(OPENFILENAME));

	ofn.lStructSize = sizeof(OPENFILENAME);
	ofn.hwndOwner = hwnd;
	ofn.hInstance = y_hInstance;
	ofn.lpstrFile = Dest;
	ofn.nMaxFile = 2047;
	ofn.lpstrFilter = Filter;
	ofn.nFilterIndex = 1;
	ofn.lpstrInitialDir = Dir;
	ofn.lpstrTitle = Titre;
	ofn.lpstrDefExt = Ext;
	ofn.Flags = OFN_FILEMUSTEXIST | OFN_HIDEREADONLY;

	if (GetOpenFileName(&ofn)) return 1;

	return 0;*/
}

int Load_WatchesClear(int clear)
{
	strncpy(Str_Tmp,Rom_Name,512);
	strcat(Str_Tmp,".wch");
	if(Change_File_L(Str_Tmp, Watch_Dir, "Load Watches", "GENs Watchlist\0*.wch\0All Files\0*.*\0\0", "wch", RamWatchHWnd))
	{
		return Load_Watches(clear, Str_Tmp);
	}
	return 0;
}

int ResetWatches()
{
	if(!AskSave())
		return 0;
	for (;WatchCount>=0;WatchCount--)
	{
		free(rswatches[WatchCount].comment);
		rswatches[WatchCount].comment = NULL;
	}
	WatchCount++;
	if (RamWatchHWnd)
		ListView_SetItemCount(GetDlgItem(RamWatchHWnd,IDC_WATCHLIST),WatchCount);
	RWfileChanged = 0;
	currentWatch[0] = NULL;
	return 1;
}

void RemoveWatch(int watchIndex)
{
	int i;

	free(rswatches[watchIndex].comment);
	rswatches[watchIndex].comment = NULL;
	for (i = watchIndex; i <= WatchCount; i++)
		rswatches[i] = rswatches[i+1];
	WatchCount--;
}

char s;
char t;

LRESULT CALLBACK EditWatchProc(HWND hDlg, UINT uMsg, WPARAM wParam, LPARAM lParam) //Gets info for a RAM Watch, and then inserts it into the Watch List
{
	RECT r;
	RECT r2;
	int dx1, dy1, dx2, dy2;
	static int index;
	//char s,t = s = 0; //static 

	struct AddressWatcher Temp;
	int i;
	char *addrstr;

	switch(uMsg)
	{
	case WM_INITDIALOG:

		GetWindowRect(YabWin, &r);
		dx1 = (r.right - r.left) / 2;
		dy1 = (r.bottom - r.top) / 2;

		GetWindowRect(hDlg, &r2);
		dx2 = (r2.right - r2.left) / 2;
		dy2 = (r2.bottom - r2.top) / 2;

		//SetWindowPos(hDlg, NULL, max(0, r.left + (dx1 - dx2)), max(0, r.top + (dy1 - dy2)), NULL, NULL, SWP_NOSIZE | SWP_NOZORDER | SWP_SHOWWINDOW);
		SetWindowPos(hDlg, NULL, r.left, r.top, NULL, NULL, SWP_NOSIZE | SWP_NOZORDER | SWP_SHOWWINDOW);
		index = (int)lParam;
		sprintf(Str_Tmp,"%08X",rswatches[index].Address);
		SetDlgItemTextA(hDlg,IDC_EDIT_COMPAREADDRESS,Str_Tmp);
		if (rswatches[index].comment != NULL)
			SetDlgItemTextA(hDlg,IDC_PROMPT_EDIT,rswatches[index].comment);
		s = rswatches[index].Size;
		t = rswatches[index].Type;
		switch (s)
		{
		case 'b':
			SendDlgItemMessage(hDlg, IDC_1_BYTE, BM_SETCHECK, BST_CHECKED, 0);
			break;
		case 'w':
			SendDlgItemMessage(hDlg, IDC_2_BYTES, BM_SETCHECK, BST_CHECKED, 0);
			break;
		case 'd':
			SendDlgItemMessage(hDlg, IDC_4_BYTES, BM_SETCHECK, BST_CHECKED, 0);
			break;
		default:
			s = 0;
			break;
		}
		switch (t)
		{
		case 's':
			SendDlgItemMessage(hDlg, IDC_SIGNED, BM_SETCHECK, BST_CHECKED, 0);
			break;
		case 'u':
			SendDlgItemMessage(hDlg, IDC_UNSIGNED, BM_SETCHECK, BST_CHECKED, 0);
			break;
		case 'h':
			SendDlgItemMessage(hDlg, IDC_HEX, BM_SETCHECK, BST_CHECKED, 0);
			break;
		default:
			t = 0;
			break;
		}

		return 1;
		break;

	case WM_COMMAND:
		switch(LOWORD(wParam))
		{
		case IDC_SIGNED:
			t='s';
			return 1;
		case IDC_UNSIGNED:
			t='u';
			return 1;
		case IDC_HEX:
			t='h';
			return 1;
		case IDC_1_BYTE:
			s = 'b';
			return 1;
		case IDC_2_BYTES:
			s = 'w';
			return 1;
		case IDC_4_BYTES:
			s = 'd';
			return 1;
		case IDOK:
			{
				if (s && t)
				{

					Temp.Size = s;
					Temp.Type = t;
					Temp.WrongEndian = 0; //replace this when I get little endian working properly
					GetDlgItemTextA(hDlg,IDC_EDIT_COMPAREADDRESS,Str_Tmp,1024);
					addrstr = Str_Tmp;
					if (strlen(Str_Tmp) > 8) addrstr = &(Str_Tmp[strlen(Str_Tmp) - 9]);
					for(i = 0; addrstr[i]; i++) {if(toupper(addrstr[i]) == 'O') addrstr[i] = '0';}
					sscanf(addrstr,"%08X",&(Temp.Address));

					if((Temp.Address & ~0xFFFFFF) == ~0xFFFFFF)
						Temp.Address &= 0xFFFFFF;

					if(IsHardwareRAMAddressValid(Temp.Address))
					{
						GetDlgItemTextA(hDlg,IDC_PROMPT_EDIT,Str_Tmp,80);
						if (index < WatchCount) RemoveWatch(index);
						InsertWatch(&Temp,Str_Tmp);
						if(RamWatchHWnd)
						{
							ListView_SetItemCount(GetDlgItem(RamWatchHWnd,IDC_WATCHLIST),WatchCount);
						}
						//							DialogsOpen--;
						EndDialog(hDlg, 1);
					}
					else
					{
						MessageBox(hDlg,(LPCWSTR)_16("Invalid Address"),(LPCWSTR)"ERROR",MB_OK);
					}
				}
				else
				{
					strcpy(Str_Tmp,"Error:");
					if (!s)
						strcat(Str_Tmp," Size must be specified.");
					if (!t)
						strcat(Str_Tmp," Type must be specified.");
					MessageBox(hDlg,(LPCWSTR)_16(Str_Tmp),(LPCWSTR)"ERROR",MB_OK);
				}
				RWfileChanged=1;
				return 1;
				break;
			}
			//				case ID_CANCEL:
		case IDCANCEL:
			EndDialog(hDlg, 0);
			return 0;
			break;
		}
		break;

	case WM_CLOSE:
		EndDialog(hDlg, 0);
		return 0;
		break;
	}

	return 0;
}

void init_list_box(HWND Box, const char* Strs[], int numColumns, int *columnWidths) //initializes the ram search and/or ram watch listbox
{
	int i;

	LVCOLUMN Col;
	Col.mask = LVCF_FMT | LVCF_ORDER | LVCF_SUBITEM | LVCF_TEXT | LVCF_WIDTH;
	Col.fmt = LVCFMT_CENTER;
	for (i = 0; i < numColumns; i++)
	{
		Col.iOrder = i;
		Col.iSubItem = i;
		Col.pszText = (LPWSTR)_16(Strs[i]);
		Col.cx = columnWidths[i];
		ListView_InsertColumn(Box,i,&Col);
	}

	ListView_SetExtendedListViewStyle(Box, LVS_EX_FULLROWSELECT);
}

LRESULT CALLBACK RamWatchProc(HWND hDlg, UINT uMsg, WPARAM wParam, LPARAM lParam)
{
	struct AddressWatcher sizething;
	RECT r;
	RECT r2;
	int dx1, dy1, dx2, dy2;
	static int watchIndex=0;

	int i;
	int t;
	int size;
	const char* formatString;

	int width;
	int height;
	int width2 ;

	unsigned int iNum;
	static char num[11];

	const char* names[3] = {"Address","Value","Notes"};
	int widths[3] = {62,64,64+51+53};

	void *tmp; 

	//Rom_Name=cdip->itemnum;
	strcpy(Rom_Name, cdip->itemnum);
	Update_RAM_Watch();

	switch(uMsg)
	{
	case WM_MOVE: {
		RECT wrect;
		GetWindowRect(hDlg,&wrect);
		ramw_x = wrect.left;
		ramw_y = wrect.top;
		break;
				  };

	case WM_INITDIALOG: {

		GetWindowRect(YabWin, &r);  //Ramwatch window
		dx1 = (r.right - r.left) / 2;
		dy1 = (r.bottom - r.top) / 2;

		GetWindowRect(hDlg, &r2); // Gens window
		dx2 = (r2.right - r2.left) / 2;
		dy2 = (r2.bottom - r2.top) / 2;


		// push it away from the main window if we can
		width = (r.right-r.left);
		height = (r.bottom - r.top);
		width2 = (r2.right-r2.left); 
		if(r.left+width2 + width < GetSystemMetrics(SM_CXSCREEN))
		{
			r.right += width;
			r.left += width;
		}
		else if((int)r.left - (int)width2 > 0)
		{
			r.right -= width2;
			r.left -= width2;
		}

		//-----------------------------------------------------------------------------------
		//If user has Save Window Pos selected, override default positioning
		if (RWSaveWindowPos)	
		{
			//If ramwindow is for some reason completely off screen, use default instead 
			if (ramw_x > (-width*2) || ramw_x < (width*2 + GetSystemMetrics(SM_CYSCREEN))   ) 
				r.left = ramw_x;	  //This also ignores cases of windows -32000 error codes
			//If ramwindow is for some reason completely off screen, use default instead 
			if (ramw_y > (0-height*2) ||ramw_y < (height*2 + GetSystemMetrics(SM_CYSCREEN))	)
				r.top = ramw_y;		  //This also ignores cases of windows -32000 error codes
		}
		//-------------------------------------------------------------------------------------
		SetWindowPos(hDlg, NULL, r.left, r.top, NULL, NULL, SWP_NOSIZE | SWP_NOZORDER | SWP_SHOWWINDOW);

		ramwatchmenu=GetMenu(hDlg);
		rwrecentmenu=CreateMenu();
		UpdateRW_RMenu(rwrecentmenu, RAMMENU_FILE_RECENT, RW_MENU_FIRST_RECENT_FILE);

		//		const char* names[3] = {"Address","Value","Notes"};
		//		int widths[3] = {62,64,64+51+53};
		init_list_box(GetDlgItem(hDlg,IDC_WATCHLIST),names,3,widths);
		/*			if (!ResultCount)  //TODO what do these do
		reset_address_info();
		else
		signal_new_frame();*/
		ListView_SetItemCount(GetDlgItem(hDlg,IDC_WATCHLIST),WatchCount);
		//			if (!noMisalign) SendDlgItemMessage(hDlg, IDC_MISALIGN, BM_SETCHECK, BST_CHECKED, 0);
		//			if (littleEndian) SendDlgItemMessage(hDlg, IDC_ENDIAN, BM_SETCHECK, BST_CHECKED, 0);

		RamWatchAccels = LoadAccelerators(y_hInstance, MAKEINTRESOURCE(IDR_ACCELERATOR1));

		// due to some bug in windows, the arrow button width from the resource gets ignored, so we have to set it here
		SetWindowPos(GetDlgItem(hDlg,ID_WATCHES_UPDOWN), 0,0,0, 30,60, SWP_NOMOVE);

		Update_RAM_Watch();

		DragAcceptFiles(hDlg, 1);

		return 1;
		break;
						}

	case WM_INITMENU:
		CheckMenuItem(ramwatchmenu, RAMMENU_FILE_AUTOLOAD, AutoRWLoad ? MF_CHECKED : MF_UNCHECKED);
		CheckMenuItem(ramwatchmenu, RAMMENU_FILE_SAVEWINDOW, RWSaveWindowPos ? MF_CHECKED : MF_UNCHECKED);
		break;

	case WM_MENUSELECT:
	case WM_ENTERSIZEMOVE:
		break;

	case WM_NOTIFY:
		{
			LPNMHDR lP = (LPNMHDR) lParam;
			switch (lP->code)
			{
			case LVN_GETDISPINFO:
				{
					LV_DISPINFO *Item = (LV_DISPINFO *)lParam;
					Item->item.mask = LVIF_TEXT;
					Item->item.state = 0;
					Item->item.iImage = 0;
					iNum = Item->item.iItem;
					switch (Item->item.iSubItem)
					{
					case 0:
						sprintf(num,"%08X",rswatches[iNum].Address);
						Item->item.pszText = (LPWSTR)_16(num);
						return 1;
					case 1: {
						i = rswatches[iNum].CurValue;
						t = rswatches[iNum].Type;
						size = rswatches[iNum].Size;
						formatString = ((t=='s') ? "%d" : (t=='u') ? "%u" : (size=='d' ? "%08X" : size=='w' ? "%04X" : "%02X"));
						switch (size)
						{
						case 'b':
						default: sprintf(num, formatString, t=='s' ? (char)(i&0xff) : (unsigned char)(i&0xff)); break;
						case 'w': sprintf(num, formatString, t=='s' ? (short)(i&0xffff) : (unsigned short)(i&0xffff)); break;
						case 'd': sprintf(num, formatString, t=='s' ? (long)(i&0xffffffff) : (unsigned long)(i&0xffffffff)); break;
						}

						Item->item.pszText = (LPWSTR)_16(num);
							}	return 1;
					case 2:
						Item->item.pszText = (LPWSTR)_16(rswatches[iNum].comment ? rswatches[iNum].comment : "");
						return 1;

					default:
						return 0;
					}
				}
			case LVN_ODFINDITEM:
				{	
					// disable search by keyboard typing,
					// because it interferes with some of the accelerators
					// and it isn't very useful here anyway
					SetWindowLong(hDlg, DWL_MSGRESULT, ListView_GetSelectionMark(GetDlgItem(hDlg,IDC_WATCHLIST)));
					return 1;
				}
			}
			break;
		}

	case WM_COMMAND:
		switch(LOWORD(wParam))
		{
		case RAMMENU_FILE_SAVE:
			QuickSaveWatches();
			break;

		case RAMMENU_FILE_SAVEAS:	
			//case IDC_C_SAVE:
			return Save_Watches();
		case RAMMENU_FILE_OPEN:
			return Load_WatchesClear(1);
		case RAMMENU_FILE_APPEND:
			//case IDC_C_LOAD:
			return Load_WatchesClear(0);
		case RAMMENU_FILE_NEW:
			//case IDC_C_RESET:
			ResetWatches();
			return 1;
		case IDC_C_WATCH_REMOVE:
			watchIndex = ListView_GetSelectionMark(GetDlgItem(hDlg,IDC_WATCHLIST));
			RemoveWatch(watchIndex);
			ListView_SetItemCount(GetDlgItem(hDlg,IDC_WATCHLIST),WatchCount);	
			RWfileChanged=1;
			SetFocus(GetDlgItem(hDlg,IDC_WATCHLIST));
			return 1;
		case IDC_C_WATCH_EDIT:
			watchIndex = ListView_GetSelectionMark(GetDlgItem(hDlg,IDC_WATCHLIST));
			DialogBoxParam(y_hInstance, MAKEINTRESOURCE(IDD_EDITWATCH), hDlg, (DLGPROC) EditWatchProc,(LPARAM) watchIndex);
			SetFocus(GetDlgItem(hDlg,IDC_WATCHLIST));
			return 1;
		case IDC_C_WATCH:
			rswatches[WatchCount].Address = rswatches[WatchCount].WrongEndian = 0;
			rswatches[WatchCount].Size = 'b';
			rswatches[WatchCount].Type = 's';
			DialogBoxParam(y_hInstance, MAKEINTRESOURCE(IDD_EDITWATCH), hDlg, (DLGPROC) EditWatchProc,(LPARAM) WatchCount);
			SetFocus(GetDlgItem(hDlg,IDC_WATCHLIST));
			return 1;
		case IDC_C_WATCH_DUPLICATE:
			watchIndex = ListView_GetSelectionMark(GetDlgItem(hDlg,IDC_WATCHLIST));
			rswatches[WatchCount].Address = rswatches[watchIndex].Address;
			rswatches[WatchCount].WrongEndian = rswatches[watchIndex].WrongEndian;
			rswatches[WatchCount].Size = rswatches[watchIndex].Size;
			rswatches[WatchCount].Type = rswatches[watchIndex].Type;
			DialogBoxParam(y_hInstance, MAKEINTRESOURCE(IDD_EDITWATCH), hDlg, (DLGPROC) EditWatchProc,(LPARAM) WatchCount);
			SetFocus(GetDlgItem(hDlg,IDC_WATCHLIST));
			return 1;
		case IDC_C_WATCH_UP:
			{
				watchIndex = ListView_GetSelectionMark(GetDlgItem(hDlg,IDC_WATCHLIST));
				if (watchIndex == 0 || watchIndex == -1)
					return 1;
				tmp = malloc(sizeof(sizething));
				memcpy(tmp,&(rswatches[watchIndex]),sizeof(sizething));
				memcpy(&(rswatches[watchIndex]),&(rswatches[watchIndex - 1]),sizeof(sizething));
				memcpy(&(rswatches[watchIndex - 1]),tmp,sizeof(sizething));
				free(tmp);
				ListView_SetSelectionMark(GetDlgItem(hDlg,IDC_WATCHLIST),watchIndex-1);
				ListView_SetItemState(GetDlgItem(hDlg,IDC_WATCHLIST),watchIndex-1,LVIS_FOCUSED|LVIS_SELECTED,LVIS_FOCUSED|LVIS_SELECTED);
				ListView_SetItemCount(GetDlgItem(hDlg,IDC_WATCHLIST),WatchCount);
				RWfileChanged=1;
				return 1;
			}
		case IDC_C_WATCH_DOWN:
			{
				void *tmp;
				watchIndex = ListView_GetSelectionMark(GetDlgItem(hDlg,IDC_WATCHLIST));
				if (watchIndex >= WatchCount - 1 || watchIndex == -1)
					return 1;
				tmp = malloc(sizeof(sizething));
				memcpy(tmp,&(rswatches[watchIndex]),sizeof(sizething));
				memcpy(&(rswatches[watchIndex]),&(rswatches[watchIndex + 1]),sizeof(sizething));
				memcpy(&(rswatches[watchIndex + 1]),tmp,sizeof(sizething));
				free(tmp);
				ListView_SetSelectionMark(GetDlgItem(hDlg,IDC_WATCHLIST),watchIndex+1);
				ListView_SetItemState(GetDlgItem(hDlg,IDC_WATCHLIST),watchIndex+1,LVIS_FOCUSED|LVIS_SELECTED,LVIS_FOCUSED|LVIS_SELECTED);
				ListView_SetItemCount(GetDlgItem(hDlg,IDC_WATCHLIST),WatchCount);
				RWfileChanged=1;
				return 1;
			}
		case ID_WATCHES_UPDOWN:
			{
				int delta = ((LPNMUPDOWN)lParam)->iDelta;
				SendMessage(hDlg, WM_COMMAND, delta<0 ? IDC_C_WATCH_UP : IDC_C_WATCH_DOWN,0);
				break;
			}
		case RAMMENU_FILE_AUTOLOAD:
			{
				AutoRWLoad ^= 1;
				CheckMenuItem(ramwatchmenu, RAMMENU_FILE_AUTOLOAD, AutoRWLoad ? MF_CHECKED : MF_UNCHECKED);
				break;
			}
		case RAMMENU_FILE_SAVEWINDOW:
			{
				RWSaveWindowPos ^=1;
				CheckMenuItem(ramwatchmenu, RAMMENU_FILE_SAVEWINDOW, RWSaveWindowPos ? MF_CHECKED : MF_UNCHECKED);
				break;
			}
		case IDC_C_ADDCHEAT:
			{
				watchIndex = ListView_GetSelectionMark(GetDlgItem(hDlg,IDC_WATCHLIST)) | (1 << 24);
				//					DialogBoxParam(hAppInst, MAKEINTRESOURCE(IDD_EDITCHEAT), hDlg, (DLGPROC) EditCheatProc,(LPARAM) searchIndex);
				break;
			}
		case IDOK:
		case IDCANCEL: 
			RamWatchHWnd = NULL;
			DragAcceptFiles(hDlg, 0);
			EndDialog(hDlg, 1);
			return 1;
		default:
			if (LOWORD(wParam) >= RW_MENU_FIRST_RECENT_FILE && LOWORD(wParam) < RW_MENU_FIRST_RECENT_FILE+MAX_RECENT_WATCHES)
				OpenRWRecentFile(LOWORD(wParam) - RW_MENU_FIRST_RECENT_FILE);
		}
		break;

	case WM_KEYDOWN: // handle accelerator keys
		{
			MSG msg;
			SetFocus(GetDlgItem(hDlg,IDC_WATCHLIST));
			msg.hwnd = hDlg;
			msg.message = uMsg;
			msg.wParam = wParam;
			msg.lParam = lParam;
			if(RamWatchAccels && TranslateAccelerator(hDlg, RamWatchAccels, &msg))
				return 1;
		}	break;

	case WM_CLOSE:
		RamWatchHWnd = NULL;
		DragAcceptFiles(hDlg, 0);
		EndDialog(hDlg, 1);
		return 1;

	case WM_DROPFILES:
		{
			HDROP hDrop = (HDROP)wParam;
			DragQueryFile(hDrop, 0, (LPWSTR)Str_Tmp, 1024);
			DragFinish(hDrop);
			return Load_Watches(1, Str_Tmp);
		}	break;
	}

	return 0;
}

