#ifndef __SCORE_H
#define __SCORE_H

int get_score(int scoretype, int scoreaddress);
void set_score(int scoretype, int scoreaddress, int highscore);
void save_highscore(int highscore,char *scorefile);
int power(int base, int higher);

#endif

