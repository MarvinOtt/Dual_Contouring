
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Xml.Serialization;
using static Dual_Contouring.ExtendedDatatypes;
using FormClosingEventArgs = System.Windows.Forms.FormClosingEventArgs;
using Form = System.Windows.Forms.Form;

namespace Dual_Contouring
{

    public class ExtendedDatatypes
    {
        public struct int3
        {
            public bool Equals(int3 other)
            {
                return X == other.X && Y == other.Y && Z == other.Z;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                return obj is int3 && Equals((int3)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hashCode = X;
                    hashCode = (hashCode * 397) ^ Y;
                    hashCode = (hashCode * 397) ^ Z;
                    return hashCode;
                }
            }

            public int X, Y, Z;

            public int3(int x, int y, int z)
            {
                X = x;
                Y = y;
                Z = z;
            }
            public static int3 operator -(int3 i1, int3 i2)
            {
                return new int3(i1.X - i2.X, i1.Y - i2.Y, i1.Z - i2.Z);
            }
            public static int3 operator +(int3 i1, int3 i2)
            {
                return new int3(i1.X + i2.X, i1.Y + i2.Y, i1.Z + i2.Z);
            }
            public static int3 operator *(int3 i1, int3 i2)
            {
                return new int3(i1.X * i2.X, i1.Y * i2.Y, i1.Z * i2.Z);
            }
            public static int3 operator *(int3 i1, int i2)
            {
                return new int3(i1.X * i2, i1.Y * i2, i1.Z * i2);
            }
            public static int3 operator /(int3 i1, int3 i2)
            {
                return new int3(i1.X / i2.X, i1.Y / i2.Y, i1.Z / i2.Z);
            }
            public static int3 operator %(int3 i1, int3 i2)
            {
                return new int3(i1.X % i2.X, i1.Y % i2.Y, i1.Z % i2.Z);
            }
            public static bool operator ==(int3 i1, int3 i2)
            {
                return (i1.X == i2.X && i1.Y == i2.Y && i1.Z == i2.Z);
            }
            public static bool operator !=(int3 i1, int3 i2)
            {
                return !(i1.X == i2.X && i1.Y == i2.Y && i1.Z == i2.Z);
            }

            public Vector3 ToVector3()
            {
                return new Vector3(X, Y, Z);
            }

            public override string ToString()
            {
                return "{" + X + ", " + Y + ", " + Z + "}";
            }
        }
        public struct short3
        {
            public short X, Y, Z;

            public short3(short x, short y, short z)
            {
                X = x;
                Y = y;
                Z = z;
            }
        }
    }

    public class Game1 : Game
    {
        #region VertexDeclarations

        public struct VertexPositionColorNormal_noTexCoo
        {
            public Vector3 Position;
            public Vector3 Normal;
            public Color Color;

            public readonly static VertexDeclaration VertexDeclaration = new VertexDeclaration
            (
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                new VertexElement(sizeof(float) * 3, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
                new VertexElement(sizeof(float) * 6, VertexElementFormat.Color, VertexElementUsage.Color, 0)
            );
            public VertexPositionColorNormal_noTexCoo(Vector3 pos, Vector3 normal, Color color)
            {
                Position = pos;
                Normal = normal;
                Color = color;
            }
        }

        public struct VertexPositionNormaltexCoo_weights
        {
            public Vector3 Position;
            public Vector3 Normal;
            private Vector2 TexCoos;
            public Vector4 TexWeights;

            public readonly static VertexDeclaration VertexDeclaration = new VertexDeclaration
            (
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                new VertexElement(sizeof(float) * 3, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0),
                new VertexElement(sizeof(float) * 6, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
                new VertexElement(sizeof(float) * 8, VertexElementFormat.Vector4, VertexElementUsage.Color, 0)

            );

            public VertexPositionNormaltexCoo_weights(Vector3 pos, Vector3 normal, Vector2 texcoo, Vector4 Texweights)
            {
                Position = pos;
                Normal = normal;
                TexCoos = texcoo;
                TexWeights = Texweights;
            }
        }

        #endregion

        public const string DLL_PATH = "..\\..\\..\\..\\x64\\Release\\";

        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        SpriteFont font;
        public static Random r = new Random();
        public static World world;
        public static BoundingFrustum camerafrustum;
        private RenderTarget2D maintarget, terraindepthtarget;

        public static Effect terrain_effect;
        public static bool HasUploaded1Chunk;
        public static bool IsOpenCLAccelerated;
        public static int OpenCLMaxTriangleDraws, OpenCLMaxDrawCalls, CurrentTriangleDraws, CurrentDrawCalls, LastTriangleDraws, LastDrawCalls;

        public static float finaltime = 0, finaltime2 = 0;
        public int terrainsmoothnesstate = 0;

        #region INPUT
        private KeyboardState oldkeyboardstate, keyboardstate;
        private MouseState oldmousestate, mousestate;
        private Vector2 cameramousepos, mouserotationbuffer;
        private System.Drawing.Point mousepointpos;
        #endregion

        #region Camera
        public static int3 currentccameraoffset;
        public static Vector3 camerapos = new Vector3(0, -286.5f, 0), camerarichtung;
        private Matrix camview, camworld, camprojection;
        private BasicEffect cameraeffect;
        private Vector3 rotation;
        private bool camerabewegen;
        private float cameraspeed = 0.1f;
        #endregion

        public static int Screenwidth = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width;
        public static int Screenheight = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height;

        public const int CS = 64;
        public const int CS4 = CS + 4;
        public const int LOADING_RADIUS = 25;
        public const int LOADING_HEIGHT = 7;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this)
            {
                GraphicsProfile = GraphicsProfile.HiDef,
                PreferredBackBufferWidth = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Width,
                PreferredBackBufferHeight = System.Windows.Forms.Screen.PrimaryScreen.Bounds.Height,
                IsFullScreen = false,
                SynchronizeWithVerticalRetrace = true

            };
            IsFixedTimeStep = false;
            Window.IsBorderless = true;
            IsMouseVisible = true;
            Content.RootDirectory = "Content";
        }

        protected override void Initialize()
        {
            Form f = Form.FromHandle(Window.Handle) as Form;
            f.Location = new System.Drawing.Point(0, 0);
            base.Initialize();
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            font = Content.Load<SpriteFont>("font");
            terrain_effect = Content.Load<Effect>("terrain_effect");
            cameraeffect = new BasicEffect(GraphicsDevice);
            cameraeffect.Projection = Matrix.CreatePerspectiveFieldOfView(MathHelper.ToRadians(80), GraphicsDevice.Viewport.AspectRatio, 0.1f, 10000);
            cameraeffect.World = Matrix.Identity;
            cameraeffect.LightingEnabled = true;
            cameraeffect.EmissiveColor = new Vector3(1, 0, 0);
            camerafrustum = new BoundingFrustum(Matrix.Identity);

            world = new World(GraphicsDevice);
            world.InitializeWorld(12);
            world.StartUpdating();
            Perlin p = new Perlin(0, 2);
            maintarget = new RenderTarget2D(GraphicsDevice, Screenwidth, Screenheight, false, SurfaceFormat.Bgra32, DepthFormat.Depth24Stencil8);
            terraindepthtarget = new RenderTarget2D(GraphicsDevice, Screenwidth, Screenheight, false, SurfaceFormat.Single, DepthFormat.None);
            IsOpenCLAccelerated = true;
            OpenCLMaxTriangleDraws = 94000000 * 3;
            OpenCLMaxDrawCalls = 99600;

        }

        protected override void UnloadContent()
        {
        }

        #region FUNKTIONS

        public Vector3 getDirectionAtPixel(Vector2 pixelpos, Matrix projection, Matrix view)
        {
            Vector3 nearSource = new Vector3((float)pixelpos.X, (float)pixelpos.Y, 0f);
            Vector3 farSource = new Vector3((float)pixelpos.X, (float)pixelpos.Y, 1f);
            Vector3 nearPoint = GraphicsDevice.Viewport.Unproject(nearSource, projection, view, Matrix.Identity);
            Vector3 farPoint = GraphicsDevice.Viewport.Unproject(farSource, projection, view, Matrix.Identity);
            return Vector3.Normalize(farPoint - nearPoint);
        }

        public void DrawLine3d(Vector3 pos1, Vector3 pos2)
        {
            DrawLine3d(pos1, pos2, Color.White, Color.White);
        }

        public void DrawLine3d(Vector3 pos1, Vector3 pos2, Color color1, Color color2)
        {
            cameraeffect.LightingEnabled = false;
            cameraeffect.EmissiveColor = Color.Transparent.ToVector3();
            var vertices = new[] { new VertexPositionColor(pos1, color1), new VertexPositionColor(pos2, color2) };
            foreach (EffectPass pass in cameraeffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                this.GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, vertices, 0, 1);
            }
        }

        public void DrawLine3d(Vector3 pos1, Vector3 pos2, Color color)
        {
            DrawLine3d(pos1, pos2, color, color);
        }

        public void BeginRender3D()
        {
            GraphicsDevice.BlendState = BlendState.Opaque;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
        }

        public static void DrawMesh(BasicEffect basiceffect, Model model, Effect shader)
        {
            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (ModelMeshPart part in mesh.MeshParts)
                {
                    shader.Parameters["World"].SetValue(basiceffect.World * mesh.ParentBone.Transform);
                    shader.Parameters["View"].SetValue(basiceffect.View);
                    shader.Parameters["Projection"].SetValue(basiceffect.Projection);
                    shader.Parameters["EyePosition"].SetValue(camerapos);
                    shader.Parameters["LightDirection"].SetValue(new Vector3(-1, 0, 0));
                    part.Effect = shader;
                }

                mesh.Draw();

            }
        }

        public static void DrawMesh(BasicEffect basiceffect, Model model, Matrix matrix)
        {
            Matrix[] transformations = new Matrix[model.Bones.Count];
            model.CopyAbsoluteBoneTransformsTo(transformations);
            foreach (ModelMesh mesh in model.Meshes)
            {
                foreach (BasicEffect effect in mesh.Effects)
                {
                    //effect.EnableDefaultLighting();
                    effect.PreferPerPixelLighting = true;
                    effect.EmissiveColor = basiceffect.EmissiveColor;
                    effect.LightingEnabled = basiceffect.LightingEnabled;
                    if (effect.LightingEnabled == true)
                    {
                        effect.AmbientLightColor = new Vector3(0.1f, 0.1f, 0.1f);
                        effect.DirectionalLight0.Enabled = true;
                        effect.DirectionalLight0.Direction = new Vector3(1, -1, 0);
                        effect.DirectionalLight0.DiffuseColor = Color.LightGoldenrodYellow.ToVector3() * 0.2f;
                        effect.DirectionalLight0.SpecularColor = Color.LightGoldenrodYellow.ToVector3() * 1;
                    }

                    effect.World = transformations[mesh.ParentBone.Index] * matrix;
                    effect.View = basiceffect.View;
                    effect.Projection = basiceffect.Projection;
                }

                mesh.Draw();
            }
        }

        public static void MeshPos(ref Model model, Matrix original, int ID, Vector3 pos)
        {
            model.Bones[ID].Transform = original;
            Vector3 oldpos = model.Bones[ID].Transform.Translation;
            model.Bones[ID].Transform *= Matrix.CreateTranslation(pos - oldpos);
        }

        public static void MeshMatrix(ref Model model, Matrix original, int ID, Matrix matrix)
        {
            model.Bones[ID].Transform = original;
            Vector3 oldpos = model.Bones[ID].Transform.Translation;
            model.Bones[ID].Transform *= Matrix.CreateTranslation(Vector3.Zero - oldpos) * matrix;
        }

        public static void MeshMatrix(ref Model model, Matrix original, int ID, Matrix matrix, Vector3 pos)
        {
            model.Bones[ID].Transform = original;
            Vector3 oldpos = model.Bones[ID].Transform.Translation;
            model.Bones[ID].Transform *= matrix * Matrix.CreateTranslation(pos - oldpos);
        }

        #endregion

        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            keyboardstate = Keyboard.GetState();
            mousestate = Mouse.GetState();
            var mousePosition = new Vector2(System.Windows.Forms.Cursor.Position.X, System.Windows.Forms.Cursor.Position.Y);
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();



            #region Updating Camera

            if (camerapos.X < 0)
            { currentccameraoffset.X += (int)((-(camerapos.X - CS)) / CS); camerapos.X += (int)((-(camerapos.X - CS)) / CS) * CS; }
            if (camerapos.Y < 0)
            { currentccameraoffset.Y += (int)((-(camerapos.Y - CS)) / CS); camerapos.Y += (int)((-(camerapos.Y - CS)) / CS) * CS; }
            if (camerapos.Z < 0)
            { currentccameraoffset.Z += (int)((-(camerapos.Z - CS)) / CS); camerapos.Z += (int)((-(camerapos.Z - CS)) / CS) * CS; }

            if (camerapos.X >= CS)
            { currentccameraoffset.X -= (int)(((camerapos.X)) / CS); camerapos.X -= (int)(((camerapos.X)) / CS) * CS; }
            if (camerapos.Y >= CS)
            { currentccameraoffset.Y -= (int)(((camerapos.Y)) / CS); camerapos.Y -= (int)(((camerapos.Y)) / CS) * CS; }
            if (camerapos.Z >= CS)
            { currentccameraoffset.Z -= (int)(((camerapos.Z)) / CS); camerapos.Z -= (int)(((camerapos.Z)) / CS) * CS; }

            if (Keyboard.GetState().IsKeyDown(Keys.A))
            {
                camerapos.Z -= (float)Math.Sin(rotation.X) * 2 * cameraspeed;
                camerapos.X += (float)Math.Cos(rotation.X) * 2 * cameraspeed;
            }

            if (Keyboard.GetState().IsKeyDown(Keys.D))
            {
                camerapos.Z += (float)Math.Sin(rotation.X) * 2 * cameraspeed;
                camerapos.X -= (float)Math.Cos(rotation.X) * 2 * cameraspeed;
            }

            if (Keyboard.GetState().IsKeyDown(Keys.Space))
            {
                camerapos.Y += 2 * cameraspeed;
            }

            if (Keyboard.GetState().IsKeyDown(Keys.LeftControl))
            {
                camerapos.Y -= 2 * cameraspeed;
            }

            cameraspeed = Keyboard.GetState().IsKeyDown(Keys.LeftShift) ? 1.75f : 0.15f;

            if (keyboardstate.IsKeyDown(Keys.Tab) && !oldkeyboardstate.IsKeyDown(Keys.Tab))
            {
                Mouse.SetPosition(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2);
                camerabewegen = !camerabewegen;
            }

            if (camerabewegen == true && this.IsActive)
            {
                int changed = 0;
                float deltax, deltay;
                deltax = System.Windows.Forms.Cursor.Position.X - cameramousepos.X;
                deltay = System.Windows.Forms.Cursor.Position.Y - cameramousepos.Y;
                mouserotationbuffer.X += 0.004f * deltax;
                mouserotationbuffer.Y += 0.004f * deltay;
                if (mouserotationbuffer.Y < MathHelper.ToRadians(-88))
                {
                    mouserotationbuffer.Y = mouserotationbuffer.Y - (mouserotationbuffer.Y - MathHelper.ToRadians(-88));
                }

                if (mouserotationbuffer.Y > MathHelper.ToRadians(88))
                {
                    mouserotationbuffer.Y = mouserotationbuffer.Y - (mouserotationbuffer.Y - MathHelper.ToRadians(88));
                }

                if (cameramousepos != mousePosition)
                    changed = 1;
                rotation = new Vector3(-mouserotationbuffer.X, -mouserotationbuffer.Y, 0);
                if (changed == 1)
                {
                    System.Windows.Forms.Cursor.Position = mousepointpos;
                }
            }

            if (Mouse.GetState().RightButton == ButtonState.Pressed && IsActive)
            {
                if (camerabewegen == false)
                {
                    camerabewegen = true;
                    cameramousepos = mousePosition;
                    mousepointpos.X = (int)mousePosition.X;
                    mousepointpos.Y = (int)mousePosition.Y;
                }
            }

            if (Mouse.GetState().RightButton == ButtonState.Released && camerabewegen == true)
            {
                camerabewegen = false;
            }


            Matrix rotationMatrix = Matrix.CreateRotationY(rotation.X); // * Matrix.CreateRotationX(rotationY);
            Vector3 transformedReference = Vector3.TransformNormal(new Vector3(0, 0, 1000), rotationMatrix);
            Vector3 cameraLookat = camerapos + transformedReference;
            camerarichtung.Y = cameraLookat.Y - (float)Math.Sin(-rotation.Y) * Vector3.Distance(camerapos, cameraLookat);
            camerarichtung.X = cameraLookat.X - (cameraLookat.X - camerapos.X) * (float)(1 - Math.Cos(rotation.Y));
            camerarichtung.Z = cameraLookat.Z - (cameraLookat.Z - camerapos.Z) * (float)(1 - Math.Cos(rotation.Y));

            if (Keyboard.GetState().IsKeyDown(Keys.W))
            {
                var camerablickrichtung = camerapos - camerarichtung;
                camerablickrichtung = camerablickrichtung / camerablickrichtung.Length();
                camerapos -= camerablickrichtung * 2 * cameraspeed;
                camerarichtung -= camerablickrichtung * 2 * cameraspeed;
            }

            if (Keyboard.GetState().IsKeyDown(Keys.S))
            {
                var camerablickrichtung = camerapos - camerarichtung;
                camerablickrichtung = camerablickrichtung / camerablickrichtung.Length();
                camerapos += camerablickrichtung * 2 * cameraspeed;
                camerarichtung += camerablickrichtung * 2 * cameraspeed;
            }

            cameraeffect.View = Matrix.CreateLookAt(camerapos, camerarichtung, Vector3.Up);
            camworld = cameraeffect.World;
            camview = cameraeffect.View;
            camprojection = cameraeffect.Projection;

            #endregion

            if (mousestate.LeftButton == ButtonState.Pressed && oldmousestate.LeftButton == ButtonState.Released && IsActive)
            {
                // Get collision point
                Vector3 Hitpos = Vector3.Zero;
                Vector3 camdir = getDirectionAtPixel(mousePosition, camprojection, camview);// Vector3.Normalize(camerarichtung - camerapos);
                float[] DATA = new float[1];
                terraindepthtarget.GetData<float>(0, new Rectangle((int)mousePosition.X, (int)mousePosition.Y, 1, 1), DATA, 0, 1);
                float dist = DATA[0];
                Hitpos = new Vector3(camerapos.X + camdir.X * dist, camerapos.Y + camdir.Y * dist, camerapos.Z + camdir.Z * dist);
                /*for (int i = 0; i < 100; ++i)
				{
					int xxx = (int)(camerapos.X + camdir.X * i) - currentccameraoffset.X * CHUNK_SIZE, yyy = (int)(camerapos.Y + camdir.Y * i) - currentccameraoffset.Y * CHUNK_SIZE, zzz = (int)(camerapos.Z + camdir.Z * i) - currentccameraoffset.Z * CHUNK_SIZE;
					if (world.GetValueAt(xxx, yyy, zzz) >= 0)
					{
						HasHit = true;
						Hitpos = new Vector3(camerapos.X + camdir.X * i, camerapos.Y + camdir.Y * i, camerapos.Z + camdir.Z * i);
						break;
					}
				}*/
                if (dist > 2.0f)
                {
                    //int minesize = 2;
                    //float radius = 2.0f;
                    //int currentindex = 0;
                    //int xx = (int) Hitpos.X, yy = (int) Hitpos.Y, zz = (int) Hitpos.Z;
                    //*float spread = 20.0f;
                    //Vector3 start = Hitpos - camdir * spread;
                    //for (int i = 0; i < 500; ++i)
                    //{
                    //	Vector3 end = Hitpos + new Vector3(r.Next(-1000, 1000), r.Next(-1000, 1000), r.Next(-1000, 1000)) * 0.005f;
                    //	Vector3 dir = Vector3.Normalize(end - start);
                    //	for (float dis = 0; dis < spread * 1.5; dis += 0.1f)
                    //	{
                    //		Vector3 curpos = start + dir * dis;
                    //		if (world.CanBeMined((int)curpos.X - currentccameraoffset.X * CHUNK_SIZE, (int)curpos.Y - currentccameraoffset.Y * CHUNK_SIZE, (int)curpos.Z - currentccameraoffset.Z * CHUNK_SIZE))
                    //		{
                    //			if (world.worlddeformer.GetDataAt((int) curpos.X, (int) curpos.Y, (int) curpos.Z) > 0)
                    //			{
                    //				int strength = (int) (750 * MathHelper.Clamp(1 - (dis / (radius * radius)), 0.0f, 1.0f));
                    //				world.worlddeformer.MineDataAt(curpos, 400);
                    //				break;
                    //			}
                    //		}
                    //	}
                    //}*/

                    //for (int x = -minesize; x < minesize + 1; ++x)
                    //{
                    //	for (int y = -minesize; y < minesize + 1; ++y)
                    //	{
                    //		for (int z = -minesize; z < minesize + 1; ++z)
                    //		{


                    //			float x2 = x - camerapos.X % 1.0f;
                    //			float y2 = y - camerapos.Y % 1.0f;
                    //			float z2 = z - camerapos.Z % 1.0f;
                    //			float length = (float) x2 * x2 + y2 * y2 + z2 * z2;
                    //			if (length <= radius * radius)
                    //			{
                    //				if (world.CanBeMined(xx - currentccameraoffset.X * CHUNK_SIZE + x, yy - currentccameraoffset.Y * CHUNK_SIZE + y, zz - currentccameraoffset.Z * CHUNK_SIZE + z))
                    //				{
                    //					Vector3 pos = new Vector3(xx + x, yy + y, zz + z);
                    //					int strength = (int) (12750 * MathHelper.Clamp(1 - ((float)Math.Pow(length, 1.0f / 1.0f) / (float)Math.Pow(radius * radius, 1.0f / 1.0f)), 0.0f, 1.0f));
                    //					world.worlddeformer.MineDataAt(pos, 1000000);
                    //					//world.SetValueAt(xx + x, yy + y, zz + z, -32000);
                    //					currentindex++;
                    //				}
                    //			}
                    //		}
                    //	}
                    //}
                    float size = 12.0f;
                    float incr = 0.999f;
                    for (float x = -size; x <= size; x += incr)
                    {
                        for (float y = -size; y <= size; y += incr)
                        {
                            for (float z = -size; z <= size; z += incr)
                            {
                                Vector3 totalpos = Hitpos + new Vector3(x, y, z);
                                float length = (float)Math.Sqrt(x * x + y * y + z * z) / size;
                                if (1 == 1 || length < 1.0f)
                                {
                                    world.worlddeformer.MineDataAt(totalpos, (int)(1250));// * (1.0f - length)));
                                }
                            }
                        }
                    }


                    world.worlddeformer.UpdateChanges2Chunks(world.things2gen);
                    //world.MineAtPositions(digingbuffer_pos, short.MaxValue);
                }
            }

            if (keyboardstate.IsKeyDown(Keys.T) && oldkeyboardstate.IsKeyUp(Keys.T))
            {
                if (terrainsmoothnesstate == 0)
                    World.SetParameters(0.0f);
                else
                    World.SetParameters(1.0f);
                lock (world.chunks)
                {
                    for (int x = 0; x < LOADING_RADIUS * 2 + 1; ++x)
                    {
                        for (int y = 0; y < LOADING_RADIUS * 2 + 1; ++y)
                        {
                            for (int z = 0; z < LOADING_RADIUS * 2 + 1; ++z)
                            {
                                if (world.chunks[x, y, z] != null && world.chunks[x, y, z].IsDisposed == false)
                                {
                                    world.chunks[x, y, z].IsGenerated = false;
                                    world.chunks[x, y, z].IsDrawable = false;
                                    world.chunks[x, y, z] = null;

                                }
                            }
                        }
                    }
                }
                terrainsmoothnesstate = (terrainsmoothnesstate + 1) % 2;
            }


            oldkeyboardstate = keyboardstate;
            oldmousestate = mousestate;

            base.Update(gameTime);
        }

        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            BeginRender3D();
            cameraeffect.LightingEnabled = true;
            cameraeffect.VertexColorEnabled = true;

            cameraeffect.LightingEnabled = false;
            //floor.Draw(cameraeffect, cameraeffect);

            terrain_effect.Parameters["campos"].SetValue(camerapos);
            GraphicsDevice.SetRenderTargets(maintarget, terraindepthtarget);
            GraphicsDevice.Clear(Color.Gray * 0.5f);

            LastDrawCalls = CurrentDrawCalls;
            LastTriangleDraws = CurrentTriangleDraws;
            CurrentTriangleDraws = CurrentDrawCalls = 0;
            camerafrustum.Matrix = camview * camprojection;
            world.Draw(camview, camprojection, currentccameraoffset);

            GraphicsDevice.SetRenderTarget(null);

            GraphicsDevice.Clear(Color.Gray * 0.5f);

            int xx = (int)camerapos.X - currentccameraoffset.X * CS, yy = (int)camerapos.Y - currentccameraoffset.Y * CS, zz = (int)camerapos.Z - currentccameraoffset.Z * CS;
            //DataChunk currentchunk = world.GetChunkAt(xx, yy, zz);

            spriteBatch.Begin();
            spriteBatch.Draw(maintarget, Vector2.Zero, Color.White);
            //.Draw(terraindepthtarget, Vector2.Zero, Color.White);
            spriteBatch.DrawString(font, currentccameraoffset.ToString(), new Vector2(50, 50), Color.Red);
            spriteBatch.DrawString(font, camerapos.ToString(), new Vector2(50, 80), Color.Red);
            spriteBatch.DrawString(font, finaltime.ToString(), new Vector2(50, 110), Color.Red);
            spriteBatch.DrawString(font, finaltime2.ToString(), new Vector2(50, 140), Color.Red);
            spriteBatch.DrawString(font, "WorldBuffer_length: " + world.unused_Dchunks_length, new Vector2(50, 170), Color.Red);
            spriteBatch.DrawString(font, "VertexBuffer_length: " + world.unused_vertexData_length, new Vector2(50, 200), Color.Red);
            spriteBatch.DrawString(font, "Triangles: " + CurrentTriangleDraws / 3, new Vector2(50, 230), Color.Red);
            spriteBatch.DrawString(font, "DrawCalls: " + CurrentDrawCalls, new Vector2(50, 260), Color.Red);
            //spriteBatch.DrawString(font, world.GetValueAt((int)(camerapos.X) - currentccameraoffset.X * CHUNK_SIZE, (int)(camerapos.Y) - currentccameraoffset.Y * CHUNK_SIZE, (int)(camerapos.Z) - currentccameraoffset.Z * CHUNK_SIZE).ToString(), new Vector2(50, 200), Color.Red);
            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
