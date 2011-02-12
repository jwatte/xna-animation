using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;

// TODO: replace these with the processor input and output types.
using TInput = Microsoft.Xna.Framework.Content.Pipeline.Graphics.NodeContent;
using TOutput = Microsoft.Xna.Framework.Content.Pipeline.Processors.ModelContent;
using KiloWatt.Animation.Physics;
using System.Diagnostics;
using System.ComponentModel;

namespace KiloWatt.Pipeline
{
  /// <summary>
  /// This class will be instantiated by the XNA Framework Content Pipeline
  /// to apply custom processing to content data, converting an object of
  /// type TInput to TOutput. The input and output types may be the same if
  /// the processor wishes to alter data without changing its type.
  ///
  /// This should be part of a Content Pipeline Extension Library project.
  ///
  /// TODO: change the ContentProcessor attribute to specify the correct
  /// display name for this processor.
  /// </summary>
  [ContentProcessor(DisplayName = "Collision Dxt5 Model")]
  public class CollisionDxt5Model : LevelProcessor.Dxt5ModelProcessor
  {
    public CollisionDxt5Model()
    {
      ExpansionFactor = 0.25f;
    }

    public override TOutput Process(TInput input, ContentProcessorContext context)
    {
      this.context = context;
      CollisionContent cc = ProcessCollision(input);
      ModelContent mc = base.Process(input, context);
      ((Dictionary<string, object>)mc.Tag).Add("Collision", cc);
      return mc;
    }
    
    ContentProcessorContext context;
    int negYCount;


    [Description("How much to inflate the bounding boxes in each direction (times diagonal).")]
    [DefaultValue(0.375f)]
    public float ExpansionFactor { get; set; }

    public virtual CollisionContent ProcessCollision(NodeContent nc)
    {
      Gather g = new Gather();
      GatherCollision(nc, Matrix.Identity, g);
      if (negYCount > g.Triangles.Count / 2)
      {
        context.Logger.LogImportantMessage("{0} of {1} triangles have down-facing normals",
          negYCount, g.Triangles.Count);
      }
      return MakeCollisionContent(g);
    }
    
    public virtual void GatherCollision(NodeContent nc, Matrix x, Gather g)
    {
      negYCount = 0;
      MeshContent mc = nc as MeshContent;
      if (mc != null)
      {
        AppendGeometry(mc, x, g);
      }
      foreach (NodeContent cld in nc.Children)
      {
        GatherCollision(cld, x * cld.Transform, g);
      }
    }
    
    void AppendGeometry(MeshContent mc, Matrix x, Gather g)
    {
      int ta = 2;
      int tb = 1;
      if (SwapWindingOrder)
      {
        ta = 1;
        tb = 2;
      }
      int vBase = g.Vertices.Count;
      bool first = (g.Bounds.Lo == Vector3.Zero && g.Bounds.Hi == Vector3.Zero);
      foreach (Vector3 v in mc.Positions)
      {
        Vector3 vt = Vector3.Transform(v, x);
        g.Vertices.Add(vt);
        if (first)
        {
          first = false;
          g.Bounds.Set(vt, vt);
        }
        g.Bounds.Include(vt);
      }
      foreach (GeometryContent gc in mc.Geometry)
      {
        for (int i = 0, n = gc.Indices.Count - 2; i < n; i += 3)
        {
          Triangle t = new Triangle();
          t.VertexA = gc.Indices[i] + vBase;
          t.VertexB = gc.Indices[i + ta] + vBase;
          t.VertexC = gc.Indices[i + tb] + vBase;
          t.Normal = Vector3.Cross(g.Vertices[t.VertexB] - g.Vertices[t.VertexA], g.Vertices[t.VertexC] - g.Vertices[t.VertexB]);
          if (t.Normal.Y < 0)
          {
            ++negYCount;
          }
          if (t.Normal.LengthSquared() > 1e-10f)
          {
            t.Normal.Normalize();
          }
          else
          {
            context.Logger.LogImportantMessage("Normal is surprisingly short: {0} for tri {1}",
              t.Normal, i / 3);
            //  set some normal; I don't really care
            if (t.Normal.X > 0)
              t.Normal = Vector3.Right;
            else if (t.Normal.X < 0)
              t.Normal = Vector3.Left;
            else if (t.Normal.Z > 0)
              t.Normal = Vector3.Backward;
            else if (t.Normal.Z < 0)
              t.Normal = Vector3.Forward;
            else if (t.Normal.Y < 0)
              t.Normal = Vector3.Down;
            else
              t.Normal = Vector3.Up;
          }
          //  Assume the center of the bounding sphere is the center of the longest edge.
          //  This is true for any triangle that is right-angled or blunt. The center will 
          float la = (g.Vertices[t.VertexB] - g.Vertices[t.VertexA]).Length();
          float lb = (g.Vertices[t.VertexC] - g.Vertices[t.VertexB]).Length();
          float lc = (g.Vertices[t.VertexA] - g.Vertices[t.VertexC]).Length();
          Vector3 c;
          if (la > lb)
            if (la > lc)
              c = (g.Vertices[t.VertexB] + g.Vertices[t.VertexA]) * 0.5f;
            else
              c = (g.Vertices[t.VertexA] + g.Vertices[t.VertexC]) * 0.5f;
          else
            if (lb > lc)
              c = (g.Vertices[t.VertexC] + g.Vertices[t.VertexB]) * 0.5f;
            else
              c = (g.Vertices[t.VertexA] + g.Vertices[t.VertexC]) * 0.5f;
          t.Distance = Vector3.Dot(t.Normal, c);
          g.Triangles.Add(t);
          float r = (g.Vertices[t.VertexA] - c).Length();
          r = Math.Max(r, (g.Vertices[t.VertexB] - c).Length());
          r = Math.Max(r, (g.Vertices[t.VertexC] - c).Length());
          BoundingSphere bs = new BoundingSphere(c, r);
          //  The center is somewhere inside for a triangle that is sharp.
          //  Calculate an alternative center to get a better bounding sphere.
          if (la < lb)
            if (la < lc)
              c = (g.Vertices[t.VertexC] * 2 + g.Vertices[t.VertexB] + g.Vertices[t.VertexA]) * 0.25f;
            else
              c = (g.Vertices[t.VertexB] * 2 + g.Vertices[t.VertexC] + g.Vertices[t.VertexA]) * 0.25f;
          else
            if (lb < lc)
              c = (g.Vertices[t.VertexA] * 2 + g.Vertices[t.VertexB] + g.Vertices[t.VertexC]) * 0.25f;
            else
              c = (g.Vertices[t.VertexB] * 2 + g.Vertices[t.VertexC] + g.Vertices[t.VertexA]) * 0.25f;
          r = (g.Vertices[t.VertexA] - c).Length();
          r = Math.Max(r, (g.Vertices[t.VertexB] - c).Length());
          r = Math.Max(r, (g.Vertices[t.VertexC] - c).Length());
          if (r < bs.Radius)
          {
            bs.Center = c;
            bs.Radius = r;
          }
          g.BoundingSpheres.Add(bs);
        }
      }
    }
    
    public class Gather
    {
      public List<Triangle> Triangles = new List<Triangle>();
      public List<Vector3> Vertices = new List<Vector3>();
      public List<BoundingSphere> BoundingSpheres = new List<BoundingSphere>();
      public AABB Bounds = new AABB();
    }
    
    public virtual CollisionContent MakeCollisionContent(Gather g)
    {
      //  todo: I really should find the major modes, and align the AABB to 
      //  those by rotating. Then counter-rotate when collision testing. 
      //  Oh, well.
      CollisionContent cc = new CollisionContent();
      cc.Bounds = g.Bounds;
      Vector3 hd = cc.Bounds.HalfDim;
      Vector3 hc = cc.Bounds.Center;
      hd.X = Math.Max(hd.X, Math.Max(hd.Y, hd.Z));
      hd.Y = hd.X;
      hd.Z = hd.X;
      cc.Bounds.Lo = hc - hd;
      cc.Bounds.Hi = hc + hd;
      cc.Triangles = g.Triangles.ToArray();
      cc.Vertices = g.Vertices.ToArray();
      List<TreeNode> TreeNodes = new List<TreeNode>();
      int[] ixs = new int[cc.Triangles.Length];
      for (int i = 0; i < ixs.Length; ++i)
        ixs[i] = i;
      BuildNodes(cc, ixs, 0, ixs.Length, TreeNodes, cc.Bounds, 
        g.BoundingSpheres.ToArray());
      cc.Nodes = TreeNodes.ToArray();
      for (int i = 0; i < ixs.Length; ++i)
      {
        cc.Triangles[i] = g.Triangles[ixs[i]];
        cc.Triangles[i].CalcColl(cc);
      }
      context.Logger.LogImportantMessage("Built CollisionContent with {0} triangles, {1} vertices, {2} cells",
        cc.Triangles.Length, cc.Vertices.Length, cc.Nodes.Length);
      return cc;
    }
    
    void BuildNodes(CollisionContent cc, int[] tris, int lo, int hi, List<TreeNode> nodes, AABB bounds, 
      BoundingSphere[] tbs)
    {
      TreeNode tn = new TreeNode();
      tn.TriStart = lo;
      tn.Expansion = (bounds.Hi - bounds.Lo).Length() * ExpansionFactor;
      GatherLargeTriangles(ref tn, cc, tris, ref lo, hi, tn.Expansion, tbs);
      tn.TriEnd = lo;
      int ix = nodes.Count;
      nodes.Add(tn);
      Vector3 lb = bounds.Lo;
      Vector3 ub = bounds.Hi;
      Vector3 cb = lb + (ub - lb) * 0.5f;
      if (hi > lo + 4)
      {
        int midX = lo;
        PartitionSmallerTriangles(cc, tris, lo, ref midX, hi, tbs, Vector3.Right, cb.X);
        int midXlo = lo;
        PartitionSmallerTriangles(cc, tris, lo, ref midXlo, midX, tbs, Vector3.Up, cb.Y);
        int midXhi = midX;
        PartitionSmallerTriangles(cc, tris, midX, ref midXhi, hi, tbs, Vector3.Up, cb.Y);
        int midXloYlo = lo;
        PartitionSmallerTriangles(cc, tris, lo, ref midXloYlo, midXlo, tbs, Vector3.Backward, cb.Z);
        int midXloYhi = midXlo;
        PartitionSmallerTriangles(cc, tris, midXlo, ref midXloYhi, midX, tbs, Vector3.Backward, cb.Z);
        int midXhiYlo = midX;
        PartitionSmallerTriangles(cc, tris, midX, ref midXhiYlo, midXhi, tbs, Vector3.Backward, cb.Z);
        int midXhiYhi = midXhi;
        PartitionSmallerTriangles(cc, tris, midXhi, ref midXhiYhi, hi, tbs, Vector3.Backward, cb.Z);
        if (lo < midXloYlo)
        {
          tn.Child000 = nodes.Count;
          BuildNodes(cc, tris, lo, midXloYlo, nodes, new AABB(lb, cb), 
            tbs);
        }
        if (midXloYlo < midXlo)
        {
          tn.Child001 = nodes.Count;
          BuildNodes(cc, tris, midXloYlo, midXlo, nodes, 
              new AABB(new Vector3(lb.X, lb.Y, cb.Z), new Vector3(cb.X, cb.Y, ub.Z)), 
              tbs);
        }
        if (midXlo < midXloYhi)
        {
          tn.Child010 = nodes.Count;
          BuildNodes(cc, tris, midXlo, midXloYhi, nodes,
              new AABB(new Vector3(lb.X, cb.Y, lb.Z), new Vector3(cb.X, ub.Y, cb.Z)), 
              tbs);
        }
        if (midXloYhi < midX)
        {
          tn.Child011 = nodes.Count;
          BuildNodes(cc, tris, midXloYhi, midX, nodes,
              new AABB(new Vector3(lb.X, cb.Y, cb.Z), new Vector3(cb.X, ub.Y, ub.Z)), 
              tbs);
        }
        if (midX < midXhiYlo)
        {
          tn.Child100 = nodes.Count;
          BuildNodes(cc, tris, midX, midXhiYlo, nodes,
              new AABB(new Vector3(cb.X, lb.Y, lb.Z), new Vector3(ub.X, cb.Y, cb.Z)), 
              tbs);
        }
        if (midXhiYlo < midXhi)
        {
          tn.Child101 = nodes.Count;
          BuildNodes(cc, tris, midXhiYlo, midXhi, nodes,
              new AABB(new Vector3(cb.X, lb.Y, cb.Z), new Vector3(ub.X, cb.Y, ub.Z)), 
              tbs);
        }
        if (midXhi < midXhiYhi)
        {
          tn.Child110 = nodes.Count;
          BuildNodes(cc, tris, midXhi, midXhiYhi, nodes,
              new AABB(new Vector3(cb.X, cb.Y, lb.Z), new Vector3(ub.X, ub.Y, cb.Z)), 
              tbs);
        }
        if (midXhiYhi < hi)
        {
          tn.Child111 = nodes.Count;
          BuildNodes(cc, tris, midXhiYhi, hi, nodes,
              new AABB(new Vector3(cb.X, cb.Y, cb.Z), new Vector3(ub.X, ub.Y, ub.Z)), 
              tbs);
        }
      }
      else
      {
        //  include them all!
        tn.TriEnd = hi;
      }
      nodes[ix] = tn;
    }

    void GatherLargeTriangles(ref TreeNode tn, CollisionContent cc, int[] tris, ref int lo, int hi, float r, BoundingSphere[] tbs)
    {
      for (int i = lo; i < hi; ++i)
      {
        if (tbs[tris[i]].Radius >= r)
        {
          int j = tris[i];
          tris[i] = tris[lo];
          tris[lo] = j;
          ++lo;
        }
      }
    }
    
    //  This partition algorithm is inspired by QuickSort; sorting on the 
    //  split axis of the node in question.
    private void PartitionSmallerTriangles(CollisionContent cc, int[] tris, int lo, ref int mid, int hi, BoundingSphere[] tbs, Vector3 d, float r)
    {
      int ilo = lo;
      int ihi = hi;
      while (lo < hi && lo < ihi && hi > ilo)
      {
        //  if "lo" and "hi - 1" alias, then one of the first ifs will succeed, and 
        //  I'll increment/decrement and break out of the loop!
        if (Vector3.Dot(tbs[tris[lo]].Center, d) < r)
        {
          ++lo;
        }
        else if (!(Vector3.Dot(tbs[tris[hi - 1]].Center, d) < r))
        {
          --hi;
        }
        else
        {
          //  I know that "lo" points at a larger tri, and "hi-1" points at a lower tri; so swap them
          int j = tris[lo];
          tris[lo] = tris[hi - 1];
          tris[hi - 1] = j;
          //  because of the failure of both of the first two tests, I know that 
          //  lo must be lower than hi-1.
          Debug.Assert(lo < hi - 1);
          ++lo;
          --hi;
        }
      }
      // "lo" points at the first "high" value; "hi" points at the last "lo" value
      Debug.Assert(lo == hi);
      Debug.Assert(lo == ihi || !(Vector3.Dot(tbs[tris[lo]].Center, d) < r));
      mid = lo;
    }

  }
}