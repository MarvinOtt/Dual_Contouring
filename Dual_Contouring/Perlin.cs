using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Graphics.PackedVector;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;

namespace Dual_Contouring
{
    public class Perlin
    {
        // CONSTS
        const float G3 = 1.0f / 6.0f;
        const float G3_2 = 2.0f / 6.0f;
        const float G3_3min1 = 3.0f / 6.0f - 1.0f;
        const float F3 = 1.0f / 3.0f;

        public int repeat;
        private static int[][] grad3 = {
            new int[] {1,1,0}, new int[] {-1,1,0}, new int[] {1,-1,0}, new int[] {-1,-1,0}, new int[] {1,0,1}, new int[] {-1,0,1}, new int[] {1,0,-1}, new int[] {-1,0,-1}, new int[] {0,1,1}, new int[] {0,-1,1}, new int[] {0,1,-1}, new int[] {0,-1,-1}
        };
        public static uint[] p;
        public uint[] p2;
        public Vector2[] vecs;
        public bool disposed;
        float[,] pointvalue;
        float pointvalue00;
        float pointvalue10;
        float pointvalue01;
        float pointvalue11;
        public Perlin(int repeat, int Seed)
        {
            this.repeat = repeat;
            Random r5 = new Random(Seed);
            permutation = new uint[256];
            p2 = new uint[1024];
            vecs = new Vector2[1024];
            pointvalue = new float[2, 2];
            for (int i = 0; i < 256; i++)
            {
                permutation[i] = (uint)r5.Next(0, 256);
            }
            for (int i = 0; i < 1024; i++)
            {
                p2[i] = (uint)r5.Next(0, 1024);
            }
            for (int i = 0; i < 1024; i++)
            {
                float rotation = (float)((r5.Next(0, 10000) / 10000.0f) * Math.PI);
                vecs[i] = new Vector2((float)Math.Sin(rotation), (float)Math.Cos(rotation)) * (r5.Next(1, 1000) / 1000.0f);
            }
            Perlin2();
        }
        public double OctavePerlin(double x, double y, double z, int octaves, double persistence)
        {
            double total = 0;
            double frequency = 1;
            double amplitude = 1;
            double maxValue = 0;            // Used for normalizing result to 0.0 - 1.0
            for (int i = 0; i < octaves; i++)
            {
                //total += perlin(x * frequency, y * frequency, z * frequency) * amplitude;
                maxValue += amplitude;
                amplitude *= persistence;
                frequency *= 2;
            }
            return total / maxValue;
        }

        public uint[] permutation;
        public int[] permutation2;                                                // Doubled permutation to avoid overflow
        void Perlin2()
        {
            p = new uint[512];
            for (int x = 0; x < 512; x++)
            {
                p[x] = permutation[x % 256];
            }
        }

        public double speedOctavePerlin2D(float x, float y, int octaves, double persistence)
        {
            double total = 0;
            float frequency = 1;
            double amplitude = 1;
            double maxValue = 0;            // Used for normalizing result to 0.0 - 1.0
            for (int i = 0; i < octaves; i++)
            {
                total += speedperlin2D(x * frequency, y * frequency) * amplitude;
                maxValue += amplitude;
                amplitude *= persistence;
                frequency *= 2;
            }
            return total / maxValue;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float OctavePerlin3D(float x, float y, float z, int octaves, float persistence)
        {
            float total = 0;
            float frequency = 1;
            float amplitude = 1;
            float maxValue = 0;
            //float ttt = (float)(1 / (Math.Pow(1.0f / 0.265f, octaves)));

            for (int i = 0; i < octaves; i++)
            {
                total += simplexnoise(x * frequency, y * frequency, z * frequency) * amplitude;
                maxValue += amplitude;
                amplitude *= persistence;
                frequency += frequency;
                //if (total - amplitude > 0.15f || total + amplitude < -0.15f)
                //break;
            }
            return total / maxValue;
        }
        public float speedperlin3D(float x, float y)
        {
            x += 100;
            y += 100;
            int xcoo, ycoo;
            xcoo = (int)x;
            ycoo = (int)y;
            pointvalue00 = p[((p[(((xcoo) * 734) % 256)] + p[(((ycoo) * 346) % 256)]) / 2)] / 256.0f;
            pointvalue01 = p[((p[(((xcoo) * 734) % 256)] + p[(((ycoo + 1) * 346) % 256)]) / 2)] / 256.0f;
            pointvalue10 = p[((p[(((xcoo + 1) * 734) % 256)] + p[(((ycoo) * 346) % 256)]) / 2)] / 256.0f;
            pointvalue11 = p[((p[(((xcoo + 1) * 734) % 256)] + p[(((ycoo + 1) * 346) % 256)]) / 2)] / 256.0f;
            float höhe = 0;
            höhe += fade(1 - (x - xcoo)) * pointvalue00 * fade(1 - (y - ycoo));
            höhe += fade(x - xcoo) * pointvalue10 * fade(1 - (y - ycoo));
            höhe += fade(1 - (x - xcoo)) * pointvalue01 * fade(y - ycoo);
            höhe += fade(x - xcoo) * pointvalue11 * fade(y - ycoo);
            return höhe / 1.5f;
        }
        public float speedperlin2D(float x, float y)
        {
            x += 100;
            y += 100;
            int xcoo, ycoo;
            xcoo = (int)x;
            ycoo = (int)y;
            pointvalue00 = p[((p[(((xcoo) * 734) % 256)] + p[(((ycoo) * 346) % 256)]) / 2)] / 256.0f;
            pointvalue01 = p[((p[(((xcoo) * 734) % 256)] + p[(((ycoo + 1) * 346) % 256)]) / 2)] / 256.0f;
            pointvalue10 = p[((p[(((xcoo + 1) * 734) % 256)] + p[(((ycoo) * 346) % 256)]) / 2)] / 256.0f;
            pointvalue11 = p[((p[(((xcoo + 1) * 734) % 256)] + p[(((ycoo + 1) * 346) % 256)]) / 2)] / 256.0f;
            float höhe = 0;
            höhe += fade(1 - (x - xcoo)) * pointvalue00 * fade(1 - (y - ycoo));
            höhe += fade(x - xcoo) * pointvalue10 * fade(1 - (y - ycoo));
            höhe += fade(1 - (x - xcoo)) * pointvalue01 * fade(y - ycoo);
            höhe += fade(x - xcoo) * pointvalue11 * fade(y - ycoo);
            return höhe / 1.5f;

            /*x += 100;
            y += 100;
            int xcoo, ycoo;
            xcoo = (int)x;
            ycoo = (int)y;
            Vector2 point = new Vector2(x, y);

            pointvector00 = vecs[p2[((p2[(((xcoo) * 734) % 1024)] + p2[(((ycoo) * 734) % 1024)]) / 2)]];
            pointvector01 = vecs[p2[((p2[(((xcoo) * 734) % 1024)] + p2[(((ycoo + 1) * 734) % 1024)]) / 2)]];
            pointvector10 = vecs[p2[((p2[(((xcoo + 1) * 734) % 1024)] + p2[(((ycoo) * 734) % 1024)]) / 2)]];
            pointvector11 = vecs[p2[((p2[(((xcoo + 1) * 734) % 1024)] + p2[(((ycoo + 1) * 734) % 1024)]) / 2)]];

            pointvalue00 = Vector2.Dot(pointvector00, point - new Vector2(xcoo, ycoo));
            pointvalue01 = Vector2.Dot(pointvector01, point - new Vector2(xcoo, ycoo + 1));
            pointvalue10 = Vector2.Dot(pointvector10, point - new Vector2(xcoo + 1, ycoo));
            pointvalue11 = Vector2.Dot(pointvector11, point - new Vector2(xcoo + 1, ycoo + 1));
            float höhe = 0;
            höhe += fade(1 - (x - xcoo)) * pointvalue00 * fade(1 - (y - ycoo));
            höhe += fade(x - xcoo) * pointvalue10 * fade(1 - (y - ycoo));
            höhe += fade(1 - (x - xcoo)) * pointvalue01 * fade(y - ycoo);
            höhe += fade(x - xcoo) * pointvalue11 * fade(y - ycoo);
            return höhe/1.5f;*/
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int fastfloor(float x)
        {
            int xx = (int)x;
            return x > 0 ? xx : xx - 1;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float dot(int[] g, float x, float y, float z)
        {
            return g[0] * x + g[1] * y + g[2] * z;
        }
        public static float simplexnoise(float xin, float yin, float zin)
        {

            float n0, n1, n2, n3; // Noise contributions from the four corners
                                  // Skew the input space to determine which simplex cell we're in
            float s = (xin + yin + zin) * F3; // Very nice and simple skew factor for 3D
            int i = fastfloor(xin + s);
            int j = fastfloor(yin + s);
            int k = fastfloor(zin + s);
            float t = (i + j + k) * G3;
            float x0 = xin - i + t; // The x,y,z distances from the cell origin
            float y0 = yin - j + t;
            float z0 = zin - k + t;
            // For the 3D case, the simplex shape is a slightly irregular tetrahedron.
            // Determine which simplex we are in.
            uint i1, j1, k1; // Offsets for second corner of simplex in (i,j,k) coords
            uint i2, j2, k2; // Offsets for third corner of simplex in (i,j,k) coords
            if (x0 >= y0)
            {
                j1 = 0;
                i2 = 1;
                if (y0 >= z0)
                { i1 = 1; k1 = 0; j2 = 1; k2 = 0; } // X Y Z order
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
            int ii = i & 255;
            int jj = j & 255;
            int kk = k & 255;
            // Calculate the contribution from the four corners
            float t0 = 0.6f - x0 * x0 - y0 * y0 - z0 * z0;
            if (t0 < 0) n0 = 0.0f;
            else
            {
                uint gi0 = p[ii + p[jj + p[kk]]] % 12;
                t0 *= t0;
                n0 = t0 * t0 * dot(grad3[gi0], x0, y0, z0);
            }
            float t1 = 0.6f - x1 * x1 - y1 * y1 - z1 * z1;
            if (t1 < 0) n1 = 0.0f;
            else
            {
                uint gi1 = p[ii + i1 + p[jj + j1 + p[kk + k1]]] % 12;
                t1 *= t1;
                n1 = t1 * t1 * dot(grad3[gi1], x1, y1, z1);
            }
            float t2 = 0.6f - x2 * x2 - y2 * y2 - z2 * z2;
            if (t2 < 0) n2 = 0.0f;
            else
            {
                uint gi2 = p[ii + i2 + p[jj + j2 + p[kk + k2]]] % 12;
                t2 *= t2;
                n2 = t2 * t2 * dot(grad3[gi2], x2, y2, z2);
            }
            float t3 = 0.6f - x3 * x3 - y3 * y3 - z3 * z3;
            if (t3 < 0) n3 = 0.0f;
            else
            {
                uint gi3 = p[ii + 1 + p[jj + 1 + p[kk + 1]]] % 12;
                t3 *= t3;
                n3 = t3 * t3 * dot(grad3[gi3], x3, y3, z3);
            }
            // Add contributions from each corner to get the final noise value.
            // The result is scaled to stay just inside [-1,1]
            return 32.0f * (n0 + n1 + n2 + n3);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float perlin3d(float x, float y, float z)
        {
            uint xi = (uint)x;                              // Calculate the "unit cube" that the point asked will be located in
            uint yi = (uint)y;                              // The left bound is ( |_x_|,|_y_|,|_z_| ) and the right bound is that
            uint zi = (uint)z;                              // plus 1.  Next we calculate the location (from 0.0 to 1.0) in that cube.
            float xf = x - (uint)x;                             // We also fade the location to smooth the result.
            float yf = y - (uint)y;
            float zf = z - (uint)z;
            float u = fade(xf);
            float v = fade(yf);
            float w = fade(zf);
            uint aaa, aba, aab, abb, baa, bba, bab, bbb;
            aaa = p[p[p[xi] + yi] + zi];
            aba = p[p[p[xi] + yi + 1] + zi];
            aab = p[p[p[xi] + yi] + zi + 1];
            abb = p[p[p[xi] + yi + 1] + zi + 1];
            baa = p[p[p[xi + 1] + yi] + zi];
            bba = p[p[p[xi + 1] + yi + 1] + zi];
            bab = p[p[p[xi + 1] + yi] + zi + 1];
            bbb = p[p[p[xi + 1] + yi + 1] + zi + 1];
            float x1, x2, y1, y2;
            x1 = lerp(grad(aaa, xf, yf, zf),                // The gradient function calculates the dot product between a pseudorandom
                        grad(baa, xf - 1, yf, zf),              // gradient vector and the vector from the input coordinate to the 8
                        u);                                     // surrounding points in its unit cube.
            x2 = lerp(grad(aba, xf, yf - 1, zf),                // This is all then lerped together as a sort of weighted average based on the faded (u,v,w)
                        grad(bba, xf - 1, yf - 1, zf),              // values we made earlier.
                          u);
            y1 = lerp(x1, x2, v);
            x1 = lerp(grad(aab, xf, yf, zf - 1),
                        grad(bab, xf - 1, yf, zf - 1),
                        u);
            x2 = lerp(grad(abb, xf, yf - 1, zf - 1),
                          grad(bbb, xf - 1, yf - 1, zf - 1),
                          u);
            y2 = lerp(x1, x2, v);
            return (lerp(y1, y2, w));                       // For convenience we don´t bound it to 0 - 1 (theoretical min/max before is -1 - 1)
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint inc(uint num)
        {
            num++;
            return num;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float grad(uint hash, float x, float y, float z)
        {
            uint h = hash & 15;                                  // Take the hashed value and take the first 4 bits of it (15 == 0b1111)
            float u = h < 8 /* 0b1000 */ ? x : y;              // If the most significant bit (MSB) of the hash is 0 then set u = x.  Otherwise y.
            float v;                                           // In Ken Perlin's original implementation this was another conditional operator (?:).  I
            // expanded it for readability.
            if (h < 4 /* 0b0100 */)                             // If the first and second significant bits are 0 set v = y
                v = y;
            else if (h == 12 /* 0b1100 */ || h == 14 /* 0b1110*/)// If the first and second significant bits are 1 set v = x
                v = x;
            else                                                // If the first and second significant bits are not equal (0/1, 1/0) set v = z
                v = z;
            return ((h & 1) == 0 ? u : -u) + ((h & 2) == 0 ? v : -v); // Use the last 2 bits to decide if u and v are positive or negative.  Then return their addition.
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float fade(float t)
        {
            // Fade function as defined by Ken Perlin.  This eases coordinate values
            // so that they will "ease" towards integral values.  This ends up smoothing
            // the final output.
            return (t * t * t * (t * (t * 6 - 15) + 10));         // 6t^5 - 15t^4 + 10t^3
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float lerp(float a, float b, float x)
        {
            return a + x * (b - a);
        }
    }
}
