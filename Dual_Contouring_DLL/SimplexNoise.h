#pragma once
#include <immintrin.h>

#ifndef SIMPLEXNOISE_H_INCLUDED
#define SIMPLEXNOISE_H_INCLUDED

#define G3 (1.0f / 6.0f)
#define G3_2 (2.0f / 6.0f)
#define G3_3min1 (3.0f / 6.0f - 1.0f)
#define F3 (1.0f / 3.0f)

class SimplexNoise
{
private:
	/*const float G3 = 1.0f / 6.0f;
	const float G3_2 = 2.0f / 6.0f;
	const float G3_3min1 = 3.0f / 6.0f - 1.0f;
	const float F3 = 1.0f / 3.0f;*/
	//__m128 G3_128 = _mm_set_ps1(0.1666666f);


	int grad3[288][3] = {
		{ 1,1,0 },{ -1,1,0 },{ 1,-1,0 },{ -1,-1,0 },{ 1,0,1 },{ -1,0,1 },{ 1,0,-1 },{ -1,0,-1 },{ 0,1,1 },{ 0,-1,1 },{ 0,1,-1 },{ 0,-1,-1 },
		{ 1,1,0 },{ -1,1,0 },{ 1,-1,0 },{ -1,-1,0 },{ 1,0,1 },{ -1,0,1 },{ 1,0,-1 },{ -1,0,-1 },{ 0,1,1 },{ 0,-1,1 },{ 0,1,-1 },{ 0,-1,-1 },
		{ 1,1,0 },{ -1,1,0 },{ 1,-1,0 },{ -1,-1,0 },{ 1,0,1 },{ -1,0,1 },{ 1,0,-1 },{ -1,0,-1 },{ 0,1,1 },{ 0,-1,1 },{ 0,1,-1 },{ 0,-1,-1 },
		{ 1,1,0 },{ -1,1,0 },{ 1,-1,0 },{ -1,-1,0 },{ 1,0,1 },{ -1,0,1 },{ 1,0,-1 },{ -1,0,-1 },{ 0,1,1 },{ 0,-1,1 },{ 0,1,-1 },{ 0,-1,-1 },
		{ 1,1,0 },{ -1,1,0 },{ 1,-1,0 },{ -1,-1,0 },{ 1,0,1 },{ -1,0,1 },{ 1,0,-1 },{ -1,0,-1 },{ 0,1,1 },{ 0,-1,1 },{ 0,1,-1 },{ 0,-1,-1 },
		{ 1,1,0 },{ -1,1,0 },{ 1,-1,0 },{ -1,-1,0 },{ 1,0,1 },{ -1,0,1 },{ 1,0,-1 },{ -1,0,-1 },{ 0,1,1 },{ 0,-1,1 },{ 0,1,-1 },{ 0,-1,-1 },
		{ 1,1,0 },{ -1,1,0 },{ 1,-1,0 },{ -1,-1,0 },{ 1,0,1 },{ -1,0,1 },{ 1,0,-1 },{ -1,0,-1 },{ 0,1,1 },{ 0,-1,1 },{ 0,1,-1 },{ 0,-1,-1 },
		{ 1,1,0 },{ -1,1,0 },{ 1,-1,0 },{ -1,-1,0 },{ 1,0,1 },{ -1,0,1 },{ 1,0,-1 },{ -1,0,-1 },{ 0,1,1 },{ 0,-1,1 },{ 0,1,-1 },{ 0,-1,-1 },
		{ 1,1,0 },{ -1,1,0 },{ 1,-1,0 },{ -1,-1,0 },{ 1,0,1 },{ -1,0,1 },{ 1,0,-1 },{ -1,0,-1 },{ 0,1,1 },{ 0,-1,1 },{ 0,1,-1 },{ 0,-1,-1 },
		{ 1,1,0 },{ -1,1,0 },{ 1,-1,0 },{ -1,-1,0 },{ 1,0,1 },{ -1,0,1 },{ 1,0,-1 },{ -1,0,-1 },{ 0,1,1 },{ 0,-1,1 },{ 0,1,-1 },{ 0,-1,-1 },
		{ 1,1,0 },{ -1,1,0 },{ 1,-1,0 },{ -1,-1,0 },{ 1,0,1 },{ -1,0,1 },{ 1,0,-1 },{ -1,0,-1 },{ 0,1,1 },{ 0,-1,1 },{ 0,1,-1 },{ 0,-1,-1 },
		{ 1,1,0 },{ -1,1,0 },{ 1,-1,0 },{ -1,-1,0 },{ 1,0,1 },{ -1,0,1 },{ 1,0,-1 },{ -1,0,-1 },{ 0,1,1 },{ 0,-1,1 },{ 0,1,-1 },{ 0,-1,-1 },
		{ 1,1,0 },{ -1,1,0 },{ 1,-1,0 },{ -1,-1,0 },{ 1,0,1 },{ -1,0,1 },{ 1,0,-1 },{ -1,0,-1 },{ 0,1,1 },{ 0,-1,1 },{ 0,1,-1 },{ 0,-1,-1 },
		{ 1,1,0 },{ -1,1,0 },{ 1,-1,0 },{ -1,-1,0 },{ 1,0,1 },{ -1,0,1 },{ 1,0,-1 },{ -1,0,-1 },{ 0,1,1 },{ 0,-1,1 },{ 0,1,-1 },{ 0,-1,-1 },
		{ 1,1,0 },{ -1,1,0 },{ 1,-1,0 },{ -1,-1,0 },{ 1,0,1 },{ -1,0,1 },{ 1,0,-1 },{ -1,0,-1 },{ 0,1,1 },{ 0,-1,1 },{ 0,1,-1 },{ 0,-1,-1 },
		{ 1,1,0 },{ -1,1,0 },{ 1,-1,0 },{ -1,-1,0 },{ 1,0,1 },{ -1,0,1 },{ 1,0,-1 },{ -1,0,-1 },{ 0,1,1 },{ 0,-1,1 },{ 0,1,-1 },{ 0,-1,-1 },
		{ 1,1,0 },{ -1,1,0 },{ 1,-1,0 },{ -1,-1,0 },{ 1,0,1 },{ -1,0,1 },{ 1,0,-1 },{ -1,0,-1 },{ 0,1,1 },{ 0,-1,1 },{ 0,1,-1 },{ 0,-1,-1 },
		{ 1,1,0 },{ -1,1,0 },{ 1,-1,0 },{ -1,-1,0 },{ 1,0,1 },{ -1,0,1 },{ 1,0,-1 },{ -1,0,-1 },{ 0,1,1 },{ 0,-1,1 },{ 0,1,-1 },{ 0,-1,-1 },
		{ 1,1,0 },{ -1,1,0 },{ 1,-1,0 },{ -1,-1,0 },{ 1,0,1 },{ -1,0,1 },{ 1,0,-1 },{ -1,0,-1 },{ 0,1,1 },{ 0,-1,1 },{ 0,1,-1 },{ 0,-1,-1 },
		{ 1,1,0 },{ -1,1,0 },{ 1,-1,0 },{ -1,-1,0 },{ 1,0,1 },{ -1,0,1 },{ 1,0,-1 },{ -1,0,-1 },{ 0,1,1 },{ 0,-1,1 },{ 0,1,-1 },{ 0,-1,-1 },
		{ 1,1,0 },{ -1,1,0 },{ 1,-1,0 },{ -1,-1,0 },{ 1,0,1 },{ -1,0,1 },{ 1,0,-1 },{ -1,0,-1 },{ 0,1,1 },{ 0,-1,1 },{ 0,1,-1 },{ 0,-1,-1 },
		{ 1,1,0 },{ -1,1,0 },{ 1,-1,0 },{ -1,-1,0 },{ 1,0,1 },{ -1,0,1 },{ 1,0,-1 },{ -1,0,-1 },{ 0,1,1 },{ 0,-1,1 },{ 0,1,-1 },{ 0,-1,-1 },
		{ 1,1,0 },{ -1,1,0 },{ 1,-1,0 },{ -1,-1,0 },{ 1,0,1 },{ -1,0,1 },{ 1,0,-1 },{ -1,0,-1 },{ 0,1,1 },{ 0,-1,1 },{ 0,1,-1 },{ 0,-1,-1 },
		{ 1,1,0 },{ -1,1,0 },{ 1,-1,0 },{ -1,-1,0 },{ 1,0,1 },{ -1,0,1 },{ 1,0,-1 },{ -1,0,-1 },{ 0,1,1 },{ 0,-1,1 },{ 0,1,-1 },{ 0,-1,-1 }
	};
	float grad3_one[864];
public:
	int p[512];
public:
	SimplexNoise(int *p_ext);
private:
	inline float dot(int *g, float x, float y, float z);
public:
	float octavenoise3d(float x, float y, float z, int octaves, float persistence);
	float octavenoise3d_H1(float x, float y, float z);
public:
	inline float noise3d(float xin, float yin, float zin);
	float noise3d_x8_p2(float x0, float y0, float z0, int i, int j, int k);
	void getFfrom_m256(__m256 in, float *out);
	inline void noise3d_x8(float x, float y, float z, float *out);
	inline void noise3d_x8_v2(float x, float y, float z, float *out);
	inline double noise3d_double(double xin, double yin, double zin);

};

#endif // SIMPLEXNOISE_H_INCLUDED
