#pragma once
#ifndef __MAIN_H__
#define __MAIN_H__

#include <math.h>
#include "SimplexNoise.h"

/*  To use this exported function of dll, include this header
*  in your project.
*/

#ifdef BUILD_DLL
#define DLL_EXPORT _declspec(dllexport)
#else
#define DLL_EXPORT _declspec(dllexport)
#endif


#ifdef __cplusplus
extern "C"
{
#endif
	typedef struct int3
	{
	public: int X, Y, Z;

	public: int3(int x, int y, int z)
	{
		X = x;
		Y = y;
		Z = z;
	}
	public: inline int3 operator -(int3 i1)
	{
		return int3(i1.X - X, i1.Y - Y, i1.Z - Z);
	}
	public: inline int3 operator +(int3 i1)
	{
		return int3(i1.X + X, i1.Y + Y, i1.Z + Z);
	}
	public: inline bool operator ==(int3 i1)
	{
		return (i1.X == X && i1.Y == Y && i1.Z == Z);
	}
	public: inline bool operator !=(int3 i1)
	{
		return !(i1.X == X && i1.Y == Y && i1.Z == Z);
	}

	}int3;

	typedef struct Vector3
	{
		Vector3()
		{
			X = 0;
			Y = 0;
			Z = 0;
		}
		Vector3(float x, float y, float z)
		{
			X = x;
			Y = y;
			Z = z;
		}
	public: inline Vector3 operator-(Vector3 v)
	{
		return Vector3(v.X - X, v.Y - Y, v.Z - Z);
	}
	public: inline Vector3 operator-()
	{
		return Vector3(-X, -Y, -Z);
	}
	public: inline Vector3 operator+(Vector3 v)
	{
		return Vector3(v.X + X, v.Y + Y, v.Z + Z);
	}
	public: inline void operator+=(Vector3 v)
	{
		X = X + v.X;
		Y = Y + v.Y;
		Z = Z + v.Z;
		return;
	}
	public: inline void operator-=(Vector3 v)
	{
		X = X - v.X;
		Y = Y - v.Y;
		Z = Z - v.Z;
		return;
	}
	public: inline void operator=(Vector3 v)
	{
		X = v.X;
		Y = v.Y;
		Z = v.Z;
		return;
	}
	public: inline Vector3 operator/(float s)
	{
		float onedivs = 1.0f / s;
		return Vector3(X * onedivs, Y * onedivs, Z * onedivs);
	}
	public: inline Vector3 operator*(float s)
	{
		return Vector3(X * s, Y * s, Z * s);
	}
	public: static Vector3 Cross(Vector3 v1, Vector3 v2)
	{
		float x = v1.Y * v2.Z - v2.Y * v1.Z;
		float y = -(v1.X * v2.Z - v2.X * v1.Z);
		float z = v1.X * v2.Y - v2.X * v1.Y;
		return Vector3(x, y, z);
	}
	public: static float Dot(Vector3 v1, Vector3 v2)
	{
		return v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z;
	}
	public: static Vector3 Normalize(Vector3 v)
	{
		return v / (sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z));
	}
	public:
		float X;
		float Y;
		float Z;
	}Vector3;

	typedef struct Vector4
	{
		Vector4()
		{
			X = 0;
			Y = 0;
			Z = 0;
			W = 0;
		}
		Vector4(float x, float y, float z, float w)
		{
			X = x;
			Y = y;
			Z = z;
			W = w;
		}
		Vector4(Vector3 vec)
		{
			X = vec.X;
			Y = vec.Y;
			Z = vec.Z;
			W = 0;
		}
	public: inline Vector4 operator-(Vector4 v)
	{
		return Vector4(v.X - X, v.Y - Y, v.Z - Z, v.W - W);
	}
	public: inline Vector4 operator-()
	{
		return Vector4(-X, -Y, -Z, -W);
	}
	public: inline Vector4 operator+(Vector4 v)
	{
		return Vector4(v.X + X, v.Y + Y, v.Z + Z, v.W + W);
	}
	public: inline void operator+=(Vector4 v)
	{
		X = X + v.X;
		Y = Y + v.Y;
		Z = Z + v.Z;
		W = W + v.W;
		return;
	}
	public: inline void operator-=(Vector4 v)
	{
		X = X - v.X;
		Y = Y - v.Y;
		Z = Z - v.Z;
		W = W - v.W;
		return;
	}
	public: inline void operator=(Vector4 v)
	{
		X = v.X;
		Y = v.Y;
		Z = v.Z;
		W = v.W;
		return;
	}
	public: inline Vector4 operator/(float s)
	{
		float onedivs = 1.0f / s;
		return Vector4(X * onedivs, Y * onedivs, Z * onedivs, W * onedivs);
	}
	public: inline Vector4 operator*(float s)
	{
		return Vector4(X * s, Y * s, Z * s, W * s);
	}
	
	public: static float Dot(Vector4 v1, Vector4 v2)
	{
		return v1.X * v2.X + v1.Y * v2.Y + v1.Z * v2.Z + v1.W * v2.W;
	}
	public: static Vector4 Normalize(Vector4 v)
	{
		return v / (sqrt(v.X * v.X + v.Y * v.Y + v.Z * v.Z + v.W * v.W));
	}
	public:
		float X;
		float Y;
		float Z;
		float W;
	}Vector4;

	typedef struct Color
	{
	public: Color()
	{
		data = 0;
	}
	public: Color(unsigned char r, unsigned char g, unsigned char b, unsigned char a)
	{
		data = a * 256 * 256 * 256 + b * 256 * 256 + g * 256 + r;
	}
	public: Color(Vector3 v)
	{
		v.X = fabsf(v.X);
		v.Y = fabsf(v.Y);
		v.Z = fabsf(v.Z);
		data = (unsigned int)(255 * 256 * 256 * 256) + (unsigned int)(v.Z * 255) * 256 * 256 + (unsigned int)(v.Y * 255) * 256 + (unsigned int)(v.X * 255);
	}
	public: inline void operator=(Color c)
	{
		data = c.data;
		return;
	}
			unsigned int data;
	}Color;

	typedef struct VertexPositionColorNormal_noTexCoo
	{
		VertexPositionColorNormal_noTexCoo(Vector3 pos, Vector3 normal, Color color2)
		{
			Position = pos;
			Normal = normal;
			color = color2;
		}
	public: Vector3 Position;
	public: Vector3 Normal;
	public: Color color;
	}VertexPositionColorNormal_noTexCoo;

	void DLL_EXPORT SetSimplexNoise(int *p_ext);

	void DLL_EXPORT SetParameters(float str);

	int DLL_EXPORT Generate_DATA(char *DATA, int x, int y, int z, int3 currentcampos);

	DLL_EXPORT unsigned char*  Generate_Chunk(Vector3 *VertexPos, char *DATA, int *returnlength, int chunkY);

	void DLL_EXPORT FreeChunkGenerationMemory(unsigned char* states);

	//void DLL_EXPORT ClearArrayofReferences(void* pointer, int length);

	void DLL_EXPORT Generate_Vertexes(VertexPositionColorNormal_noTexCoo *vertexes, unsigned char* states, char *DATA, Vector3* VertexPos, int chunkY);

#ifdef __cplusplus
}
#endif

#endif // __MAIN_H__
