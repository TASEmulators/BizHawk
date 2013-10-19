/*	Copyright 2008 Filipe Azevedo <pasnox@gmail.com>

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
#include "UIPadSetting.h"
#include "UIPortManager.h"
#include "../Settings.h"

#include <QKeyEvent>
#include <QTimer>
#include <QStylePainter>
#include <QStyleOptionToolButton>

UIPadSetting::UIPadSetting( PerInterface_struct* core, PerPad_struct* padbits, uint port, uint pad, QWidget* parent )
	: QDialog( parent )
{
	Q_ASSERT( core );
	Q_ASSERT( padbits );
	
	setupUi( this );
	mCore = core;
	mPadBits = padbits;
	mPort = port;
	mPad = pad;
	mTimer = new QTimer( this );
	mTimer->setInterval( 25 );
	
	mPadButtons[ tbUp ] = PERPAD_UP;
	mPadButtons[ tbRight ] = PERPAD_RIGHT;
	mPadButtons[ tbDown ] = PERPAD_DOWN;
	mPadButtons[ tbLeft ] = PERPAD_LEFT;
	mPadButtons[ tbRightTrigger ] = PERPAD_RIGHT_TRIGGER;
	mPadButtons[ tbLeftTrigger ] = PERPAD_LEFT_TRIGGER;
	mPadButtons[ tbStart ] = PERPAD_START;
	mPadButtons[ tbA ] = PERPAD_A;
	mPadButtons[ tbB ] = PERPAD_B;
	mPadButtons[ tbC ] = PERPAD_C;
	mPadButtons[ tbX ] = PERPAD_X;
	mPadButtons[ tbY ] = PERPAD_Y;
	mPadButtons[ tbZ ] = PERPAD_Z;
	
	mPadNames[ PERPAD_UP ] = QtYabause::translate( "Up" );
	mPadNames[ PERPAD_RIGHT ] = QtYabause::translate( "Right" );
	mPadNames[ PERPAD_DOWN ] = QtYabause::translate( "Down" );
	mPadNames[ PERPAD_LEFT ] = QtYabause::translate( "Left" );
	mPadNames[ PERPAD_RIGHT_TRIGGER ] = QtYabause::translate( "Right trigger" );;
	mPadNames[ PERPAD_LEFT_TRIGGER ] = QtYabause::translate( "Left trigger" );;
	mPadNames[ PERPAD_START ] = "Start";
	mPadNames[ PERPAD_A ] = "A";
	mPadNames[ PERPAD_B ] = "B";
	mPadNames[ PERPAD_C ] = "C";
	mPadNames[ PERPAD_X ] = "X";
	mPadNames[ PERPAD_Y ] = "Y";
	mPadNames[ PERPAD_Z ] = "Z";
	
	loadPadSettings();
	
	foreach ( QToolButton* tb, findChildren<QToolButton*>() )
	{
		tb->installEventFilter( this );
		connect( tb, SIGNAL( clicked() ), this, SLOT( tbButton_clicked() ) );
	}
	
	connect( mTimer, SIGNAL( timeout() ), this, SLOT( timer_timeout() ) );

	QtYabause::retranslateWidget( this );
}

UIPadSetting::~UIPadSetting()
{
}

void UIPadSetting::keyPressEvent( QKeyEvent* e )
{
	if ( mTimer->isActive() )
	{
		if ( e->key() != Qt::Key_Escape )
		{
			setPadKey( e->key() );
		}
		else
		{
			e->ignore();
			mPadButtons.key( mPadKey )->setChecked( false );
			lInfos->clear();
			mTimer->stop();
		}
	}
	else if ( e->key() == Qt::Key_Escape )
	{
		reject();
	}
	else
	{
		QWidget::keyPressEvent( e );
	}
}

void UIPadSetting::setPadKey( u32 key )
{
	const QString settingsKey = QString( UIPortManager::mSettingsKey )
		.arg( mPort )
		.arg( mPad )
		.arg( PERPAD )
		.arg( mPadKey );
	
	QtYabause::settings()->setValue( settingsKey, (quint32)key );
	mPadButtons.key( mPadKey )->setIcon( QIcon( ":/actions/icons/actions/button_ok.png" ) );
	mPadButtons.key( mPadKey )->setChecked( false );
	lInfos->clear();
	mTimer->stop();
}

void UIPadSetting::loadPadSettings()
{
	Settings* settings = QtYabause::settings();
	
	foreach ( const u8& name, mPadNames.keys() )
	{
		mPadKey = name;
		const QString settingsKey = QString( UIPortManager::mSettingsKey )
			.arg( mPort )
			.arg( mPad )
			.arg( PERPAD )
			.arg( mPadKey );
		
		if ( settings->contains( settingsKey ) )
		{
			setPadKey( settings->value( settingsKey ).toUInt() );
		}
	}
}

bool UIPadSetting::eventFilter( QObject* object, QEvent* event )
{
	if ( event->type() == QEvent::Paint )
	{
		QToolButton* tb = qobject_cast<QToolButton*>( object );
		
		if ( tb )
		{
			if ( tb->isChecked() )
			{
				QStylePainter sp( tb );
				QStyleOptionToolButton options;
				
				options.initFrom( tb );
				options.arrowType = Qt::NoArrow;
				options.features = QStyleOptionToolButton::None;
				options.icon = tb->icon();
				options.iconSize = tb->iconSize();
				options.state = QStyle::State_Enabled | QStyle::State_HasFocus | QStyle::State_On | QStyle::State_AutoRaise;
				
				sp.drawComplexControl( QStyle::CC_ToolButton, options );
				
				return true;
			}
		}
	}
	
	return false;
}

void UIPadSetting::tbButton_clicked()
{
	QToolButton* tb = qobject_cast<QToolButton*>( sender() );
	
	if ( !mTimer->isActive() )
	{
		tb->setChecked( true );
		mPadKey = mPadButtons[ tb ];
		
		lInfos->setText( QtYabause::translate( QString( "Waiting input for : %1\nEscape for cancel" ).arg( mPadNames[ mPadKey ] ) ) );
		mTimer->start();
	}
	else
	{
		tb->setChecked( tb == mPadButtons.key( mPadKey ) );
	}
}

void UIPadSetting::timer_timeout()
{
	u32 key = 0;
	mCore->Flush();
	key = mCore->Scan();
	
	if ( key != 0 )
	{
		setPadKey( key );
	}
}
