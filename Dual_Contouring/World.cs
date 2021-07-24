using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using OpenCL.Net;

namespace Dual_Contouring
{
    public struct DataChunk
    {
        // Undefined: IsEmpty: 0 | DATA: null
        // Defined: IsEmpty: 0 | Data: !null
        // Empty: IsEmpty: !0
        public bool IsReady, GetsGenerated;
        public sbyte IsEmpty;
        public sbyte[,,] DATA;

        public DataChunk(sbyte IsEmpty, sbyte[,,] DATA)
        {
            IsReady = false;
            GetsGenerated = false;
            this.IsEmpty = IsEmpty;
            this.DATA = DATA;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public sbyte GetValue(ExtendedDatatypes.int3 pos)
        {
            return GetValue(pos.X, pos.Y, pos.Z);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public sbyte GetValue(int x, int y, int z)
        {
            if (IsEmpty != 0)
                return IsEmpty;
            return DATA[x, y, z];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetValue(sbyte value, ExtendedDatatypes.int3 pos)
        {
            SetValue(value, pos.X, pos.Y, pos.Z);
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetValue(sbyte value, int x, int y, int z)
        {
            if (IsEmpty != 0)
            {
                //if ((IsEmpty > 0 && value <= 0) || (IsEmpty <= 0 && value > 0) || true)
                //{
                DATA = new sbyte[Game1.CS, Game1.CS, Game1.CS];
                for (int xx = 0; xx < Game1.CS; ++xx)
                    for (int yy = 0; yy < Game1.CS; ++yy)
                        for (int zz = 0; zz < Game1.CS; ++zz)
                            DATA[xx, yy, zz] = IsEmpty;
                DATA[x, y, z] = value;
                IsEmpty = 0;
                //}
            }
            else
            {
                DATA[x, y, z] = value;
            }
        }
    }

    public class GenerationList4Threads
    {
        public ExtendedDatatypes.int3[] DATA, CHUNKS;
        public int[] CHUNKS_SYNC;
        public byte[] GEN_BYTES;
        public Chunk[] CHUNKS2UPDATE;
        public int C_start, C_end, D_start, D_end, C2U_end;

        public GenerationList4Threads(int chunk_size, int data_size)
        {
            DATA = new ExtendedDatatypes.int3[data_size * data_size * data_size];
            CHUNKS = new ExtendedDatatypes.int3[chunk_size * chunk_size * chunk_size];
            CHUNKS_SYNC = new int[chunk_size * chunk_size * chunk_size];
            CHUNKS2UPDATE = new Chunk[1000];
            GEN_BYTES = new byte[1000000];
            C_start = C_end = D_start = D_end = C2U_end = 0;
        }
    }

    public class WorldDeformer
    {
        private HashSet<Chunk> changedChunks;
        public DataChunk[,,] AccessDATA;
        public Chunk[,,] AccessCHUNKS;
        public ExtendedDatatypes.int3 accesscampos;
        public WorldDeformer()
        {
            changedChunks = new HashSet<Chunk>();
        }

        public void MineDataAt(Vector3 pos, int value)
        {
            float xmod = pos.X % 1.0f;
            float ymod = pos.Y % 1.0f;
            float zmod = pos.Z % 1.0f;
            if (pos.X < 0)
                xmod = 1.0f + xmod;
            if (pos.Y < 0)
                ymod = 1.0f + ymod;
            if (pos.Z < 0)
                zmod = 1.0f + zmod;

            int xfloor = (int)Math.Floor(pos.X) - Game1.currentccameraoffset.X * Game1.CS + (Game1.LOADING_RADIUS + 1) * Game1.CS;
            int yfloor = (int)Math.Floor(pos.Y) - Game1.currentccameraoffset.Y * Game1.CS + (Game1.LOADING_RADIUS + 1) * Game1.CS;
            int zfloor = (int)Math.Floor(pos.Z) - Game1.currentccameraoffset.Z * Game1.CS + (Game1.LOADING_RADIUS + 1) * Game1.CS;

            MineDataAt(xfloor, yfloor, zfloor, (int)(value * (1 - xmod) * (1 - ymod) * (1 - zmod)));
            MineDataAt(xfloor, yfloor, zfloor + 1, (int)(value * (1 - xmod) * (1 - ymod) * (zmod)));
            MineDataAt(xfloor, yfloor + 1, zfloor, (int)(value * (1 - xmod) * (ymod) * (1 - zmod)));
            MineDataAt(xfloor, yfloor + 1, zfloor + 1, (int)(value * (1 - xmod) * (ymod) * (zmod)));

            MineDataAt(xfloor + 1, yfloor, zfloor, (int)(value * (xmod) * (1 - ymod) * (1 - zmod)));
            MineDataAt(xfloor + 1, yfloor, zfloor + 1, (int)(value * (xmod) * (1 - ymod) * (zmod)));
            MineDataAt(xfloor + 1, yfloor + 1, zfloor, (int)(value * (xmod) * (ymod) * (1 - zmod)));
            MineDataAt(xfloor + 1, yfloor + 1, zfloor + 1, (int)(value * (xmod) * (ymod) * (zmod)));
        }

        public void MineDataAt(int x, int y, int z, int value)
        {
            ExtendedDatatypes.int3 pos = new ExtendedDatatypes.int3(x, y, z);
            ExtendedDatatypes.int3 chunksize3 = new ExtendedDatatypes.int3(Game1.CS, Game1.CS, Game1.CS);
            //int3 loadingRp1 = new int3(Game1.LOADING_RADIUS + 1, Game1.LOADING_RADIUS + 1, Game1.LOADING_RADIUS + 1);
            //pos += loadingRp1;
            lock (AccessDATA)
            {
                ExtendedDatatypes.int3 datachunkpos = pos / chunksize3 + accesscampos;
                if (x < 0)
                    datachunkpos.X = (x + 1) / Game1.CS + accesscampos.X - 1;
                if (y < 0)
                    datachunkpos.Y = (y + 1) / Game1.CS + accesscampos.Y - 1;
                if (z < 0)
                    datachunkpos.Z = (z + 1) / Game1.CS + accesscampos.Z - 1;
                ExtendedDatatypes.int3 dataposinchunk = pos % chunksize3;
                if (dataposinchunk.X < 0)
                    dataposinchunk.X += Game1.CS;
                if (dataposinchunk.Y < 0)
                    dataposinchunk.Y += Game1.CS;
                if (dataposinchunk.Z < 0)
                    dataposinchunk.Z += Game1.CS;
                //if (AccessDATA[datachunkpos.X, datachunkpos.Y, datachunkpos.Z].DATA == null && AccessDATA[datachunkpos.X, datachunkpos.Y, datachunkpos.Z].IsEmpty != 0)
                //{
                //	AccessDATA[datachunkpos.X, datachunkpos.Y, datachunkpos.Z].DATA = new sbyte[Game1.CS, Game1.CS, Game1.CS];
                //	for (int xx = 0; xx < Game1.CS; ++xx)
                //	for (int yy = 0; yy < Game1.CS; ++yy)
                //	for (int zz = 0; zz < Game1.CS; ++zz)
                //		AccessDATA[datachunkpos.X, datachunkpos.Y, datachunkpos.Z].DATA[xx, yy, zz] = AccessDATA[datachunkpos.X, datachunkpos.Y, datachunkpos.Z].IsEmpty;
                //	AccessDATA[datachunkpos.X, datachunkpos.Y, datachunkpos.Z].IsEmpty = 0;
                //}
                //AccessDATA[datachunkpos.X, datachunkpos.Y, datachunkpos.Z].IsEmpty = 0;
                sbyte newvalue = (sbyte)MathHelper.Max(AccessDATA[datachunkpos.X, datachunkpos.Y, datachunkpos.Z].GetValue(dataposinchunk) - value, sbyte.MinValue); //(sbyte)MathHelper.Max(AccessDATA[datachunkpos.X, datachunkpos.Y, datachunkpos.Z].DATA[dataposinchunk.X + 2, dataposinchunk.Y + 2, dataposinchunk.Z + 2] - value, sbyte.MinValue);
                AccessDATA[datachunkpos.X, datachunkpos.Y, datachunkpos.Z].SetValue(newvalue, dataposinchunk);

                int count = 0;
                int xoff = 0, yoff = 0, zoff = 0;
                if (dataposinchunk.X < 2) { xoff = -1; ++count; }
                else if (dataposinchunk.X >= Game1.CS - 2) { xoff = 1; ++count; }
                if (dataposinchunk.Y < 2) { yoff = -1; ++count; }
                else if (dataposinchunk.Y >= Game1.CS - 2) { yoff = 1; ++count; }
                if (dataposinchunk.Z < 2) { zoff = -1; ++count; }
                else if (dataposinchunk.Z >= Game1.CS - 2) { zoff = 1; ++count; }

                ExtendedDatatypes.int3 index;
                if (count == 1)
                {
                    index = datachunkpos + new ExtendedDatatypes.int3(xoff, yoff, zoff);
                    changedChunks.Add(AccessCHUNKS[index.X - 1, index.Y - 1, index.Z - 1]);
                }
                else if (count == 2)
                {
                    index = datachunkpos + new ExtendedDatatypes.int3(xoff, yoff, 0);
                    changedChunks.Add(AccessCHUNKS[index.X - 1, index.Y - 1, index.Z - 1]);
                    index = datachunkpos + new ExtendedDatatypes.int3(xoff, 0, zoff);
                    changedChunks.Add(AccessCHUNKS[index.X - 1, index.Y - 1, index.Z - 1]);
                    index = datachunkpos + new ExtendedDatatypes.int3(0, yoff, zoff);
                    changedChunks.Add(AccessCHUNKS[index.X - 1, index.Y - 1, index.Z - 1]);
                }
                else if (count == 3)
                {
                    index = datachunkpos + new ExtendedDatatypes.int3(xoff, yoff, zoff);
                    changedChunks.Add(AccessCHUNKS[index.X - 1, index.Y - 1, index.Z - 1]);
                    index = datachunkpos + new ExtendedDatatypes.int3(0, yoff, zoff);
                    changedChunks.Add(AccessCHUNKS[index.X - 1, index.Y - 1, index.Z - 1]);
                    index = datachunkpos + new ExtendedDatatypes.int3(xoff, 0, zoff);
                    changedChunks.Add(AccessCHUNKS[index.X - 1, index.Y - 1, index.Z - 1]);
                    index = datachunkpos + new ExtendedDatatypes.int3(xoff, yoff, 0);
                    changedChunks.Add(AccessCHUNKS[index.X - 1, index.Y - 1, index.Z - 1]);
                    index = datachunkpos + new ExtendedDatatypes.int3(xoff, 0, 0);
                    changedChunks.Add(AccessCHUNKS[index.X - 1, index.Y - 1, index.Z - 1]);
                    index = datachunkpos + new ExtendedDatatypes.int3(0, yoff, 0);
                    changedChunks.Add(AccessCHUNKS[index.X - 1, index.Y - 1, index.Z - 1]);
                    index = datachunkpos + new ExtendedDatatypes.int3(0, 0, zoff);
                    changedChunks.Add(AccessCHUNKS[index.X - 1, index.Y - 1, index.Z - 1]);
                }

                index = datachunkpos + new ExtendedDatatypes.int3(0, 0, 0);
                changedChunks.Add(AccessCHUNKS[index.X - 1, index.Y - 1, index.Z - 1]);
            }
        }

        public short GetDataAt(int x2, int y2, int z2)
        {
            int x = (int)x2 - Game1.currentccameraoffset.X * Game1.CS + (Game1.LOADING_RADIUS + 1) * Game1.CS;
            int y = (int)y2 - Game1.currentccameraoffset.Y * Game1.CS + (Game1.LOADING_RADIUS + 1) * Game1.CS;
            int z = (int)z2 - Game1.currentccameraoffset.Z * Game1.CS + (Game1.LOADING_RADIUS + 1) * Game1.CS;

            ExtendedDatatypes.int3 pos = new ExtendedDatatypes.int3(x, y, z);
            ExtendedDatatypes.int3 chunksize3 = new ExtendedDatatypes.int3(Game1.CS, Game1.CS, Game1.CS);
            //int3 loadingRp1 = new int3(Game1.LOADING_RADIUS + 1, Game1.LOADING_RADIUS + 1, Game1.LOADING_RADIUS + 1);
            //pos += loadingRp1;
            lock (AccessDATA)
            {
                ExtendedDatatypes.int3 datachunkpos = pos / chunksize3 + accesscampos;
                if (x < 0)
                    datachunkpos.X = (x + 1) / Game1.CS + accesscampos.X - 1;
                if (y < 0)
                    datachunkpos.Y = (y + 1) / Game1.CS + accesscampos.Y - 1;
                if (z < 0)
                    datachunkpos.Z = (z + 1) / Game1.CS + accesscampos.Z - 1;
                ExtendedDatatypes.int3 dataposinchunk = pos % chunksize3;
                if (dataposinchunk.X < 0)
                    dataposinchunk.X += Game1.CS;
                if (dataposinchunk.Y < 0)
                    dataposinchunk.Y += Game1.CS;
                if (dataposinchunk.Z < 0)
                    dataposinchunk.Z += Game1.CS;
                if (AccessDATA[datachunkpos.X, datachunkpos.Y, datachunkpos.Z].DATA == null && AccessDATA[datachunkpos.X, datachunkpos.Y, datachunkpos.Z].IsEmpty != 0)
                {
                    return AccessDATA[datachunkpos.X, datachunkpos.Y, datachunkpos.Z].IsEmpty;
                }

                return AccessDATA[datachunkpos.X, datachunkpos.Y, datachunkpos.Z].DATA[dataposinchunk.X, dataposinchunk.Y, dataposinchunk.Z]; // = (short)MathHelper.Max(AccessDATA[datachunkpos.X, datachunkpos.Y, datachunkpos.Z].DATA[dataposinchunk.X, dataposinchunk.Y, dataposinchunk.Z] - value, short.MinValue);
            }
        }

        public void UpdateChanges2Chunks(GenerationList4Threads things2gen)
        {
            lock (things2gen.CHUNKS2UPDATE)
            {
                foreach (Chunk changedChunk in changedChunks)
                {
                    things2gen.CHUNKS2UPDATE[things2gen.C2U_end++] = changedChunk;
                }
            }
            changedChunks.Clear();
        }
    }

    public class World
    {
        private Event OpenCL_event;
        private int seed;
        public SimplexNoise simplexnoise;
        private Thread updatethread;
        private Thread[] generationthreads;
        public uint threadcount;

        public Chunk[,,] chunks, bufferchunks, bufferchunks2;
        public DataChunk[,,] Dchunks, bufferDchunks, bufferDchunks2;

        public GenerationList4Threads things2gen;
        public WorldDeformer worlddeformer;

        private Chunk[] TooDeletingChunks;
        private int TooDeletingChunksLength;

        private int chunksize, worlddatasize;
        public ExtendedDatatypes.int3 currentcampos, oldcampos;
        private byte HasNewUpdate;
        private static int[] generatethreadstates;
        public sbyte[][,,] DATA_global, unused_Dchunks;
        public byte[][,,] IsVertex_global;
        public Vector3[][,,] VertexPos_global;
        public Game1.VertexPositionColorNormal_noTexCoo[][] unused_vertexData;
        public int lowestGEN_ID;
        public int unused_Dchunks_length, unused_vertexData_length;

        private Context context;
        private Kernel OCL_CHUNK_KERNEL;
        private CommandQueue cmdqueue;
        private OpenCL.Net.Program program;
        private string program_string;

        private Mem OCL_CHUNK_DATA, OCL_SUMS;
        private Mem OCL_P;

        private int[] p;
        private long[] sums;

        #region OPEN CL

        OpenCL.Net.Program OpenCL_CompileProgram(Context context, Device device, string Sourcecode, out string errorstring)
        {
            ErrorCode errorcode;
            OpenCL.Net.Program program;
            program = Cl.CreateProgramWithSource(context, 1, new[] { Sourcecode }, new[] { (IntPtr)Sourcecode.Length }, out errorcode);
            if (errorcode != ErrorCode.Success)
                Console.WriteLine(errorcode.ToString());
            //-cl-opt-disable
            //-cl-mad-enable
            //-cl-fast-relaxed-math
            //-cl-strict-aliasing // BEST
            errorcode = Cl.BuildProgram(program, 0, null, "-cl-strict-aliasing -cl-mad-enable -cl-no-signed-zeros -cl-fast-relaxed-math", null, IntPtr.Zero);
            errorstring = "";
            if (Cl.GetProgramBuildInfo(program, device, ProgramBuildInfo.Status, out errorcode).CastTo<BuildStatus>() != BuildStatus.Success)
            {

                if (errorcode != ErrorCode.Success) // Couldn´t get Programm Build Info
                    errorstring += "ERROR: " + "Cl.GetProgramBuildInfo" + " (" + errorcode.ToString() + ")" + "\r\n";
                errorstring += "Cl.GetProgramBuildInfo != Success" + "\r\n";
                errorstring += Cl.GetProgramBuildInfo(program, device, ProgramBuildInfo.Log, out errorcode); // Printing Log
            }
            else
                errorstring = "SUCCESS";
            return program;
        }

        ErrorCode OpenCL_RunKernel(Kernel kernel, CommandQueue queue, uint work_dim, int size)
        {
            return Cl.EnqueueNDRangeKernel(queue, kernel, work_dim, null, new IntPtr[] { new IntPtr(size), new IntPtr(size), new IntPtr(size) }, new IntPtr[] { new IntPtr(4), new IntPtr(4), new IntPtr(2) }, 0, null, out OpenCL_event);
        }

        ErrorCode OpenCL_ReadGPUMemory<T>(CommandQueue queue, Mem GPUMemory, T[,,] data) where T : struct
        {
            int size = Marshal.SizeOf<T>() * data.Length;
            return Cl.EnqueueReadBuffer(queue, GPUMemory, Bool.True, IntPtr.Zero, new IntPtr(size), data, 0, null, out OpenCL_event);
        }
        ErrorCode OpenCL_ReadGPUMemory2<T>(CommandQueue queue, Mem GPUMemory, T[] data) where T : struct
        {
            int size = Marshal.SizeOf<T>() * data.Length;
            return Cl.EnqueueReadBuffer(queue, GPUMemory, Bool.True, IntPtr.Zero, new IntPtr(size), data, 0, null, out OpenCL_event);
        }

        Mem OpenCL_CreateGPUBuffer<T>(Context context, int length, out ErrorCode error) where T : struct
        {
            int size = Marshal.SizeOf<T>() * length;
            return (Mem)Cl.CreateBuffer(context, MemFlags.ReadWrite, size, out error);
        }
        ErrorCode OpenCL_WriteGPUMemory<T>(CommandQueue queue, Mem GPUMemory, T[] data) where T : struct
        {
            int size = Marshal.SizeOf<T>() * data.Length;
            return Cl.EnqueueWriteBuffer(queue, GPUMemory, Bool.True, IntPtr.Zero, new IntPtr(size), data, 0, null, out OpenCL_event);

        }

        Context OpenCL_CreateContext(int numdevices, Device[] devices, out ErrorCode error)
        {
            return Cl.CreateContext(null, 1, devices, null, IntPtr.Zero, out error);
        }

        #endregion

        public World(GraphicsDevice GD)
        {
            updatethread = new Thread(UpdateThread);
            updatethread.IsBackground = true;
            Chunk.graphicsdevice = GD;
            Chunk.size = Game1.CS;
        }

        [DllImport(Game1.DLL_PATH + "Dual_Contouring_DLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetSimplexNoise(int[] p);

        [DllImport(Game1.DLL_PATH + "Dual_Contouring_DLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void SetParameters(float str);

        [DllImport(Game1.DLL_PATH + "Dual_Contouring_DLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern int Generate_DATA(sbyte[,,] DATA, int x, int y, int z, ExtendedDatatypes.int3 currentcampos);

        [DllImport(Game1.DLL_PATH + "Dual_Contouring_DLL.dll", CallingConvention = CallingConvention.Cdecl)]
        public static extern void ClearArrayofReferences(IntPtr p, int length);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        static void udelay(long us)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            long v = (us * System.Diagnostics.Stopwatch.Frequency) / 1000000;
            while (sw.ElapsedTicks < v) { }
        }

        public void SetSeed(int seed)
        {
            this.seed = seed;
        }

        public void InitializeWorld(uint threadcount, int seed)
        {
            SetSeed(seed);
            InitializeWorld(threadcount);
        }
        public void InitializeWorld(uint threadcount)
        {
            this.threadcount = threadcount;
            simplexnoise = new SimplexNoise(seed);
            worlddeformer = new WorldDeformer();
            chunksize = Game1.LOADING_RADIUS * 2 + 1;
            worlddatasize = Game1.LOADING_RADIUS * 2 + 3;
            IsVertex_global = new byte[threadcount][,,];
            VertexPos_global = new Vector3[threadcount][,,];
            DATA_global = new sbyte[threadcount][,,];
            for (int i = 0; i < threadcount; ++i)
            {
                IsVertex_global[i] = new byte[Game1.CS + 1, Game1.CS + 1, Game1.CS + 1];
                VertexPos_global[i] = new Vector3[Game1.CS + 1, Game1.CS + 1, Game1.CS + 1];
                DATA_global[i] = new sbyte[Game1.CS + 4, Game1.CS + 4, Game1.CS + 4];
            }


            bufferchunks = new Chunk[chunksize, chunksize, chunksize];
            bufferchunks2 = new Chunk[chunksize, chunksize, chunksize];
            chunks = new Chunk[chunksize, chunksize, chunksize];
            TooDeletingChunks = new Chunk[chunksize * chunksize * chunksize];
            bufferDchunks = new DataChunk[worlddatasize, worlddatasize, worlddatasize];
            bufferDchunks2 = new DataChunk[worlddatasize, worlddatasize, worlddatasize];
            Dchunks = new DataChunk[worlddatasize, worlddatasize, worlddatasize];
            worlddeformer.AccessDATA = Dchunks;
            unused_Dchunks = new sbyte[5000][,,];
            unused_vertexData = new Game1.VertexPositionColorNormal_noTexCoo[200][];
            for (int x = 0; x < worlddatasize; ++x)
                for (int y = 0; y < worlddatasize; ++y)
                    for (int z = 0; z < worlddatasize; ++z)
                    {
                        Dchunks[x, y, z] = new DataChunk(0, null);
                    }


            // Generating Threads
            generationthreads = new Thread[threadcount];
            generatethreadstates = new int[threadcount];

            //OpenCl

            program_string = File.ReadAllText("chunk_generation.c");

            // Inizialising GPU and Kernel
            ErrorCode err;
            //OpenCL_event = new Event[Game1.world.threadcount];
            string errorstring;
            Platform[] platforms = Cl.GetPlatformIDs(out err); // Getting all Platforms
            Console.WriteLine("Length:" + platforms.Length);
            Device[] devices = Cl.GetDeviceIDs(platforms[0], DeviceType.Gpu, out err); // Getting all devices
            Console.WriteLine("Length_devices0:" + devices.Length);
            context = OpenCL_CreateContext(1, devices, out err);

            //cmdqueue = new CommandQueue[threadcount];
            //      for (int i = 0; i < threadcount; ++i)
            //      {
            cmdqueue = Cl.CreateCommandQueue(context, devices[0], CommandQueueProperties.None, out err);
            //}
            program = OpenCL_CompileProgram(context, devices[0], program_string, out errorstring);
            if (errorstring != "SUCCESS")
            {
                Console.WriteLine(errorstring);
                throw new System.InvalidOperationException("Error during Building the Program");
            }
            else
                Console.WriteLine("Building program succeeded");
            //OCL_CHUNK_KERNEL = new Kernel[threadcount];
            //      for (int i = 0; i < 1; ++i)
            //      {
            OCL_CHUNK_KERNEL = Cl.CreateKernel(program, "Generate_DATA", out err);
            //}
            //OCL_CHUNK_DATA = new Mem[threadcount];
            //OCL_SUMS =  new Mem[threadcount];
            //   for (int i = 0; i < threadcount; ++i)
            //{
            OCL_CHUNK_DATA = OpenCL_CreateGPUBuffer<sbyte>(context, Game1.CS * Game1.CS * Game1.CS, out err);
            OCL_SUMS = OpenCL_CreateGPUBuffer<long>(context, 2, out err);
            OCL_P = OpenCL_CreateGPUBuffer<int>(context, 512, out err);
            //}


            //sums = new long[threadcount][];
            //      for (int i = 0; i < threadcount; ++i)
            //      {
            sums = new long[2];
            //}
            uint[] p2 = new uint[256];
            p = new int[512];
            for (int x = 0; x < 256; ++x)
            {
                p2[x] = (uint)Game1.r.Next(0, 256);
            }
            for (int x = 0; x < 512; ++x)
            {
                p[x] = (int)p2[x % 256];
            }
            SetSimplexNoise(p);

            for (int i = 0; i < threadcount; ++i)
            {
            }
            OpenCL_WriteGPUMemory(cmdqueue, OCL_P, p);
            Cl.ReleaseEvent(OpenCL_event);
            Cl.SetKernelArg(OCL_CHUNK_KERNEL, 0, OCL_CHUNK_DATA);
            Cl.SetKernelArg(OCL_CHUNK_KERNEL, 4, OCL_P);
            /*for (int i = 0; i < threadcount; ++i)
	        {
		        generationthreads[i] = new Thread(new ParameterizedThreadStart(GenerationThread));
	        }*/
        }

        public void StartUpdating()
        {
            generationthreads = new Thread[threadcount];
            for (int i = 0; i < threadcount; ++i)
            {
                generationthreads[i] = new Thread(GenerationThread);
                generationthreads[i].IsBackground = true;
                generationthreads[i].Priority = ThreadPriority.BelowNormal;
                generationthreads[i].Start(i);
            }
            updatethread.Priority = ThreadPriority.Normal;
            updatethread.Start();
        }

        public void StopUpdating()
        {
            if (updatethread.IsAlive)
                updatethread.Abort();
            for (int i = 0; i < threadcount; ++i)
            {
                if (generationthreads[i].IsAlive)
                    generationthreads[i].Abort();
            }
        }

        // UPDATE THREAD
        public unsafe void UpdateThread()
        {
            currentcampos = Game1.currentccameraoffset;
            oldcampos = Game1.currentccameraoffset;
            TooDeletingChunksLength = 0;
            Array.Copy(chunks, bufferchunks, chunksize * chunksize * chunksize);
            things2gen = new GenerationList4Threads(chunksize, worlddatasize);
            while (true)
            {

                Stopwatch watch = new Stopwatch();
                TooDeletingChunksLength = 0;

                HasNewUpdate = 1;
                // From here Generation Threads ar beginning to pause
                while (true)
                {
                    bool AllThreadsWaiting = true;
                    for (int i = 0; i < threadcount; ++i)
                    {
                        if (generatethreadstates[i] == 0)
                            AllThreadsWaiting = false;
                    }
                    if (AllThreadsWaiting)
                        break;
                }

                // From here all the Generation Threads are paused
                watch.Start();
                //Array.Copy(chunks, bufferchunks, chunksize * chunksize * chunksize);
                currentcampos = Game1.currentccameraoffset;

                // Deleting, Moving and Generating final Chunks
                lock (chunks)
                {
                    if (oldcampos != currentcampos)
                    {
                        /*for (int x = 0; x < chunksize; ++x)
					    {
						    for (int z = 0; z < chunksize; ++z)
						    {
							    for (int y = 0; y < chunksize; ++y)
							    {
								    int3 newarraypos = new int3(x - oldcampos.X + currentcampos.X, y - oldcampos.Y + currentcampos.Y, z - oldcampos.Z + currentcampos.Z);
								    if (newarraypos.X >= 0 && newarraypos.Y >= 0 && newarraypos.Z >= 0 && newarraypos.X < chunksize && newarraypos.Y < chunksize && newarraypos.Z < chunksize) //Chunk still in Array
								    {
									    if (bufferchunks[x, y, z] != null)
									    {
										    chunks[newarraypos.X, newarraypos.Y, newarraypos.Z] = bufferchunks[x, y, z];
										    chunks[newarraypos.X, newarraypos.Y, newarraypos.Z].worldarraypos = new int3(newarraypos.X + 1, newarraypos.Y + 1, newarraypos.Z + 1);
                                            if (bufferchunks[x, y, z].Equals(chunks[x, y, z]))
											    chunks[x, y, z] = null;
									    }
								    }
								    else
								    {
									    if (bufferchunks[x, y, z] != null)
									    {
										    TooDeletingChunks[TooDeletingChunksLength] = bufferchunks[x, y, z];
										    TooDeletingChunksLength++;
										    if (bufferchunks[x, y, z].Equals(chunks[x, y, z]))
											    chunks[x, y, z] = null;
									    }
								    }
							    }
						    }
					    }*/
                        for (int x = 0; x < chunksize; ++x)
                        {
                            for (int z = 0; z < chunksize; ++z)
                            {
                                for (int y = 0; y < chunksize; ++y)
                                {
                                    bufferchunks2[x, y, z] = null;
                                    ExtendedDatatypes.int3 newarraypos = new ExtendedDatatypes.int3(x - oldcampos.X + currentcampos.X, y - oldcampos.Y + currentcampos.Y, z - oldcampos.Z + currentcampos.Z);
                                    if (newarraypos.X >= 0 && newarraypos.Y >= 0 && newarraypos.Z >= 0 && newarraypos.X < chunksize && newarraypos.Y < chunksize && newarraypos.Z < chunksize) //Chunk still in Array
                                    {
                                        if (chunks[x, y, z] != null)
                                        {
                                            bufferchunks[newarraypos.X, newarraypos.Y, newarraypos.Z] = chunks[x, y, z];
                                            bufferchunks[newarraypos.X, newarraypos.Y, newarraypos.Z].worldarraypos = new ExtendedDatatypes.int3(newarraypos.X + 1, newarraypos.Y + 1, newarraypos.Z + 1);
                                            if (chunks[x, y, z].Equals(bufferchunks[x, y, z]))
                                                bufferchunks[x, y, z] = null;
                                        }
                                    }
                                    else
                                    {
                                        if (chunks[x, y, z] != null)
                                        {
                                            TooDeletingChunks[TooDeletingChunksLength] = chunks[x, y, z];
                                            TooDeletingChunksLength++;
                                            if (chunks[x, y, z].Equals(bufferchunks[x, y, z]))
                                                bufferchunks[x, y, z] = null;
                                        }
                                    }
                                }
                            }
                        }

                        Chunk[,,] buf = chunks;
                        chunks = bufferchunks;
                        bufferchunks = bufferchunks2;
                        bufferchunks2 = buf;
                    }
                }
                // Moving World Data
                if (oldcampos != currentcampos)
                {
                    /*bufferDchunks = Dchunks;
		            Dchunks = new DataChunk[worlddatasize, worlddatasize, worlddatasize];
                    for (int x = 0; x < worlddatasize; ++x)
		            {
			            for (int z = 0; z < worlddatasize; ++z)
			            {
				            for (int y = 0; y < worlddatasize; ++y)
				            {
					            int3 newdataarraypos = new int3(x - oldcampos.X + currentcampos.X, y - oldcampos.Y + currentcampos.Y, z - oldcampos.Z + currentcampos.Z);
					            if (newdataarraypos.X >= 0 && newdataarraypos.Y >= 0 && newdataarraypos.Z >= 0 && newdataarraypos.X < worlddatasize && newdataarraypos.Y < worlddatasize && newdataarraypos.Z < worlddatasize) //DATAChunk still in Array
						            Dchunks[newdataarraypos.X, newdataarraypos.Y, newdataarraypos.Z] = bufferDchunks[x, y, z];
					            else
					            {
						            if (bufferDchunks[x, y, z].DATA != null && unused_Dchunks_length < unused_Dchunks.Length)
							            unused_Dchunks[unused_Dchunks_length++] = bufferDchunks[x, y, z].DATA;
					            }
				            }
			            }
		            }*/
                    Array.Clear(bufferDchunks2, 0, bufferDchunks2.Length);
                    for (int x = 0; x < worlddatasize; ++x)
                    {
                        for (int z = 0; z < worlddatasize; ++z)
                        {
                            for (int y = 0; y < worlddatasize; ++y)
                            {
                                //bufferDchunks2[x, y, z].DATA = null;
                                //bufferDchunks2[x, y, z].IsEmpty = 0;
                                //bufferDchunks2[x, y, z].GetsGenerated = bufferDchunks2[x, y, z].IsReady = false;
                                ExtendedDatatypes.int3 newdataarraypos = new ExtendedDatatypes.int3(x - oldcampos.X + currentcampos.X, y - oldcampos.Y + currentcampos.Y, z - oldcampos.Z + currentcampos.Z);
                                if (newdataarraypos.X >= 0 && newdataarraypos.Y >= 0 && newdataarraypos.Z >= 0 && newdataarraypos.X < worlddatasize && newdataarraypos.Y < worlddatasize && newdataarraypos.Z < worlddatasize) //DATAChunk still in Array
                                    bufferDchunks[newdataarraypos.X, newdataarraypos.Y, newdataarraypos.Z] = Dchunks[x, y, z];
                                else
                                {
                                    if (Dchunks[x, y, z].DATA != null && unused_Dchunks_length < unused_Dchunks.Length)
                                        unused_Dchunks[unused_Dchunks_length++] = Dchunks[x, y, z].DATA;
                                }
                            }
                        }
                    }
                    DataChunk[,,] buf = Dchunks;
                    Dchunks = bufferDchunks;
                    bufferDchunks = bufferDchunks2;
                    bufferDchunks2 = buf;
                }
                lock (worlddeformer.AccessDATA)
                {
                    worlddeformer.AccessDATA = Dchunks;
                    worlddeformer.accesscampos = currentcampos;
                    worlddeformer.AccessCHUNKS = chunks;
                }
                // Selecting Chunks and DATAChunks to generate
                int chunks2genpos = 0, data2genpos = 0, chunk_synccount = 0;
                int xx = Game1.LOADING_RADIUS, zz = Game1.LOADING_RADIUS;
                int mode = 0, count = 0;
                int currentbound = 1;
                while (true)
                {
                    count++;
                    if (Math.Abs(xx - Game1.LOADING_RADIUS) > Game1.LOADING_RADIUS || Math.Abs(zz - Game1.LOADING_RADIUS) > Game1.LOADING_RADIUS)
                        break;
                    for (int yy = 0; yy < chunksize; ++yy)
                    {
                        if (chunks[xx, yy, zz] == null && new Vector3(xx - Game1.LOADING_RADIUS, yy - Game1.LOADING_RADIUS, zz - Game1.LOADING_RADIUS).Length() < chunksize * 0.5f - 1.5f && Math.Abs(yy - Game1.LOADING_RADIUS) < Game1.LOADING_HEIGHT)
                        {
                            things2gen.CHUNKS[chunks2genpos++] = new ExtendedDatatypes.int3(xx, yy, zz);
                            for (int x2 = 0; x2 < 3; ++x2)
                            {
                                for (int z2 = 0; z2 < 3; ++z2)
                                {
                                    for (int y2 = 0; y2 < 3; ++y2)
                                    {
                                        if (Dchunks[x2 + xx, y2 + yy, z2 + zz].GetsGenerated == false && Dchunks[x2 + xx, y2 + yy, z2 + zz].IsReady == false)
                                        {
                                            things2gen.GEN_BYTES[data2genpos] = 0; things2gen.DATA[data2genpos++] = new ExtendedDatatypes.int3(x2 + xx, y2 + yy, z2 + zz); chunk_synccount++; Dchunks[x2 + xx, y2 + yy, z2 + zz].GetsGenerated = true;
                                        }
                                    }
                                }
                            }
                            things2gen.CHUNKS_SYNC[chunks2genpos - 1] = chunk_synccount;
                        }
                    }
                    if (mode == 0) xx--;
                    if (mode == 1) zz--;
                    if (mode == 2) xx++;
                    if (mode == 3) zz++;

                    int xx2 = xx, zz2 = zz;
                    if (mode == 0) xx2 = xx - 1;
                    if (mode == 1) zz2 = zz - 1;
                    if (mode == 2) xx2 = xx + 1;
                    if (mode == 3) zz2 = zz + 1;

                    if (Math.Abs(xx2 - Game1.LOADING_RADIUS) > currentbound || Math.Abs(zz2 - Game1.LOADING_RADIUS) > currentbound)
                    {
                        if (mode != 3)
                            mode++;
                        else
                        {
                            mode = 0;
                            currentbound++;
                        }
                    }
                }
                things2gen.C_start = things2gen.D_start = 0;
                things2gen.C_end = chunks2genpos;
                things2gen.D_end = data2genpos;
                for (int i = 0; i < 50; ++i)
                {
                    things2gen.GEN_BYTES[data2genpos + i] = 0;
                }
                //things2gen.GEN_BYTES = new byte[data2genpos + 10];
                lowestGEN_ID = 0;
                HasNewUpdate = 0;

                // Generation Threads are running again

                for (int i = 0; i < TooDeletingChunksLength; ++i)
                {
                    if (TooDeletingChunks[i] != null && TooDeletingChunks[i].IsDisposed == false)
                        TooDeletingChunks[i].Dispose();
                    TooDeletingChunks[i] = null;
                }
                for (int x = 0; x < worlddatasize; ++x)
                {
                    for (int z = 0; z < worlddatasize; ++z)
                    {
                        for (int y = 0; y < worlddatasize; ++y)
                        {
                            Dchunks[x, y, z].GetsGenerated = false;
                        }
                    }
                }

                watch.Stop();
                Console.WriteLine("TIIIIIIIIMMMMMMMMEEEE: " + (watch.ElapsedTicks / (float)(Stopwatch.Frequency / 1000.0f)));
                Thread.Sleep(333);
                oldcampos = currentcampos;
            }
        }

        // GENERATION THREADS
        public void GenerationThread(object obj)
        {
            int ID = (int)obj;
            GenerationList4Threads currentWA = null;
            while (true)
            {
                if (HasNewUpdate == 1)
                {
                    generatethreadstates[ID] = 1;
                    while (HasNewUpdate == 1) { Thread.Sleep(1); }
                    generatethreadstates[ID] = 0;

                    currentWA = things2gen;
                }

                if (currentWA != null)
                {
                    bool IsOpenCLAcceleartion = Game1.IsOpenCLAccelerated && Game1.LastDrawCalls < Game1.OpenCLMaxDrawCalls && Game1.LastTriangleDraws < Game1.OpenCLMaxTriangleDraws;

                    if (ID != 0 || !IsOpenCLAcceleartion)
                    {
                        #region Updating Chunks

                        Chunk CHUNK_2_GENERATE = null;
                        lock (currentWA.CHUNKS2UPDATE)
                        {
                            if (currentWA.C2U_end > 0)
                            {
                                CHUNK_2_GENERATE = currentWA.CHUNKS2UPDATE[--currentWA.C2U_end];
                                //currentWA.CHUNKS2UPDATE[currentWA.C2U_end] = null;
                            }
                        }

                        if (CHUNK_2_GENERATE != null)
                        {
                            lock (CHUNK_2_GENERATE)
                            {
                                //CHUNK_2_GENERATE.chunkpos = new int3((CHUNK_2_GENERATE.worldarraypos.X - 1) - Game1.LOADING_RADIUS - currentcampos.X, (CHUNK_2_GENERATE.worldarraypos.Y - 1) - Game1.LOADING_RADIUS - currentcampos.Y, (CHUNK_2_GENERATE.worldarraypos.Z - 1) - Game1.LOADING_RADIUS - currentcampos.Z);
                                CHUNK_2_GENERATE.Generate2(ID, true);
                            }

                            continue;
                        }

                        #endregion
                    }

                    if (ID == 0 || !IsOpenCLAcceleartion)
                    {
                        #region Generating Data

                        int DATA_ID_2_GENERATE = -1;
                        lock (currentWA.DATA)
                        {
                            if (currentWA.D_start != currentWA.D_end)
                            {
                                DATA_ID_2_GENERATE = currentWA.D_start;
                                currentWA.D_start++;
                            }
                        }

                        if (DATA_ID_2_GENERATE != -1)
                        {
                            ExtendedDatatypes.int3 ID_pos = currentWA.DATA[DATA_ID_2_GENERATE];
                            GenerateNewData(ID_pos.X, ID_pos.Y, ID_pos.Z, ID, IsOpenCLAcceleartion);
                            lock (things2gen.GEN_BYTES)
                            {
                                currentWA.GEN_BYTES[DATA_ID_2_GENERATE] = 1;
                                if (lowestGEN_ID == DATA_ID_2_GENERATE)
                                {


                                    for (int i = 0; ; ++i)
                                    {
                                        if (currentWA.GEN_BYTES[i + DATA_ID_2_GENERATE] == 0)
                                        {
                                            lowestGEN_ID = lowestGEN_ID + i;
                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        #endregion
                    }

                    if (ID != 0 || !IsOpenCLAcceleartion)
                    {
                        #region Generating Chunks

                        int CHUNK_ID_2_GENERATE = -1;
                        lock (currentWA.CHUNKS)
                        {
                            lock (currentWA.DATA)
                            {
                                if (currentWA.C_start != currentWA.C_end)
                                {
                                    if (currentWA.CHUNKS_SYNC[currentWA.C_start] < lowestGEN_ID - 28 || currentWA.D_start == currentWA.D_end)
                                    {
                                        CHUNK_ID_2_GENERATE = currentWA.C_start;
                                        currentWA.C_start++;
                                    }
                                }
                            }
                        }

                        if (CHUNK_ID_2_GENERATE != -1)
                        {
                            ExtendedDatatypes.int3 ID_pos = currentWA.CHUNKS[CHUNK_ID_2_GENERATE];
                            GenerateNewChunk(ID_pos.X, ID_pos.Y, ID_pos.Z, ID);
                        }

                        #endregion
                    }

                    if ((currentWA.C_end == 0 && currentWA.D_end == 0) || (currentWA.D_end == currentWA.D_start && currentWA.C_end == currentWA.C_start))
                        Thread.Sleep(20);
                }
                else
                    Thread.Sleep(10);
            }
        }

        public void GenerateNewData(int x, int y, int z, int threadID, bool IsOpenCLAccelerated)
        {
            Stopwatch watch = new Stopwatch();
            watch.Start();
            sbyte[,,] currentDATA = null;
            bool GenerateNewDATA = false;
            lock (unused_Dchunks)
            {
                if (unused_Dchunks_length > 0)
                {
                    currentDATA = unused_Dchunks[--unused_Dchunks_length];
                    if (unused_Dchunks_length + 1 < unused_Dchunks.Length)
                        unused_Dchunks[unused_Dchunks_length + 1] = null;
                }
                else
                    GenerateNewDATA = true;
            }

            if (GenerateNewDATA)
                currentDATA = new sbyte[Game1.CS, Game1.CS, Game1.CS];

            int state = 0;
            if (IsOpenCLAccelerated)
            {
                sums[0] = sums[1] = 0;
                OpenCL_WriteGPUMemory(cmdqueue, OCL_SUMS, sums);
                Cl.ReleaseEvent(OpenCL_event);
                Cl.SetKernelArg(OCL_CHUNK_KERNEL, 1, x - currentcampos.X - Game1.LOADING_RADIUS);
                Cl.SetKernelArg(OCL_CHUNK_KERNEL, 2, y - currentcampos.Y - Game1.LOADING_RADIUS);
                Cl.SetKernelArg(OCL_CHUNK_KERNEL, 3, z - currentcampos.Z - Game1.LOADING_RADIUS);
                Cl.SetKernelArg(OCL_CHUNK_KERNEL, 5, OCL_SUMS);

                ErrorCode err = OpenCL_RunKernel(OCL_CHUNK_KERNEL, cmdqueue, 3, Game1.CS);
                err = Cl.Finish(cmdqueue);
                err = Cl.ReleaseEvent(OpenCL_event);

                OpenCL_ReadGPUMemory(cmdqueue, OCL_CHUNK_DATA, currentDATA);
                Cl.ReleaseEvent(OpenCL_event);
                OpenCL_ReadGPUMemory2(cmdqueue, OCL_SUMS, sums);
                Cl.ReleaseEvent(OpenCL_event);
                if (Math.Abs(sums[0]) == sums[1])
                {
                    if (sums[0] < 0)
                        state = sbyte.MinValue;
                    else
                        state = sbyte.MaxValue;
                }
            }
            else
                state = Generate_DATA(currentDATA, x - currentcampos.X - Game1.LOADING_RADIUS, y - currentcampos.Y - Game1.LOADING_RADIUS, z - currentcampos.Z - Game1.LOADING_RADIUS, currentcampos);

            watch.Stop();

            if (state == 0)
            {
                Dchunks[x, y, z].DATA = currentDATA;
                Dchunks[x, y, z].IsEmpty = 0;
                Dchunks[x, y, z].IsReady = true;
            }
            else
            {
                lock (unused_Dchunks)
                {
                    if (unused_Dchunks_length < unused_Dchunks.Length)
                        unused_Dchunks[unused_Dchunks_length++] = currentDATA;
                }

                Dchunks[x, y, z].DATA = null;
                Dchunks[x, y, z].IsEmpty = (sbyte)state;
                Dchunks[x, y, z].IsReady = true;
            }

            Console.WriteLine("Value Generator Time: " + (watch.ElapsedTicks / (float)TimeSpan.TicksPerMillisecond));
            Game1.finaltime2 += (watch.ElapsedTicks / (float)(Stopwatch.Frequency / 1000.0f)); // 3850
        }

        public void GenerateNewChunk(int x, int y, int z, int threadid)
        {
            chunks[x, y, z] = new Chunk(this, new ExtendedDatatypes.int3(x - Game1.LOADING_RADIUS - currentcampos.X, y - Game1.LOADING_RADIUS - currentcampos.Y, z - Game1.LOADING_RADIUS - currentcampos.Z));
            chunks[x, y, z].worldarraypos = new ExtendedDatatypes.int3(x + 1, y + 1, z + 1);
            chunks[x, y, z].Generate2(threadid, false);
        }

        public short GetValueAt(int x, int y, int z)
        {
            x += (Game1.LOADING_RADIUS + 1) * Game1.CS;// + Game1.CHUNK_SIZE / 2;
            y += (Game1.LOADING_RADIUS + 1) * Game1.CS;// + Game1.CHUNK_SIZE / 2;
            z += (Game1.LOADING_RADIUS + 1) * Game1.CS;// + Game1.CHUNK_SIZE / 2;
            lock (worlddeformer.AccessDATA)
            {
                ExtendedDatatypes.int3 chunkpos = new ExtendedDatatypes.int3(x / Game1.CS + worlddeformer.accesscampos.X, y / Game1.CS + worlddeformer.accesscampos.Y, z / Game1.CS + worlddeformer.accesscampos.Z);
                if (x < 0)
                    chunkpos.X = (x + 1) / Game1.CS + worlddeformer.accesscampos.X - 1;
                if (y < 0)
                    chunkpos.Y = (y + 1) / Game1.CS + worlddeformer.accesscampos.Y - 1;
                if (z < 0)
                    chunkpos.Z = (z + 1) / Game1.CS + worlddeformer.accesscampos.Z - 1;
                if (worlddeformer.AccessDATA[chunkpos.X, chunkpos.Y, chunkpos.Z].DATA != null || worlddeformer.AccessDATA[chunkpos.X, chunkpos.Y, chunkpos.Z].IsEmpty != 0)
                {
                    ExtendedDatatypes.int3 datainchunkpos = new ExtendedDatatypes.int3(x % Game1.CS, y % Game1.CS, z % Game1.CS);
                    if (datainchunkpos.X < 0)
                        datainchunkpos.X = Game1.CS + datainchunkpos.X;
                    if (datainchunkpos.Y < 0)
                        datainchunkpos.Y = Game1.CS + datainchunkpos.Y;
                    if (datainchunkpos.Z < 0)
                        datainchunkpos.Z = Game1.CS + datainchunkpos.Z;
                    if (worlddeformer.AccessDATA[chunkpos.X, chunkpos.Y, chunkpos.Z].IsEmpty != 0)
                        return worlddeformer.AccessDATA[chunkpos.X, chunkpos.Y, chunkpos.Z].IsEmpty;
                    return worlddeformer.AccessDATA[chunkpos.X, chunkpos.Y, chunkpos.Z].DATA[datainchunkpos.X, datainchunkpos.Y, datainchunkpos.Z];
                }

            }
            return 0;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool CanBeMined(int x, int y, int z)
        {
            if (GetValueAt(x, y, z) == short.MinValue)
                return false;
            for (int xx = -1; xx < 2; ++xx)
            {
                for (int yy = -1; yy < 2; ++yy)
                {
                    if (GetValueAt(x + xx, y + yy, z + 1) < 0)
                        return true;
                    if (GetValueAt(x + xx, y + yy, z) < 0)
                        return true;
                    if (GetValueAt(x + xx, y + yy, z - 1) < 0)
                        return true;
                }
            }
            /*if (GetValueAt(x + 1, y, z) < 0 || GetValueAt(x - 1, y, z) < 0 || GetValueAt(x, y + 1, z) < 0 || GetValueAt(x, y - 1, z) < 0 || GetValueAt(x, y, z + 1) < 0 || GetValueAt(x, y, z - 1) < 0)
			    return true;
		    if (GetValueAt(x + 1, y, z + 1) < 0 || GetValueAt(x - 1, y, z + 1) < 0 || GetValueAt(x - 1, y, z + 1) < 0 || GetValueAt(x - 1, y, z - 1) < 0)
			    return true;
		    if (GetValueAt(x + 1, y, z) < 0 || GetValueAt(x - 1, y, z + 1) < 0 || GetValueAt(x - 1, y, z + 1) < 0 || GetValueAt(x - 1, y, z - 1) < 0)
			    return true;*/
            return false;
        }

        /*public void SubtractValueAt(int x, int y, int z, short value) // BUGGY
	    {
            lock (AccessDATA)
            {
                int3 chunkDATApos = new int3(x / Game1.CHUNK_SIZE + accesscampos.X, y / Game1.CHUNK_SIZE + accesscampos.Y, z / Game1.CHUNK_SIZE + accesscampos.Z);
                if (x < 0)
                    chunkDATApos.X = (x + 1) / Game1.CHUNK_SIZE + accesscampos.X - 1;
                if (y < 0)
                    chunkDATApos.Y = (y + 1) / Game1.CHUNK_SIZE + accesscampos.Y - 1;
                if (z < 0)
                    chunkDATApos.Z = (z + 1) / Game1.CHUNK_SIZE + accesscampos.Z - 1;
                if (AccessDATA[chunkDATApos.X, chunkDATApos.Y, chunkDATApos.Z].DATA != null || AccessDATA[chunkDATApos.X, chunkDATApos.Y, chunkDATApos.Z].IsEmpty != 0)
                {
                    int3 datainchunkDATApos = new int3(x % Game1.CHUNK_SIZE, y % Game1.CHUNK_SIZE, z % Game1.CHUNK_SIZE);
                    if (datainchunkDATApos.X < 0)
                        datainchunkDATApos.X = Game1.CHUNK_SIZE + datainchunkDATApos.X;
                    if (datainchunkDATApos.Y < 0)
                        datainchunkDATApos.Y = Game1.CHUNK_SIZE + datainchunkDATApos.Y;
                    if (datainchunkDATApos.Z < 0)
                        datainchunkDATApos.Z = Game1.CHUNK_SIZE + datainchunkDATApos.Z;
                    if (AccessDATA[chunkDATApos.X, chunkDATApos.Y, chunkDATApos.Z].DATA != null)
                    {
                        AccessDATA[chunkDATApos.X, chunkDATApos.Y, chunkDATApos.Z].DATA[datainchunkDATApos.X, datainchunkDATApos.Y, datainchunkDATApos.Z] = (short)MathHelper.Max(AccessDATA[chunkDATApos.X, chunkDATApos.Y, chunkDATApos.Z].DATA[datainchunkDATApos.X, datainchunkDATApos.Y, datainchunkDATApos.Z] - value, short.MinValue);
                        AccessDATA[chunkDATApos.X, chunkDATApos.Y, chunkDATApos.Z].IsEmpty = 0;
                    }
                    else
                    {
                        AccessDATA[chunkDATApos.X, chunkDATApos.Y, chunkDATApos.Z].DATA = new short[Game1.CHUNK_SIZE, Game1.CHUNK_SIZE, Game1.CHUNK_SIZE];
                        for (int xx = 0; xx < Game1.CHUNK_SIZE; ++xx)
                            for (int yy = 0; yy < Game1.CHUNK_SIZE; ++yy)
                                for (int zz = 0; zz < Game1.CHUNK_SIZE; ++zz)
                                    AccessDATA[chunkDATApos.X, chunkDATApos.Y, chunkDATApos.Z].DATA[xx, yy, zz] = AccessDATA[chunkDATApos.X, chunkDATApos.Y, chunkDATApos.Z].IsEmpty;
                        AccessDATA[chunkDATApos.X, chunkDATApos.Y, chunkDATApos.Z].IsEmpty = 0;
                        AccessDATA[chunkDATApos.X, chunkDATApos.Y, chunkDATApos.Z].DATA[datainchunkDATApos.X, datainchunkDATApos.Y, datainchunkDATApos.Z] = (short)MathHelper.Max(AccessDATA[chunkDATApos.X, chunkDATApos.Y, chunkDATApos.Z].DATA[datainchunkDATApos.X, datainchunkDATApos.Y, datainchunkDATApos.Z] - value, short.MinValue);
                    }

                }
            }
        }


	    public void SetDataAt(int x, int y, int z, short value)
	    {
		    x += (Game1.LOADING_RADIUS + 1) * Game1.CHUNK_SIZE;
		    y += (Game1.LOADING_RADIUS + 1) * Game1.CHUNK_SIZE;
		    z += (Game1.LOADING_RADIUS + 1) * Game1.CHUNK_SIZE;
		    lock (AccessDATA)
		    {
			    int3 chunkDATApos = new int3(x / Game1.CHUNK_SIZE + accesscampos.X, y / Game1.CHUNK_SIZE + accesscampos.Y, z / Game1.CHUNK_SIZE + accesscampos.Z);
			    if (x < 0)
				    chunkDATApos.X = (x + 1) / Game1.CHUNK_SIZE + accesscampos.X - 1;
                if (y < 0)
	                chunkDATApos.Y = (y + 1) / Game1.CHUNK_SIZE + accesscampos.Y - 1;
                if (z < 0)
	                chunkDATApos.Z = (z + 1) / Game1.CHUNK_SIZE + accesscampos.Z - 1;
                if (AccessDATA[chunkDATApos.X, chunkDATApos.Y, chunkDATApos.Z].DATA != null || AccessDATA[chunkDATApos.X, chunkDATApos.Y, chunkDATApos.Z].IsEmpty != 0)
			    {
				    int3 datainchunkDATApos = new int3(x % Game1.CHUNK_SIZE, y % Game1.CHUNK_SIZE, z % Game1.CHUNK_SIZE);
				    if (datainchunkDATApos.X < 0)
					    datainchunkDATApos.X = Game1.CHUNK_SIZE + datainchunkDATApos.X;
				    if (datainchunkDATApos.Y < 0)
					    datainchunkDATApos.Y = Game1.CHUNK_SIZE + datainchunkDATApos.Y;
				    if (datainchunkDATApos.Z < 0)
					    datainchunkDATApos.Z = Game1.CHUNK_SIZE + datainchunkDATApos.Z;
				    if (AccessDATA[chunkDATApos.X, chunkDATApos.Y, chunkDATApos.Z].DATA != null)
				    {
						AccessDATA[chunkDATApos.X, chunkDATApos.Y, chunkDATApos.Z].DATA[datainchunkDATApos.X, datainchunkDATApos.Y, datainchunkDATApos.Z] = value;
					    AccessDATA[chunkDATApos.X, chunkDATApos.Y, chunkDATApos.Z].IsEmpty = 0;
				    }
				    else
				    {
					    AccessDATA[chunkDATApos.X, chunkDATApos.Y, chunkDATApos.Z].DATA = new short[Game1.CHUNK_SIZE, Game1.CHUNK_SIZE, Game1.CHUNK_SIZE];
					    for (int xx = 0; xx < Game1.CHUNK_SIZE; ++xx)
							for (int yy = 0; yy < Game1.CHUNK_SIZE; ++yy)
								for (int zz = 0; zz < Game1.CHUNK_SIZE; ++zz)
									 AccessDATA[chunkDATApos.X, chunkDATApos.Y, chunkDATApos.Z].DATA[xx, yy, zz] = AccessDATA[chunkDATApos.X, chunkDATApos.Y, chunkDATApos.Z].IsEmpty;
					    AccessDATA[chunkDATApos.X, chunkDATApos.Y, chunkDATApos.Z].IsEmpty = 0;
                        AccessDATA[chunkDATApos.X, chunkDATApos.Y, chunkDATApos.Z].DATA[datainchunkDATApos.X, datainchunkDATApos.Y, datainchunkDATApos.Z] = value;
				    }

			    }
		    }
        }
        public void Subtract2DataAt(int x, int y, int z, int value)
        {
            x += (Game1.LOADING_RADIUS + 1) * Game1.CHUNK_SIZE;
            y += (Game1.LOADING_RADIUS + 1) * Game1.CHUNK_SIZE;
            z += (Game1.LOADING_RADIUS + 1) * Game1.CHUNK_SIZE;
            lock (AccessDATA)
            {
                int3 chunkDATApos = new int3(x / Game1.CHUNK_SIZE + accesscampos.X, y / Game1.CHUNK_SIZE + accesscampos.Y, z / Game1.CHUNK_SIZE + accesscampos.Z);
                if (x < 0)
                    chunkDATApos.X = (x + 1) / Game1.CHUNK_SIZE + accesscampos.X - 1;
                if (y < 0)
                    chunkDATApos.Y = (y + 1) / Game1.CHUNK_SIZE + accesscampos.Y - 1;
                if (z < 0)
                    chunkDATApos.Z = (z + 1) / Game1.CHUNK_SIZE + accesscampos.Z - 1;
                if (AccessDATA[chunkDATApos.X, chunkDATApos.Y, chunkDATApos.Z].DATA != null || AccessDATA[chunkDATApos.X, chunkDATApos.Y, chunkDATApos.Z].IsEmpty != 0)
                {
                    int3 datainchunkDATApos = new int3(x % Game1.CHUNK_SIZE, y % Game1.CHUNK_SIZE, z % Game1.CHUNK_SIZE);
                    if (datainchunkDATApos.X < 0)
                        datainchunkDATApos.X = Game1.CHUNK_SIZE + datainchunkDATApos.X;
                    if (datainchunkDATApos.Y < 0)
                        datainchunkDATApos.Y = Game1.CHUNK_SIZE + datainchunkDATApos.Y;
                    if (datainchunkDATApos.Z < 0)
                        datainchunkDATApos.Z = Game1.CHUNK_SIZE + datainchunkDATApos.Z;
                    if (AccessDATA[chunkDATApos.X, chunkDATApos.Y, chunkDATApos.Z].DATA != null)
                    {
                        AccessDATA[chunkDATApos.X, chunkDATApos.Y, chunkDATApos.Z].DATA[datainchunkDATApos.X, datainchunkDATApos.Y, datainchunkDATApos.Z] = (short)MathHelper.Max((int)AccessDATA[chunkDATApos.X, chunkDATApos.Y, chunkDATApos.Z].DATA[datainchunkDATApos.X, datainchunkDATApos.Y, datainchunkDATApos.Z] - value, short.MinValue);
                        AccessDATA[chunkDATApos.X, chunkDATApos.Y, chunkDATApos.Z].IsEmpty = 0;
                    }
                    else
                    {
                        AccessDATA[chunkDATApos.X, chunkDATApos.Y, chunkDATApos.Z].DATA = new short[Game1.CHUNK_SIZE, Game1.CHUNK_SIZE, Game1.CHUNK_SIZE];
                        for (int xx = 0; xx < Game1.CHUNK_SIZE; ++xx)
                            for (int yy = 0; yy < Game1.CHUNK_SIZE; ++yy)
                                for (int zz = 0; zz < Game1.CHUNK_SIZE; ++zz)
                                    AccessDATA[chunkDATApos.X, chunkDATApos.Y, chunkDATApos.Z].DATA[xx, yy, zz] = AccessDATA[chunkDATApos.X, chunkDATApos.Y, chunkDATApos.Z].IsEmpty;
                        AccessDATA[chunkDATApos.X, chunkDATApos.Y, chunkDATApos.Z].IsEmpty = 0;
                        AccessDATA[chunkDATApos.X, chunkDATApos.Y, chunkDATApos.Z].DATA[datainchunkDATApos.X, datainchunkDATApos.Y, datainchunkDATApos.Z] = (short)MathHelper.Max(((int)AccessDATA[chunkDATApos.X, chunkDATApos.Y, chunkDATApos.Z].DATA[datainchunkDATApos.X, datainchunkDATApos.Y, datainchunkDATApos.Z]) - value, short.MinValue);
                    }

                }
            }
        }

        public DataChunk GetChunkAt(int x, int y, int z)
	    {
		    x += (Game1.LOADING_RADIUS + 1) * Game1.CHUNK_SIZE;
		    y += (Game1.LOADING_RADIUS + 1) * Game1.CHUNK_SIZE;
		    z += (Game1.LOADING_RADIUS + 1) * Game1.CHUNK_SIZE;
		    int3 chunkpos = new int3(x / Game1.CHUNK_SIZE + accesscampos.X, y / Game1.CHUNK_SIZE + accesscampos.Y, z / Game1.CHUNK_SIZE + accesscampos.Z);
		    if (x < 0)
			    chunkpos.X = (x + 1) / Game1.CHUNK_SIZE + accesscampos.X - 1;
		    if (y < 0)
			    chunkpos.Y = (y + 1) / Game1.CHUNK_SIZE + accesscampos.Y - 1;
		    if (z < 0)
			    chunkpos.Z = (z + 1) / Game1.CHUNK_SIZE + accesscampos.Z - 1;
		    return AccessDATA[chunkpos.X, chunkpos.Y, chunkpos.Z];
	    }

	    public void SetValuesAt(int3[] pos, short[] value, int length)
	    {
			HashSet<Chunk> Chunks2Update = new HashSet<Chunk>();
		    for (int i = 0; i < length; ++i)
		    {
				Subtract2DataAt(pos[i].X, pos[i].Y, pos[i].Z, value[i]);
			    int x = (Game1.LOADING_RADIUS) * Game1.CHUNK_SIZE + pos[i].X;
			    int y = (Game1.LOADING_RADIUS) * Game1.CHUNK_SIZE + pos[i].Y;
			    int z = (Game1.LOADING_RADIUS) * Game1.CHUNK_SIZE + pos[i].Z;
			    lock (AccessDATA)
			    {
				    int3 chunkpos = new int3(x / Game1.CHUNK_SIZE + accesscampos.X, y / Game1.CHUNK_SIZE + accesscampos.Y, z / Game1.CHUNK_SIZE + accesscampos.Z);
				    if (x < 0)
					    chunkpos.X = (x + 1) / Game1.CHUNK_SIZE + accesscampos.X - 1;
				    if (y < 0)
					    chunkpos.Y = (y + 1) / Game1.CHUNK_SIZE + accesscampos.Y - 1;
				    if (z < 0)
					    chunkpos.Z = (z + 1) / Game1.CHUNK_SIZE + accesscampos.Z - 1;
                    int3 datapos = new int3(x % Game1.CHUNK_SIZE, y % Game1.CHUNK_SIZE, z % Game1.CHUNK_SIZE);
				    if (datapos.X < 0)
					    datapos.X = Game1.CHUNK_SIZE + datapos.X;
				    if (datapos.Y < 0)
					    datapos.Y = Game1.CHUNK_SIZE + datapos.Y;
				    if (datapos.Z < 0)
					    datapos.Z = Game1.CHUNK_SIZE + datapos.Z;
                    for (int xx = -1; xx < 2; ++xx)
				    {
					    for (int yy = -1; yy < 2; ++yy)
					    {
						    for (int zz = -1; zz < 2; ++zz)
						    {
							    int x2 = (xx) * Game1.CHUNK_SIZE;
							    int y2 = (yy) * Game1.CHUNK_SIZE;
							    int z2 = (zz) * Game1.CHUNK_SIZE;

							    if (datapos.X >= x2 - 3 && datapos.X < x2 + Game1.CHUNK_SIZE + 3 && datapos.Y >= y2 - 3 && datapos.Y < y2 + Game1.CHUNK_SIZE + 3 && datapos.Z >= z2 - 3 && datapos.Z < z2 + Game1.CHUNK_SIZE + 3)
							    {
									if(chunkpos.X + xx > 0 && chunkpos.X + xx < Game1.LOADING_RADIUS * 2 + 1 && chunkpos.Y + yy > 0 && chunkpos.Y + yy < Game1.LOADING_RADIUS * 2 + 1 && chunkpos.Z + zz > 0 && chunkpos.Z + zz < Game1.LOADING_RADIUS * 2 + 1)
										Chunks2Update.Add(chunks[chunkpos.X + xx, chunkpos.Y + yy, chunkpos.Z + zz]);
                                }
						    }
                        }
                    }

                }
            }

		    Chunk[] chunks2update_array= Chunks2Update.ToArray();
		    for (int i = 0; i < chunks2update_array.Length; ++i)
		    {
			    lock (things2gen.CHUNKS2UPDATE)
			    {
				    things2gen.CHUNKS2UPDATE[things2gen.C2U_end++] = chunks2update_array[i];
			    }
		    }
	    }*/

        public void Draw(Matrix view, Matrix projection, ExtendedDatatypes.int3 camoffset)
        {
            Game1.HasUploaded1Chunk = false;
            lock (chunks)
            {
                for (int x = 0; x < chunksize; ++x)
                {
                    for (int z = 0; z < chunksize; ++z)
                    {
                        for (int y = 0; y < chunksize; ++y)
                        {
                            Chunk currentchunk = chunks[x, y, z];
                            if (currentchunk != null)
                            {
                                lock (currentchunk)
                                {
                                    if (currentchunk.IsDrawable == false && currentchunk.IsGenerated)// && Game1.HasUploaded1Chunk == false)
                                    {
                                        currentchunk.UploadVertexBuffer2GPU();
                                        //Console.WriteLine("UPLOADED VERTEXES!!!");
                                        Game1.HasUploaded1Chunk = true;
                                    }

                                    if (currentchunk.IsDrawable)
                                    {
                                        Matrix world = Matrix.Identity; //CreateScale(0.2995f);
                                        world = Matrix.CreateTranslation(new Vector3(camoffset.X + currentchunk.chunkpos.X, camoffset.Y + currentchunk.chunkpos.Y, camoffset.Z + currentchunk.chunkpos.Z) * Game1.CS);
                                        //if((currentchunk.chunkpos.Y == -7 && Game1.finaltime % 1000 < 500) || (currentchunk.chunkpos.Y == -6 && Game1.finaltime % 1000 >= 500))
                                        currentchunk.Draw(world, view, projection);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
