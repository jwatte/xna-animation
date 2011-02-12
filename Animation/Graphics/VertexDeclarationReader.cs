using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace KiloWatt.Animation.Graphics
{
  public class VertexDeclarationReader : ContentTypeReader<VertexDeclarationIndirect>
  {
    protected override VertexDeclarationIndirect Read(ContentReader input, VertexDeclarationIndirect xi)
    {
      List<VertexElement> vtx = new List<VertexElement>();
      VertexDeclarationContent vdc = new VertexDeclarationContent();
      vdc.Read(input);
      short offset = 0;
      if (vdc.HasPosition)
      {
        vtx.Add(new VertexElement(offset, VertexElementFormat.Vector3, VertexElementUsage.Position, 0));
        offset += 12;
      }
      if (vdc.HasNormal)
      {
        vtx.Add(new VertexElement(offset, VertexElementFormat.Vector3, VertexElementUsage.Normal, 0));
        offset += 12;
      }
      if (vdc.HasUV)
      {
        vtx.Add(new VertexElement(offset, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0));
        offset += 8;
      }
      if (vdc.HasColor)
      {
        vtx.Add(new VertexElement(offset, VertexElementFormat.Color, VertexElementUsage.Position, 0));
        offset += 4;
      }
      if (vdc.HasTangent)
      {
        vtx.Add(new VertexElement(offset, VertexElementFormat.Vector3, VertexElementUsage.Tangent, 0));
        offset += 12;
      }
      if (vdc.HasBitangent)
      {
        vtx.Add(new VertexElement(offset, VertexElementFormat.Vector3, VertexElementUsage.Binormal, 0));
        offset += 12;
      }
      if (vdc.HasWeights)
      {
        vtx.Add(new VertexElement(offset, VertexElementFormat.Byte4, VertexElementUsage.BlendWeight, 0));
        offset += 4;
      }
      if (vdc.HasIndices)
      {
        vtx.Add(new VertexElement(offset, VertexElementFormat.Byte4, VertexElementUsage.BlendIndices, 0));
        offset += 4;
      }
      return new VertexDeclarationIndirect(vtx.ToArray());
    }

    public static GraphicsDevice Device;
  }

  public class VertexDeclarationIndirect : IDisposable
  {
    public VertexDeclarationIndirect(VertexElement[] vd)
    {
      Declaration = vd;
    }
    public VertexElement[] Declaration;
    public static implicit operator VertexElement[](VertexDeclarationIndirect vd) { return vd.Declaration; }
    public void Dispose() {}
  }

  public class VertexDeclarationContent : IDisposable
  {
    public bool HasPosition;
    public bool HasNormal;
    public bool HasUV;
    public bool HasColor;
    public bool HasTangent;
    public bool HasBitangent;
    public bool HasWeights;
    public bool HasIndices;
    public void Read(ContentReader input)
    {
      HasPosition = input.ReadBoolean();
      HasNormal = input.ReadBoolean();
      HasUV = input.ReadBoolean();
      HasColor = input.ReadBoolean();
      HasTangent = input.ReadBoolean();
      HasBitangent = input.ReadBoolean();
      HasWeights = input.ReadBoolean();
      HasIndices = input.ReadBoolean();
    }
    
    public void Dispose() { }

    public static int FormatSize(VertexElementFormat vef)
    {
      switch (vef)
      {
        case VertexElementFormat.Byte4:
        case VertexElementFormat.Color:
        case VertexElementFormat.HalfVector2:
        case VertexElementFormat.NormalizedShort2:
        case VertexElementFormat.Short2:
        case VertexElementFormat.Single:
          return 4;
        case VertexElementFormat.HalfVector4:
        case VertexElementFormat.NormalizedShort4:
        case VertexElementFormat.Short4:
        case VertexElementFormat.Vector2:
          return 8;
        case VertexElementFormat.Vector3:
          return 12;
        case VertexElementFormat.Vector4:
          return 16;
      }
      throw new ArgumentException(String.Format("Unknown vertex element format: {0}", vef));
    }
  }
}
