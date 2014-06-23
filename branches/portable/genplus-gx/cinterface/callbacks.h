#ifndef CALLBACKS_H
#define CALLBACKS_H

extern void (*biz_execcb)(unsigned addr);
extern void (*biz_readcb)(unsigned addr);
extern void (*biz_writecb)(unsigned addr);

#endif
