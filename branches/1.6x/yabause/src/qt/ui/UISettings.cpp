/*  Copyright 2005 Guillaume Duhamel
	Copyright 2005-2006 Theo Berkau
	Copyright 2008 Filipe Azevedo <pasnox@gmail.com>

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
#include "UISettings.h"
#include "../Settings.h"
#include "../CommonDialogs.h"
#include "UIWaitInput.h"
#include "UIPortManager.h"

#include <QDir>
#include <QList>

extern "C" {
extern M68K_struct* M68KCoreList[];
extern SH2Interface_struct* SH2CoreList[];
extern PerInterface_struct* PERCoreList[];
extern CDInterface* CDCoreList[];
extern SoundInterface_struct* SNDCoreList[];
extern VideoInterface_struct* VIDCoreList[];
extern OSD_struct* OSDCoreList[];
}

struct Item
{
	Item( const QString& k, const QString& s )
	{ id = k; Name = s; }
	
	QString id;
	QString Name;
};

typedef QList<Item> Items;

const Items mRegions = Items()
	<< Item( "Auto" , "Auto-detect" )
	<< Item( "J" , "Japan (NTSC)" )
	<< Item( "T", "Asia (NTSC)" )
	<< Item( "U", "North America (NTSC)" )
	<< Item( "B", "Central/South America (NTSC)" )
	<< Item( "K", "Korea (NTSC)" )
	<< Item( "A", "Asia (PAL)" )
	<< Item( "E", "Europe + others (PAL)" )
	<< Item( "L", "Central/South America (PAL)" );

const Items mCartridgeTypes = Items()
	<< Item( "0", "None" )
	<< Item( "1", "Pro Action Replay" )
	<< Item( "2", "4 Mbit Backup Ram" )
	<< Item( "3", "8 Mbit Backup Ram" )
	<< Item( "4", "16 Mbit Backup Ram" )
	<< Item( "5", "32 Mbit Backup Ram" )
	<< Item( "6", "8 Mbit Dram" )
	<< Item( "7", "32 Mbit Dram" )
	<< Item( "8", "Netlink" )
	<< Item( "9", "16 Mbit ROM" );

const Items mVideoFromats = Items()
	<< Item( "0", "NTSC" )
	<< Item( "1", "PAL" );

UISettings::UISettings( QWidget* p )
	: QDialog( p )
{
	// setup dialog
	setupUi( this );
	pmPort1->setPort( 1 );
	pmPort1->loadSettings();
	pmPort2->setPort( 2 );
	pmPort2->loadSettings();
	
	if ( p && !p->isFullScreen() )
	{
		setWindowFlags( Qt::Sheet );
	}

	// load cores informations
	loadCores();

	// load settings
	loadSettings();
	
	// connections
	foreach ( QToolButton* tb, findChildren<QToolButton*>() )
	{
		connect( tb, SIGNAL( clicked() ), this, SLOT( tbBrowse_clicked() ) );
	}
	
	// retranslate widgets
	QtYabause::retranslateWidget( this );
}

void UISettings::requestFile( const QString& c, QLineEdit* e, const QString& filters )
{
	const QString s = CommonDialogs::getOpenFileName( e->text(), c, filters );
	if ( !s.isNull() )
		e->setText( s );
}

void UISettings::requestNewFile( const QString& c, QLineEdit* e, const QString& filters )
{
	const QString s = CommonDialogs::getSaveFileName( e->text(), c, filters );
	if ( !s.isNull() )
		e->setText( s );
}

void UISettings::requestFolder( const QString& c, QLineEdit* e )
{
	const QString s = CommonDialogs::getExistingDirectory( e->text(), c );
	if ( !s.isNull() )
		e->setText( s );
}

void UISettings::tbBrowse_clicked()
{
	// get toolbutton sender
	QToolButton* tb = qobject_cast<QToolButton*>( sender() );
	
	if ( tb == tbBios )
		requestFile( QtYabause::translate( "Choose a bios file" ), leBios );
	else if ( tb == tbCdRom )
	{
		if ( cbCdRom->currentText().contains( "dummy", Qt::CaseInsensitive ) )
		{
			CommonDialogs::information( QtYabause::translate( "The dummies cores don't need configuration." ) );
			return;
		}
		else if ( cbCdRom->currentText().contains( "iso", Qt::CaseInsensitive ) )
			requestFile( QtYabause::translate( "Select your iso/cue/bin file" ), leCdRom, QtYabause::translate( "CD Images (*.iso *.cue *.bin)" ) );
		else
			requestFolder( QtYabause::translate( "Choose a cdrom drive/mount point" ), leCdRom );
	}
	else if ( tb == tbSaveStates )
		requestFolder( QtYabause::translate( "Choose a folder to store save states" ), leSaveStates );
	else if ( tb == tbTranslation )
		requestFile( QtYabause::translate( "Choose the translation file to use" ), leTranslation, QtYabause::translate( "Yabause Translation Files (*.yts)" ) );
	else if ( tb == tbCartridge )
		requestNewFile( QtYabause::translate( "Choose a cartridge file" ), leCartridge );
	else if ( tb == tbMemory )
		requestNewFile( QtYabause::translate( "Choose a memory file" ), leMemory );
	else if ( tb == tbMpegROM )
		requestFile( QtYabause::translate( "Choose a mpeg rom" ), leMpegROM );
}

void UISettings::on_cbInput_currentIndexChanged( int id )
{
	PerInterface_struct* core = QtYabause::getPERCore( cbInput->itemData( id ).toInt() );
        core->Init();
	
	Q_ASSERT( core );
	
	pmPort1->setCore( core );
	pmPort2->setCore( core );
}

void UISettings::loadCores()
{
	// CD Drivers
	for ( int i = 0; CDCoreList[i] != NULL; i++ )
		cbCdRom->addItem( QtYabause::translate( CDCoreList[i]->Name ), CDCoreList[i]->id );
	
	// VDI Drivers
	for ( int i = 0; VIDCoreList[i] != NULL; i++ )
		cbVideoCore->addItem( QtYabause::translate( VIDCoreList[i]->Name ), VIDCoreList[i]->id );

#if YAB_PORT_OSD
	// OSD Drivers
	for ( int i = 0; OSDCoreList[i] != NULL; i++ )
		cbOSDCore->addItem( QtYabause::translate( OSDCoreList[i]->Name ), OSDCoreList[i]->id );
#else
	delete cbOSDCore;
	delete lOSDCore;
#endif
	
	// Video Formats
	foreach ( const Item& it, mVideoFromats )
		cbVideoFormat->addItem( QtYabause::translate( it.Name ), it.id );
	
	// SND Drivers
	for ( int i = 0; SNDCoreList[i] != NULL; i++ )
		cbSoundCore->addItem( QtYabause::translate( SNDCoreList[i]->Name ), SNDCoreList[i]->id );
	
	// Cartridge Types
	foreach ( const Item& it, mCartridgeTypes )
		cbCartridge->addItem( QtYabause::translate( it.Name ), it.id );
	
	// Input Drivers
	for ( int i = 0; PERCoreList[i] != NULL; i++ )
		cbInput->addItem( QtYabause::translate( PERCoreList[i]->Name ), PERCoreList[i]->id );
	
	// Regions
	foreach ( const Item& it, mRegions )
		cbRegion->addItem( QtYabause::translate( it.Name ), it.id );
	
	// SH2 Interpreters
	for ( int i = 0; SH2CoreList[i] != NULL; i++ )
		cbSH2Interpreter->addItem( QtYabause::translate( SH2CoreList[i]->Name ), SH2CoreList[i]->id );
}

void UISettings::loadSettings()
{
	// get settings pointer
	Settings* s = QtYabause::settings();

	// general
	leBios->setText( s->value( "General/Bios" ).toString() );
	cbCdRom->setCurrentIndex( cbCdRom->findData( s->value( "General/CdRom", QtYabause::defaultCDCore().id ).toInt() ) );
	leCdRom->setText( s->value( "General/CdRomISO" ).toString() );
	leSaveStates->setText( s->value( "General/SaveStates", getDataDirPath() ).toString() );
	leTranslation->setText( s->value( "General/Translation" ).toString() );
	cbEnableFrameSkipLimiter->setChecked( s->value( "General/EnableFrameSkipLimiter" ).toBool() );
	cbShowFPS->setChecked( s->value( "General/ShowFPS" ).toBool() );
	cbAutostart->setChecked( s->value( "autostart" ).toBool() );

	// video
	cbVideoCore->setCurrentIndex( cbVideoCore->findData( s->value( "Video/VideoCore", QtYabause::defaultVIDCore().id ).toInt() ) );
#if YAB_PORT_OSD
	cbOSDCore->setCurrentIndex( cbOSDCore->findData( s->value( "Video/OSDCore", QtYabause::defaultOSDCore().id ).toInt() ) );
#endif
	leWidth->setText( s->value( "Video/Width" ).toString() );
	leHeight->setText( s->value( "Video/Height" ).toString() );
	cbFullscreen->setChecked( s->value( "Video/Fullscreen", false ).toBool() );
	cbVideoFormat->setCurrentIndex( cbVideoFormat->findData( s->value( "Video/VideoFormat", mVideoFromats.at( 0 ).id ).toInt() ) );

	// sound
	cbSoundCore->setCurrentIndex( cbSoundCore->findData( s->value( "Sound/SoundCore", QtYabause::defaultSNDCore().id ).toInt() ) );

	// cartridge/memory
	cbCartridge->setCurrentIndex( cbCartridge->findData( s->value( "Cartridge/Type", mCartridgeTypes.at( 0 ).id ).toInt() ) );
	leCartridge->setText( s->value( "Cartridge/Path" ).toString() );
	leMemory->setText( s->value( "Memory/Path", getDataDirPath().append( "/bkram.bin" ) ).toString() );
	leMpegROM->setText( s->value( "MpegROM/Path" ).toString() );
	
	// input
	cbInput->setCurrentIndex( cbInput->findData( s->value( "Input/PerCore", QtYabause::defaultPERCore().id ).toInt() ) );
	
	// advanced
	cbRegion->setCurrentIndex( cbRegion->findData( s->value( "Advanced/Region", mRegions.at( 0 ).id ).toString() ) );
	cbSH2Interpreter->setCurrentIndex( cbSH2Interpreter->findData( s->value( "Advanced/SH2Interpreter", QtYabause::defaultSH2Core().id ).toInt() ) );

	// view
	bgShowMenubar->setId( rbMenubarNever, 0 );
	bgShowMenubar->setId( rbMenubarFullscreen, 1 );
	bgShowMenubar->setId( rbMenubarAlways, 2 );
	bgShowMenubar->button( s->value( "View/Menubar", 0 ).toInt() )->setChecked( true );

	bgShowToolbar->setId( rbToolbarNever, 0 );
	bgShowToolbar->setId( rbToolbarFullscreen, 1 );
	bgShowToolbar->setId( rbToolbarAlways, 2 );
	bgShowToolbar->button( s->value( "View/Toolbar", 1 ).toInt() )->setChecked( true );
}

void UISettings::saveSettings()
{
	// get settings pointer
	Settings* s = QtYabause::settings();

	// general
	s->setValue( "General/Bios", leBios->text() );
	s->setValue( "General/CdRom", cbCdRom->itemData( cbCdRom->currentIndex() ).toInt() );
	s->setValue( "General/CdRomISO", leCdRom->text() );
	s->setValue( "General/SaveStates", leSaveStates->text() );
	s->setValue( "General/Translation", leTranslation->text() );
	s->setValue( "General/EnableFrameSkipLimiter", cbEnableFrameSkipLimiter->isChecked() );
	s->setValue( "General/ShowFPS", cbShowFPS->isChecked() );
	s->setValue( "autostart", cbAutostart->isChecked() );

	// video
	s->setValue( "Video/VideoCore", cbVideoCore->itemData( cbVideoCore->currentIndex() ).toInt() );
#if YAB_PORT_OSD
	s->setValue( "Video/OSDCore", cbOSDCore->itemData( cbOSDCore->currentIndex() ).toInt() );
#endif
	s->setValue( "Video/Width", leWidth->text() );
	s->setValue( "Video/Height", leHeight->text() );
	s->setValue( "Video/Fullscreen", cbFullscreen->isChecked() );
	s->setValue( "Video/VideoFormat", cbVideoFormat->itemData( cbVideoFormat->currentIndex() ).toInt() );

	// sound
	s->setValue( "Sound/SoundCore", cbSoundCore->itemData( cbSoundCore->currentIndex() ).toInt() );

	// cartridge/memory
	s->setValue( "Cartridge/Type", cbCartridge->itemData( cbCartridge->currentIndex() ).toInt() );
	s->setValue( "Cartridge/Path", leCartridge->text() );
	s->setValue( "Memory/Path", leMemory->text() );
	s->setValue( "MpegROM/Path", leMpegROM->text() );
	
	// input
	s->setValue( "Input/PerCore", cbInput->itemData( cbInput->currentIndex() ).toInt() );
	
	// advanced
	s->setValue( "Advanced/Region", cbRegion->itemData( cbRegion->currentIndex() ).toString() );
	s->setValue( "Advanced/SH2Interpreter", cbSH2Interpreter->itemData( cbSH2Interpreter->currentIndex() ).toInt() );

	// view
	s->setValue( "View/Menubar", bgShowMenubar->checkedId() );
	s->setValue( "View/Toolbar", bgShowToolbar->checkedId() );
}

void UISettings::accept()
{
	saveSettings();
	QDialog::accept();
}
