class vdint2 {
public:
	typedef vdint2 self_type;
	typedef int value_type;

	void set(int x2, int y2) { x=x2; y=y2; }

	int			lensq() const						{ return x*x + y*y; }
	int			len() const							{ return (int)sqrtf((float)(x*x + y*y)); }
	self_type	normalized() const					{ return *this / len(); }

	self_type	operator-() const					{ const self_type a = {-x, -y}; return a; }

	self_type	operator+(const self_type& r) const	{ const self_type a = {x+r.x, y+r.y}; return a; }
	self_type	operator-(const self_type& r) const	{ const self_type a = {x-r.x, y-r.y}; return a; }

	self_type&	operator+=(const self_type& r)		{ x+=r.x; y+=r.y; return *this; }
	self_type&	operator-=(const self_type& r)		{ x-=r.x; y-=r.y; return *this; }

	self_type	operator*(const int s) const		{ const self_type a = {x*s, x*s}; return a; }
	self_type&	operator*=(const int s)				{ x*=s; y*=s; return *this; }

	self_type	operator/(const int s) const		{ const self_type a = {x/s, y/s}; return a; }
	self_type&	operator/=(const int s)				{ x/=s; y/=s; return *this; }

	self_type	operator*(const self_type& r) const		{ self_type a = {x*r.x, y*r.y}; return a; }
	self_type&	operator*=(const self_type& r)			{ x*=r.x; y*=r.y; return *this; }

	self_type	operator/(const self_type& r) const		{ self_type a = {x/r.x, y/r.y}; return a; }
	self_type&	operator/=(const self_type& r)			{ x/=r.x; y/=r.y; return *this; }

	int x;
	int y;
};

VDFORCEINLINE vdint2 operator*(const int s, const vdint2& v) { return v*s; }

///////////////////////////////////////////////////////////////////////////

class vdint3 {
public:
	typedef vdint3 self_type;
	typedef sint32 value_type;

	int			lensq() const						{ return x*x + y*y + z*z; }
	int			len() const							{ return (int)sqrtf((float)(x*x + y*y + z*z)); }
	self_type	normalized() const					{ return *this / len(); }

	vdint2		project() const						{ const int inv(int(1)/z); const vdint2 a = {x*inv, y*inv}; return a; }
	vdint2		as2d() const						{ const vdint2 a = {x, y}; return a; }

	vdint3		operator+() const					{ return *this; }
	self_type	operator-() const					{ const self_type a = {-x, -y, -z}; return a; }

	self_type	operator+(const self_type& r) const	{ const self_type a = {x+r.x, y+r.y, z+r.z}; return a; }
	self_type	operator-(const self_type& r) const	{ const self_type a = {x-r.x, y-r.y, z-r.z}; return a; }

	self_type&	operator+=(const self_type& r)		{ x+=r.x; y+=r.y; z+=r.z; return *this; }
	self_type&	operator-=(const self_type& r)		{ x-=r.x; y-=r.y; z-=r.z; return *this; }

	self_type	operator*(const int s) const		{ const self_type a = {x*s, y*s, z*s}; return a; }
	self_type&	operator*=(const int s)				{ x*=s; y*=s; z*=s; return *this; }

	self_type	operator/(const int s) const		{ const self_type a = {x/s, y/s, z/s}; return a; }
	self_type&	operator/=(const int s)				{ x /= s; y /= s; z /= s; return *this; }

	self_type	operator*(const self_type& r) const	{ self_type a = {x*r.x, y*r.y, z*r.z}; return a; }
	self_type&	operator*=(const self_type& r)		{ x*=r.x; y*=r.y; z*=r.z; return *this; }

	self_type	operator/(const self_type& r) const	{ self_type a = {x/r.x, y/r.y, z/r.z}; return a; }
	self_type&	operator/=(const self_type& r)		{ x/=r.x; y/=r.y; z/=r.z; return *this; }

	vdint3		operator<<(int amount) const		{ return vdint3 { x << amount, y << amount, z << amount }; }
	vdint3&		operator<<=(int amount)				{ x <<= amount; y <<= amount; z <<= amount; return *this; }

	vdint3		operator>>(int amount) const		{ return vdint3 { x >> amount, y >> amount, z >> amount }; }
	vdint3&		operator>>=(int amount)				{ x >>= amount; y >>= amount; z >>= amount; return *this; }

	sint32 x;
	sint32 y;
	sint32 z;
};

VDFORCEINLINE vdint3 operator*(const sint32 s, const vdint3& v) { return v*s; }

///////////////////////////////////////////////////////////////////////////

class vdint4 {
public:
	typedef vdint4 self_type;
	typedef int value_type;

	int			lensq() const						{ return x*x + y*y + z*z + w*w; }
	int			len() const							{ return (int)sqrtf((float)(x*x + y*y + z*z + w*w)); }
	self_type	normalized() const					{ return *this / len(); }

	vdint3	project() const						{ const int inv(int(1)/w); const vdint3 a = {x*inv, y*inv, z*inv}; return a; }

	self_type	operator-() const					{ const self_type a = {-x, -y, -z, -w}; return a; }

	self_type	operator+(const self_type& r) const	{ const self_type a = {x+r.x, y+r.y, z+r.z, w+r.w}; return a; }
	self_type	operator-(const self_type& r) const	{ const self_type a = {x-r.x, y-r.y, z-r.z, w-r.w}; return a; }

	self_type&	operator+=(const self_type& r)		{ x+=r.x; y+=r.y; z+=r.z; w+=r.w; return *this; }
	self_type&	operator-=(const self_type& r)		{ x-=r.x; y-=r.y; z-=r.z; w-=r.w; return *this; }

	self_type	operator*(const int factor) const	{ const self_type a = {x*factor, y*factor, z*factor, w*factor}; return a; }
	self_type	operator/(const int factor) const	{ const self_type a = {x/factor, y/factor, z/factor, w/factor}; return a; }

	self_type&	operator*=(const int factor)		{ x *= factor; y *= factor; z *= factor; w *= factor; return *this; }
	self_type&	operator/=(const int factor)		{ x /= factor; y /= factor; z /= factor; w /= factor; return *this; }

	self_type	operator*(const self_type& r) const		{ self_type a = {x*r.x, y*r.y, z*r.z, w*r.w}; return a; }
	self_type&	operator*=(const self_type& r)			{ x*=r.x; y*=r.y; z*=r.z; w*=r.w; return *this; }

	self_type	operator/(const self_type& r) const		{ self_type a = {x/r.x, y/r.y, z/r.z, w*r.w}; return a; }
	self_type&	operator/=(const self_type& r)			{ x/=r.x; y/=r.y; z/=r.z; w/=r.w; return *this; }

	int x;
	int y;
	int z;
	int w;
};

VDFORCEINLINE vdint4 operator*(const int s, const vdint4& v) { return v*s; }

///////////////////////////////////////////////////////////////////////////

namespace nsVDMath {
	VDFORCEINLINE int dot(const vdint2& a, const vdint2& b) {
		return a.x*b.x + a.y*b.y;
	}

	VDFORCEINLINE int dot(const vdint3& a, const vdint3& b) {
		return a.x*b.x + a.y*b.y + a.z*b.z;
	}

	VDFORCEINLINE int dot(const vdint4& a, const vdint4& b) {
		return a.x*b.x + a.y*b.y + a.z*b.z + a.w*b.w;
	}

	VDFORCEINLINE vdint3 cross(const vdint3& a, const vdint3& b) {
		const vdint3 r = {a.y*b.z - a.z*b.y, a.z*b.x - a.x*b.z, a.x*b.y - a.y*b.x};
		return r;
	}
};
