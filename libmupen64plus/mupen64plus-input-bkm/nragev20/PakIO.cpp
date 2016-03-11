/*	
	N-Rage`s Dinput8 Plugin
    (C) 2002, 2006  Norbert Wladyka

	Author`s Email: norbert.wladyka@chello.at
	Website: http://go.to/nrage


    This program is free software; you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the free Software Foundation; either version 2 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the free Software
    Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
*/

//#include "commonIncludes.h"
#include <windows.h>
#include <Commctrl.h>
#include <tchar.h>
#include <stdio.h>
#include "../plugin.hpp"
//#include "NRagePluginV2.h"
//#include "DirectInput.h"
//#include "Interface.h"
//#include "FileAccess.h"
#include "PakIO.h"
#include "GBCart.h"

// ProtoTypes
BYTE AddressCRC( LPCBYTE Address );
BYTE DataCRC( LPCBYTE Data, const int iLength );
VOID CALLBACK WritebackProc( HWND hWnd, UINT msg, UINT_PTR idEvent, DWORD dwTime );

bool InitTransferPak( const int iControl )
// Prepares the Pak
{
	if( !controller[iControl].control->Present )
		return false;
	bool bReturn = false;

	controller[iControl].pPakData = malloc( sizeof(TRANSFERPAK));
	LPTRANSFERPAK tPak = (LPTRANSFERPAK)controller[iControl].pPakData;
	tPak->bPakType = PAK_TRANSFER;

	tPak->gbCart.hRomFile = NULL;
	tPak->gbCart.hRamFile = NULL;
	tPak->gbCart.RomData = NULL;
	tPak->gbCart.RamData = NULL;

	/*
		* Once the Interface is implemented g_pcControllers[iControl].szTransferRom will hold filename of the GB-Rom
		* and g_pcControllers[iControl].szTransferSave holds Filename of the SRAM Save
		* 
		* Here, both files should be opened and the handles stored in tPak ( modify the struct for Your own purposes, only bPakType must stay at first )
		*/


	//CreateFile( g_pcControllers[iControl].szTransferSave, GENERIC_READ, FILE_SHARE_READ, NULL, OPEN_ALWAYS, 0, NULL );
	tPak->iCurrentAccessMode = 0;
	tPak->iCurrentBankNo = 0;
	tPak->iEnableState = false;
	tPak->iAccessModeChanged = 0x44;

	tPak->bPakInserted = LoadCart( &tPak->gbCart, "gb_rom.gb", "gb_sav.sav", _T("") );

	/*
	if (tPak->bPakInserted) {
		//DebugWriteA( "*** Init Transfer Pak - Success***\n" );
	} else {
		//DebugWriteA( "*** Init Transfer Pak - FAILURE***\n" );
	}
	*/
	bReturn = true;

	/*
	// if there were any unrecoverable errors and we have allocated pPakData, free it and set paktype to NONE
	if( !bReturn && g_pcControllers[iControl].pPakData )
		CloseControllerPak( iControl );
	*/
	return bReturn;
}


BYTE ReadTransferPak( const int iControl, LPBYTE Command )
{
	BYTE bReturn = RD_ERROR;
	LPBYTE Data = &Command[2];

	if( !controller[iControl].pPakData )
		return RD_ERROR;

	WORD dwAddress = (Command[0] << 8) + (Command[1] & 0xE0);

	LPTRANSFERPAK tPak = (LPTRANSFERPAK)controller[iControl].pPakData;	// TODO: null pointer check on tPak
	// set bReturn = RD_OK when implementing Transferpak
	bReturn = RD_OK;
	//DebugWriteA( "TPak Read:\n" );
	//DebugWriteA( "  Address: %04X\n", dwAddress );

	switch (dwAddress >> 12)
	{
	case 0x8: //	if ((dwAddress >= 0x8000) && (dwAddress <= 0x8FFF))
		//DebugWriteA( "Query Enable State: %u\n", tPak->iEnableState );
		if (tPak->iEnableState == false)
			ZeroMemory(Data, 32);
		else
			FillMemory(Data, 32, 0x84);
		break;
	case 0xB: //	if ((dwAddress >= 0xB000) && (dwAddress <= 0xBFFF))
		if (tPak->iEnableState == true) {
			//DebugWriteA( "Query Cart. State:" );
			if (tPak->bPakInserted) {
				if (tPak->iCurrentAccessMode == 1) {
					FillMemory(Data, 32, 0x89);
					//DebugWriteA( " Inserted, Access Mode 1\n" );
				} else {
					FillMemory(Data, 32, 0x80);
					//DebugWriteA( " Inserted, Access Mode 0\n" );
				}
				Data[0] = Data[0] | (BYTE)tPak->iAccessModeChanged;
			} else {
				FillMemory(Data, 32, 0x40); // Cart not inserted.
				//DebugWriteA( " Not Inserted\n" );
			}
			tPak->iAccessModeChanged = 0;
		}
		break;
	case 0xC:
	case 0xD:
	case 0xE:
	case 0xF: //	if ((dwAddress >= 0xC000))
		if (tPak->iEnableState == true) {
			//DebugWriteA( "Cart Read: Bank:%i\n", tPak->iCurrentBankNo );
			//DebugWriteA( "    Address:%04X\n", ((dwAddress & 0xFFE0) - 0xC000) + ((tPak->iCurrentBankNo & 3) * 0x4000) );

			tPak->gbCart.ptrfnReadCart(&tPak->gbCart, ((dwAddress & 0xFFE0) - 0xC000) + ((tPak->iCurrentBankNo & 3) * 0x4000), Data);
		}
		break;
	//default:
		//DebugWriteA("WARNING: Unusual Pak Read\n" );
		//DebugWriteA("  Address: %04X\n", dwAddress);
	} // end switch (dwAddress >> 12)

	//Data[32] = DataCRC( Data, 32 );

	bReturn = RD_OK;

	return bReturn;
}

// Called when the N64 tries to write to the controller pak, e.g. a mempak
BYTE WriteTransferPak( const int iControl, LPBYTE Command )
{
	BYTE bReturn = RD_ERROR;
	BYTE *Data = &Command[2];


	if( !controller[iControl].pPakData )
		return RD_ERROR;

	WORD dwAddress = (Command[0] << 8) + (Command[1] & 0xE0);

	LPTRANSFERPAK tPak = (LPTRANSFERPAK)controller[iControl].pPakData;
	// set bReturn = RD_OK when implementing Transferpak
	//DebugWriteA( "TPak Write:\n" );
	//DebugWriteA( "  Address: %04X\n", dwAddress );

	switch (dwAddress >> 12)
	{
	case 0x8: //	if ((dwAddress >= 0x8000) && (dwAddress <= 0x8FFF))
		if (Data[0] == 0xFE) {
			//DebugWriteA("Cart Disable\n" );
			tPak->iEnableState = false;
		}
		else if (Data[0] == 0x84) {
			//DebugWriteA("Cart Enable\n" );
			tPak->iEnableState = true;
		}
		else {
			//DebugWriteA("WARNING: Unusual Cart Enable/Disable\n" );
			//DebugWriteA("  Address: " );
			//DebugWriteWordA(dwAddress);
			//DebugWriteA("\n" );
			//DebugWriteA("  Data: " );
			//DebugWriteByteA(Data[0]);
			//DebugWriteA("\n" );
		}
		break;
	case 0xA: //	if ((dwAddress >= 0xA000) && (dwAddress <= 0xAFFF))
		if (tPak->iEnableState == true) {
			tPak->iCurrentBankNo = Data[0];
			//DebugWriteA("Set TPak Bank No:%02X\n", Data[0] );
		}
		break;
	case 0xB: //	if ((dwAddress >= 0xB000) && (dwAddress <= 0xBFFF))
		if (tPak->iEnableState == true) {
			tPak->iCurrentAccessMode = Data[0] & 1;
			tPak->iAccessModeChanged = 4;
			//DebugWriteA("Set TPak Access Mode: %04X\n", tPak->iCurrentAccessMode);
			if ((Data[0] != 1) && (Data[0] != 0)) {
				//DebugWriteA("WARNING: Unusual Access Mode Change\n" );
				//DebugWriteA("  Address: " );
				//DebugWriteWordA(dwAddress);
				//DebugWriteA("\n" );
				//DebugWriteA("  Data: " );
				//DebugWriteByteA(Data[0]);
				//DebugWriteA("\n" );
			}
		}
		break;
	case 0xC:
	case 0xD:
	case 0xE:
	case 0xF: //	if (dwAddress >= 0xC000)
		tPak->gbCart.ptrfnWriteCart(&tPak->gbCart, ((dwAddress & 0xFFE0) - 0xC000) + ((tPak->iCurrentBankNo & 3) * 0x4000), Data);
		/*
		if (tPak->gbCart.hRamFile != NULL )
			SetTimer( g_strEmuInfo.hMainWindow, PAK_TRANSFER, 2000, (TIMERPROC) WritebackProc ); // if we go 2 seconds without a write, call the Writeback proc (which will flush the cache)
		*/
		break;
	//default:
		//DebugWriteA("WARNING: Unusual Pak Write\n" );
		//DebugWriteA("  Address: %04X\n", dwAddress);
	} // end switch (dwAddress >> 12)

	//Data[32] = DataCRC( Data, 32 );
	bReturn = RD_OK; 

	return bReturn;
}

void SaveControllerPak( const int iControl )
{
	/*
	if( !g_pcControllers[iControl].pPakData )
		return;

	switch( *(BYTE*)g_pcControllers[iControl].pPakData )
	{
	case PAK_MEM:
		{
			MEMPAK *mPak = (MEMPAK*)g_pcControllers[iControl].pPakData;

			if( !mPak->fReadonly )
				FlushViewOfFile( mPak->aMemPakData, PAK_MEM_SIZE );	// we've already written the stuff, just flush the cache
		}
		break;
	case PAK_RUMBLE:
		break;
	case PAK_TRANSFER:
		{
			LPTRANSFERPAK tPak = (LPTRANSFERPAK)g_pcControllers[iControl].pPakData;
			// here the changes( if any ) in the SRAM should be saved

			if (tPak->gbCart.hRamFile != NULL)
			{
				SaveCart(&tPak->gbCart, g_pcControllers[iControl].szTransferSave, _T(""));
				//DebugWriteA( "*** Save Transfer Pak ***\n" );
			}
		}
		break;
	case PAK_VOICE:
		break;
	case PAK_ADAPTOID:
		break;

	//case PAK_NONE:
	//	break;
	}
	*/
}

// if there is pPakData for the controller, does any closing of handles before freeing the pPakData struct and setting it to NULL
// also sets fPakInitialized to false
void CloseControllerPak( const int iControl )
{
	/*
	if( !g_pcControllers[iControl].pPakData )
		return;

	g_pcControllers[iControl].fPakInitialized = 0;

	switch( *(BYTE*)g_pcControllers[iControl].pPakData )
	{
	case PAK_MEM:
		{
			MEMPAK *mPak = (MEMPAK*)g_pcControllers[iControl].pPakData;
			
			if( mPak->fReadonly )
			{
				P_free( mPak->aMemPakData );
				mPak->aMemPakData = NULL;
			}
			else
			{
				FlushViewOfFile( mPak->aMemPakData, PAK_MEM_SIZE );
				// if it's a dexsave, our original mapped view is not aMemPakData 
				UnmapViewOfFile( mPak->fDexSave ? mPak->aMemPakData - PAK_MEM_DEXOFFSET : mPak->aMemPakData );
				if ( mPak->hMemPakHandle != NULL )
					CloseHandle( mPak->hMemPakHandle );
			}
		}
		break;
	case PAK_RUMBLE:
		ReleaseEffect( g_apdiEffect[iControl] );
		g_apdiEffect[iControl] = NULL;
		break;
	case PAK_TRANSFER:
		{
			LPTRANSFERPAK tPak = (LPTRANSFERPAK)g_pcControllers[iControl].pPakData;
			UnloadCart(&tPak->gbCart);
			//DebugWriteA( "*** Close Transfer Pak ***\n" );
			// close files and free any additionally ressources
		}
		break;
	case PAK_VOICE:
		break;
	
	case PAK_ADAPTOID:
		break;

	//case PAK_NONE:
	//	break;
	}

	freePakData( &g_pcControllers[iControl] );
	*/
	return;
}

BYTE AddressCRC( LPCBYTE Address )
{
	bool HighBit;
	WORD Data = MAKEWORD( Address[1], Address[0] );
	register BYTE Remainder = ( Data >> 11 ) & 0x1F;

	BYTE bBit = 5;

	while( bBit < 16 )
	{
		HighBit = (Remainder & 0x10) != 0;
		Remainder = (Remainder << 1) & 0x1E;

		Remainder += ( bBit < 11 && Data & (0x8000 >> bBit )) ? 1 : 0;

		Remainder ^= (HighBit) ? 0x15 : 0;
		
		bBit++;
	}

	return Remainder;
}

BYTE DataCRC( LPCBYTE Data, const int iLength )
{
	register BYTE Remainder = Data[0];

	int iByte = 1;
	BYTE bBit = 0;

	while( iByte <= iLength )
	{
		bool HighBit = ((Remainder & 0x80) != 0);
		Remainder = Remainder << 1;

		Remainder += ( iByte < iLength && Data[iByte] & (0x80 >> bBit )) ? 1 : 0;

		Remainder ^= (HighBit) ? 0x85 : 0;
		
		bBit++;
		iByte += bBit/8;
		bBit %= 8;
	}

	return Remainder;
}

VOID CALLBACK WritebackProc( HWND hWnd, UINT msg, UINT_PTR idEvent, DWORD dwTime )
{
	/*
	KillTimer(hWnd, idEvent); // timer suicide

	switch (idEvent)
	{
	case PAK_MEM:
		//DebugWriteA("Mempak: WritebackProc flushed file writes\n");
		for( int i = 0; i < 4; i++ )
		{
			MEMPAK *mPak = (MEMPAK*)g_pcControllers[i].pPakData;

			if ( mPak && mPak->bPakType == PAK_MEM && !mPak->fReadonly && mPak->hMemPakHandle != NULL )
				FlushViewOfFile( mPak->aMemPakData, PAK_MEM_SIZE );
		}
		return;
	case PAK_TRANSFER:
		//DebugWriteA("TPak: WritebackProc flushed file writes\n");
		for( int i = 0; i < 4; i++ )
		{
			LPTRANSFERPAK tPak = (LPTRANSFERPAK)g_pcControllers[i].pPakData;

			if (tPak && tPak->bPakType == PAK_TRANSFER && tPak->bPakInserted && tPak->gbCart.hRamFile != NULL )
				FlushViewOfFile( tPak->gbCart.RamData, (tPak->gbCart.RomData[0x149] == 1 ) ? 0x0800 : tPak->gbCart.iNumRamBanks * 0x2000);
		}
		return;
	}
	*/
}
