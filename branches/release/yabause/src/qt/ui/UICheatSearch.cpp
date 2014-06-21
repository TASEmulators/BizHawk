/*	Copyright 2012 Theo Berkau <cwx@cyberwarriorx.com>

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
#include "UICheatSearch.h"
#include "UICheatRaw.h"
#include "../CommonDialogs.h"

UICheatSearch::UICheatSearch( QWidget* p, result_struct *searchResults, 
   u32 numSearchResults, int searchType) : QDialog( p )
{
	// set up dialog
	setupUi( this );
	if ( p && !p->isFullScreen() )
		setWindowFlags( Qt::Sheet );

   this->searchResults = searchResults;
   this->numSearchResults = numSearchResults;
   this->searchType = searchType;

   // If cheat search hasn't been started yet, disable search and add
   // cheat
   if (searchResults == NULL)
   {
      pbRestart->setText(QtYabause::translate("Start"));
      pbSearch->setEnabled( false );
      pbAddCheat->setEnabled( false );
   }
      
   getSearchTypes();
   listResults();
	
	// retranslate widgets
	QtYabause::retranslateWidget( this );
}

void UICheatSearch::getSearchTypes()
{
   switch(searchType & 0xC)
   {
   case SEARCHEXACT:
      rbExact->setChecked(true);
      break;
   case SEARCHLESSTHAN:
      rbLessThan->setChecked(true);
      break;
   case SEARCHGREATERTHAN:
      rbGreaterThan->setChecked(true);
      break;
   default: break;
   }

   switch(searchType & 0x70)
   {
   case SEARCHUNSIGNED:
      rbUnsigned->setChecked(true);
      break;
   case SEARCHSIGNED:
      rbSigned->setChecked(true);
      break;
   default: break;
   }

   switch(searchType & 0x3)
   {
   case SEARCHBYTE:
      rb8Bit->setChecked(true);
      break;
   case SEARCHWORD:
      rb16Bit->setChecked(true);
      break;
   case SEARCHLONG:
      rb32Bit->setChecked(true);
      break;
   default: break;
   }
}

result_struct * UICheatSearch::getSearchVariables( u32* numSearchResults, int *searchType)
{
   *numSearchResults = this->numSearchResults;
   *searchType = this->searchType;
   return searchResults;
}

void UICheatSearch::setSearchTypes()
{
   searchType = 0;
   if (rbExact->isChecked())
      searchType |= SEARCHEXACT;
   else if (rbLessThan->isChecked())
      searchType |= SEARCHLESSTHAN;
   else
      searchType |= SEARCHGREATERTHAN;

   if (rbUnsigned->isChecked())
      searchType |= SEARCHUNSIGNED;
   else
      searchType |= SEARCHSIGNED;

   if (rb8Bit->isChecked())
      searchType |= SEARCHBYTE;
   else if (rb16Bit->isChecked())
      searchType |= SEARCHWORD;
   else
      searchType |= SEARCHLONG;
}

void UICheatSearch::listResults()
{
   u32 i;
   
   // Clear old info
   twSearchResults->clear();
   pbAddCheat->setEnabled(false);

   if (searchResults)
   {
      // Show results
      for (i = 0; i < numSearchResults; i++)
      {
         QTreeWidgetItem* it = new QTreeWidgetItem( twSearchResults );
         QString s;
         s.sprintf("%08X", searchResults[i].addr);
         it->setText( 0, s );

         switch(searchType & 0x3)
         {
         case SEARCHBYTE:
            s.sprintf("%d", MappedMemoryReadByte(searchResults[i].addr));
            break;
         case SEARCHWORD:
            s.sprintf("%d", MappedMemoryReadWord(searchResults[i].addr));
            break;
         case SEARCHLONG:
            s.sprintf("%d", MappedMemoryReadLong(searchResults[i].addr));
            break;
         default: break;
         }
         it->setText( 1, s );
      }
   }
}

void UICheatSearch::adjustSearchValueQValidator()
{
   // Figure out range
   int min, max;

   min = 0;

   if (rb8Bit->isChecked())
      max = 0xFF;
   else if (rb16Bit->isChecked())
      max = 0xFFFF;
   else
      max = 0xFFFFFFFF;

   if (rbSigned->isChecked())
   {
      min = -(max >> 1) + 1;
      max >>= 1;
   }

   leSearchValue->setValidator(new QIntValidator(min, max, leSearchValue));
}

void UICheatSearch::on_twSearchResults_itemSelectionChanged()
{ 
   pbAddCheat->setEnabled( twSearchResults->selectedItems().count() ); 
}

void UICheatSearch::on_rbUnsigned_toggled(bool checked)
{
   if (checked)
      adjustSearchValueQValidator();
}

void UICheatSearch::on_rbSigned_toggled(bool checked)
{
   if (checked)
      adjustSearchValueQValidator();
}

void UICheatSearch::on_rb8Bit_toggled(bool checked)
{
   if (checked)
      adjustSearchValueQValidator();
}

void UICheatSearch::on_rb16Bit_toggled(bool checked)
{
   if (checked)
      adjustSearchValueQValidator();
}

void UICheatSearch::on_rb32Bit_toggled(bool checked)
{
   if (checked)
      adjustSearchValueQValidator();
}

void UICheatSearch::on_pbRestart_clicked()
{   
   // If there were search result, clear them, otherwise adjust GUI
   if (searchResults == NULL)
   {
      pbRestart->setText(QtYabause::translate("Restart"));
      pbSearch->setEnabled(true);
   }
   else
      free(searchResults);

   // Setup initial values
   numSearchResults = 0x100000;
   twSearchResults->clear();
}

void UICheatSearch::on_pbSearch_clicked()
{
   // Search low wram and high wram areas
   setSearchTypes();
   
   searchResults = MappedMemorySearch(0x06000000, 0x06100000, searchType,
      leSearchValue->text().toLatin1(), searchResults, &numSearchResults);

   listResults();
}

void UICheatSearch::on_pbAddCheat_clicked()
{
   UICheatRaw d( this );
   QString s;

   // Insert current address/values into dialog
   QTreeWidgetItem *currentItem = twSearchResults->currentItem();
   d.leAddress->setText(currentItem->text(0));
   s.sprintf("%X", currentItem->text(1).toUInt());
   d.leValue->setText(s);
   d.rbByte->setChecked(rb8Bit->isChecked());
   d.rbWord->setChecked(rb16Bit->isChecked());
   d.rbLong->setChecked(rb32Bit->isChecked());
   if ( d.exec())
   {
      if ( CheatAddCode( d.type(), d.leAddress->text().toUInt(), d.leValue->text().toUInt() ) != 0 )
      {
         CommonDialogs::information( QtYabause::translate( "Unable to add code" ) );
         return;
      }
      else
      {
         cheatlist_struct *mCheats;
         int cheatsCount;
         mCheats = CheatGetList( &cheatsCount );

         CheatChangeDescriptionByIndex( cheatsCount -1, d.leDescription->text().toAscii().data() );
      }
   }
}
