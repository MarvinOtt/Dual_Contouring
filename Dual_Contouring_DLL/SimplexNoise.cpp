#include <immintrin.h>
#include "SimplexNoise.h"
#include <stdlib.h>
#include <math.h>

#define str 0.4f

union U128f {
	__m128 v;
	float a[4];
};

SimplexNoise::SimplexNoise(int *p_ext)
{
	for (int x = 0; x < 512; ++x)
	{
		p[x] = p_ext[x];
	}
	for (int i = 0; i < 288; ++i)
	{
		grad3_one[i * 3] = grad3[i][0];
		grad3_one[i * 3 + 1] = grad3[i][1];
		grad3_one[i * 3 + 2] = grad3[i][2];
	}
	return;
}

inline float SimplexNoise::dot(int *g, float x, float y, float z)
{
	return g[0] * x + g[1] * y + g[2] * z;
}

float SimplexNoise::octavenoise3d(float x, float y, float z, int octaves, float persistence)
{
	float total = 0;
	float frequency = 1;
	float amplitude = 1;
	float maxValue = 0;
	for (int i = 0; i < octaves; ++i)
	{
		total += noise3d(x * frequency, y * frequency, z * frequency) * amplitude;
		maxValue += amplitude;
		amplitude *= persistence;
		frequency += frequency;
	}
	return (total * 32.0f) / maxValue;
}
float SimplexNoise::octavenoise3d_H1(float x, float y, float z)
{
	float total = 0;
	/*float frequency = 1;
	float amplitude = 1;
	float maxValue = 0;
	for (int i = 0; i < octaves; ++i)
	{
	total += noise3d(x * frequency, y * frequency, z * frequency) * amplitude;
	maxValue += amplitude;
	amplitude *= persistence;
	frequency += frequency;
	}
	*/
	//total += noise3d(x, y, z);
	//total += noise3d(x + x, y + y, z + z) * str;
	//total += noise3d(x * 3, y * 3, z * 3) * str * str;
	//total += noise3d(x * 4, y * 4, z * 4) * str * str * str;
	float out[8];
	noise3d_x8_v2(x, y, z, out);
	total += out[0] + out[1] * str + out[2] * str * str + out[3] * str * str * str + out[4] * str * str * str * str + out[5] * str * str * str * str * str + out[6] * str * str * str * str * str * str + out[7] * str * str * str * str * str * str * str;

	return total * 32.0f;
}

inline float SimplexNoise::noise3d(float xin, float yin, float zin)
{
	float n0, n1, n2, n3; // Noise contributions from the four corners

	float s = (xin + yin + zin) * F3; // Very nice and simple skew factor for 3D
	int i = floor(xin + s);
	int j = floor(yin + s);
	int k = floor(zin + s);
	float t = (i + j + k) * G3;
	float x0 = xin - i + t; // The x,y,z distances from the cell origin
	float y0 = yin - j + t;
	float z0 = zin - k + t;

	// For the 3D case, the simplex shape is a slightly irregular tetrahedron.
	// Determine which simplex we are in.
	unsigned int i1, j1, k1; // Offsets for second corner of simplex in (i,j,k) coords
	unsigned int i2, j2, k2; // Offsets for third corner of simplex in (i,j,k) coords
	if (x0 >= y0)
	{
		j1 = 0;
		i2 = 1;
		if (y0 >= z0)
		{
			i1 = 1; k1 = 0; j2 = 1; k2 = 0;
		} // X Y Z order
		else if (x0 >= z0) { i1 = 1; k1 = 0; j2 = 0; k2 = 1; } // X Z Y order
		else { i1 = 0; k1 = 1; j2 = 0; k2 = 1; } // Z X Y order
	}
	else
	{ // x0<y0
		i1 = 0;
		if (y0 < z0) { j1 = 0; k1 = 1; i2 = 0; j2 = 1; k2 = 1; } // Z Y X order
		else if (x0 < z0) { j1 = 1; k1 = 0; i2 = 0; j2 = 1; k2 = 1; } // Y Z X order
		else { j1 = 1; k1 = 0; i2 = 1; j2 = 1; k2 = 0; } // Y X Z order
	}
	// A step of (1,0,0) in (i,j,k) means a step of (1-c,-c,-c) in (x,y,z),
	// a step of (0,1,0) in (i,j,k) means a step of (-c,1-c,-c) in (x,y,z), and
	// a step of (0,0,1) in (i,j,k) means a step of (-c,-c,1-c) in (x,y,z), where
	// c = 1/6.
	float x1 = x0 - i1 + G3; // Offsets for second corner in (x,y,z) coords
	float y1 = y0 - j1 + G3;
	float z1 = z0 - k1 + G3;
	float x2 = x0 - i2 + G3_2; // Offsets for third corner in (x,y,z) coords
	float y2 = y0 - j2 + G3_2;
	float z2 = z0 - k2 + G3_2;
	float x3 = x0 + G3_3min1; // Offsets for last corner in (x,y,z) coords
	float y3 = y0 + G3_3min1;
	float z3 = z0 + G3_3min1;
	// Work out the hashed gradient indices of the four simplex corners
	unsigned char ii = i;
	unsigned char jj = j;
	unsigned char kk = k;
	// Calculate the contribution from the four corners
	float t0 = 0.5f - x0 * x0 - y0 * y0 - z0 * z0;
	if (t0 < 0) n0 = 0.0f;
	else
	{
		unsigned int gi0 = p[ii + p[jj + p[kk]]];
		t0 *= t0;
		n0 = t0 * t0 * dot(grad3[gi0], x0, y0, z0);
	}
	float t1 = 0.5f - x1 * x1 - y1 * y1 - z1 * z1;
	if (t1 < 0) n1 = 0.0f;
	else
	{
		unsigned int gi1 = p[ii + i1 + p[jj + j1 + p[kk + k1]]];
		t1 *= t1;
		n1 = t1 * t1 * dot(grad3[gi1], x1, y1, z1);
	}
	float t2 = 0.5f - x2 * x2 - y2 * y2 - z2 * z2;
	if (t2 < 0) n2 = 0.0f;
	else
	{
		unsigned int gi2 = p[ii + i2 + p[jj + j2 + p[kk + k2]]];
		t2 *= t2;
		n2 = t2 * t2 * dot(grad3[gi2], x2, y2, z2);
	}
	float t3 = 0.5f - x3 * x3 - y3 * y3 - z3 * z3;
	if (t3 < 0) n3 = 0.0f;
	else
	{
		unsigned int gi3 = p[ii + 1 + p[jj + 1 + p[kk + 1]]];
		t3 *= t3;
		n3 = t3 * t3 * dot(grad3[gi3], x3, y3, z3);
	}
	// Add contributions from each corner to get the final noise value.
	// The result is scaled to stay just inside [-1,1]
	return (n0 + n1 + n2 + n3);
}

float SimplexNoise::noise3d_x8_p2(float x0, float y0, float z0, int i, int j, int k)
{
	float n0, n1, n2, n3; // Noise contributions from the four corners
						  // For the 3D case, the simplex shape is a slightly irregular tetrahedron.
						  // Determine which simplex we are in.
	unsigned int i1, j1, k1; // Offsets for second corner of simplex in (i,j,k) coords
	unsigned int i2, j2, k2; // Offsets for third corner of simplex in (i,j,k) coords
	if (x0 >= y0)
	{
		j1 = 0;
		i2 = 1;
		if (y0 >= z0)
		{
			i1 = 1; k1 = 0; j2 = 1; k2 = 0;
		} // X Y Z order
		else if (x0 >= z0) { i1 = 1; k1 = 0; j2 = 0; k2 = 1; } // X Z Y order
		else { i1 = 0; k1 = 1; j2 = 0; k2 = 1; } // Z X Y order
	}
	else
	{ // x0<y0
		i1 = 0;
		if (y0 < z0) { j1 = 0; k1 = 1; i2 = 0; j2 = 1; k2 = 1; } // Z Y X order
		else if (x0 < z0) { j1 = 1; k1 = 0; i2 = 0; j2 = 1; k2 = 1; } // Y Z X order
		else { j1 = 1; k1 = 0; i2 = 1; j2 = 1; k2 = 0; } // Y X Z order
	}
	// A step of (1,0,0) in (i,j,k) means a step of (1-c,-c,-c) in (x,y,z),
	// a step of (0,1,0) in (i,j,k) means a step of (-c,1-c,-c) in (x,y,z), and
	// a step of (0,0,1) in (i,j,k) means a step of (-c,-c,1-c) in (x,y,z), where
	// c = 1/6.
	float x1 = x0 - i1 + G3; // Offsets for second corner in (x,y,z) coords
	float y1 = y0 - j1 + G3;
	float z1 = z0 - k1 + G3;
	float x2 = x0 - i2 + G3_2; // Offsets for third corner in (x,y,z) coords
	float y2 = y0 - j2 + G3_2;
	float z2 = z0 - k2 + G3_2;
	float x3 = x0 + G3_3min1; // Offsets for last corner in (x,y,z) coords
	float y3 = y0 + G3_3min1;
	float z3 = z0 + G3_3min1;
	// Work out the hashed gradient indices of the four simplex corners
	unsigned char ii = i;
	unsigned char jj = j;
	unsigned char kk = k;
	// Calculate the contribution from the four corners
	float t0 = 0.5f - x0 * x0 - y0 * y0 - z0 * z0;
	if (t0 < 0) n0 = 0.0f;
	else
	{
		unsigned int gi0 = p[ii + p[jj + p[kk]]];
		t0 *= t0;
		n0 = t0 * t0 * dot(grad3[gi0], x0, y0, z0);
	}
	float t1 = 0.5f - x1 * x1 - y1 * y1 - z1 * z1;
	if (t1 < 0) n1 = 0.0f;
	else
	{
		unsigned int gi1 = p[ii + i1 + p[jj + j1 + p[kk + k1]]];
		t1 *= t1;
		n1 = t1 * t1 * dot(grad3[gi1], x1, y1, z1);
	}
	float t2 = 0.5f - x2 * x2 - y2 * y2 - z2 * z2;
	if (t2 < 0) n2 = 0.0f;
	else
	{
		unsigned int gi2 = p[ii + i2 + p[jj + j2 + p[kk + k2]]];
		t2 *= t2;
		n2 = t2 * t2 * dot(grad3[gi2], x2, y2, z2);
	}
	float t3 = 0.5f - x3 * x3 - y3 * y3 - z3 * z3;
	if (t3 < 0) n3 = 0.0f;
	else
	{
		unsigned int gi3 = p[ii + 1 + p[jj + 1 + p[kk + 1]]];
		t3 *= t3;
		n3 = t3 * t3 * dot(grad3[gi3], x3, y3, z3);
	}
	// Add contributions from each corner to get the final noise value.
	// The result is scaled to stay just inside [-1,1]
	return (n0 + n1 + n2 + n3);
}

void SimplexNoise::getFfrom_m256(__m256 in, float *out)
{
	__m128 in1 = _mm256_extractf128_ps(in, 0);
	__m128 in2 = _mm256_extractf128_ps(in, 1);
	_mm_store_ps(out, in1);
	_mm_store_ps(&(out[4]), in2);
}
/*float _mm256_EF(__m256 in, int index)
{
if (index < 4)
return extract_float(_mm256_extractf128_ps(in, 0), index);
else
return extract_float(_mm256_extractf128_ps(in, 1), index - 4);
}*/

inline void SimplexNoise::noise3d_x8(float x, float y, float z, float *out)
{
	__m256 xin = _mm256_setr_ps(x, x + x, x * 4.0f, x * 8.0f, x * 16.0f, x * 32.0f, x * 64.0f, x * 128.0f);
	__m256 yin = _mm256_setr_ps(y, y + y, y * 4.0f, y * 8.0f, y * 16.0f, y * 32.0f, y * 64.0f, y * 128.0f);
	__m256 zin = _mm256_setr_ps(z, z + z, z * 4.0f, z * 8.0f, z * 16.0f, z * 32.0f, z * 64.0f, z * 128.0f);

	const __m256 F3_256 = _mm256_set1_ps(F3);
	const __m256 G3_256 = _mm256_set1_ps(G3);
	__m256 s = _mm256_mul_ps(_mm256_add_ps(_mm256_add_ps(xin, yin), zin), F3_256);
	__m256 i = _mm256_floor_ps(_mm256_add_ps(xin, s));
	__m256 j = _mm256_floor_ps(_mm256_add_ps(yin, s));
	__m256 k = _mm256_floor_ps(_mm256_add_ps(zin, s));
	__m256 t = _mm256_mul_ps(_mm256_add_ps(_mm256_add_ps(i, j), k), G3_256);
	__m256 x0 = _mm256_add_ps(_mm256_sub_ps(xin, i), t);
	__m256 y0 = _mm256_add_ps(_mm256_sub_ps(yin, j), t);
	__m256 z0 = _mm256_add_ps(_mm256_sub_ps(zin, k), t);
	for (int ii = 0; ii < 8; ++ii)
	{
		/*float xx = xin.m256_f32[ii];
		float yy = yin.m256_f32[ii];
		float zz = zin.m256_f32[ii];

		float sss = (xx + yy + zz) * F3; // Very nice and simple skew factor for 3D
		int iii = floorf(xx + sss);
		int jjj = floorf(yy + sss);
		int kkk = floorf(zz + sss);
		float ttt = (iii + jjj + kkk) * G3;
		float xxx0 = xx - iii + ttt; // The x,y,z distances from the cell origin
		float yyy0 = yy - jjj + ttt;
		float zzz0 = zz - kkk + ttt;
		out[ii] = noise3d_x8_p2(xxx0, yyy0, zzz0, iii, jjj, kkk);*/
		out[ii] = noise3d_x8_p2(x0.m256_f32[ii], y0.m256_f32[ii], z0.m256_f32[ii], (int)i.m256_f32[ii], (int)j.m256_f32[ii], (int)k.m256_f32[ii]);
	}
	return;
}

inline __m256i mod_256(__m256 in, __m256 factor)
{
	__m256 indivfac = _mm256_div_ps(in, factor);
	__m256 rounded = _mm256_floor_ps(indivfac);
	__m256 multiplied = _mm256_mul_ps(rounded, factor);
	return _mm256_cvttps_epi32(_mm256_sub_ps(in, multiplied));
}

inline void SimplexNoise::noise3d_x8_v2(float x, float y, float z, float *out)
{
	__m256 xin = _mm256_setr_ps(x, x + x, x * 4, x * 8, x * 16, x * 32, x * 64, x * 128);
	__m256 yin = _mm256_setr_ps(y, y + y, y * 4, y * 8, y * 16, y * 32, y * 64, y * 128);
	__m256 zin = _mm256_setr_ps(z, z + z, z * 4, z * 8, z * 16, z * 32, z * 64, z * 128);

	const __m256 F3_256 = _mm256_set1_ps(F3);
	const __m256 G3_256 = _mm256_set1_ps(G3);
	const __m256 G3_2_256 = _mm256_set1_ps(G3_2);
	const __m256 G3_3min1_256 = _mm256_set1_ps(G3_3min1);
	__m256 s = _mm256_mul_ps(_mm256_add_ps(_mm256_add_ps(xin, yin), zin), F3_256);
	__m256 i = _mm256_floor_ps(_mm256_add_ps(xin, s));
	__m256 j = _mm256_floor_ps(_mm256_add_ps(yin, s));
	__m256 k = _mm256_floor_ps(_mm256_add_ps(zin, s));
	__m256 t = _mm256_mul_ps(_mm256_add_ps(_mm256_add_ps(i, j), k), G3_256);
	__m256 x0 = _mm256_add_ps(_mm256_sub_ps(xin, i), t);
	__m256 y0 = _mm256_add_ps(_mm256_sub_ps(yin, j), t);
	__m256 z0 = _mm256_add_ps(_mm256_sub_ps(zin, k), t);

	const __m256i C_0 = _mm256_set1_epi32(0);
	const __m256i C_1 = _mm256_set1_epi32(1);

	__m256i x0_G_z0 = _mm256_castps_si256(_mm256_cmp_ps(x0, z0, _CMP_GE_OQ));
	__m256i y0_G_z0 = _mm256_castps_si256(_mm256_cmp_ps(y0, z0, _CMP_GE_OQ));
	__m256i x0_G_y0 = _mm256_castps_si256(_mm256_cmp_ps(x0, y0, _CMP_GE_OQ));
	__m256i final_G_i1 = _mm256_blendv_epi8(C_0, C_1, x0_G_z0);
	final_G_i1 = _mm256_blendv_epi8(final_G_i1, C_1, y0_G_z0);
	__m256i final_G_k1 = _mm256_blendv_epi8(C_1, C_0, x0_G_z0);
	final_G_k1 = _mm256_blendv_epi8(final_G_k1, C_0, y0_G_z0);
	__m256i final_G_j2 = _mm256_blendv_epi8(C_0, C_0, x0_G_z0);
	final_G_j2 = _mm256_blendv_epi8(final_G_j2, C_1, y0_G_z0);
	__m256i final_G_k2 = _mm256_blendv_epi8(C_1, C_1, x0_G_z0);
	final_G_k2 = _mm256_blendv_epi8(final_G_k2, C_0, y0_G_z0);

	__m256i final_L_j1 = _mm256_blendv_epi8(C_1, C_1, x0_G_z0);
	final_L_j1 = _mm256_blendv_epi8(C_0, final_L_j1, y0_G_z0);
	__m256i final_L_k1 = _mm256_blendv_epi8(C_0, C_0, x0_G_z0);
	final_L_k1 = _mm256_blendv_epi8(C_1, final_L_k1, y0_G_z0);
	__m256i final_L_i2 = _mm256_blendv_epi8(C_0, C_1, x0_G_z0);
	final_L_i2 = _mm256_blendv_epi8(C_0, final_L_i2, y0_G_z0);
	__m256i final_L_k2 = _mm256_blendv_epi8(C_1, C_0, x0_G_z0);
	final_L_k2 = _mm256_blendv_epi8(C_1, final_L_k2, y0_G_z0);

	__m256i i1 = _mm256_blendv_epi8(C_0, final_G_i1, x0_G_y0);
	__m256i i2 = _mm256_blendv_epi8(final_L_i2, C_1, x0_G_y0);
	__m256i j1 = _mm256_blendv_epi8(final_L_j1, C_0, x0_G_y0);
	__m256i j2 = _mm256_blendv_epi8(C_1, final_G_j2, x0_G_y0);
	__m256i k1 = _mm256_blendv_epi8(final_L_k1, final_G_k1, x0_G_y0);
	__m256i k2 = _mm256_blendv_epi8(final_L_k2, final_G_k2, x0_G_y0);

	__m256 x1 = _mm256_add_ps(_mm256_sub_ps(x0, _mm256_cvtepi32_ps(i1)), G3_256);
	__m256 y1 = _mm256_add_ps(_mm256_sub_ps(y0, _mm256_cvtepi32_ps(j1)), G3_256);
	__m256 z1 = _mm256_add_ps(_mm256_sub_ps(z0, _mm256_cvtepi32_ps(k1)), G3_256);
	__m256 x2 = _mm256_add_ps(_mm256_sub_ps(x0, _mm256_cvtepi32_ps(i2)), G3_2_256);
	__m256 y2 = _mm256_add_ps(_mm256_sub_ps(y0, _mm256_cvtepi32_ps(j2)), G3_2_256);
	__m256 z2 = _mm256_add_ps(_mm256_sub_ps(z0, _mm256_cvtepi32_ps(k2)), G3_2_256);
	__m256 x3 = _mm256_add_ps(x0, G3_3min1_256);
	__m256 y3 = _mm256_add_ps(y0, G3_3min1_256);
	__m256 z3 = _mm256_add_ps(z0, G3_3min1_256);

	const __m256 F_256 = _mm256_set1_ps(256.0f);
	__m256i ii = mod_256(i, F_256);
	__m256i jj = mod_256(j, F_256);
	__m256i kk = mod_256(k, F_256);

	const __m256 F_0_5 = _mm256_set1_ps(0.5f);
	__m256 t0 = _mm256_sub_ps(_mm256_sub_ps(_mm256_sub_ps(F_0_5, _mm256_mul_ps(x0, x0)), _mm256_mul_ps(y0, y0)), _mm256_mul_ps(z0, z0));
	__m256 t1 = _mm256_sub_ps(_mm256_sub_ps(_mm256_sub_ps(F_0_5, _mm256_mul_ps(x1, x1)), _mm256_mul_ps(y1, y1)), _mm256_mul_ps(z1, z1));
	__m256 t2 = _mm256_sub_ps(_mm256_sub_ps(_mm256_sub_ps(F_0_5, _mm256_mul_ps(x2, x2)), _mm256_mul_ps(y2, y2)), _mm256_mul_ps(z2, z2));
	__m256 t3 = _mm256_sub_ps(_mm256_sub_ps(_mm256_sub_ps(F_0_5, _mm256_mul_ps(x3, x3)), _mm256_mul_ps(y3, y3)), _mm256_mul_ps(z3, z3));

	const __m256i I_NAN = _mm256_castps_si256(_mm256_set1_ps(NAN));
	const __m256 F_NAN = _mm256_set1_ps(NAN);
	const __m256i I_1 = _mm256_set1_epi32(1);
	const __m256i I_2 = _mm256_set1_epi32(2);
	const __m256i I_3 = _mm256_set1_epi32(3);
	__m256i gi0 = _mm256_add_epi32(_mm256_mask_i32gather_epi32(C_0, p, kk, I_NAN, 4), jj);
	gi0 = _mm256_mask_i32gather_epi32(C_0, p, _mm256_add_epi32(_mm256_mask_i32gather_epi32(C_0, p, gi0, I_NAN, 4), ii), I_NAN, 4);
	gi0 = _mm256_mullo_epi32(gi0, I_3);
	__m256i gi1 = _mm256_add_epi32(_mm256_add_epi32(_mm256_mask_i32gather_epi32(C_0, p, _mm256_add_epi32(kk, k1), I_NAN, 4), jj), j1);
	gi1 = _mm256_mask_i32gather_epi32(C_0, p, _mm256_add_epi32(_mm256_add_epi32(_mm256_mask_i32gather_epi32(C_0, p, gi1, I_NAN, 4), ii), i1), I_NAN, 4);
	gi1 = _mm256_mullo_epi32(gi1, I_3);
	__m256i gi2 = _mm256_add_epi32(_mm256_add_epi32(_mm256_mask_i32gather_epi32(C_0, p, _mm256_add_epi32(kk, k2), I_NAN, 4), jj), j2);
	gi2 = _mm256_mask_i32gather_epi32(C_0, p, _mm256_add_epi32(_mm256_add_epi32(_mm256_mask_i32gather_epi32(C_0, p, gi2, I_NAN, 4), ii), i2), I_NAN, 4);
	gi2 = _mm256_mullo_epi32(gi2, I_3);
	__m256i gi3 = _mm256_add_epi32(_mm256_add_epi32(_mm256_mask_i32gather_epi32(C_0, p, _mm256_add_epi32(kk, I_1), I_NAN, 4), jj), I_1);
	gi3 = _mm256_mask_i32gather_epi32(C_0, p, _mm256_add_epi32(_mm256_add_epi32(_mm256_mask_i32gather_epi32(C_0, p, gi3, I_NAN, 4), ii), I_1), I_NAN, 4);
	gi3 = _mm256_mullo_epi32(gi3, I_3);

	const __m256 F_0 = _mm256_set1_ps(0.0f);
	__m256 t0_L_0 = _mm256_cmp_ps(t0, F_0, _CMP_LT_OQ);
	__m256 t1_L_0 = _mm256_cmp_ps(t1, F_0, _CMP_LT_OQ);
	__m256 t2_L_0 = _mm256_cmp_ps(t2, F_0, _CMP_LT_OQ);
	__m256 t3_L_0 = _mm256_cmp_ps(t3, F_0, _CMP_LT_OQ);
	/*__m256 dot_0 = _mm256_add_ps(_mm256_mul_ps(x0, _mm256_mask_i32gather_ps(F_0, grad3_one, gi0, F_NAN, 4)), _mm256_add_ps(_mm256_mul_ps(y0, _mm256_mask_i32gather_ps(F_0, grad3_one, _mm256_add_epi32(gi0, I_1), F_NAN, 4)), _mm256_mul_ps(z0, _mm256_mask_i32gather_ps(F_0, grad3_one, _mm256_add_epi32(gi0, I_2), F_NAN, 4))));
	__m256 dot_1 = _mm256_add_ps(_mm256_mul_ps(x1, _mm256_mask_i32gather_ps(F_0, grad3_one, gi1, F_NAN, 4)), _mm256_add_ps(_mm256_mul_ps(y1, _mm256_mask_i32gather_ps(F_0, grad3_one, _mm256_add_epi32(gi1, I_1), F_NAN, 4)), _mm256_mul_ps(z1, _mm256_mask_i32gather_ps(F_0, grad3_one, _mm256_add_epi32(gi1, I_2), F_NAN, 4))));
	__m256 dot_2 = _mm256_add_ps(_mm256_mul_ps(x2, _mm256_mask_i32gather_ps(F_0, grad3_one, gi2, F_NAN, 4)), _mm256_add_ps(_mm256_mul_ps(y2, _mm256_mask_i32gather_ps(F_0, grad3_one, _mm256_add_epi32(gi2, I_1), F_NAN, 4)), _mm256_mul_ps(z2, _mm256_mask_i32gather_ps(F_0, grad3_one, _mm256_add_epi32(gi2, I_2), F_NAN, 4))));
	__m256 dot_3 = _mm256_add_ps(_mm256_mul_ps(x3, _mm256_mask_i32gather_ps(F_0, grad3_one, gi3, F_NAN, 4)), _mm256_add_ps(_mm256_mul_ps(y3, _mm256_mask_i32gather_ps(F_0, grad3_one, _mm256_add_epi32(gi3, I_1), F_NAN, 4)), _mm256_mul_ps(z3, _mm256_mask_i32gather_ps(F_0, grad3_one, _mm256_add_epi32(gi3, I_2), F_NAN, 4))));*/

	__m256 dot_0 = _mm256_add_ps(_mm256_mul_ps(x0, _mm256_i32gather_ps(grad3_one, gi0, 4)), _mm256_add_ps(_mm256_mul_ps(y0, _mm256_i32gather_ps(grad3_one, _mm256_add_epi32(gi0, I_1), 4)), _mm256_mul_ps(z0, _mm256_i32gather_ps(grad3_one, _mm256_add_epi32(gi0, I_2), 4))));
	__m256 dot_1 = _mm256_add_ps(_mm256_mul_ps(x1, _mm256_i32gather_ps(grad3_one, gi1, 4)), _mm256_add_ps(_mm256_mul_ps(y1, _mm256_i32gather_ps(grad3_one, _mm256_add_epi32(gi1, I_1), 4)), _mm256_mul_ps(z1, _mm256_i32gather_ps(grad3_one, _mm256_add_epi32(gi1, I_2), 4))));
	__m256 dot_2 = _mm256_add_ps(_mm256_mul_ps(x2, _mm256_i32gather_ps(grad3_one, gi2, 4)), _mm256_add_ps(_mm256_mul_ps(y2, _mm256_i32gather_ps(grad3_one, _mm256_add_epi32(gi2, I_1), 4)), _mm256_mul_ps(z2, _mm256_i32gather_ps(grad3_one, _mm256_add_epi32(gi2, I_2), 4))));
	__m256 dot_3 = _mm256_add_ps(_mm256_mul_ps(x3, _mm256_i32gather_ps(grad3_one, gi3, 4)), _mm256_add_ps(_mm256_mul_ps(y3, _mm256_i32gather_ps(grad3_one, _mm256_add_epi32(gi3, I_1), 4)), _mm256_mul_ps(z3, _mm256_i32gather_ps(grad3_one, _mm256_add_epi32(gi3, I_2), 4))));

	t0 = _mm256_mul_ps(t0, t0);
	__m256 n0 = _mm256_mul_ps(_mm256_mul_ps(t0, t0), dot_0);
	t1 = _mm256_mul_ps(t1, t1);
	__m256 n1 = _mm256_mul_ps(_mm256_mul_ps(t1, t1), dot_1);
	t2 = _mm256_mul_ps(t2, t2);
	__m256 n2 = _mm256_mul_ps(_mm256_mul_ps(t2, t2), dot_2);
	t3 = _mm256_mul_ps(t3, t3);
	__m256 n3 = _mm256_mul_ps(_mm256_mul_ps(t3, t3), dot_3);

	n0 = _mm256_blendv_ps(n0, F_0, t0_L_0);
	n1 = _mm256_blendv_ps(n1, F_0, t1_L_0);
	n2 = _mm256_blendv_ps(n2, F_0, t2_L_0);
	n3 = _mm256_blendv_ps(n3, F_0, t3_L_0);

	__m256 n_all = _mm256_add_ps(_mm256_add_ps(n0, n1), _mm256_add_ps(n2, n3));
	out[0] = n_all.m256_f32[0];
	out[1] = n_all.m256_f32[1];
	out[2] = n_all.m256_f32[2];
	out[3] = n_all.m256_f32[3];
	out[4] = n_all.m256_f32[4];
	out[5] = n_all.m256_f32[5];
	out[6] = n_all.m256_f32[6];
	out[7] = n_all.m256_f32[7];

	return;
}


inline double SimplexNoise::noise3d_double(double xin, double yin, double zin)
{

	double n0, n1, n2, n3; // Noise contributions from the four corners
						   // Skew the input space to determine which simplex cell we're in
	double s = (xin + yin + zin) * F3; // Very nice and simple skew factor for 3D
	int i = floorf(xin + s);
	int j = floorf(yin + s);
	int k = floorf(zin + s);
	double t = (i + j + k) * G3;
	double x0 = xin - i + t; // The x,y,z distances from the cell origin
	double y0 = yin - j + t;
	double z0 = zin - k + t;
	// For the 3D case, the simplex shape is a slightly irregular tetrahedron.
	// Determine which simplex we are in.
	unsigned int i1, j1, k1; // Offsets for second corner of simplex in (i,j,k) coords
	unsigned int i2, j2, k2; // Offsets for third corner of simplex in (i,j,k) coords
	if (x0 >= y0)
	{
		j1 = 0;
		i2 = 1;
		if (y0 >= z0)
		{
			i1 = 1; k1 = 0; j2 = 1; k2 = 0;
		} // X Y Z order
		else if (x0 >= z0) { i1 = 1; k1 = 0; j2 = 0; k2 = 1; } // X Z Y order
		else { i1 = 0; k1 = 1; j2 = 0; k2 = 1; } // Z X Y order
	}
	else
	{ // x0<y0
		i1 = 0;
		j2 = 1;
		if (y0 < z0) { j1 = 0; k1 = 1; i2 = 0; k2 = 1; } // Z Y X order
		else if (x0 < z0) { j1 = 1; k1 = 0; i2 = 0; k2 = 1; } // Y Z X order
		else { j1 = 1; k1 = 0; i2 = 1; k2 = 0; } // Y X Z order
	}
	// A step of (1,0,0) in (i,j,k) means a step of (1-c,-c,-c) in (x,y,z),
	// a step of (0,1,0) in (i,j,k) means a step of (-c,1-c,-c) in (x,y,z), and
	// a step of (0,0,1) in (i,j,k) means a step of (-c,-c,1-c) in (x,y,z), where
	// c = 1/6.
	double x1 = x0 - i1 + G3; // Offsets for second corner in (x,y,z) coords
	double y1 = y0 - j1 + G3;
	double z1 = z0 - k1 + G3;
	double x2 = x0 - i2 + G3_2; // Offsets for third corner in (x,y,z) coords
	double y2 = y0 - j2 + G3_2;
	double z2 = z0 - k2 + G3_2;
	double x3 = x0 + G3_3min1; // Offsets for last corner in (x,y,z) coords
	double y3 = y0 + G3_3min1;
	double z3 = z0 + G3_3min1;
	// Work out the hashed gradient indices of the four simplex corners
	unsigned char ii = i;
	unsigned char jj = j;
	unsigned char kk = k;
	// Calculate the contribution from the four corners
	double t0 = 0.5f - x0 * x0 - y0 * y0 - z0 * z0;
	if (t0 < 0) n0 = 0.0f;
	else
	{
		unsigned int gi0 = p[ii + p[jj + p[kk]]];
		t0 *= t0;
		n0 = t0 * t0 * dot(grad3[gi0], x0, y0, z0);
	}
	double t1 = 0.5f - x1 * x1 - y1 * y1 - z1 * z1;
	if (t1 < 0) n1 = 0.0f;
	else
	{
		unsigned int gi1 = p[ii + i1 + p[jj + j1 + p[kk + k1]]];
		t1 *= t1;
		n1 = t1 * t1 * dot(grad3[gi1], x1, y1, z1);
	}
	double t2 = 0.5f - x2 * x2 - y2 * y2 - z2 * z2;
	if (t2 < 0) n2 = 0.0f;
	else
	{
		unsigned int gi2 = p[ii + i2 + p[jj + j2 + p[kk + k2]]];
		t2 *= t2;
		n2 = t2 * t2 * dot(grad3[gi2], x2, y2, z2);
	}
	double t3 = 0.5f - x3 * x3 - y3 * y3 - z3 * z3;
	if (t3 < 0) n3 = 0.0f;
	else
	{
		unsigned int gi3 = p[ii + 1 + p[jj + 1 + p[kk + 1]]];
		t3 *= t3;
		n3 = t3 * t3 * dot(grad3[gi3], x3, y3, z3);
	}
	// Add contributions from each corner to get the final noise value.
	// The result is scaled to stay just inside [-1,1]
	return (n0 + n1 + n2 + n3);
}
