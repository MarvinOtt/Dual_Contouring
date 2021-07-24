//#define ISDEBUG

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static Dual_Contouring.ExtendedDatatypes;

namespace Dual_Contouring
{
    public unsafe class Chunk : IDisposable
    {
        public static GraphicsDevice graphicsdevice;
        public static int size;
        public World baseworld;
        public bool IsDisposed = false;
        public bool IsDrawable, IsGenerated;

        public int vertexlength;
        public Game1.VertexPositionColorNormal_noTexCoo[] vertexes;
        private byte[,,] IsVertex;
        private Vector3[,,] VertexPos;
        private sbyte[,,] DATA;
        public VertexBuffer vertexbuffer;
        public int3 chunkpos, worldarraypos;
        private BoundingBox hitbox;

        public Chunk(World baseworld, int3 CHpos)
        {
            IsDrawable = false;
            this.baseworld = baseworld;
            chunkpos = CHpos;
            /*if(IsVertex == null)
				IsVertex = new byte[size + 3, size + 3, size + 3];
			if(VertexPos == null)
				VertexPos = new Vector3[size + 3, size + 3, size + 3];
			if(DATA == null)
				DATA = new short[size + 6, size + 6, size + 6];*/
        }
        public float getvalueatpos(float x, float y, float z)
        {
            uint ux = (uint)x;
            uint uy = (uint)y;
            uint uz = (uint)z;

            short value_xyz = DATA[ux, uy, uz];
            short value_xYz = DATA[ux, uy + 1, uz];
            short value_Xyz = DATA[ux + 1, uy, uz];
            short value_XYz = DATA[ux + 1, uy + 1, uz];

            short value_xyZ = DATA[ux, uy, uz + 1];
            short value_xYZ = DATA[ux, uy + 1, uz + 1];
            short value_XyZ = DATA[ux + 1, uy, uz + 1];
            short value_XYZ = DATA[ux + 1, uy + 1, uz + 1];

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

        public unsafe void getnewvertexpos(int x, int y, int z, byte state)
        {
            Vector3 direction = Vector3.Zero;
            if ((state & (1 << 0)) > 0)
                direction += new Vector3(1, 1, 1);
            if ((state & (1 << 1)) > 0)
                direction += new Vector3(1, -1, 1);
            if ((state & (1 << 2)) > 0)
                direction += new Vector3(-1, 1, 1);
            if ((state & (1 << 3)) > 0)
                direction += new Vector3(-1, -1, 1);

            if ((state & (1 << 4)) > 0)
                direction += new Vector3(1, 1, -1);
            if ((state & (1 << 5)) > 0)
                direction += new Vector3(1, -1, -1);
            if ((state & (1 << 6)) > 0)
                direction += new Vector3(-1, 1, -1);
            if ((state & (1 << 7)) > 0)
                direction += new Vector3(-1, -1, -1);

            direction = Vector3.Normalize(direction);
            Vector3 currentpos = Vector3.Zero;
            const float strength = 1.0f;
            float value1 = getvalueatpos(x - 0.5f - direction.X * strength, y - 0.5f - direction.Y * strength, z - 0.5f - direction.Z * strength);
            Vector3 pos1 = new Vector3(x - 0.5f - direction.X * strength, y - 0.5f - direction.Y * strength, z - 0.5f - direction.Z * strength);
            Vector3 pos2 = new Vector3(x - 0.5f + direction.X * strength, y - 0.5f + direction.Y * strength, z - 0.5f + direction.Z * strength);
            for (int i = 0; i < 8; ++i)
            {
                currentpos = (pos1 + pos2) * 0.5f;
                float currentvalue = getvalueatpos(currentpos.X, currentpos.Y, currentpos.Z);
                float dif1 = currentvalue - value1;
                if (currentvalue < 0)
                    dif1 = -dif1;
                if (dif1 > 0)
                    pos2 = currentpos;
                else
                {
                    value1 = currentvalue;
                    pos1 = currentpos;
                }

            }

            VertexPos[x - 2, y - 2, z - 2] = currentpos;
        }

        public Vector3 getnormal(Vector3 p1, Vector3 p2, Vector3 p3)
        {
            return Vector3.Cross(p2 - p1, p3 - p1);
        }

        /*public void AddMesh(int startx, int starty, int startz, int endx, int endy, int endz)
        {
            if (startx < 3)
                startx = 3;
            if (starty < 3)
                starty = 3;
            if (startz < 3)
                startz = 3;
            if (endx > size + 3)
                endx = size + 3;
            if (endy > size + 3)
                endy = size + 3;
            if (endz > size + 3)
                endz = size + 3;

            // Generating Mesh
            int count = -1;
            Color col;
            Vector3 normal, p1, p2, p3;
            for (int x2 = startx; x2 < endx; ++x2)
            {
                for (int z2 = startz; z2 < endz; ++z2)
                {
                    for (int y2 = starty; y2 < endy; ++y2)
                    {
                        if (DATA[x2, y2, z2] > 0)
                        {
                            bool state1 = (DATA[x2, y2 + 1, z2] > 0);
                            bool state2 = (DATA[x2, y2 - 1, z2] > 0);
                            bool state3 = (DATA[x2 + 1, y2, z2] > 0);
                            bool state4 = (DATA[x2 - 1, y2, z2] > 0);
                            bool state5 = (DATA[x2, y2, z2 + 1] > 0);
                            bool state6 = (DATA[x2, y2, z2 - 1] > 0);
                            if (!(state1 && state2 && state3 && state4 && state5 && state6))
                            {
								if(count > vertexes.Length - 50)
									Array.Resize(ref vertexes, vertexes.Length + 20000);

                                int x = x2 - 1;
                                int y = y2 - 1;
                                int z = z2 - 1;
                                if (!state1)
                                {
                                    p1 = VertexPos[x - 1, y, z - 1]; p2 = VertexPos[x, y, z]; p3 = VertexPos[x, y, z - 1];
                                    normal = Vector3.Normalize(getnormal(p1, p2, p3)); col = new Color(-normal);
                                    vertexes[++count] = new Game1.VertexPositionColorNormal_noTexCoo(p1, normal, col);
                                    vertexes[++count] = new Game1.VertexPositionColorNormal_noTexCoo(p3, normal, col);
                                    vertexes[++count] = new Game1.VertexPositionColorNormal_noTexCoo(p2, normal, col);

                                    p2 = VertexPos[x - 1, y, z]; p3 = VertexPos[x, y, z];
                                    normal = Vector3.Normalize(getnormal(p1, p2, p3)); col = new Color(-normal);
                                    vertexes[++count] = new Game1.VertexPositionColorNormal_noTexCoo(p1, normal, col);
                                    vertexes[++count] = new Game1.VertexPositionColorNormal_noTexCoo(p3, normal, col);
                                    vertexes[++count] = new Game1.VertexPositionColorNormal_noTexCoo(p2, normal, col);
                                    //vertexes_ID.Add(new short3((short)x2, (short)y2, (short)z2));
                                }
                                if (!state2)
                                {
                                    p2 = VertexPos[x - 1, y - 1, z - 1]; p1 = VertexPos[x, y - 1, z]; p3 = VertexPos[x, y - 1, z - 1];
                                    normal = Vector3.Normalize(getnormal(p1, p2, p3)); col = new Color(-normal);
                                    vertexes[++count] = new Game1.VertexPositionColorNormal_noTexCoo(p1, normal, col);
                                    vertexes[++count] = new Game1.VertexPositionColorNormal_noTexCoo(p3, normal, col);
                                    vertexes[++count] = new Game1.VertexPositionColorNormal_noTexCoo(p2, normal, col);

                                    p1 = VertexPos[x - 1, y - 1, z]; p3 = VertexPos[x, y - 1, z];
                                    normal = Vector3.Normalize(getnormal(p1, p2, p3)); col = new Color(-normal);
                                    vertexes[++count] = new Game1.VertexPositionColorNormal_noTexCoo(p1, normal, col);
                                    vertexes[++count] = new Game1.VertexPositionColorNormal_noTexCoo(p3, normal, col);
                                    vertexes[++count] = new Game1.VertexPositionColorNormal_noTexCoo(p2, normal, col);
                                    //vertexes_ID.Add(new short3((short)x2, (short)y2, (short)z2));
                                }

                                if (!state3)
                                {
                                    p1 = VertexPos[x, y - 1, z - 1]; p2 = VertexPos[x, y, z]; p3 = VertexPos[x, y - 1, z];
                                    normal = Vector3.Normalize(getnormal(p1, p2, p3)); col = new Color(-normal);
                                    vertexes[++count] = new Game1.VertexPositionColorNormal_noTexCoo(p1, normal, col);
                                    vertexes[++count] = new Game1.VertexPositionColorNormal_noTexCoo(p3, normal, col);
                                    vertexes[++count] = new Game1.VertexPositionColorNormal_noTexCoo(p2, normal, col);

                                    p2 = VertexPos[x, y, z - 1]; p3 = VertexPos[x, y, z];
                                    normal = Vector3.Normalize(getnormal(p1, p2, p3)); col = new Color(-normal);
                                    vertexes[++count] = new Game1.VertexPositionColorNormal_noTexCoo(p1, normal, col);
                                    vertexes[++count] = new Game1.VertexPositionColorNormal_noTexCoo(p3, normal, col);
                                    vertexes[++count] = new Game1.VertexPositionColorNormal_noTexCoo(p2, normal, col);
                                    //vertexes_ID.Add(new short3((short)x2, (short)y2, (short)z2));
                                }
                                if (!state4)
                                {
                                    p2 = VertexPos[x - 1, y - 1, z - 1]; p1 = VertexPos[x - 1, y, z]; p3 = VertexPos[x - 1, y - 1, z];
                                    normal = Vector3.Normalize(getnormal(p1, p2, p3)); col = new Color(-normal);
                                    vertexes[++count] = new Game1.VertexPositionColorNormal_noTexCoo(p1, normal, col);
                                    vertexes[++count] = new Game1.VertexPositionColorNormal_noTexCoo(p3, normal, col);
                                    vertexes[++count] = new Game1.VertexPositionColorNormal_noTexCoo(p2, normal, col);

                                    p1 = VertexPos[x - 1, y, z - 1]; p3 = VertexPos[x - 1, y, z];
                                    normal = Vector3.Normalize(getnormal(p1, p2, p3)); col = new Color(-normal);
                                    vertexes[++count] = new Game1.VertexPositionColorNormal_noTexCoo(p1, normal, col);
                                    vertexes[++count] = new Game1.VertexPositionColorNormal_noTexCoo(p3, normal, col);
                                    vertexes[++count] = new Game1.VertexPositionColorNormal_noTexCoo(p2, normal, col);
                                    //vertexes_ID.Add(new short3((short)x2, (short)y2, (short)z2));
                                }
                                if (!state5)
                                {
                                    p1 = VertexPos[x, y - 1, z]; p2 = VertexPos[x - 1, y, z]; p3 = VertexPos[x - 1, y - 1, z];
                                    normal = Vector3.Normalize(getnormal(p1, p2, p3)); col = new Color(-normal);
                                    vertexes[++count] = new Game1.VertexPositionColorNormal_noTexCoo(p1, normal, col);
                                    vertexes[++count] = new Game1.VertexPositionColorNormal_noTexCoo(p3, normal, col);
                                    vertexes[++count] = new Game1.VertexPositionColorNormal_noTexCoo(p2, normal, col);

                                    p2 = VertexPos[x, y, z]; p3 = VertexPos[x - 1, y, z];
                                    normal = Vector3.Normalize(getnormal(p1, p2, p3)); col = new Color(-normal);
                                    vertexes[++count] = new Game1.VertexPositionColorNormal_noTexCoo(p1, normal, col);
                                    vertexes[++count] = new Game1.VertexPositionColorNormal_noTexCoo(p3, normal, col);
                                    vertexes[++count] = new Game1.VertexPositionColorNormal_noTexCoo(p2, normal, col);
                                    //vertexes_ID.Add(new short3((short)x2, (short)y2, (short)z2));
                                }
                                if (!state6)
                                {
                                    p2 = VertexPos[x, y - 1, z - 1]; p1 = VertexPos[x - 1, y, z - 1]; p3 = VertexPos[x - 1, y - 1, z - 1];
                                    normal = Vector3.Normalize(getnormal(p1, p2, p3)); col = new Color(-normal);
                                    vertexes[++count] = new Game1.VertexPositionColorNormal_noTexCoo(p1, normal, col);
                                    vertexes[++count] = new Game1.VertexPositionColorNormal_noTexCoo(p3, normal, col);
                                    vertexes[++count] = new Game1.VertexPositionColorNormal_noTexCoo(p2, normal, col);

                                    p1 = VertexPos[x, y, z - 1]; p3 = VertexPos[x - 1, y, z - 1];
                                    normal = Vector3.Normalize(getnormal(p1, p2, p3)); col = new Color(-normal);
                                    vertexes[++count] = new Game1.VertexPositionColorNormal_noTexCoo(p1, normal, col);
                                    vertexes[++count] = new Game1.VertexPositionColorNormal_noTexCoo(p3, normal, col);
                                    vertexes[++count] = new Game1.VertexPositionColorNormal_noTexCoo(p2, normal, col);
                                    //vertexes_ID.Add(new short3((short)x2, (short)y2, (short)z2));
                                }
                            }
                        }
                    }
                }
            }
			Array.Resize(ref vertexes, count + 1);

        }*/

        [DllImport(Game1.DLL_PATH + "Dual_Contouring_DLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Generate_Chunk(IntPtr vertexpos, IntPtr DATA, ref int returnlength, int chunkY);

        [DllImport(Game1.DLL_PATH + "Dual_Contouring_DLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void Generate_Vertexes(IntPtr vertexes, IntPtr states, sbyte[,,] DATA, IntPtr vertexpos, int chunkY);

        [DllImport(Game1.DLL_PATH + "Dual_Contouring_DLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void FreeChunkGenerationMemory(IntPtr states);

        public unsafe void Generate2(int threadindex, bool ForceUpload)
        {
            DATA = baseworld.DATA_global[threadindex];
            IsVertex = baseworld.IsVertex_global[threadindex];
            VertexPos = baseworld.VertexPos_global[threadindex];

            Stopwatch watch = new Stopwatch();
            watch.Start();

            int topy = 0, bottomy = 0, rightx = 0, leftx = 0, frontz = 0, backz = 0;
            int yfac = 0, xfac = 0, zfac = 0;

            bool reference = DATA[0, 0, 0] <= 0;
            int counter = 0;
            for (int x = -1; x < 2; ++x)
            {
                for (int z = -1; z < 2; ++z)
                {
                    for (int y = -1; y < 2; ++y)
                    {
                        DataChunk currentdatachunk = baseworld.Dchunks[worldarraypos.X + x, worldarraypos.Y + y, worldarraypos.Z + z];
                        if (currentdatachunk.IsEmpty == 0 && currentdatachunk.DATA == null)
                            continue;

                        if (y == -1) { topy = Game1.CS; bottomy = Game1.CS - 2; }
                        else if (y == 0) { topy = Game1.CS; bottomy = 0; }
                        else { topy = 2; bottomy = 0; }

                        if (x == -1) { rightx = Game1.CS; leftx = Game1.CS - 2; }
                        else if (x == 0) { rightx = Game1.CS; leftx = 0; }
                        else { rightx = 2; leftx = 0; }

                        if (z == -1) { backz = Game1.CS; frontz = Game1.CS - 2; }
                        else if (z == 0) { backz = Game1.CS; frontz = 0; }
                        else { backz = 2; frontz = 0; }



                        if (y == -1) { yfac = -Game1.CS + 2; }
                        else if (y == 0) { yfac = 2; }
                        else { yfac = Game1.CS + 2; }

                        if (x == -1) { xfac = -Game1.CS + 2; }
                        else if (x == 0) { xfac = 2; }
                        else { xfac = Game1.CS + 2; }

                        if (z == -1) { zfac = -Game1.CS + 2; }
                        else if (z == 0) { zfac = 2; }
                        else { zfac = Game1.CS + 2; }

                        sbyte[,,] currentdata = currentdatachunk.DATA;
                        for (int x2 = leftx + xfac; x2 < rightx + xfac; ++x2)
                        {
                            for (int z2 = frontz + zfac; z2 < backz + zfac; ++z2)
                            {
                                if (currentdatachunk.IsEmpty == 0)
                                {
                                    for (int y2 = bottomy + yfac; y2 < topy + yfac; ++y2)
                                    {
                                        sbyte val = currentdata[x2 - xfac, y2 - yfac, z2 - zfac];
                                        DATA[x2, y2, z2] = val;
                                        if (reference == (val <= 0))
                                            counter++;
                                    }
                                }
                                else
                                {
                                    for (int y2 = bottomy + yfac; y2 < topy + yfac; ++y2)
                                    {
                                        DATA[x2, y2, z2] = currentdatachunk.IsEmpty;
                                        if (reference == (currentdatachunk.IsEmpty <= 0))
                                            counter++;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            bool SkipGenerateVertexes = counter == 0 || counter == Game1.CS4 * Game1.CS4 * Game1.CS4;

            watch.Stop();
            float gettingdatatime = (watch.ElapsedTicks / (Stopwatch.Frequency / 1000.0f));
            watch.Restart();
            fixed (Vector3* p = VertexPos)
            {
                int arraylength = 0;
                IntPtr statepointer = IntPtr.Zero;
                if (!SkipGenerateVertexes)
                {
                    fixed (sbyte* dat = DATA)
                    {
                        statepointer = Generate_Chunk((IntPtr)p, (IntPtr)dat, ref arraylength, chunkpos.Y);
                    }
                }

                if (arraylength > 0)
                {
                    bool Has2GeneratenewData = false;
                    lock (baseworld.unused_vertexData)
                    {
                        if (baseworld.unused_vertexData_length > 0)
                            vertexes = baseworld.unused_vertexData[--baseworld.unused_vertexData_length];
                        else
                            Has2GeneratenewData = true;
                    }
                    if (Has2GeneratenewData)
                        vertexes = new Game1.VertexPositionColorNormal_noTexCoo[145000];
                    fixed (Game1.VertexPositionColorNormal_noTexCoo* v = vertexes)
                    {
                        Generate_Vertexes((IntPtr)v, statepointer, DATA, (IntPtr)p, chunkpos.Y);
                    }
                }
                if (statepointer != IntPtr.Zero)
                    FreeChunkGenerationMemory(statepointer);
                vertexlength = arraylength;
            }

            //watch.Start();
            // Getting Surface Vertexes
            /*Parallel.For(2, size + 5, x =>
		        //for (int x = 2; x < size + 5; ++x)
	        {
		        for (int z = 2; z < size + 5; ++z)
		        {
			        for (int y = 2; y < size + 5; ++y)
			        {
				        bool state1 = (DATA[x, y, z] > 0);
				        bool state2 = (DATA[x, y - 1, z] > 0);
				        bool state3 = (DATA[x - 1, y, z] > 0);
				        bool state4 = (DATA[x - 1, y - 1, z] > 0);
				        bool state5 = (DATA[x, y, z - 1] > 0);
				        bool state6 = (DATA[x, y - 1, z - 1] > 0);
				        bool state7 = (DATA[x - 1, y, z - 1] > 0);
				        bool state8 = (DATA[x - 1, y - 1, z - 1] > 0);

				        byte states = (byte) (((*(byte*) (&state1)) << 0) | ((*(byte*) (&state2)) << 1) | ((*(byte*) (&state3)) << 2) | ((*(byte*) (&state4)) << 3) | ((*(byte*) (&state5)) << 4) | ((*(byte*) (&state6)) << 5) | ((*(byte*) (&state7)) << 6) | ((*(byte*) (&state8)) << 7));

				        if (states != 0 && states != 255)
				        {
					        IsVertex[x - 2, y - 2, z - 2] = states;
					        getnewvertexpos(x, y, z, states);
				        }
				        else
					        IsVertex[x - 2, y - 2, z - 2] = 0;

			        }
		        }
	        });*/
            watch.Stop();
            float smoothingtime = (watch.ElapsedTicks / (Stopwatch.Frequency / 1000.0f));
            //watch.Restart();
            //VertexPos = null;
            //DATA2 = null;
            //DATA = null;
            //watch.Stop();
#if ISDEBUG
            Console.WriteLine("GEN_TIME: " + (gettingdatatime + smoothingtime + meshtime) + " (" + gettingdatatime + ", " + smoothingtime + ", " + meshtime + ")");
#endif
            Game1.finaltime += (smoothingtime + gettingdatatime);
            IsDrawable = false;
            if (ForceUpload)
                UploadVertexBuffer2GPU();
            IsGenerated = true;
        }

        // This function should be called in the main thread for extra performance
        public void UploadVertexBuffer2GPU()
        {
            if (vertexlength <= 0) return;

            Stopwatch watch = new Stopwatch();
            watch.Start();
            if (vertexbuffer != null && vertexbuffer.IsDisposed == false)
                vertexbuffer.Dispose();
            vertexbuffer = new VertexBuffer(graphicsdevice, Game1.VertexPositionColorNormal_noTexCoo.VertexDeclaration, vertexlength, BufferUsage.WriteOnly);
            vertexbuffer.SetData(vertexes, 0, vertexlength);

            IsDrawable = true;
            lock (baseworld.unused_vertexData)
            {
                if (baseworld.unused_vertexData_length < baseworld.unused_vertexData.Length && vertexes != null)
                    baseworld.unused_vertexData[baseworld.unused_vertexData_length++] = vertexes;
            }

            vertexes = null;
            watch.Stop();
            //Console.WriteLine("Uploading Vertex Data: " + (watch.ElapsedTicks / (float)TimeSpan.TicksPerMillisecond));
        }

        public void Draw(Matrix world, Matrix view, Matrix projecton)
        {
            if (!IsDrawable) return;

            Vector3 pos = ((chunkpos + Game1.currentccameraoffset) * Game1.CS).ToVector3();// + Vector3.Normalize((Game1.camerapos - Game1.camerarichtung)) * 100;
            hitbox = new BoundingBox(pos, pos + new Vector3(64));
            if (!Game1.camerafrustum.Intersects(hitbox)) return;

            Game1.CurrentTriangleDraws += vertexlength;
            Game1.CurrentDrawCalls++;
            Game1.terrain_effect.Parameters["World"].SetValue(world);
            Game1.terrain_effect.Parameters["View"].SetValue(view);
            Game1.terrain_effect.Parameters["Projection"].SetValue(projecton);
            Game1.terrain_effect.CurrentTechnique.Passes[0].Apply();
            graphicsdevice.SetVertexBuffer(vertexbuffer);
            //graphicsdevice.DrawPrimitives(PrimitiveType.TriangleList, 0, vertexbuffer.VertexCount / 3);
            graphicsdevice.DrawPrimitives(PrimitiveType.TriangleList, 0, vertexbuffer.VertexCount / 3);
        }

        // Disposing
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        protected virtual void Dispose(bool disposing)
        {
            if (IsDisposed)
                return;

            if (disposing)
            {
                //vertexes = null;
                //vertexes_ID?.Clear();
                if (vertexes != null)
                {
                    lock (baseworld.unused_vertexData)
                    {
                        if (baseworld.unused_vertexData_length < baseworld.unused_vertexData.Length)
                            baseworld.unused_vertexData[baseworld.unused_vertexData_length++] = vertexes;
                    }
                }

                vertexes = null;
                IsVertex = null;
                VertexPos = null;
                DATA = null;
                //GC.SuppressFinalize(this);
                // Free any other managed objects here.
            }

            vertexbuffer?.Dispose();
            vertexbuffer = null;
            IsDrawable = false;
            IsGenerated = false;
            // Free any unmanaged objects here.
            IsDisposed = true;
        }
        ~Chunk()
        {
            Dispose(false);
        }
    }
}
