/***************************************************************************
 *   Copyright (C) 2008 by Sindre Aamås                                    *
 *   sinamas@users.sourceforge.net                                         *
 *                                                                         *
 *   This program is free software; you can redistribute it and/or modify  *
 *   it under the terms of the GNU General Public License version 2 as     *
 *   published by the Free Software Foundation.                            *
 *                                                                         *
 *   This program is distributed in the hope that it will be useful,       *
 *   but WITHOUT ANY WARRANTY; without even the implied warranty of        *
 *   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the         *
 *   GNU General Public License version 2 for more details.                *
 *                                                                         *
 *   You should have received a copy of the GNU General Public License     *
 *   version 2 along with this program; if not, write to the               *
 *   Free Software Foundation, Inc.,                                       *
 *   51 Franklin St, Fifth Floor, Boston, MA  02110-1301, USA.             *
 ***************************************************************************/
#ifndef ARRAY_H
#define ARRAY_H

#include <cstddef>

template<typename T>
class SimpleArray : Uncopyable {
public:
	explicit SimpleArray(std::size_t size = 0) : a_(size ? new T[size] : 0) {}
	~SimpleArray() { delete[] defined_ptr(a_); }
	void reset(std::size_t size = 0) { delete[] defined_ptr(a_); a_ = size ? new T[size] : 0; }
	T* get() const { return a_; }
	operator T* () const { return a_; }

private:
	T* a_;
};

class Uncopyable {
protected:
	Uncopyable() {}
private:
	Uncopyable(Uncopyable const&);
	Uncopyable& operator=(Uncopyable const&);
};

template<class T>
inline T* defined_ptr(T* t) {
	typedef char type_is_defined[sizeof * t ? 1 : -1];
	(void)sizeof(type_is_defined);
	return t;
}

template<class T>
inline void defined_delete(T* t) { delete defined_ptr(t); }

struct defined_deleter { template<class T> static void del(T* p) { defined_delete(p); } };

#endif