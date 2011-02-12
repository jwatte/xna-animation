using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace KiloWatt.Animation.Graphics
{
  public class DebugLines
  {
    static DebugLines global_;
    public static DebugLines Global { get { return global_; } }
    public static bool Rendering = true;

    VertexBuffer vb_;
    BasicEffect fx_;
    int numLines_;
    VertexPositionColor[] lines_ = new VertexPositionColor[1000];
    GraphicsDevice dev_;
    bool depthTest_;
    public bool DepthTest { get { return depthTest_; } set { depthTest_ = value; } }

    public DebugLines()
    {
      if (global_ == null)
        global_ = this;
    }

    public DebugLines(GraphicsDevice dev)
    {
      if (global_ == null)
        global_ = this;
      Load(dev);
    }

    public void Load(GraphicsDevice dev)
    {
      dev_ = dev;
      vb_ = new DynamicVertexBuffer(dev_, typeof(VertexPositionColor), 1000, BufferUsage.WriteOnly);
      fx_ = new BasicEffect(dev_);
      fx_.World = Matrix.Identity;
      fx_.LightingEnabled = false;
      fx_.TextureEnabled = false;
      fx_.VertexColorEnabled = true;
    }

    public void Unload()
    {
      dev_ = null;
      vb_ = null;
      fx_ = null;
    }

    static float Sqrt2 = (float)Math.Sqrt(0.5f);

    public void AddDebugSphere(Vector3 c, float r)
    {
#if DEBUG
      float r2 = Sqrt2 * r;
      AddLine(c + Vector3.UnitX * r, c + Vector3.UnitX * r2 + Vector3.UnitZ * r2, Color.Green);
      AddLine(c + Vector3.UnitZ * r, c + Vector3.UnitX * r2 + Vector3.UnitZ * r2, Color.Green);
      AddLine(c - Vector3.UnitX * r, c - Vector3.UnitX * r2 + Vector3.UnitZ * r2, Color.Green);
      AddLine(c + Vector3.UnitZ * r, c - Vector3.UnitX * r2 + Vector3.UnitZ * r2, Color.Green);
      AddLine(c - Vector3.UnitX * r, c - Vector3.UnitX * r2 - Vector3.UnitZ * r2, Color.Green);
      AddLine(c - Vector3.UnitZ * r, c - Vector3.UnitX * r2 - Vector3.UnitZ * r2, Color.Green);
      AddLine(c + Vector3.UnitX * r, c + Vector3.UnitX * r2 - Vector3.UnitZ * r2, Color.Green);
      AddLine(c - Vector3.UnitZ * r, c + Vector3.UnitX * r2 - Vector3.UnitZ * r2, Color.Green);

      AddLine(c + Vector3.UnitY * r, c + Vector3.UnitY * r2 + Vector3.UnitZ * r2, Color.Red);
      AddLine(c + Vector3.UnitZ * r, c + Vector3.UnitY * r2 + Vector3.UnitZ * r2, Color.Red);
      AddLine(c - Vector3.UnitY * r, c - Vector3.UnitY * r2 + Vector3.UnitZ * r2, Color.Red);
      AddLine(c + Vector3.UnitZ * r, c - Vector3.UnitY * r2 + Vector3.UnitZ * r2, Color.Red);
      AddLine(c - Vector3.UnitY * r, c - Vector3.UnitY * r2 - Vector3.UnitZ * r2, Color.Red);
      AddLine(c - Vector3.UnitZ * r, c - Vector3.UnitY * r2 - Vector3.UnitZ * r2, Color.Red);
      AddLine(c + Vector3.UnitY * r, c + Vector3.UnitY * r2 - Vector3.UnitZ * r2, Color.Red);
      AddLine(c - Vector3.UnitZ * r, c + Vector3.UnitY * r2 - Vector3.UnitZ * r2, Color.Red);

      AddLine(c + Vector3.UnitX * r, c + Vector3.UnitX * r2 + Vector3.UnitY * r2, Color.Blue);
      AddLine(c + Vector3.UnitY * r, c + Vector3.UnitX * r2 + Vector3.UnitY * r2, Color.Blue);
      AddLine(c - Vector3.UnitX * r, c - Vector3.UnitX * r2 + Vector3.UnitY * r2, Color.Blue);
      AddLine(c + Vector3.UnitY * r, c - Vector3.UnitX * r2 + Vector3.UnitY * r2, Color.Blue);
      AddLine(c - Vector3.UnitX * r, c - Vector3.UnitX * r2 - Vector3.UnitY * r2, Color.Blue);
      AddLine(c - Vector3.UnitY * r, c - Vector3.UnitX * r2 - Vector3.UnitY * r2, Color.Blue);
      AddLine(c + Vector3.UnitX * r, c + Vector3.UnitX * r2 - Vector3.UnitY * r2, Color.Blue);
      AddLine(c - Vector3.UnitY * r, c + Vector3.UnitX * r2 - Vector3.UnitY * r2, Color.Blue);

      AddLine(c - Vector3.UnitX * r, c + Vector3.UnitX * r, Color.Red);
      AddLine(c - Vector3.UnitY * r, c + Vector3.UnitY * r, Color.Green);
      AddLine(c - Vector3.UnitZ * r, c + Vector3.UnitZ * r, Color.Blue);
#endif
    }

    public void AddLine(Vector3 a, Vector3 b, Color color)
    {
#if DEBUG
      if (numLines_ * 2 == lines_.Length) return;
      lines_[numLines_ * 2].Position = a;
      lines_[numLines_ * 2].Color = color;
      lines_[numLines_ * 2 + 1].Position = b;
      lines_[numLines_ * 2 + 1].Color = color;
      numLines_ += 1;
#endif
    }

    public void Draw(Matrix view, Matrix projection)
    {
#if DEBUG
      if (dev_ == null || !Rendering) return;
      if (numLines_ > 0)
      {
        if (depthTest_)
            dev_.DepthStencilState = DepthStencilState.Default;
        else
            dev_.DepthStencilState = DepthStencilState.None;
        vb_.SetData<VertexPositionColor>(lines_);
        fx_.View = view;
        fx_.Projection = projection;
        fx_.CurrentTechnique.Passes[0].Apply();
        dev_.SetVertexBuffer(vb_);
        dev_.DrawPrimitives(PrimitiveType.LineList, 0, numLines_);
      }
#endif
    }

    public void Reset()
    {
      numLines_ = 0;
    }
  }
}
