#ifndef __MDFN_SS_DB_H
#define __MDFN_SS_DB_H

namespace MDFN_IEN_SS
{

bool DB_LookupRegionDB(const uint8* fd_id, unsigned* const region);
bool DB_LookupCartDB(const char* sgid, const uint8* fd_id, int* const cart_type);


}


#endif

