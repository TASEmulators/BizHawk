extern volatile bool cd_audio_mode_changed;

class cda_audio {
private:
    int bufsize;
    int volume[2];
    bool playing;
    bool active;
    int buffer_ids[2];

public:
    unsigned char *buffers[2];
#if 0
    uae_thread_id mThread;
    volatile int mBufferDone[2];
    int mStopThread;
#endif
    int num_sectors;
	int sectorsize;

	cda_audio(int num_sectors, int sectorsize, int samplerate, bool internalmode);
	~cda_audio();
	void setvolume(int left, int right);
	bool play(int bufnum);
	void wait(void);
	void wait(int bufnum);
	bool isplaying(int bufnum);
};
