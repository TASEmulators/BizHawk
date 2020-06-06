#include <stdint.h>

typedef int64_t ll;

__attribute__((sysv_abi)) ll Depart0(void)
{
	return ((__attribute__((ms_abi)) ll (*)(void))0xdeadbeeffeedface)();
}

__attribute__((sysv_abi)) ll Depart1(ll a)
{
	return ((__attribute__((ms_abi)) ll (*)(ll))0xdeadbeeffeedface)(a);
}

__attribute__((sysv_abi)) ll Depart2(ll a, ll b)
{
	return ((__attribute__((ms_abi)) ll (*)(ll, ll))0xdeadbeeffeedface)(a, b);
}

__attribute__((sysv_abi)) ll Depart3(ll a, ll b, ll c)
{
	return ((__attribute__((ms_abi)) ll (*)(ll, ll, ll))0xdeadbeeffeedface)(a, b, c);
}

__attribute__((sysv_abi)) ll Depart4(ll a, ll b, ll c, ll d)
{
	return ((__attribute__((ms_abi)) ll (*)(ll, ll, ll, ll))0xdeadbeeffeedface)(a, b, c, d);
}

__attribute__((sysv_abi)) ll Depart5(ll a, ll b, ll c, ll d, ll e)
{
	return ((__attribute__((ms_abi)) ll (*)(ll, ll, ll, ll, ll))0xdeadbeeffeedface)(a, b, c, d, e);
}

__attribute__((sysv_abi)) ll Depart6(ll a, ll b, ll c, ll d, ll e, ll f)
{
	return ((__attribute__((ms_abi)) ll (*)(ll, ll, ll, ll, ll, ll))0xdeadbeeffeedface)(a, b, c, d, e, f);
}

__attribute__((ms_abi)) ll Arrive0(void)
{
	return ((__attribute__((sysv_abi)) ll (*)(void))0xdeadbeeffeedface)();
}

__attribute__((ms_abi)) ll Arrive1(ll a)
{
	return ((__attribute__((sysv_abi)) ll (*)(ll))0xdeadbeeffeedface)(a);
}

__attribute__((ms_abi)) ll Arrive2(ll a, ll b)
{
	return ((__attribute__((sysv_abi)) ll (*)(ll, ll))0xdeadbeeffeedface)(a, b);
}

__attribute__((ms_abi)) ll Arrive3(ll a, ll b, ll c)
{
	return ((__attribute__((sysv_abi)) ll (*)(ll, ll, ll))0xdeadbeeffeedface)(a, b, c);
}

__attribute__((ms_abi)) ll Arrive4(ll a, ll b, ll c, ll d)
{
	return ((__attribute__((sysv_abi)) ll (*)(ll, ll, ll, ll))0xdeadbeeffeedface)(a, b, c, d);
}

__attribute__((ms_abi)) ll Arrive5(ll a, ll b, ll c, ll d, ll e)
{
	return ((__attribute__((sysv_abi)) ll (*)(ll, ll, ll, ll, ll))0xdeadbeeffeedface)(a, b, c, d, e);
}

__attribute__((ms_abi)) ll Arrive6(ll a, ll b, ll c, ll d, ll e, ll f)
{
	return ((__attribute__((sysv_abi)) ll (*)(ll, ll, ll, ll, ll, ll))0xdeadbeeffeedface)(a, b, c, d, e, f);
}

void End(void)
{
}

#include <stdio.h>
const void* ptrs[] = { Depart0, Depart1, Depart2, Depart3, Depart4, Depart5, Depart6,
	Arrive0, Arrive1, Arrive2, Arrive3, Arrive4, Arrive5, Arrive6, End };

void print(const char* name, int offs)
{
	printf("\t\t\tprivate static readonly byte[][] %s =\n\t\t\t{\n", name);
	for (int i = offs; i < offs + 7; i++)
	{
		printf("\t\t\t\tnew byte[] { ");
		const uint8_t* start = ptrs[i];
		const uint8_t* end = ptrs[i + 1];
		while (start < end)
			printf("0x%02x, ", *start++);
		printf("},\n");		
	}
	printf("\t\t\t};\n");
}	

int main(void)
{
	print("Depart", 0);
	print("Arrive", 7);
	return 0;
}
