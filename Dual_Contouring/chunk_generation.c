#pragma OPENCL EXTENSION cl_khr_int64_base_atomics: enable

#define G3 0.166666667f
#define G3_2 (2.0f / 6.0f)
#define G3_3min1 (3.0f / 6.0f - 1.0f)
#define F3 0.333333333f
#define size 64
#define freq 0.0025f
#define cavefreq 0.001f
#define cavefreq2 0.00136534f

__constant int grad3[288][3] = {
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
		{ 1,1,0 },{ -1,1,0 },{ 1,-1,0 },{ -1,-1,0 },{ 1,0,1 },{ -1,0,1 },{ 1,0,-1 },{ -1,0,-1 },{ 0,1,1 },{ 0,-1,1 },{ 0,1,-1 },{ 0,-1,-1 },
	};

inline float dot2(__constant int *g, float x, float y, float z)
{
	return g[0] * x + g[1] * y + g[2] * z;
}

inline float noise3d(float xin, float yin, float zin, __constant int *p)
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
		n0 = t0 * t0 * dot2(grad3[gi0], x0, y0, z0);
	}
	float t1 = 0.5f - x1 * x1 - y1 * y1 - z1 * z1;
	if (t1 < 0) n1 = 0.0f;
	else
	{
		unsigned int gi1 = p[ii + i1 + p[jj + j1 + p[kk + k1]]];
		t1 *= t1;
		n1 = t1 * t1 * dot2(grad3[gi1], x1, y1, z1);
	}
	float t2 = 0.5f - x2 * x2 - y2 * y2 - z2 * z2;
	if (t2 < 0) n2 = 0.0f;
	else
	{
		unsigned int gi2 = p[ii + i2 + p[jj + j2 + p[kk + k2]]];
		t2 *= t2;
		n2 = t2 * t2 * dot2(grad3[gi2], x2, y2, z2);
	}
	float t3 = 0.5f - x3 * x3 - y3 * y3 - z3 * z3;
	if (t3 < 0) n3 = 0.0f;
	else
	{
		unsigned int gi3 = p[ii + 1 + p[jj + 1 + p[kk + 1]]];
		t3 *= t3;
		n3 = t3 * t3 * dot2(grad3[gi3], x3, y3, z3);
	}
	// Add contributions from each corner to get the final noise value.
	// The result is scaled to stay just inside [-1,1]
	return 32.0f * (n0 + n1 + n2 + n3);
}

inline float octavenoise3d(float x, float y, float z, int octaves, float persistence, __constant int *p)
{
	float total = 0;
	float frequency = 1;
	float amplitude = 1;
	float maxValue = 0;
	for (int i = 0; i < octaves; ++i)
	{
		total += noise3d(x * frequency, y * frequency, z * frequency, p) * amplitude;
		maxValue += amplitude;
		amplitude *= persistence;
		frequency += frequency;
	}
	return (total);
}

kernel void Generate_DATA(global char *DATA, private int x, private int y, private int z, __constant int *p, global long *sums)
{
	int xx = get_global_id(0);
	int yy = get_global_id(1);
	int zz = get_global_id(2);
	
	float value_terrain = 1.75f * octavenoise3d((x * size + xx) * freq, (y * size + yy) * freq, (z * size + zz) * freq, 8, 0.4f, p) - (y * size + yy) * 0.006f + 0.5f;
	float value1 = 13.0f * pow(fabs(octavenoise3d((x * size + xx) * cavefreq, (y * size + yy) * cavefreq, (z * size + zz) * cavefreq, 6, 0.46f, p)), 2);
	value1 -= 0.001f;
	float value2 = 13.0f * pow(fabs(octavenoise3d((x * size + xx + 1000.0f) * cavefreq2, (y * size + yy + 1000.0f) * cavefreq2, (z * size + zz + 1000.0f) * cavefreq2, 6, 0.46f, p)), 2);
	value2 -= 0.001f;
	float value_caves = min(value1 + value2, 0.002f) * 500 - 1.0f;
	value_terrain = clamp(value_terrain * 50, -1.0f, 1.0f);
	
	float mul_terrain = clamp(value_terrain * 10000.0f, 0.0f, 1.0f);
	float mul_caves = 1.0f - value_caves;
	//float value_terrain = 1.75f * octavenoise3d((x * size + xx) * freq, (y * size + yy) * freq, (z * size + zz) * freq, 8, 0.4f, p);
	//char data = (char)((float)clamp(1500000.0f * ((value/* + ((y * size + yy) % 50) * 0.003f*/) - (y * size + yy) * 0.006f + 0.5f), -32767.0f, 32766.0f) / 256.0f);
	//short data = (short)((float)clamp(value, -32767.0f, 32767.0f));
	float finalvalue = value_terrain + value_caves;
	//finalvalue += (1 - mul_terrain) * value_terrain;
	//if(value_terrain > 0)
		//finalvalue *= value_caves;
	char data = (char)((float)clamp(20000.0f * (finalvalue), -32767.0f, 32766.0f) / 256.0f);
	DATA[(xx) * size * size + (yy) * size + (zz)] = data;
	atom_add(sums, (long)data);
	atom_add(sums + 1, (long)abs(data));//, abs((int)data));
}

/*kernel void Generate_DATA(short *DATA, int x, int y, int z, int3 currentcampos)
{
	long long sum = 0;
	long long abssum = 0;
	for (int x2 = 0; x2 < size; ++x2)
	{
		for (int z2 = 0; z2 < size; ++z2)
		{
			//float value = 1.75f * simplexnoise->octavenoise3d_H1(((x - currentcampos.X - LOADING_RADIUS) * size + x2) * freq, 2, ((z - currentcampos.Z - LOADING_RADIUS) * size + z2) * freq);// , 4, 0.456f);
			for (int y2 = 0; y2 < size; ++y2)
			{
				float value = 1.75f * simplexnoise->octavenoise3d_H1(((x - currentcampos.X - LOADING_RADIUS) * size + x2) * freq, ((y - currentcampos.Y - LOADING_RADIUS) * size + y2) * freq, ((z - currentcampos.Z - LOADING_RADIUS) * size + z2) * freq);
				short data = (short)clamp(1000000.0f * (value - ((y - 1 - currentcampos.Y - LOADING_RADIUS + 2) * size + y2) * 0.004f + 0.5f), -32767.0f, 32767.0f);
				DATA[x2 * sizem2 + y2 * size + z2] = data;
				//if((y - 1 - currentcampos.Y - LOADING_RADIUS + 2) * size + y2 > 0)
				//	DATA[x2 * sizem2 + y2 * size + z2] = -100;// data;
				//else
				//	DATA[x2 * sizem2 + y2 * size + z2] = 100;
				sum += data;
				abssum += llabs(data);
				//DATA[x2 * size2 + y2 * size + z2] = (short)clamp(100000.0f * simplexnoise->octavenoise3d_H1(((x - currentcampos.X) * size + x2) * freq, ((y - currentcampos.Y) * size + y2) * freq, ((z - currentcampos.Z) * size + z2) * freq), -32767.0f, 32767.0f);
			}
		}
	}
	if (llabs(sum) == abssum)
	{
		if (sum < 0)
			return -32767;
		else
			return 32767;
	}
	return 0;
}*/








