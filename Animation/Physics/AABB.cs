using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;
using System.Diagnostics;

namespace KiloWatt.Animation.Physics
{
  public struct AABB
  {
    public AABB(Vector3 lo, Vector3 hi)
    {
      Lo = lo;
      Hi = hi;
    }
    public void Set(float ax, float ay, float az, float bx, float by, float bz)
    {
      Lo.X = ax; Lo.Y = ay; Lo.Z = az;
      Hi.X = bx; Hi.Y = by; Hi.Z = bz;
    }
    public void Set(Vector3 a, Vector3 b)
    {
      Lo.X = Math.Min(a.X, b.X);
      Hi.X = Math.Max(a.X, b.X);
      Lo.Y = Math.Min(a.Y, b.Y);
      Hi.Y = Math.Max(a.Y, b.Y);
      Lo.Z = Math.Min(a.Z, b.Z);
      Hi.Z = Math.Max(a.Z, b.Z);
    }
    public void Set(Vector3 center, float radius)
    {
      Lo.X = center.X - radius;
      Lo.Y = center.Y - radius;
      Lo.Z = center.Z - radius;
      Hi.X = center.X + radius;
      Hi.Y = center.Y + radius;
      Hi.Z = center.Z + radius;
    }
    public void Inflate(float ds)
    {
      System.Diagnostics.Debug.Assert(ds >= 0);
      Lo.X -= ds;
      Lo.Y -= ds;
      Lo.Z -= ds;
      Hi.X += ds;
      Hi.Y += ds;
      Hi.Z += ds;
    }
    public void Include(Vector3 pt)
    {
      if (Lo.X > pt.X) Lo.X = pt.X;
      if (Hi.X < pt.X) Hi.X = pt.X;
      if (Lo.Y > pt.Y) Lo.Y = pt.Y;
      if (Hi.Y < pt.Y) Hi.Y = pt.Y;
      if (Lo.Z > pt.Z) Lo.Z = pt.Z;
      if (Hi.Z < pt.Z) Hi.Z = pt.Z;
    }
    public void Set(Vector3 start, Vector3 dim, float len)
    {
      Set(start, start + dim * len);
    }
    public Vector3 Lo;
    public Vector3 Hi;
    public Vector3 Center { get { return (Lo + Hi) * 0.5f; } }
    public Vector3 HalfDim { get { return (Hi - Lo) * 0.5f; } }

    public bool Empty { get { return Hi.X <= Lo.X || Hi.Y <= Lo.Y || Hi.Z <= Lo.Z; } }

    public bool Overlaps(AABB other)
    {
      return Overlaps(ref other, 0);
    }

    public bool Overlaps(ref AABB other, float expand)
    {
      if (other.Lo.X >= Hi.X + expand || other.Hi.X <= Lo.X - expand) return false;
      if (other.Lo.Y >= Hi.Y + expand || other.Hi.Y <= Lo.Y - expand) return false;
      if (other.Lo.Z >= Hi.Z + expand || other.Hi.Z <= Lo.Z - expand) return false;
      return true;
    }
    public void SetUnion(AABB other)
    {
      Lo.X = Math.Min(Lo.X, other.Lo.X);
      Lo.Y = Math.Min(Lo.Y, other.Lo.Y);
      Lo.Z = Math.Min(Lo.Z, other.Lo.Z);
      Hi.X = Math.Max(Hi.X, other.Hi.X);
      Hi.Y = Math.Max(Hi.Y, other.Hi.Y);
      Hi.Z = Math.Max(Hi.Z, other.Hi.Z);
    }
    public bool SetIntersection(AABB other)
    {
      if (Empty)
      {
        Lo = other.Lo;
        Hi = other.Hi;
      }
      else if (!other.Empty)
      {
        Lo.X = Math.Max(Lo.X, other.Lo.X);
        Lo.Y = Math.Max(Lo.Y, other.Lo.Y);
        Lo.Z = Math.Max(Lo.Z, other.Lo.Z);
        Hi.X = Math.Min(Hi.X, other.Hi.X);
        Hi.Y = Math.Min(Hi.Y, other.Hi.Y);
        Hi.Z = Math.Min(Hi.Z, other.Hi.Z);
      }
      if (Hi.X > Lo.X && Hi.Y > Lo.Y && Hi.Z > Lo.Z)
        return true;
      Hi = Lo;
      return false;
    }

    public override string ToString()
    {
      return String.Format("{0}-{1}", Lo, Hi);
    }

    public bool Intersects(ref Ray collRay, ref float dd)
    {
      return Intersects(ref collRay, ref dd, 0);
    }

    public bool Intersects(ref Ray collRay, ref float dd, float expand)
    {
      float minI = 0;
      float maxI = dd;
      Vector3 lo = Lo - new Vector3(expand, expand, expand);
      Vector3 hi = Hi + new Vector3(expand, expand, expand);

      if (Math.Abs(collRay.Direction.X) < 1e-10f)
      {
        if (collRay.Position.X < lo.X || collRay.Position.X > hi.X)
          return false;
      }
      else
      {
        float d = 1.0f / collRay.Direction.X;
        if (collRay.Direction.X > 0)
        {
          minI = Math.Max(minI, (lo.X - collRay.Position.X) * d);
          maxI = Math.Min(maxI, (hi.X - collRay.Position.X) * d);
        }
        else
        {
          minI = Math.Max(minI, (hi.X - collRay.Position.X) * d);
          maxI = Math.Min(maxI, (lo.X - collRay.Position.X) * d);
        }
        if (minI >= maxI)
          return false;
      }

      if (Math.Abs(collRay.Direction.Y) < 1e-10f)
      {
        if (collRay.Position.Y < lo.Y || collRay.Position.Y > hi.Y)
          return false;
      }
      else
      {
        float d = 1.0f / collRay.Direction.Y;
        if (collRay.Direction.Y > 0)
        {
          minI = Math.Max(minI, (lo.Y - collRay.Position.Y) * d);
          maxI = Math.Min(maxI, (hi.Y - collRay.Position.Y) * d);
        }
        else
        {
          minI = Math.Max(minI, (hi.Y - collRay.Position.Y) * d);
          maxI = Math.Min(maxI, (lo.Y - collRay.Position.Y) * d);
        }
        if (minI >= maxI)
          return false;
      }

      if (Math.Abs(collRay.Direction.Z) < 1e-10f)
      {
        if (collRay.Position.Z < lo.Z || collRay.Position.Z > hi.Z)
          return false;
      }
      else
      {
        float d = 1.0f / collRay.Direction.Z;
        if (collRay.Direction.Z > 0)
        {
          minI = Math.Max(minI, (lo.Z - collRay.Position.Z) * d);
          maxI = Math.Min(maxI, (hi.Z - collRay.Position.Z) * d);
        }
        else
        {
          minI = Math.Max(minI, (hi.Z - collRay.Position.Z) * d);
          maxI = Math.Min(maxI, (lo.Z - collRay.Position.Z) * d);
        }
        if (minI >= maxI)
          return false;
      }

      dd = minI;
      return true;
    }

    public bool Contains(Vector3 c)
    {
      return (Lo.X <= c.X && Lo.Y <= c.Y && Lo.Z <= c.Z
          && Hi.X > c.X && Hi.Y > c.Y && Hi.Z > c.Z);
    }

    internal Vector3 Vertex(int p)
    {
      return new Vector3(
        ((p & 1) == 0) ? Lo.X : Hi.X,
        ((p & 2) == 0) ? Lo.Y : Hi.Y,
        ((p & 4) == 0) ? Lo.Z : Hi.Z);
    }

    static int[] HasVerts = new int[8 * 3] {
      1, 2, 4,
      0, 3, 5,
      1, 2, 6,
      0, 3, 7,
      0, 5, 6,
      1, 4, 7,
      2, 5, 6,
      3, 4, 7,
    };
    internal bool HasEdgeBetweenVertices(int i, int j, out Ray ray)
    {
      int q = i + i + i;
      bool ret = (HasVerts[q] == j) || (HasVerts[q + 1] == j) || (HasVerts[q + 2] == j);
      ray = new Ray(
        new Vector3(((i & 1) == 0) ? Lo.X : Hi.X, ((i & 2) == 0) ? Lo.Y : Hi.Y, ((i & 4) == 0) ? Lo.Z : Hi.Z),
        new Vector3((i & 1) - (j & 1), ((i & 4) - (j & 4)) * 0.25f, ((i & 2) - (j & 2)) * 0.5f));
      Debug.Assert(ret == false || (ray.Direction.Y == 0 && ray.Direction.Z == 0)
          || (ray.Direction.Z == 0 && ray.Direction.X == 0)
          || (ray.Direction.X == 0 && ray.Direction.Y == 0));
      Debug.Assert(ret == false || ray.Direction.LengthSquared() == 1);
      return ret;
    }

    internal bool Overlaps(ref BoundingSphere sphere, float expand)
    {
      //  find the point in the box that's closest to the center
      Vector3 c = sphere.Center;
      Vector3.Min(ref c, ref Hi, out c);
      Vector3.Max(ref c, ref Lo, out c);
      Vector3.Subtract(ref c, ref sphere.Center, out c);
      //  calculate distance squared between that and center
      float d2;
      Vector3.Dot(ref c, ref c, out d2);
      //  if within range, we overlap
      return (sphere.Radius + expand) * (sphere.Radius + expand) >= d2;
    }
  }
}
