#ifndef f_AT_VIDEOWRITER_H
#define f_AT_VIDEOWRITER_H

#include "gtia.h"
#include "audiooutput.h"

struct VDPixmap;
class VDFraction;

enum ATVideoEncoding {
	kATVideoEncoding_Raw,
	kATVideoEncoding_RLE,
	kATVideoEncoding_ZMBV,
	kATVideoEncodingCount
};

class IATVideoWriter : public IATGTIAVideoTap, public IATAudioTap {
public:
	virtual ~IATVideoWriter() {}

	virtual void CheckExceptions() = 0;

	virtual void Init(const wchar_t *filename, ATVideoEncoding venc, uint32 w, uint32 h, const VDFraction& frameRate, const uint32 *palette, double samplingRate, bool stereo, double timestampRate, bool halfRate, bool encodeAllFrames, IATUIRenderer *r) = 0;
	virtual void Shutdown() = 0;
};

void ATCreateVideoWriter(IATVideoWriter **w);

#endif	// f_AT_VIDEOWRITER_H
