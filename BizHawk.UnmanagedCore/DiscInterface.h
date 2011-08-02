#ifndef _DISCINTERFACE_H_
#define _DISCINTERFACE_H_

#include "DiscInterface.h"
#include "core.h"

class DiscInterface
{
	void* ManagedOpaque;

public:

	enum ETrackType : int
	{
		ETrackType_Mode1_2352,
		ETrackType_Mode1_2048,
		ETrackType_Mode2_2352,
		ETrackType_Audio
	};

	struct TrackInfo
	{
		ETrackType TrackType;
		int length_lba;
		int start_lba;
	};

	FUNC<void()> Dispose;
	FUNC<int()> GetNumSessions;
	FUNC<int(int)> GetNumTracks;
	FUNC<TrackInfo(int,int)> GetTrack;

	~DiscInterface()
	{
		Dispose.func();
	}
	
	DiscInterface(void* _ManagedOpaque)
		: ManagedOpaque(_ManagedOpaque)
	{
	}

	void* Construct(void* ManagedOpaque);

	void Delete()
	{
		delete this;
	}

	void Set_fp(const char* param, void* value)
	{
		if(!strcmp(param,"GetNumSessions")) GetNumSessions.set(value);
		if(!strcmp(param,"GetNumTracks")) GetNumTracks.set(value);
		if(!strcmp(param,"GetTrack")) GetTrack.set(value);
		if(!strcmp(param,"Dispose")) Dispose.set(value);
	}

};


#endif //_DISCINTERFACE_H_
