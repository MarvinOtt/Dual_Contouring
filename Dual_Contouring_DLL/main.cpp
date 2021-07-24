#include "main.h"
#include <stdio.h>
#include <math.h>
#include <amp_math.h>
#include <stdlib.h>
#include <Windows.h>

//#define clamp(v, min, max) (v < min ? min : (v > max ? max : v))

#define LOADING_RADIUS 10
#define LOADING_HEIGHT 5

#define size 64
#define sizep1 (size+1)
#define sizes1 (size-1)
#define sizep2 (size+2)
#define sizep4 (size+4)
#define sizep5 (size+5)
#define sizep6 (size+6)
#define sizem2 (size*size)
#define sizep1m2 (sizep1*sizep1)
#define sizes1m2 (sizes1*sizes1)
#define sizep2m2 (sizep2*sizep2)
#define sizep4m2 (sizep4*sizep4)
#define sizep5m2 (sizep5*sizep5)
#define sizep6m2 (sizep6*sizep6)

#define PSUEDO_INVERSE_THRESHOLD (0.1f)
#define SVD_NUM_SWEEPS 5

//#define AS(Size, X, Y, Z) ((X) * Size * 2 + (Y) * Size + (Z))

float QEF_strength = 0.7f;

float strength = 0.85f;
#define freq 0.0025f
SimplexNoise* simplexnoise;
inline Vector3 getnormal(Vector3 p1, Vector3 p2, Vector3 p3)
{
	return Vector3::Cross(p2 - p1, p3 - p1);
}
inline float clamp(float v, float min, float max)
{
	return (v < min ? min : (v > max ? max : v));
}
void DLL_EXPORT SetSimplexNoise(int* p_ext)
{
	simplexnoise = new SimplexNoise(p_ext);
}
void DLL_EXPORT SetParameters(float str)
{
	strength = str;
}

inline float GetValueAtPosInChunk(float x, float y, float z, char* DATA)
{
	unsigned int ux = (unsigned int)x;
	unsigned int uy = (unsigned int)y;
	unsigned int uz = (unsigned int)z;

	char value_xyz = DATA[ux * sizep4m2 + uy * sizep4 + uz];
	char value_xYz = DATA[ux * sizep4m2 + (uy + 1) * sizep4 + uz];
	char value_Xyz = DATA[(ux + 1) * sizep4m2 + uy * sizep4 + uz];
	char value_XYz = DATA[(ux + 1) * sizep4m2 + (uy + 1) * sizep4 + uz];

	char value_xyZ = DATA[ux * sizep4m2 + uy * sizep4 + uz + 1];
	char value_xYZ = DATA[ux * sizep4m2 + (uy + 1) * sizep4 + uz + 1];
	char value_XyZ = DATA[(ux + 1) * sizep4m2 + uy * sizep4 + uz + 1];
	char value_XYZ = DATA[(ux + 1) * sizep4m2 + (uy + 1) * sizep4 + uz + 1];

	float xratio = x - (float)ux;
	float yratio = y - (float)uy;
	float zratio = z - (float)uz;

	float OUT1 = 0, OUT2 = 0;
	OUT1 += value_xyz * (1 - xratio) * (1 - yratio);
	OUT1 += value_xYz * (1 - xratio) * (yratio);
	OUT1 += value_Xyz * (xratio) * (1 - yratio);
	OUT1 += value_XYz * (xratio) * (yratio);
	OUT1 *= (1 - zratio);

	OUT2 += value_xyZ * (1 - xratio) * (1 - yratio);
	OUT2 += value_xYZ * (1 - xratio) * (yratio);
	OUT2 += value_XyZ * (xratio) * (1 - yratio);
	OUT2 += value_XYZ * (xratio) * (yratio);
	OUT2 *= zratio;

	return (OUT1 + OUT2) * 0.125f;
}

inline Vector3 GetNormalAtPosInChunk(float x, float y, float z, char* DATA)
{
	x += 2;
	y += 2;
	z += 2;
	//if(x > size && y > size && )

	float dis = 0.01f;
	float basevalue = GetValueAtPosInChunk(x, y, z, DATA);
	float value_xyz = GetValueAtPosInChunk(x - dis, y - dis, z - dis, DATA);
	float value_Xyz = GetValueAtPosInChunk(x + dis, y - dis, z - dis, DATA);
	float value_xYz = GetValueAtPosInChunk(x - dis, y + dis, z - dis, DATA);
	float value_XYz = GetValueAtPosInChunk(x + dis, y + dis, z - dis, DATA);

	float value_xyZ = GetValueAtPosInChunk(x - dis, y - dis, z + dis, DATA);
	float value_XyZ = GetValueAtPosInChunk(x + dis, y - dis, z + dis, DATA);
	float value_xYZ = GetValueAtPosInChunk(x - dis, y + dis, z + dis, DATA);
	float value_XYZ = GetValueAtPosInChunk(x + dis, y + dis, z + dis, DATA);
	Vector3 basenormal = Vector3(0, 0, 0);
	basenormal += Vector3(1, 1, 1) * (value_xyz - basevalue);
	basenormal += Vector3(-1, 1, 1) * (value_Xyz - basevalue);
	basenormal += Vector3(1, -1, 1) * (value_xYz - basevalue);
	basenormal += Vector3(-1, -1, 1) * (value_XYz - basevalue);

	basenormal += Vector3(1, 1, -1) * (value_xyZ - basevalue);
	basenormal += Vector3(-1, 1, -1) * (value_XyZ - basevalue);
	basenormal += Vector3(1, -1, -1) * (value_xYZ - basevalue);
	basenormal += Vector3(-1, -1, -1) * (value_XYZ - basevalue);
	return Vector3::Normalize(basenormal);
}

typedef float mat3x3[3][3];
typedef float mat3x3_tri[6];

inline Vector3 QEF4OnePlane(Vector3 planepos, Vector3 planenormal, Vector3 vertexpos)
{
	float dist = Vector3::Dot(planenormal, (vertexpos - planepos));
	return planenormal * (dist);// +Vector3(0, 10.7f, 0);
}

int DLL_EXPORT Generate_DATA(char* DATA, int x, int y, int z, int3 currentcampos)
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
				float value = 1.75f * simplexnoise->octavenoise3d_H1((x * size + x2) * freq, (y * size + y2) * freq, (z * size + z2) * freq);
				char data = (char)(clamp(1500000.0f * ((value)-(y * size + y2) * 0.006f + 0.5f), -32767.0f, 32766.0f) / 256.0f);
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
			return -128;
		else
			return 127;
	}
	return 0;
}
DLL_EXPORT unsigned char* Generate_Chunk(Vector3* VertexPos, char* DATA, int* returnlength, int chunkY)
{
	byte* states = (byte*)malloc(sizep2 * sizep2 * sizep1 * 12 * 2 * 3 + sizep2 * sizep2 * sizep1 * 3);
	Vector3* EdgePos_X = (Vector3*)states;
	Vector3* EdgeNormal_X = (Vector3*)(states + sizep2 * sizep2 * sizep1 * 12);
	Vector3* EdgePos_Y = (Vector3*)(states + sizep2 * sizep2 * sizep1 * 12 * 2);
	Vector3* EdgeNormal_Y = (Vector3*)(states + sizep2 * sizep2 * sizep1 * 12 * 3);
	Vector3* EdgePos_Z = (Vector3*)(states + sizep2 * sizep2 * sizep1 * 12 * 4);
	Vector3* EdgeNormal_Z = (Vector3*)(states + sizep2 * sizep2 * sizep1 * 12 * 5);
	byte* IsEdge_X = (byte*)(states + sizep2 * sizep2 * sizep1 * 12 * 6);
	byte* IsEdge_Y = (byte*)(states + sizep2 * sizep2 * sizep1 * 12 * 6 + sizep2 * sizep2 * sizep1);
	byte* IsEdge_Z = (byte*)(states + sizep2 * sizep2 * sizep1 * 12 * 6 + sizep2 * sizep2 * sizep1 * 2);

	int count = 0;

	// Calculating Edge Positions and Normals
	for (int x = 0; x < sizep1; ++x)
	{
		for (int y = 0; y < sizep2; ++y)
		{
			for (int z = 0; z < sizep2; ++z)
			{
				float dataxp0 = (float)DATA[(x + 1) * sizep4m2 + (y + 1) * sizep4 + (z + 1)];
				float dataxp1 = (float)DATA[(x + 2) * sizep4m2 + (y + 1) * sizep4 + (z + 1)];
				if ((dataxp0 > 0 && dataxp1 <= 0) || (dataxp0 <= 0 && dataxp1 > 0))
				{
					Vector3 pos = Vector3(x + dataxp0 / (float)(dataxp0 - dataxp1) - 1, y - 1, z - 1);
					IsEdge_X[x * sizep2m2 + y * sizep2 + z] = 1;
					EdgePos_X[x * sizep2m2 + y * sizep2 + z] = pos;
					EdgeNormal_X[x * sizep2m2 + y * sizep2 + z] = GetNormalAtPosInChunk(pos.X, pos.Y, pos.Z, DATA);
					if (y != 0 && z != 0 && x != size && y != sizep1 && z != sizep1)
						count++;
				}
				else
					IsEdge_X[x * sizep2m2 + y * sizep2 + z] = 0;
			}
		}
	}

	for (int y = 0; y < sizep1; ++y)
	{
		for (int x = 0; x < sizep2; ++x)
		{
			for (int z = 0; z < sizep2; ++z)
			{
				float datayp0 = (float)DATA[(x + 1) * sizep4m2 + (y + 1) * sizep4 + (z + 1)]; // AS(sizep2, x, y, z)
				float datayp1 = (float)DATA[(x + 1) * sizep4m2 + (y + 2) * sizep4 + (z + 1)];
				if ((datayp0 > 0 && datayp1 <= 0) || (datayp0 <= 0 && datayp1 > 0))
				{
					Vector3 pos = Vector3(x - 1, y + datayp0 / (float)(datayp0 - datayp1) - 1, z - 1);
					IsEdge_Y[y * sizep2m2 + x * sizep2 + z] = 1;
					EdgePos_Y[y * sizep2m2 + x * sizep2 + z] = pos;
					EdgeNormal_Y[y * sizep2m2 + x * sizep2 + z] = GetNormalAtPosInChunk(pos.X, pos.Y, pos.Z, DATA);
					if (x != 0 && z != 0 && y != size && x != sizep1 && z != sizep1)
						count++;
				}
				else
					IsEdge_Y[y * sizep2m2 + x * sizep2 + z] = 0;
			}
		}
	}

	for (int z = 0; z < sizep1; ++z)
	{
		for (int x = 0; x < sizep2; ++x)
		{
			for (int y = 0; y < sizep2; ++y)
			{
				float datazp0 = (float)DATA[(x + 1) * sizep4m2 + (y + 1) * sizep4 + (z + 1)];
				float datazp1 = (float)DATA[(x + 1) * sizep4m2 + (y + 1) * sizep4 + (z + 2)];
				if ((datazp0 > 0 && datazp1 <= 0) || (datazp0 <= 0 && datazp1 > 0))
				{
					Vector3 pos = Vector3(x - 1, y - 1, z + datazp0 / (float)(datazp0 - datazp1) - 1);
					IsEdge_Z[z * sizep2m2 + x * sizep2 + y] = 1;
					EdgePos_Z[z * sizep2m2 + x * sizep2 + y] = pos;
					EdgeNormal_Z[z * sizep2m2 + x * sizep2 + y] = GetNormalAtPosInChunk(pos.X, pos.Y, pos.Z, DATA);
					if (x != 0 && y != 0 && z != size && x != sizep1 && y != sizep1)
						count++;
				}
				else
					IsEdge_Z[z * sizep2m2 + x * sizep2 + y] = 0;
			}
		}
	}

	for (int x = 0; x < sizep1; ++x)
	{
		for (int y = 0; y < sizep1; ++y)
		{
			for (int z = 0; z < sizep1; ++z)
			{
				Vector3 basepos = Vector3(0, 0, 0);
				int xp1 = x + 1;
				int yp1 = y + 1;
				int zp1 = z + 1;
				int edgecount = 0;
				if (IsEdge_X[x * sizep2m2 + y * sizep2 + z])
				{
					basepos += EdgePos_X[x * sizep2m2 + y * sizep2 + z]; edgecount++;
				}
				if (IsEdge_X[x * sizep2m2 + yp1 * sizep2 + z])
				{
					basepos += EdgePos_X[x * sizep2m2 + yp1 * sizep2 + z]; edgecount++;
				}
				if (IsEdge_X[x * sizep2m2 + y * sizep2 + zp1])
				{
					basepos += EdgePos_X[x * sizep2m2 + y * sizep2 + zp1]; edgecount++;
				}
				if (IsEdge_X[x * sizep2m2 + yp1 * sizep2 + zp1])
				{
					basepos += EdgePos_X[x * sizep2m2 + yp1 * sizep2 + zp1]; edgecount++;
				}

				if (IsEdge_Y[y * sizep2m2 + x * sizep2 + z])
				{
					basepos += EdgePos_Y[y * sizep2m2 + x * sizep2 + z]; edgecount++;
				}
				if (IsEdge_Y[y * sizep2m2 + xp1 * sizep2 + z])
				{
					basepos += EdgePos_Y[y * sizep2m2 + xp1 * sizep2 + z]; edgecount++;
				}
				if (IsEdge_Y[y * sizep2m2 + x * sizep2 + zp1])
				{
					basepos += EdgePos_Y[y * sizep2m2 + x * sizep2 + zp1]; edgecount++;
				}
				if (IsEdge_Y[y * sizep2m2 + xp1 * sizep2 + zp1])
				{
					basepos += EdgePos_Y[y * sizep2m2 + xp1 * sizep2 + zp1]; edgecount++;
				}

				if (IsEdge_Z[z * sizep2m2 + x * sizep2 + y])
				{
					basepos += EdgePos_Z[z * sizep2m2 + x * sizep2 + y]; edgecount++;
				}
				if (IsEdge_Z[z * sizep2m2 + xp1 * sizep2 + y])
				{
					basepos += EdgePos_Z[z * sizep2m2 + xp1 * sizep2 + y]; edgecount++;
				}
				if (IsEdge_Z[z * sizep2m2 + x * sizep2 + yp1])
				{
					basepos += EdgePos_Z[z * sizep2m2 + x * sizep2 + yp1]; edgecount++;
				}
				if (IsEdge_Z[z * sizep2m2 + xp1 * sizep2 + yp1])
				{
					basepos += EdgePos_Z[z * sizep2m2 + xp1 * sizep2 + yp1]; edgecount++;
				}
				float onedivedgecount = 1.0f / (float)edgecount;
				basepos = basepos * onedivedgecount;
				float onedivedgecountQEF = onedivedgecount * QEF_strength;
				//Vector4 pointaccum = { 0.f, 0.f, 0.f, 0.f };
				//Vector4 ATb = { 0.f, 0.f, 0.f, 0.f };
				//mat3x3_tri ATA = { 0.f, 0.f, 0.f, 0.f, 0.f, 0.f };

				for (int i = 0; i < 8; ++i)
				{
					//Vector3 addedvec = Vector3(0, 0, 0);
					//if (IsEdge_X[x * sizep2m2 + y * sizep2 + z])
					//	qef_add(Vector4(EdgeNormal_X[x * sizep2m2 + y * sizep2 + z]), Vector4(EdgePos_X[x * sizep2m2 + y * sizep2 + z]), ATA, &ATb, &pointaccum);
					//if (IsEdge_X[x * sizep2m2 + yp1 * sizep2 + z])
					//	qef_add(Vector4(EdgeNormal_X[x * sizep2m2 + yp1 * sizep2 + z]), Vector4(EdgePos_X[x * sizep2m2 + yp1 * sizep2 + z]), ATA, &ATb, &pointaccum);
					//if (IsEdge_X[x * sizep2m2 + y * sizep2 + zp1])
					//	qef_add(Vector4(EdgeNormal_X[x * sizep2m2 + y * sizep2 + zp1]), Vector4(EdgePos_X[x * sizep2m2 + y * sizep2 + zp1]), ATA, &ATb, &pointaccum);
					//if (IsEdge_X[x * sizep2m2 + yp1 * sizep2 + zp1])
					//	qef_add(Vector4(EdgeNormal_X[x * sizep2m2 + yp1 * sizep2 + zp1]), Vector4(EdgePos_X[x * sizep2m2 + yp1 * sizep2 + zp1]), ATA, &ATb, &pointaccum);

					//if (IsEdge_Y[y * sizep2m2 + x * sizep2 + z])
					//	qef_add(Vector4(EdgeNormal_Y[y * sizep2m2 + x * sizep2 + z]), Vector4(EdgePos_Y[y * sizep2m2 + x * sizep2 + z]), ATA, &ATb, &pointaccum);
					//if (IsEdge_Y[y * sizep2m2 + xp1 * sizep2 + z])
					//	qef_add(Vector4(EdgeNormal_Y[y * sizep2m2 + xp1 * sizep2 + z]), Vector4(EdgePos_Y[y * sizep2m2 + xp1 * sizep2 + z]), ATA, &ATb, &pointaccum);
					//if (IsEdge_Y[y * sizep2m2 + x * sizep2 + zp1])
					//	qef_add(Vector4(EdgeNormal_Y[y * sizep2m2 + x * sizep2 + zp1]), Vector4(EdgePos_Y[y * sizep2m2 + x * sizep2 + zp1]), ATA, &ATb, &pointaccum);
					//if (IsEdge_Y[y * sizep2m2 + xp1 * sizep2 + zp1])
					//	qef_add(Vector4(EdgeNormal_Y[y * sizep2m2 + xp1 * sizep2 + zp1]), Vector4(EdgePos_Y[y * sizep2m2 + xp1 * sizep2 + zp1]), ATA, &ATb, &pointaccum);

					//if (IsEdge_Z[z * sizep2m2 + x * sizep2 + y])
					//	qef_add(Vector4(EdgeNormal_Z[z * sizep2m2 + x * sizep2 + y]), Vector4(EdgePos_Z[z * sizep2m2 + x * sizep2 + y]), ATA, &ATb, &pointaccum);
					//if (IsEdge_Z[z * sizep2m2 + xp1 * sizep2 + y])
					//	qef_add(Vector4(EdgeNormal_Z[z * sizep2m2 + xp1 * sizep2 + y]), Vector4(EdgePos_Z[z * sizep2m2 + xp1 * sizep2 + y]), ATA, &ATb, &pointaccum);
					//if (IsEdge_Z[z * sizep2m2 + x * sizep2 + yp1])
					//	qef_add(Vector4(EdgeNormal_Z[z * sizep2m2 + x * sizep2 + yp1]), Vector4(EdgePos_Z[z * sizep2m2 + x * sizep2 + yp1]), ATA, &ATb, &pointaccum);
					//if (IsEdge_Z[z * sizep2m2 + xp1 * sizep2 + yp1])
					//	qef_add(Vector4(EdgeNormal_Z[z * sizep2m2 + xp1 * sizep2 + yp1]), Vector4(EdgePos_Z[z * sizep2m2 + xp1 * sizep2 + yp1]), ATA, &ATb, &pointaccum);

					Vector3 addedvec = Vector3(0, 0, 0);
					if (IsEdge_X[x * sizep2m2 + y * sizep2 + z])
						addedvec += QEF4OnePlane(EdgePos_X[x * sizep2m2 + y * sizep2 + z], EdgeNormal_X[x * sizep2m2 + y * sizep2 + z], basepos);
					if (IsEdge_X[x * sizep2m2 + yp1 * sizep2 + z])
						addedvec += QEF4OnePlane(EdgePos_X[x * sizep2m2 + yp1 * sizep2 + z], EdgeNormal_X[x * sizep2m2 + yp1 * sizep2 + z], basepos);
					if (IsEdge_X[x * sizep2m2 + y * sizep2 + zp1])
						addedvec += QEF4OnePlane(EdgePos_X[x * sizep2m2 + y * sizep2 + zp1], EdgeNormal_X[x * sizep2m2 + y * sizep2 + zp1], basepos);
					if (IsEdge_X[x * sizep2m2 + yp1 * sizep2 + zp1])
						addedvec += QEF4OnePlane(EdgePos_X[x * sizep2m2 + yp1 * sizep2 + zp1], EdgeNormal_X[x * sizep2m2 + yp1 * sizep2 + zp1], basepos);

					if (IsEdge_Y[y * sizep2m2 + x * sizep2 + z])
						addedvec += QEF4OnePlane(EdgePos_Y[y * sizep2m2 + x * sizep2 + z], EdgeNormal_Y[y * sizep2m2 + x * sizep2 + z], basepos);
					if (IsEdge_Y[y * sizep2m2 + xp1 * sizep2 + z])
						addedvec += QEF4OnePlane(EdgePos_Y[y * sizep2m2 + xp1 * sizep2 + z], EdgeNormal_Y[y * sizep2m2 + xp1 * sizep2 + z], basepos);
					if (IsEdge_Y[y * sizep2m2 + x * sizep2 + zp1])
						addedvec += QEF4OnePlane(EdgePos_Y[y * sizep2m2 + x * sizep2 + zp1], EdgeNormal_Y[y * sizep2m2 + x * sizep2 + zp1], basepos);
					if (IsEdge_Y[y * sizep2m2 + xp1 * sizep2 + zp1])
						addedvec += QEF4OnePlane(EdgePos_Y[y * sizep2m2 + xp1 * sizep2 + zp1], EdgeNormal_Y[y * sizep2m2 + xp1 * sizep2 + zp1], basepos);

					if (IsEdge_Z[z * sizep2m2 + x * sizep2 + y])
						addedvec += QEF4OnePlane(EdgePos_Z[z * sizep2m2 + x * sizep2 + y], EdgeNormal_Z[z * sizep2m2 + x * sizep2 + y], basepos);
					if (IsEdge_Z[z * sizep2m2 + xp1 * sizep2 + y])
						addedvec += QEF4OnePlane(EdgePos_Z[z * sizep2m2 + xp1 * sizep2 + y], EdgeNormal_Z[z * sizep2m2 + xp1 * sizep2 + y], basepos);
					if (IsEdge_Z[z * sizep2m2 + x * sizep2 + yp1])
						addedvec += QEF4OnePlane(EdgePos_Z[z * sizep2m2 + x * sizep2 + yp1], EdgeNormal_Z[z * sizep2m2 + x * sizep2 + yp1], basepos);
					if (IsEdge_Z[z * sizep2m2 + xp1 * sizep2 + yp1])
						addedvec += QEF4OnePlane(EdgePos_Z[z * sizep2m2 + xp1 * sizep2 + yp1], EdgeNormal_Z[z * sizep2m2 + xp1 * sizep2 + yp1], basepos);
					basepos += addedvec * onedivedgecountQEF;
				}
				//Vector4 solved_position = { 0.f, 0.f, 0.f, 0.f };
				//float error = qef_solve(ATA, ATb, pointaccum, &solved_position);

				VertexPos[x * sizep1m2 + y * sizep1 + z] = basepos;// Vector3(solved_position.X, solved_position.Y, solved_position.Z);
			}
		}
	}






	//VertexPositionColorNormal_noTexCoo *vertexes = (VertexPositionColorNormal_noTexCoo*)malloc(counter * 6 * 28);
	*returnlength = count * 6;

	return states;
}

/*void DLL_EXPORT ClearArrayofReferences(void* pointer, int length)
{
memset(pointer, 0, length);
}*/

void DLL_EXPORT FreeChunkGenerationMemory(unsigned char* states)
{
	free(states);
}

void DLL_EXPORT Generate_Vertexes(VertexPositionColorNormal_noTexCoo* vertexes, unsigned char* states, char* DATA, Vector3* VertexPos, int chunkY)
{
	Vector3* EdgePos_X = (Vector3*)states;
	Vector3* EdgeNormal_X = (Vector3*)(states + sizep2 * sizep2 * sizep1 * 12);
	Vector3* EdgePos_Y = (Vector3*)(states + sizep2 * sizep2 * sizep1 * 12 * 2);
	Vector3* EdgeNormal_Y = (Vector3*)(states + sizep2 * sizep2 * sizep1 * 12 * 3);
	Vector3* EdgePos_Z = (Vector3*)(states + sizep2 * sizep2 * sizep1 * 12 * 4);
	Vector3* EdgeNormal_Z = (Vector3*)(states + sizep2 * sizep2 * sizep1 * 12 * 5);
	byte* IsEdge_X = (byte*)(states + sizep2 * sizep2 * sizep1 * 12 * 6);
	byte* IsEdge_Y = (byte*)(states + sizep2 * sizep2 * sizep1 * 12 * 6 + sizep2 * sizep2 * sizep1);
	byte* IsEdge_Z = (byte*)(states + sizep2 * sizep2 * sizep1 * 12 * 6 + sizep2 * sizep2 * sizep1 * 2);

	long count = -1;
	Color col(0, 0, 0, 0);
	Vector3 normal, p1, p2, p3, p4;

	float minlight = 0.15f;
	Vector3 lightdir = Vector3::Normalize(Vector3(0.0f, 1.0f, 0.0f));

	for (int x = 0; x < size; ++x)
	{
		for (int y = 0; y < size; ++y)
		{
			for (int z = 0; z < size; ++z)
			{
				int xp1 = x + 1;
				int yp1 = y + 1;
				int zp1 = z + 1;
				if (IsEdge_X[x * sizep2m2 + yp1 * sizep2 + zp1])
				{

					// Generate Quad
					p1 = VertexPos[x * sizep1m2 + y * sizep1 + z]; p2 = VertexPos[x * sizep1m2 + yp1 * sizep1 + z]; p3 = VertexPos[x * sizep1m2 + y * sizep1 + zp1]; p4 = VertexPos[x * sizep1m2 + yp1 * sizep1 + zp1];
					Vector3 cross = Vector3::Normalize(Vector3::Cross(p1 - p3, p4 - p3) + Vector3::Cross(p1 - p4, p2 - p4));
					Vector3 edgepos = EdgePos_X[x * sizep2m2 + yp1 * sizep2 + zp1];

					if (Vector3::Dot(EdgeNormal_X[x * sizep2m2 + yp1 * sizep2 + zp1], cross) > 0)
					{
						normal = Vector3::Normalize(getnormal(p1, p3, p4)); col = Color(Vector3(0.2f, 0.1f, 0.0f) * clamp(-Vector3::Dot(lightdir, normal), minlight + (rand() % 100) * 0.0005f, 1.0f));
						vertexes[++count] = VertexPositionColorNormal_noTexCoo(p1, normal, col);
						vertexes[++count] = VertexPositionColorNormal_noTexCoo(p3, normal, col);
						vertexes[++count] = VertexPositionColorNormal_noTexCoo(p4, normal, col);
						normal = Vector3::Normalize(getnormal(p1, p4, p2)); col = Color(Vector3(0.2f, 0.1f, 0.0f) * clamp(-Vector3::Dot(lightdir, normal), minlight + (rand() % 100) * 0.0005f, 1.0f));
						vertexes[++count] = VertexPositionColorNormal_noTexCoo(p1, normal, col);
						vertexes[++count] = VertexPositionColorNormal_noTexCoo(p4, normal, col);
						vertexes[++count] = VertexPositionColorNormal_noTexCoo(p2, normal, col);
					}
					else
					{
						normal = Vector3::Normalize(getnormal(p1, p4, p3)); col = Color(Vector3(0.2f, 0.1f, 0.0f) * clamp(-Vector3::Dot(lightdir, normal), minlight + (rand() % 100) * 0.0005f, 1.0f));
						vertexes[++count] = VertexPositionColorNormal_noTexCoo(p1, normal, col);
						vertexes[++count] = VertexPositionColorNormal_noTexCoo(p4, normal, col);
						vertexes[++count] = VertexPositionColorNormal_noTexCoo(p3, normal, col);
						normal = Vector3::Normalize(getnormal(p1, p2, p4)); col = Color(Vector3(0.2f, 0.1f, 0.0f) * clamp(-Vector3::Dot(lightdir, normal), minlight + (rand() % 100) * 0.0005f, 1.0f));
						vertexes[++count] = VertexPositionColorNormal_noTexCoo(p1, normal, col);
						vertexes[++count] = VertexPositionColorNormal_noTexCoo(p2, normal, col);
						vertexes[++count] = VertexPositionColorNormal_noTexCoo(p4, normal, col);
					}
				}
			}
		}
	}

	for (int y = 0; y < size; ++y)
	{
		for (int x = 0; x < size; ++x)
		{
			for (int z = 0; z < size; ++z)
			{
				int xp1 = x + 1;
				int yp1 = y + 1;
				int zp1 = z + 1;
				if (IsEdge_Y[y * sizep2m2 + xp1 * sizep2 + zp1])
				{
					// Generate Quad
					p1 = VertexPos[x * sizep1m2 + y * sizep1 + z]; p2 = VertexPos[xp1 * sizep1m2 + y * sizep1 + z]; p3 = VertexPos[x * sizep1m2 + y * sizep1 + zp1]; p4 = VertexPos[xp1 * sizep1m2 + y * sizep1 + zp1];
					Vector3 cross = Vector3::Normalize(Vector3::Cross(p1 - p3, p4 - p3) + Vector3::Cross(p1 - p4, p2 - p4));
					if (Vector3::Dot(EdgeNormal_Y[y * sizep2m2 + xp1 * sizep2 + zp1], cross) < 0)
					{
						normal = Vector3::Normalize(getnormal(p1, p4, p3)); col = Color(Vector3(0.2f, 0.1f, 0.0f) * clamp(-Vector3::Dot(lightdir, normal), minlight + (rand() % 100) * 0.0005f, 1.0f));
						vertexes[++count] = VertexPositionColorNormal_noTexCoo(p1, normal, col);
						vertexes[++count] = VertexPositionColorNormal_noTexCoo(p4, normal, col);
						vertexes[++count] = VertexPositionColorNormal_noTexCoo(p3, normal, col);
						normal = Vector3::Normalize(getnormal(p1, p2, p4)); col = Color(Vector3(0.2f, 0.1f, 0.0f) * clamp(-Vector3::Dot(lightdir, normal), minlight + (rand() % 100) * 0.0005f, 1.0f));
						vertexes[++count] = VertexPositionColorNormal_noTexCoo(p1, normal, col);
						vertexes[++count] = VertexPositionColorNormal_noTexCoo(p2, normal, col);
						vertexes[++count] = VertexPositionColorNormal_noTexCoo(p4, normal, col);
					}
					else
					{
						normal = Vector3::Normalize(getnormal(p1, p3, p4)); col = Color(Vector3(0.2f, 0.1f, 0.0f) * clamp(-Vector3::Dot(lightdir, normal), minlight + (rand() % 100) * 0.0005f, 1.0f));
						vertexes[++count] = VertexPositionColorNormal_noTexCoo(p1, normal, col);
						vertexes[++count] = VertexPositionColorNormal_noTexCoo(p3, normal, col);
						vertexes[++count] = VertexPositionColorNormal_noTexCoo(p4, normal, col);
						normal = Vector3::Normalize(getnormal(p1, p4, p2)); col = Color(Vector3(0.2f, 0.1f, 0.0f) * clamp(-Vector3::Dot(lightdir, normal), minlight + (rand() % 100) * 0.0005f, 1.0f));
						vertexes[++count] = VertexPositionColorNormal_noTexCoo(p1, normal, col);
						vertexes[++count] = VertexPositionColorNormal_noTexCoo(p4, normal, col);
						vertexes[++count] = VertexPositionColorNormal_noTexCoo(p2, normal, col);
					}
				}
			}
		}
	}

	for (int z = 0; z < size; ++z)
	{
		for (int x = 0; x < size; ++x)
		{
			for (int y = 0; y < size; ++y)
			{
				int xp1 = x + 1;
				int yp1 = y + 1;
				int zp1 = z + 1;
				if (IsEdge_Z[z * sizep2m2 + xp1 * sizep2 + yp1])
				{
					// Generate Quad
					p1 = VertexPos[x * sizep1m2 + y * sizep1 + z]; p2 = VertexPos[xp1 * sizep1m2 + y * sizep1 + z]; p3 = VertexPos[x * sizep1m2 + yp1 * sizep1 + z]; p4 = VertexPos[xp1 * sizep1m2 + yp1 * sizep1 + z];
					Vector3 cross = Vector3::Normalize(Vector3::Cross(p1 - p3, p4 - p3) + Vector3::Cross(p1 - p4, p2 - p4));
					if (Vector3::Dot(EdgeNormal_Z[z * sizep2m2 + xp1 * sizep2 + yp1], cross) > 0)
					{
						normal = Vector3::Normalize(getnormal(p1, p3, p4)); col = Color(Vector3(0.2f, 0.1f, 0.0f) * clamp(-Vector3::Dot(lightdir, normal), minlight + (rand() % 100) * 0.0005f, 1.0f));
						vertexes[++count] = VertexPositionColorNormal_noTexCoo(p1, normal, col);
						vertexes[++count] = VertexPositionColorNormal_noTexCoo(p3, normal, col);
						vertexes[++count] = VertexPositionColorNormal_noTexCoo(p4, normal, col);
						normal = Vector3::Normalize(getnormal(p1, p4, p2)); col = Color(Vector3(0.2f, 0.1f, 0.0f) * clamp(-Vector3::Dot(lightdir, normal), minlight + (rand() % 100) * 0.0005f, 1.0f));
						vertexes[++count] = VertexPositionColorNormal_noTexCoo(p1, normal, col);
						vertexes[++count] = VertexPositionColorNormal_noTexCoo(p4, normal, col);
						vertexes[++count] = VertexPositionColorNormal_noTexCoo(p2, normal, col);
					}
					else
					{
						normal = Vector3::Normalize(getnormal(p1, p4, p3)); col = Color(Vector3(0.2f, 0.1f, 0.0f) * clamp(-Vector3::Dot(lightdir, normal), minlight + (rand() % 100) * 0.0005f, 1.0f));
						vertexes[++count] = VertexPositionColorNormal_noTexCoo(p1, normal, col);
						vertexes[++count] = VertexPositionColorNormal_noTexCoo(p4, normal, col);
						vertexes[++count] = VertexPositionColorNormal_noTexCoo(p3, normal, col);
						normal = Vector3::Normalize(getnormal(p1, p2, p4)); col = Color(Vector3(0.2f, 0.1f, 0.0f) * clamp(-Vector3::Dot(lightdir, normal), minlight + (rand() % 100) * 0.0005f, 1.0f));
						vertexes[++count] = VertexPositionColorNormal_noTexCoo(p1, normal, col);
						vertexes[++count] = VertexPositionColorNormal_noTexCoo(p2, normal, col);
						vertexes[++count] = VertexPositionColorNormal_noTexCoo(p4, normal, col);
					}
				}
			}
		}
	}

	//free(states);
}





extern "C" DLL_EXPORT BOOL APIENTRY DllMain(HINSTANCE hinstDLL, DWORD fdwReason, LPVOID lpvReserved)
{
	switch (fdwReason)
	{
	case DLL_PROCESS_ATTACH:
		// attach to process
		// return FALSE to fail DLL load
		break;

	case DLL_PROCESS_DETACH:
		// detach from process
		break;

	case DLL_THREAD_ATTACH:
		// attach to thread
		break;

	case DLL_THREAD_DETACH:
		// detach from thread
		break;
	}
	return TRUE; // succesful
}