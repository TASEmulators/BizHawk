
/*
 *   O2EM Free Odyssey2 / Videopac+ Emulator
 *
 *   Created by Daniel Boris <dboris@comcast.net>  (c) 1997,1998
 *
 *   Developed by Andre de la Rocha <adlroc@users.sourceforge.net>
 *
 *   http://o2em.sourceforge.net
 *
 *
 *
 *   Score loading/saving by manopac
 */

#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <ctype.h>
#include <time.h>
#include <errno.h>
#include "vmachine.h"
#include "types.h"
#include "score.h"

/*--------------------------------------------------
          Calculate Score from given Values 
             Scoretype = abcd:
	ramtype		a = 1: ext ram / 2: int ram
	valuetype	b = 1: 1 Byte each Digit  / 2: 1/2 Byte each Digit
	direct�on	c = 1: higher value digits first /  2: low value digits first
	count		d = number of digits
  --------------------------------------------------*/
int get_score(int scoretype, int scoreaddress)
{
	int score=0;

	if (scoretype!=0)
	{
		int position;
		int i;
		Byte *RAM;

		int count = scoretype%10;
		int direction = ((scoretype/10)%10)==1?1:-1;
		float valuetype = (float) (3-((scoretype/100)%10))/2;
		int ramtype = scoretype/1000;

		position = scoreaddress+ (direction==1?0:(count*valuetype-1));
		RAM = ramtype==1?extRAM:intRAM;

		for(i=0;i<count;i++)
		{
			score = score*10+((RAM[position+(int)(valuetype*i*direction)]>>(((i+1)%2)*4)*(abs((int) ((valuetype-1)*2))))&15);
		}
	}

	return(score);
}

/*--------------------------------------------------
      Set HighScore into Memory
             Scoretype = abcd:
	ramtype		a = 1: ext ram / 2: int ram
	valuetype	b = 1: 1 Byte each Digit  / 2: 1/2 Byte each Digit
	direct�on	c = 1: higher value digits first /  2: low value digits first
	count		d = number of digits
  --------------------------------------------------*/

void set_score(int scoretype, int scoreaddress, int score)
{

	if (scoretype!=0 && score>0)
	{
		int position;
		int i;
		Byte *RAM;
		int digit;

		int count = scoretype%10;
		int direction = ((scoretype/10)%10)==1?-1:1;
		float valuetype = (float) (3-((scoretype/100)%10))/2;
		int ramtype = scoretype/1000;

		position = scoreaddress+ (direction==1?0:(count*valuetype-1));
		RAM = ramtype==1?extRAM:intRAM;

		for(i=count-1;i>=0;i--)
		{
			digit = score / power(10,i);
			RAM[position+(int)(valuetype*i*direction)]=((valuetype==0.5)&&(i%2==0))?(RAM[position+(int)(valuetype*i*direction)]<<4)+digit:digit;
			score = score - digit*power(10,i);
		}
	}
}



/*-----------------------------------------------------
	Save Highscore to File
-------------------------------------------------------*/
void save_highscore(int highscore,char *scorefile)
{
	FILE *fn;

	highscore = highscore==app_data.default_highscore?0:highscore;

        fn = fopen(scorefile,"w");
	if (fn==NULL) {
		fprintf(stderr,"Error opening highscore-file %s: %i\n",scorefile,errno);
		exit(EXIT_FAILURE);
	}
	
	if (fprintf(fn,"%i",highscore)<=0)
	{
		fprintf(stderr,"Error writing to highscore-file %s: %i\n",scorefile,errno);
		exit(EXIT_FAILURE);
	}	

	fclose(fn);
}


/***********************************
   Integer-Implementation of pow
 ***********************************/
int power(int base, int higher)
{
	if (higher==0) {
		return(1);
	} else if (higher==1) {
		return(base);
	} else {
		int i;
		int value=base;

		for (i=2;i<=higher;i++)
		{
			value = value*base;
		}
		return(value);
	}
}
