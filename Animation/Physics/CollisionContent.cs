using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using System.Diagnostics;

namespace KiloWatt.Animation.Physics
{
  //  todo: could turn ints into shorts quite easily  

  public class CollisionContent
  {
    public CollisionContent()
    {
    }

    public Vector3[] Vertices;
    public Triangle[] Triangles;
    public TreeNode[] Nodes;
    public AABB Bounds;
    
    List<int> ReturnTriangles = new List<int>();

    public interface ITester
    {
      //  The AABB overlap test can be conservative
      bool Overlaps(ref AABB aabb, float expand);
      //  The Triangle intersection test must be precise
      bool Intersects(ref Triangle t, CollisionContent cc);
    }

    struct RayTester : ITester
    {
      internal Ray collRay;
      internal float collRayD;
      public bool Overlaps(ref AABB aabb, float expand)
      {
        float dd = collRayD;
        return aabb.Intersects(ref collRay, ref dd, expand);
      }
      public bool Intersects(ref Triangle t, CollisionContent cc)
      {
        float dd = collRayD;
        return t.Intersects(cc, ref collRay, ref dd);
      }
    }

    struct AABBTester : ITester
    {
      internal AABB box;
      public bool Overlaps(ref AABB aabb, float expand)
      {
        return aabb.Overlaps(ref box, expand);
      }
      public bool Intersects(ref Triangle t, CollisionContent cc)
      {
        return t.Intersects(cc, ref box);
      }
    }

    struct OBBTester : ITester
    {
      internal OBB box;
      public bool Overlaps(ref AABB aabb, float expand)
      {
        return box.Overlaps(ref aabb, expand);
      }
      public bool Intersects(ref Triangle t, CollisionContent cc)
      {
        return t.Intersects(cc, ref box);
      }
    }

    struct SphereTester : ITester
    {
      internal BoundingSphere sphere;
      public bool Overlaps(ref AABB aabb, float expand)
      {
        return aabb.Overlaps(ref sphere, expand);
      }
      public bool Intersects(ref Triangle t, CollisionContent cc)
      {
        return t.Intersects(cc, ref sphere);
      }
    }

#if VALIDATE
    bool[] tested;

    void MarkTriangles(int ix)
    {
      TreeNode tn = Nodes[ix];
      for (int i = tn.TriStart, n = tn.TriEnd;
          i != n; ++i)
      {
        Debug.Assert(!tested[i]);
        tested[i] = true;
      }
      if (tn.Child000 != 0) MarkTriangles(tn.Child000);
      if (tn.Child001 != 0) MarkTriangles(tn.Child001);
      if (tn.Child010 != 0) MarkTriangles(tn.Child010);
      if (tn.Child011 != 0) MarkTriangles(tn.Child011);
      if (tn.Child100 != 0) MarkTriangles(tn.Child100);
      if (tn.Child101 != 0) MarkTriangles(tn.Child101);
      if (tn.Child110 != 0) MarkTriangles(tn.Child110);
      if (tn.Child111 != 0) MarkTriangles(tn.Child111);
    }
#endif

    public List<int> CollectTester(ITester tester)
    {
      ReturnTriangles.Clear();
      if (tester.Overlaps(ref Bounds, 0))
      {
        TraverseNode(0, ref Bounds, tester);
      }
      return ReturnTriangles;
    }

    public List<int> CollectSphere(BoundingSphere sphere)
    {
      ReturnTriangles.Clear();
      if (Bounds.Overlaps(ref sphere, 0))
      {
        SphereTester rt = new SphereTester();
        rt.sphere = sphere;
        TraverseNode(0, ref Bounds, rt);
      }
      return ReturnTriangles;
    }

    public List<int> CollectAABB(AABB bounds)
    {
      ReturnTriangles.Clear();
      if (Bounds.Overlaps(ref bounds, 0))
      {
        AABBTester rt = new AABBTester();
        rt.box = bounds;
        TraverseNode(0, ref Bounds, rt);
      }
      return ReturnTriangles;
    }

    public List<int> CollectRay(Ray ray, float d)
    {
#if VALIDATE
      if (tested == null)
      {
        tested = new bool[Triangles.Length];
        MarkTriangles(0);
        for (int i = 0, n = tested.Length; i != n; ++i)
        {
          Debug.Assert(tested[i]);
        }
      }
#endif
      ReturnTriangles.Clear();
      float dd = d;
#if DEBUG
      trisTested.Clear();
      nodesTested.Clear();
#endif
      if (Bounds.Intersects(ref ray, ref dd))
      {
        RayTester rt = new RayTester();
        rt.collRay = ray;
        rt.collRayD = d;
        TraverseNode(0, ref Bounds, rt);
      }
      return ReturnTriangles;
    }

#if DEBUG
    public static List<int> trisTested = new List<int>();
    public static List<AABB> nodesTested = new List<AABB>();
#endif

    public void TraverseNode(int ix, ref AABB bounds, ITester tester)
    {
#if DEBUG
      nodesTested.Add(bounds);
#endif
#if VALIDATE
      GetNodeTris(ix, ref bounds, tester);
#else
      GetNodeTris(ix, tester);
#endif
      float expand = Nodes[ix].Expansion;
      Vector3 c = bounds.Lo + (bounds.Hi - bounds.Lo) * 0.5f;
      AABB aabb;
      aabb.Lo = bounds.Lo;
      aabb.Hi = c;
      if (Nodes[ix].Child000 != 0 && tester.Overlaps(ref aabb, expand))
      {
        TraverseNode(Nodes[ix].Child000, ref aabb, tester);
      }
      aabb.Lo.Z = c.Z;
      aabb.Hi.Z = bounds.Hi.Z;
      if (Nodes[ix].Child001 != 0 && tester.Overlaps(ref aabb, expand))
      {
        TraverseNode(Nodes[ix].Child001, ref aabb, tester);
      }
      aabb.Lo.Z = bounds.Lo.Z;
      aabb.Hi.Z = c.Z;
      aabb.Lo.Y = c.Y;
      aabb.Hi.Y = bounds.Hi.Y;
      if (Nodes[ix].Child010 != 0 && tester.Overlaps(ref aabb, expand))
      {
        TraverseNode(Nodes[ix].Child010, ref aabb, tester);
      }
      aabb.Lo.Z = c.Z;
      aabb.Hi.Z = bounds.Hi.Z;
      if (Nodes[ix].Child011 != 0 && tester.Overlaps(ref aabb, expand))
      {
        TraverseNode(Nodes[ix].Child011, ref aabb, tester);
      }
      aabb.Lo.Z = bounds.Lo.Z;
      aabb.Hi.Z = c.Z;
      aabb.Lo.Y = bounds.Lo.Y;
      aabb.Hi.Y = c.Y;
      aabb.Lo.X = c.X;
      aabb.Hi.X = bounds.Hi.X;
      if (Nodes[ix].Child100 != 0 && tester.Overlaps(ref aabb, expand))
      {
        TraverseNode(Nodes[ix].Child100, ref aabb, tester);
      }
      aabb.Lo.Z = c.Z;
      aabb.Hi.Z = bounds.Hi.Z;
      if (Nodes[ix].Child101 != 0 && tester.Overlaps(ref aabb, expand))
      {
        TraverseNode(Nodes[ix].Child101, ref aabb, tester);
      }
      aabb.Lo.Z = bounds.Lo.Z;
      aabb.Hi.Z = c.Z;
      aabb.Lo.Y = c.Y;
      aabb.Hi.Y = bounds.Hi.Y;
      if (Nodes[ix].Child110 != 0 && tester.Overlaps(ref aabb, expand))
      {
        TraverseNode(Nodes[ix].Child110, ref aabb, tester);
      }
      aabb.Lo.Z = c.Z;
      aabb.Hi.Z = bounds.Hi.Z;
      if (Nodes[ix].Child111 != 0 && tester.Overlaps(ref aabb, expand))
      {
        TraverseNode(Nodes[ix].Child111, ref aabb, tester);
      }
    }

#if VALIDATE
    void GetNodeTris(int ix, ref AABB aabb, ITester tester)
#else
    void GetNodeTris(int ix, ITester tester)
#endif
    {
      for (int i = Nodes[ix].TriStart, n = Nodes[ix].TriEnd; i != n; ++i)
      {
#if DEBUG
        trisTested.Add(i);
#endif
        if (tester.Intersects(ref Triangles[i], this))
        {
          ReturnTriangles.Add(i);
        }
#if VALIDATE
        Triangle t = Triangles[i];
        Vector3 c = (Vertices[t.VertexA] + Vertices[t.VertexB] + Vertices[t.VertexC]) / 3;
        Debug.Assert(aabb.Contains(c));
#endif
      }
    }
  }

  public struct TreeNode
  {
    public float Expansion;       //  loose octree style, but based off AABB
    public int TriStart;
    public int TriEnd;
    public int Child000;
    public int Child001;
    public int Child010;
    public int Child011;
    public int Child100;
    public int Child101;
    public int Child110;
    public int Child111;
  }

  public struct Triangle
  {
    public int VertexA;
    public int VertexB;
    public int VertexC;
    public float Distance;
    public Vector3 Normal;
    public Vector3 U;
    public Vector3 V;
    public float uu;
    public float vv;
    public float uv;
    public float di;

    static float CollMin = -0.01f;
    static float CollMax = 1.01f;
    public static float CollDelta
    {
      get { return -CollMin; }
      set { CollMin = -value; CollMax = 1.0f + value; }
    }

    public override string ToString()
    {
      return String.Format("Triangle({0},{1},{2}) N={3} D={4} U={5} V={6}",
        VertexA, VertexB, VertexC, Normal, Distance, U, V);
    }

    public bool Intersects(CollisionContent cc, ref Ray collRay, ref float dd)
    {
      Vector3 P = cc.Vertices[VertexA];
      Vector3 w0 = collRay.Position - P;  //  ray position in triangle space
      float b = Vector3.Dot(Normal, collRay.Direction);
      if (-b < 1e-10)      //  ray is in plane, or pointing at backside of tri
        return false;
      float a = Vector3.Dot(Normal, w0);
      if (a < 0)
        return false;     //  ray starts below triangle
      float r = -a / b;
      if (r > dd)
        return false;     //  triangle too far away
      Vector3 I = collRay.Position + collRay.Direction * r;
      Vector3 W = I - P;
      float uw = Vector3.Dot(U, W);
      float vw = Vector3.Dot(V, W);
      float s = (uv * vw - vv * uw) * di;
      if (s < CollMin || s > CollMax) //  outside in "s" space
        return false;
      float t = (uv * uw - uu * vw) * di;
      if (t < CollMin || (s + t) > CollMax) //  outside in "s-t" space
        return false;
      //  found an intersection
      dd = r;
      return true;
    }

    public void CalcColl(CollisionContent cc)
    {
      U = cc.Vertices[VertexB] - cc.Vertices[VertexA];
      V = cc.Vertices[VertexC] - cc.Vertices[VertexA];
      uu = U.LengthSquared();
      vv = V.LengthSquared();
      uv = Vector3.Dot(U, V);
      float d = uv * uv - uu * vv;
      if (Math.Abs(d) < 1e-10f)
      {
        throw new InvalidOperationException(String.Format(
            "Degenerate triangle {1} in mesh. Verts: {2} {3} {4}", this,
            cc.Vertices[VertexA], cc.Vertices[VertexB], cc.Vertices[VertexC]));
      }
      di = 1.0f / d;
    }

    static int[] vertLower = new int[8];
    static int[] vertHigher = new int[8];
    static Vector3[] hull = new Vector3[6];

    public bool Intersects(CollisionContent cc, ref AABB box)
    {
      //  transform to box-relative coordinates
      //  triangle culled by box major axes?
      Vector3 bc = box.Center;
      Vector3 va = cc.Vertices[VertexA] - bc;
      Vector3 vb = cc.Vertices[VertexB] - bc;
      Vector3 vc = cc.Vertices[VertexC] - bc;
      return AABBIntersects(ref va, ref vb, ref vc, ref box);
    }

    bool AABBIntersects(ref Vector3 va, ref Vector3 vb, ref Vector3 vc, ref AABB box)
    {
      Vector3 hd = box.HalfDim;
      if (va.X >= hd.X && vb.X >= hd.X && vc.X >= hd.X)
        return false;
      if (-va.X >= hd.X && -vb.X >= hd.X && -vc.X >= hd.X)
        return false;
      if (va.Y >= hd.Y && vb.Y >= hd.Y && vc.Y >= hd.Y)
        return false;
      if (-va.Y >= hd.Y && -vb.Y >= hd.Y && -vc.Y >= hd.Y)
        return false;
      if (va.Z >= hd.Z && vb.Z >= hd.Z && vc.Z >= hd.Z)
        return false;
      if (-va.Z >= hd.Z && -vb.Z >= hd.Z && -vc.Z >= hd.Z)
        return false;

      //  box culled by triangle?
      float d = Vector3.Dot(Normal, box.Vertex(0));
      bool culled = true;
      if (d < Distance)
      {
        for (int i = 1; i < 8; ++i)
        {
          if (Vector3.Dot(Normal, box.Vertex(i)) >= Distance)
          {
            culled = false;
            break;
          }
        }
      }
      else
      {
        for (int i = 1; i < 8; ++i)
        {
          if (Vector3.Dot(Normal, box.Vertex(i)) < Distance)
          {
            culled = false;
            break;
          }
        }
      }
      if (culled)
        return false;

      //  triangle culled by cross product between triangle edge and box edge?
      //  (separating axis theorem)
      if (AxisSeparates(ref va, ref vb, ref vc, Vector3.UnitX, ref hd))
        return false;
      if (AxisSeparates(ref vb, ref vc, ref va, Vector3.UnitX, ref hd))
        return false;
      if (AxisSeparates(ref vc, ref va, ref vb, Vector3.UnitX, ref hd))
        return false;
      if (AxisSeparates(ref va, ref vb, ref vc, Vector3.UnitY, ref hd))
        return false;
      if (AxisSeparates(ref vb, ref vc, ref va, Vector3.UnitY, ref hd))
        return false;
      if (AxisSeparates(ref vc, ref va, ref vb, Vector3.UnitY, ref hd))
        return false;
      if (AxisSeparates(ref va, ref vb, ref vc, Vector3.UnitZ, ref hd))
        return false;
      if (AxisSeparates(ref vb, ref vc, ref va, Vector3.UnitZ, ref hd))
        return false;
      if (AxisSeparates(ref vc, ref va, ref vb, Vector3.UnitZ, ref hd))
        return false;

      //  ok, so they intersect
      return true;
    }

    static bool AxisSeparates(ref Vector3 va, ref Vector3 vb, ref Vector3 vc, Vector3 axis, ref Vector3 halfDim)
    {
      Vector3 edge = vb - va;
      Vector3 separating;
      Vector3.Cross(ref axis, ref edge, out separating);
      if (separating.X == 0 && separating.Y == 0 && separating.Z == 0)
        return false;
      //  calculate the time of intersection that the triangle spans
      float ta, tb, tc;
      Vector3.Dot(ref separating, ref va, out ta);
      Vector3.Dot(ref separating, ref vb, out tb);
      Vector3.Dot(ref separating, ref vc, out tc);
      //  sort so ta is min and tc is max
      if (ta > tb)
      {
        float f = tb;
        tb = ta;
        ta = f;
      }
      if (ta > tc)
      {
        float f = tc;
        tc = tb;
        tb = ta;
        ta = f;
      }
      Debug.Assert(ta <= tb);
      Debug.Assert(tb <= tc);
      //  calculate the closest point (hence, abs())
      float x = Math.Abs(halfDim.X * separating.X) + Math.Abs(halfDim.Y * separating.Y) + Math.Abs(halfDim.Z * separating.Z);
      //  If the first time of intersection is after the ebd of the extent, 
      //  or the last time of intersection is before the beginning of the extent,
      //  then they are separated.
      if (ta >= x || tc <= -x)
        return true;  //  separated!
      return false; //  not separated
    }

    internal bool Intersects(CollisionContent cc, ref BoundingSphere sphere)
    {
      //  Test triangle plane against sphere
      float sd;
      Vector3.Dot(ref sphere.Center, ref Normal, out sd);
      if (sd < Distance - sphere.Radius || sd > Distance + sphere.Radius)
        return false;
      Vector3 pc;
      Vector3.Multiply(ref Normal, Distance - sd, out pc);
      Vector3.Add(ref pc, ref sphere.Center, out pc);
      //  Find the closest point on each edge of the triangle
      //  Set up the triangle vertices
      Vector3 a = cc.Vertices[this.VertexA];
      Vector3 b = cc.Vertices[this.VertexB];
      Vector3 c = cc.Vertices[this.VertexC];
      //  Calculate the edges (non-normalized)
      Vector3 ba, cb, ac;
      Vector3.Subtract(ref b, ref a, out ba);
      Vector3.Subtract(ref c, ref b, out cb);
      Vector3.Subtract(ref a, ref c, out ac);
      //  Find the square of the length of the edges
      float lba, lcb, lac;
      Vector3.Dot(ref ba, ref ba, out lba);
      Vector3.Dot(ref cb, ref cb, out lcb);
      Vector3.Dot(ref ac, ref ac, out lac);
      //  Calculate vertex-relative position of sphere center
      Vector3 pca, pcb, pcc;
      Vector3.Subtract(ref pc, ref a, out pca);
      Vector3.Subtract(ref pc, ref b, out pcb);
      Vector3.Subtract(ref pc, ref c, out pcc);
      //  Caluclate length-scaled distance along each edge
      float dab, dbc, dca;
      Vector3.Dot(ref ba, ref pca, out dab);
      Vector3.Dot(ref cb, ref pcb, out dbc);
      Vector3.Dot(ref ac, ref pcc, out dca);
      //  calculate 0..1 barycentric coordinate for each edge
      float vba = dab / lba;
      float vcb = dbc / lcb;
      float vac = dca / lac;
      //  Test barycentric coordinates: if (s >= 0) and (t >= 0) and (s + t <= 1) we're inside.
      //  Because I've wound each edge, I have to negate one of them (and I only need 2)
      if (vba >= 0 && (1 - vac) >= 0 && vba + (1 - vac) <= 1)
      {
        return true;
      }
      float r2 = sphere.Radius * sphere.Radius;
      float d;

      //  find actual points, clipped to triangle, and test distance to sphere center

      //  B-A
      if (vba < 0)
        ba = a;
      else if (vba > 1)
        ba = b;
      else
      {
        Vector3.Multiply(ref ba, vba, out ba);
        Vector3.Add(ref ba, ref a, out pca);
      }
      Vector3.Subtract(ref pca, ref sphere.Center, out pca);
      Vector3.Dot(ref pca, ref pca, out d);
      if (d <= r2)
      {
        return true;
      }

      //  C-B
      if (vcb < 0)
        cb = b;
      else if (vcb > 1)
        cb = c;
      else
      {
        Vector3.Multiply(ref cb, vcb, out cb);
        Vector3.Add(ref cb, ref b, out pcb);
      }
      Vector3.Subtract(ref pcb, ref sphere.Center, out pcb);
      Vector3.Dot(ref pcb, ref pcb, out d);
      if (d <= r2)
      {
        return true;
      }

      //  A-C
      if (vac < 0)
        ac = c;
      else if (vac > 1)
        ac = a;
      else
      {
        Vector3.Multiply(ref ac, vac, out ac);
        Vector3.Add(ref ac, ref c, out pcc);
      }
      Vector3.Subtract(ref pcc, ref sphere.Center, out pcc);
      Vector3.Dot(ref pcc, ref pcc, out d);
      if (d <= r2)
      {
        return true;
      }
      //  nothing fit
      return false;
    }

    static AABB tempAABB = new AABB();
    internal bool Intersects(CollisionContent cc, ref OBB box)
    {
      Vector3 bc = box.Pos;
      Vector3 va = cc.Vertices[VertexA] - bc;
      Vector3 vb = cc.Vertices[VertexB] - bc;
      Vector3 vc = cc.Vertices[VertexC] - bc;
      Vector3.TransformNormal(ref va, ref box.InvOriMatrix, out va);
      Vector3.TransformNormal(ref vb, ref box.InvOriMatrix, out vb);
      Vector3.TransformNormal(ref vc, ref box.InvOriMatrix, out vc);
      tempAABB.Set(-box.HalfDim, box.HalfDim);
      return AABBIntersects(ref va, ref vb, ref vc, ref tempAABB);
    }
  }
}

