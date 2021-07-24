using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Dual_Contouring
{
    public class Floor
    {
        private int floorwidth;
        private int floorheight;
        private int floory;
        private float floorscale;
        private VertexBuffer floorbuffer;
        private GraphicsDevice device;
        private Color[] floorcolors = new Color[2] { Color.Black, Color.White };

        public Floor(GraphicsDevice device, int width, int height, int y, float scale)
        {
            this.device = device;
            this.floorwidth = width;
            this.floorheight = height;
            floory = y;
            floorscale = scale;
            BuildFloorBuffer();
        }
        private void BuildFloorBuffer()
        {
            List<VertexPositionColor> vertexlist = new List<VertexPositionColor>();
            int counter = 0;
            for (int x = -floorwidth / 2; x < floorwidth / 2; x++)
            {
                counter++;
                for (int z = -floorwidth / 2; z < floorheight / 2; z++)
                {
                    counter++;
                    foreach (VertexPositionColor vertex in Floortile(x, floory, z, floorcolors[counter % 2]))
                    {
                        vertexlist.Add(vertex);
                    }
                }
            }
            floorbuffer = new VertexBuffer(device, typeof(VertexPositionColor), vertexlist.Count, BufferUsage.WriteOnly);
            floorbuffer.SetData<VertexPositionColor>(vertexlist.ToArray());
        }

        private List<VertexPositionColor> Floortile(int xoffset, int yoffset, int zoffset, Color tilecolor)
        {
            List<VertexPositionColor> vlist = new List<VertexPositionColor>();
            vlist.Add(new VertexPositionColor(new Vector3((0 + xoffset) * floorscale, yoffset, (0 + zoffset) * floorscale), tilecolor));
            vlist.Add(new VertexPositionColor(new Vector3((1 + xoffset) * floorscale, yoffset, (0 + zoffset) * floorscale), tilecolor));
            vlist.Add(new VertexPositionColor(new Vector3((0 + xoffset) * floorscale, yoffset, (1 + zoffset) * floorscale), tilecolor));
            vlist.Add(new VertexPositionColor(new Vector3((1 + xoffset) * floorscale, yoffset, (0 + zoffset) * floorscale), tilecolor));
            vlist.Add(new VertexPositionColor(new Vector3((1 + xoffset) * floorscale, yoffset, (1 + zoffset) * floorscale), tilecolor));
            vlist.Add(new VertexPositionColor(new Vector3((0 + xoffset) * floorscale, yoffset, (1 + zoffset) * floorscale), tilecolor));
            return vlist;
        }
        public int Draw(BasicEffect effect, BasicEffect camera)
        {
            int lol = 0;
            effect.VertexColorEnabled = true;
            effect.View = camera.View;
            effect.Projection = camera.Projection;
            effect.World = Matrix.Identity * Matrix.CreateTranslation(Game1.CS * new Vector3(Game1.currentccameraoffset.X, Game1.currentccameraoffset.Y, Game1.currentccameraoffset.Z));
            foreach (EffectPass pass in effect.CurrentTechnique.Passes)
            {

                pass.Apply();
                device.SetVertexBuffer(floorbuffer);
                device.DrawPrimitives(PrimitiveType.TriangleList, 0, floorbuffer.VertexCount / 3);
                lol++;
            }
            return lol;
        }
    }
}
