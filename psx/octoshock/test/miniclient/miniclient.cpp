#include <stdio.h>

#include "octoshock.h"
#include "psx/psx.h"

// lookup table for crc calculation
static uint16 subq_crctab[256] = 
{
  0x0000, 0x1021, 0x2042, 0x3063, 0x4084, 0x50A5, 0x60C6, 0x70E7, 0x8108,
  0x9129, 0xA14A, 0xB16B, 0xC18C, 0xD1AD, 0xE1CE, 0xF1EF, 0x1231, 0x0210,
  0x3273, 0x2252, 0x52B5, 0x4294, 0x72F7, 0x62D6, 0x9339, 0x8318, 0xB37B,
  0xA35A, 0xD3BD, 0xC39C, 0xF3FF, 0xE3DE, 0x2462, 0x3443, 0x0420, 0x1401,
  0x64E6, 0x74C7, 0x44A4, 0x5485, 0xA56A, 0xB54B, 0x8528, 0x9509, 0xE5EE,
  0xF5CF, 0xC5AC, 0xD58D, 0x3653, 0x2672, 0x1611, 0x0630, 0x76D7, 0x66F6,
  0x5695, 0x46B4, 0xB75B, 0xA77A, 0x9719, 0x8738, 0xF7DF, 0xE7FE, 0xD79D,
  0xC7BC, 0x48C4, 0x58E5, 0x6886, 0x78A7, 0x0840, 0x1861, 0x2802, 0x3823,
  0xC9CC, 0xD9ED, 0xE98E, 0xF9AF, 0x8948, 0x9969, 0xA90A, 0xB92B, 0x5AF5,
  0x4AD4, 0x7AB7, 0x6A96, 0x1A71, 0x0A50, 0x3A33, 0x2A12, 0xDBFD, 0xCBDC,
  0xFBBF, 0xEB9E, 0x9B79, 0x8B58, 0xBB3B, 0xAB1A, 0x6CA6, 0x7C87, 0x4CE4,
  0x5CC5, 0x2C22, 0x3C03, 0x0C60, 0x1C41, 0xEDAE, 0xFD8F, 0xCDEC, 0xDDCD,
  0xAD2A, 0xBD0B, 0x8D68, 0x9D49, 0x7E97, 0x6EB6, 0x5ED5, 0x4EF4, 0x3E13,
  0x2E32, 0x1E51, 0x0E70, 0xFF9F, 0xEFBE, 0xDFDD, 0xCFFC, 0xBF1B, 0xAF3A,
  0x9F59, 0x8F78, 0x9188, 0x81A9, 0xB1CA, 0xA1EB, 0xD10C, 0xC12D, 0xF14E,
  0xE16F, 0x1080, 0x00A1, 0x30C2, 0x20E3, 0x5004, 0x4025, 0x7046, 0x6067,
  0x83B9, 0x9398, 0xA3FB, 0xB3DA, 0xC33D, 0xD31C, 0xE37F, 0xF35E, 0x02B1,
  0x1290, 0x22F3, 0x32D2, 0x4235, 0x5214, 0x6277, 0x7256, 0xB5EA, 0xA5CB,
  0x95A8, 0x8589, 0xF56E, 0xE54F, 0xD52C, 0xC50D, 0x34E2, 0x24C3, 0x14A0,
  0x0481, 0x7466, 0x6447, 0x5424, 0x4405, 0xA7DB, 0xB7FA, 0x8799, 0x97B8,
  0xE75F, 0xF77E, 0xC71D, 0xD73C, 0x26D3, 0x36F2, 0x0691, 0x16B0, 0x6657,
  0x7676, 0x4615, 0x5634, 0xD94C, 0xC96D, 0xF90E, 0xE92F, 0x99C8, 0x89E9,
  0xB98A, 0xA9AB, 0x5844, 0x4865, 0x7806, 0x6827, 0x18C0, 0x08E1, 0x3882,
  0x28A3, 0xCB7D, 0xDB5C, 0xEB3F, 0xFB1E, 0x8BF9, 0x9BD8, 0xABBB, 0xBB9A,
  0x4A75, 0x5A54, 0x6A37, 0x7A16, 0x0AF1, 0x1AD0, 0x2AB3, 0x3A92, 0xFD2E,
  0xED0F, 0xDD6C, 0xCD4D, 0xBDAA, 0xAD8B, 0x9DE8, 0x8DC9, 0x7C26, 0x6C07,
  0x5C64, 0x4C45, 0x3CA2, 0x2C83, 0x1CE0, 0x0CC1, 0xEF1F, 0xFF3E, 0xCF5D,
  0xDF7C, 0xAF9B, 0xBFBA, 0x8FD9, 0x9FF8, 0x6E17, 0x7E36, 0x4E55, 0x5E74,
  0x2E93, 0x3EB2, 0x0ED1, 0x1EF0
};

#ifndef __PACKED
	#ifdef __GNUC__
	#define __PACKED __attribute__((__packed__))
	#else
	#define __PACKED
	#endif
#endif

#ifndef __GNUC__
#pragma pack(push, 1)
#pragma warning(disable : 4103)
#endif
struct bmpimgheader_struct
{
	u32 size;
	s32 width;
	s32 height;
	u16 planes;
	u16 bpp;
	u32 cmptype;
	u32 imgsize;
	s32 hppm;
	s32 vppm;
	u32 numcol;
	u32 numimpcol;
} ;
struct bmpfileheader_struct
{
	u16 id __PACKED;
	u32 size __PACKED;
	u16 reserved1 __PACKED;
	u16 reserved2 __PACKED;
	u32 imgoffset __PACKED;
};
#ifndef __GNUC__
#pragma pack(pop)
#endif

int WriteBMP32(int width, int height, const void* buf, const char *filename)
{
	bmpfileheader_struct fileheader;
	bmpimgheader_struct imageheader;
	FILE *file;
	size_t elems_written = 0;
	memset(&fileheader, 0, sizeof(fileheader));
	fileheader.size = sizeof(fileheader);
	fileheader.id = 'B' | ('M' << 8);
	fileheader.imgoffset = sizeof(fileheader)+sizeof(imageheader);

	memset(&imageheader, 0, sizeof(imageheader));
	imageheader.size = sizeof(imageheader);
	imageheader.width = width;
	imageheader.height = height;
	imageheader.planes = 1;
	imageheader.bpp = 32;
	imageheader.cmptype = 0; // None
	imageheader.imgsize = imageheader.width * imageheader.height * 4;

	if ((file = fopen(filename,"wb")) == NULL)
		return 0;

	elems_written += fwrite(&fileheader, 1, sizeof(fileheader), file);
	elems_written += fwrite(&imageheader, 1, sizeof(imageheader), file);

	for(int i=0;i<height;i++)
		for(int x=0;x<width;x++)
		{
			u8* pixel = (u8*)buf + (height-i-1)*width*4;
			pixel += (x*4);
			elems_written += fwrite(pixel+2,1,1,file);
			elems_written += fwrite(pixel+1,1,1,file);
			elems_written += fwrite(pixel+0,1,1,file);
			elems_written += fwrite(pixel+3,1,1,file);
		}
	fclose(file);

	return 1;
}

class BinReader2352
{
public:
	BinReader2352(const char* path)
	{
		inf = fopen(path,"rb");
		fseek(inf,0,SEEK_END);
		size_t sz = ftell(inf);
		fseek(inf,0,SEEK_SET);
		lbaCount = (int)(sz/2352);

		shock_CreateDisc(&disc,this,lbaCount,s_ReadTOC,s_ReadLBA2448,false);
	}

	ShockDiscRef* disc;

	static s32 s_ReadTOC(void* opaque, ShockTOC *read_target, ShockTOCTrack tracks[100 + 1]) { return ((BinReader2352*)opaque)->ReadTOC(read_target, tracks); }
	static s32 s_ReadLBA2448(void* opaque, s32 lba, void* dst) { return ((BinReader2352*)opaque)->ReadLBA2448(lba,dst); }

	~BinReader2352()
	{
		fclose(inf);
		shock_DestroyDisc(disc);
	}

private:
	int lbaCount;
	FILE* inf;


	union Sector {
		struct {
			u8 sync[12];
			u8 adr[3];
			u8 mode;
			union {
				struct {
					u8 data2048[2048];
					u8 ecc[4];
					u8 reserved[8];
					u8 ecm[276];
				};
				u8 data2336[2336];
			};
		};
		u8 buf[2352];
	};

	union XASector {
		struct {
			u8 sync[12];
			u8 adr[3];
			u8 mode;
			u8 subheader[8];
			union {
				u8 data2048[2048];
				u8 ecc[4];
				u8 ecm[276];
			} form1;
			union {
				u8 data2334[2334];
				u8 ecc[4];
			} form2;
		};
		u8 buf[2352];
	};

	union {
		XASector xasector;
		Sector sector;
	};


	s32 ReadTOC( ShockTOC *read_target, ShockTOCTrack tracks[100 + 1]) 
	{
		memset(read_target,0,sizeof(*read_target));
		read_target->disc_type = 0;
		read_target->first_track = 1;
		read_target->last_track = 1;
		tracks[1].adr = 1;
		tracks[1].lba = 0;
		tracks[1].control = 4;
		tracks[2].adr = 1;
		tracks[2].lba = lbaCount;
		tracks[2].control = 0;
		tracks[100].adr = 1;
		tracks[100].lba = lbaCount;
		tracks[100].control = 0;
		return SHOCK_OK;
	}

	s32 ReadLBA2448(s32 lba, void* dst)
	{
		fseek(inf,lba*2352,SEEK_SET);
		fread(dst,1,2352,inf);
		//do something for subcode I guess
		memset((u8*)dst+2352,0,96);
		hacky_MakeSubPQ(lba,(u8*)dst+2352,1,0);

		//not the right thing to do
		//return ((Sector*)dst)->mode;

		return SHOCK_OK;
	}

	 uint8 U8_to_BCD(uint8 num)
	 {
		return( ((num / 10) << 4) + (num % 10) );
	 }

	 void subq_generate_checksum(uint8 *buf)
	 {
		 uint16 crc = 0;

		 for(int i = 0; i < 0xA; i++)
			 crc = subq_crctab[(crc >> 8) ^ buf[i]] ^ (crc << 8);

		 // Checksum
		 buf[0xa] = ~(crc >> 8);
		 buf[0xb] = ~(crc);
	 }
	 void hacky_MakeSubPQ(int lba, u8* SubPWBuf, int track, int track_start)
	 {
		 uint8 buf[0xC];
		 uint32 lba_relative;
		 uint32 ma, sa, fa;
		 uint32 m, s, f;
		 uint8 pause_or = 0x00;

		 lba_relative = abs((int32)lba - track_start);

		 f = (lba_relative % 75);
		 s = ((lba_relative / 75) % 60);
		 m = (lba_relative / 75 / 60);

		 fa = (lba + 150) % 75;
		 sa = ((lba + 150) / 75) % 60;
		 ma = ((lba + 150) / 75 / 60);

		 uint8 adr = 0x1; // Q channel data encodes position

		 memset(buf, 0, 0xC);
		 buf[0] = (adr << 0) | (0x04 << 4);
		 buf[1] = U8_to_BCD(track);

		 buf[2] = U8_to_BCD(0x01);

		 // Track relative MSF address
		 buf[3] = U8_to_BCD(m);
		 buf[4] = U8_to_BCD(s);
		 buf[5] = U8_to_BCD(f);

		 buf[6] = 0; // Zerroooo

		 // Absolute MSF address
		 buf[7] = U8_to_BCD(ma);
		 buf[8] = U8_to_BCD(sa);
		 buf[9] = U8_to_BCD(fa);

		 subq_generate_checksum(buf);

		 for(int i = 0; i < 96; i++)
			 SubPWBuf[i] |= (((buf[i >> 3] >> (7 - (i & 0x7))) & 1) ? 0x40 : 0x00) | pause_or;
	 }


};

int main(int argc, char **argv)
{
	const char* fwpath = argv[1];
	const char* discpath = argv[2];
	const char* outdir = argv[3];

	FILE* inf;
	
	//load up the firmware
	char firmware[512*1024];
	inf = fopen(fwpath,"rb");
	fread(firmware,1,512*1024,inf);
	fclose(inf);

	BinReader2352 bin(discpath);
	ShockDiscInfo info;
	shock_AnalyzeDisc(bin.disc, &info);
	printf("disc id: %s\n",info.id);

	//placeholder for instance
	void* psx = NULL;

	shock_Create(&psx, REGION_NA, firmware);
	shock_OpenTray(psx);
	shock_SetDisc(psx,bin.disc);
	shock_CloseTray(psx);
	shock_Peripheral_Connect(psx,0x01,ePeripheralType_DualShock);
	shock_PowerOn(psx);

	int framectr = 0;
	for(;;)
	{
		printf("frame %d\n",framectr);
		shock_Step(psx,eShockStep_Frame);
		if(framectr%60==0)
		{
			//dump a screen grab
			ShockFramebufferInfo fbinfo;
			static u32 buf[1024*1024];
			fbinfo.ptr = buf;
			fbinfo.flags = eShockFramebufferFlags_Normalize;
			shock_GetFramebuffer(psx,&fbinfo);
			char fname[128];
			sprintf(fname,"%s\\test%03d.bmp",outdir,framectr/60);
			WriteBMP32(fbinfo.width,fbinfo.height,buf,fname); //rgb is backwards
		}

		framectr++;
	}
}