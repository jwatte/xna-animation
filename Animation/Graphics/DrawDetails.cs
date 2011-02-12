using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;

namespace KiloWatt.Animation.Graphics
{
  public class DrawDetails
  {
    public GraphicsDevice dev;
    public Matrix world = Matrix.Identity;
    public Matrix view = Matrix.Identity;
    public Matrix viewProj = Matrix.Identity;
    public Matrix viewInv = Matrix.Identity;
    public Matrix projection = Matrix.Identity;
    public TextureCube environmentTexture;
    public Vector3 lightDir = new Vector3(0.57735f, 0.57735f, 0.57735f);
    public Vector4 lightDiffuse = new Vector4(0.7f, 0.6f, 0.5f, 0.0f);
    public Vector4 lightAmbient = new Vector4(0.3f, 0.4f, 0.5f, 1.0f);
    public float fogDistance = 1000;
    public Vector4 fogColor = new Vector4(1, 1, 1, 1);
    public float fogHeight = -100;
    public float fogDepth = 100;
    public int frame;
    public float time;

    public static VertexDeclaration VertexPD;
    public static Vector3[] BufP = new Vector3[256];
    public static VertexDeclaration VertexPCD;
    public static VertexPositionColor[] BufPC = new VertexPositionColor[256];
    public static VertexDeclaration VertexPTD;
    public static VertexPositionTexture[] BufPT = new VertexPositionTexture[256];
    public static VertexDeclaration VertexPCTD;
    public static VertexPositionColorTexture[] BufPCT = new VertexPositionColorTexture[256];
    public static VertexDeclaration VertexPNTD;
    public static VertexPositionNormalTexture[] BufPNT = new VertexPositionNormalTexture[256];
    public static GraphicsDevice Dev;
    public static SpriteFont FontTitle;
    public static SpriteFont FontMenu;
    public static SpriteFont FontHud;
    public static SpriteFont FontText;
    public static SpriteBatch Batch;
    public static Texture2D White;
    public static float Time;

    public static VertexDeclaration VertexPNhThBhTD;

    public static void Initialize(GraphicsDevice dev)
    {
      Dev = dev;
      VertexPD = new VertexDeclaration(new VertexElement[] {
        new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
      });
      VertexPCD = VertexPositionColor.VertexDeclaration;
      VertexPTD = VertexPositionTexture.VertexDeclaration;
      VertexPCTD = VertexPositionColorTexture.VertexDeclaration;
      VertexPNTD = VertexPositionNormalTexture.VertexDeclaration;
      VertexPNhThBhTD = new VertexDeclaration(new VertexElement[] {
        new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
        new VertexElement(12, VertexElementFormat.HalfVector4, VertexElementUsage.Tangent, 0),
        new VertexElement(20, VertexElementFormat.HalfVector4, VertexElementUsage.Normal, 0),
        new VertexElement(28, VertexElementFormat.HalfVector4, VertexElementUsage.Binormal, 0),
        new VertexElement(36, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
      });
    }

    public void CopyTo(DrawDetails o)
    {
      o.dev = dev;
      o.world = world;
      o.view = view;
      o.viewProj = viewProj;
      o.viewInv = viewInv;
      o.projection = projection;
      o.environmentTexture = environmentTexture;
      o.lightDir = lightDir;
      o.lightDiffuse = lightDiffuse;
      o.lightAmbient = lightAmbient;
      o.fogDistance = fogDistance;
      o.fogColor = fogColor;
      o.fogHeight = fogHeight;
      o.fogDepth = fogDepth;
      o.frame = frame;
      o.time = time;
    }

    public void SetViewProjection(Matrix matrix, Matrix matrix_2)
    {
      view = matrix;
      Matrix.Invert(ref matrix, out viewInv);
      projection = matrix_2;
      Matrix.Multiply(ref view, ref projection, out viewProj);
    }
  }
}
