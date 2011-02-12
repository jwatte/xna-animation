using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using KiloWatt.Animation.Graphics;

namespace KiloWatt.Animation.Animation
{
  public interface IAnimationProvider
  {
    Animation GetAnimation(string name);
    IAnimationInstance CurrentAnimation { get; set; }
    Model TargetModel { get; }
  }
  
  //  Create a ModelDraw instance to make animating and rendering a model simpler.
  //  The ModelDraw represents a given instance of a given model -- you can use 
  //  multiple ModelDraw instances targeted at the same Model to draw multiple 
  //  instances in different positions.
  public class ModelDraw : ISceneRenderable, IDisposable, IAnimationProvider
  {
    public ModelDraw(Model m, string name)
    {
      name_ = name;
      model_ = m;
      world_ = Matrix.Identity;

      //  I'll need the world-space pose of each bone
      matrices_ = new Matrix[m.Bones.Count];

      //  inverse bind pose for skinning pose only
      object ibp;
      Dictionary<string, object> tagDict = m.Tag as Dictionary<string, object>;
      if (tagDict == null)
        throw new System.ArgumentException(String.Format(
            "Model {0} wasn't processed with the AnimationProcessor.", name));
      if (tagDict.TryGetValue("InverseBindPose", out ibp))
      {
        inverseBindPose_ = ibp as SkinnedBone[];
        CalculateIndices();
      }

      //  information about bounds, in case the bind pose contains scaling
      object bi;
      if (((Dictionary<string, object>)m.Tag).TryGetValue("BoundsInfo", out bi))
        boundsInfo_ = bi as BoundsInfo;
      if (boundsInfo_ == null)
        boundsInfo_ = new BoundsInfo(1, 0);

      //  pick apart the model, so I know how to draw the different pieces
      List<Chunk> chl = new List<Chunk>();
      foreach (ModelMesh mm in m.Meshes)
      {
        int mmpIx = 0;
        foreach (ModelMeshPart mmp in mm.MeshParts)
        {
          ++mmpIx;
          //  chunk is used to draw an individual subset
          Chunk ch = new Chunk();

          //  set up all the well-known parameters through the EffectConfig helper.
          ch.Fx = new EffectConfig(mmp.Effect, mm.Name + "_" + mmpIx.ToString());

          //  if this effect is skinned, set up additional data
          if (ch.Fx.HasPose)
          {
            //  If I haven't built the pose, then build it now
            if (pose_ == null)
            {
              if (inverseBindPose_ == null)
                throw new System.ArgumentNullException(String.Format(
                    "The model {0} should have an inverse bone transform because it has a pose, but it doesn't.",
                    name));
              //  Send bones as sets of three 4-vectors (column major) to the shader
              pose_ = new Vector4[inverseBindPose_.Length * 3];
              for (int i = 0; i != inverseBindPose_.Length; ++i)
              {
                //  start out with the identity pose (which is terrible)
                pose_[i * 3 + 0] = new Vector4(1, 0, 0, 0);
                pose_[i * 3 + 1] = new Vector4(0, 1, 0, 0);
                pose_[i * 3 + 2] = new Vector4(0, 0, 1, 0);
              }
            }
            ch.Fx.PoseData = pose_;
          }

          ch.Mesh = mm;
          ch.Part = mmp;

          //  check out whether the technique contains transparency
          EffectAnnotation ea = mmp.Effect.CurrentTechnique.Annotations["transparent"];
          if (ea != null && ea.GetValueBoolean() == true)
            ch.Deferred = true;

          chl.Add(ch);
        }
      }
      //  use a native array instead of a List<> for permanent storage
      chunks_ = chl.ToArray();
      //  calculate bounds information (won't take animation into account)
      CalcBoundingSphere();

      //  animate this instance based on the bind pose
      Animation an = GetAnimation("$bind$", false);
      if (an != null)
      {
        instance_ = new AnimationInstance(an);
        instance_.Advance(0);
      }
    }

    ISource<Matrix> poseSource_;
    public ISource<Matrix> PoseSource
    {
      get { return poseSource_; }
      set
      {
        poseSource_ = value;
        if (poseSource_ != null)
        {
          poseSource_.Get(out world_);
        }
      }
    }

    public void Dispose()
    {
      if (scene_ != null)
      {
        scene_.RemoveRenderable(this);
        scene_ = null;
      }
    }
    
    public void Attach(IScene scene)
    {
      scene_ = scene;
      foreach (Chunk ch in chunks_)
      {
        ch.Bitmask = 0;
        foreach (EffectTechnique et in ch.Fx.FX.Techniques)
        {
          ch.Bitmask |= (1U << scene.TechniqueIndex(et.Name));
        }
      }
    }

    public void Detach()
    {
      scene_ = null;
    }

    //  verify that the bone indices are what I expect them to be (debugging help)
    void CalculateIndices()
    {
#if DEBUG
      string findBone = "";
      try
      {
        for (int i = 0, n = inverseBindPose_.Length; i != n; ++i)
        {
          findBone = inverseBindPose_[i].Name;
          int index = model_.Bones[findBone].Index;
          if (index != inverseBindPose_[i].Index)
          {
            throw new System.ArgumentException(String.Format(
                "Bone {0} was re-indexed during import! from {1} to {2}.",
                findBone, inverseBindPose_[i].Index, index));
          }
          for (int j = 0, m = model_.Bones.Count; j != m; ++j)
          {
            if (j != inverseBindPose_[i].Index && model_.Bones[j].Name == findBone)
            {
              throw new System.ArgumentException(String.Format(
                  "Duplicate bone name found: {0}",
                  inverseBindPose_[i].Name));
            }
          }
        }
      }
      catch (System.Exception x)
      {
        throw new System.ArgumentException(
            String.Format("The required bone '{0}' could not be found.", findBone),
            x);
      }
#endif
    }

    public Animation GetAnimation(string name)
    {
      return GetAnimation(name, true);
    }
    
    internal Animation GetAnimation(string name, bool throwIfNotFound)
    {
      Dictionary<string, object> dict = model_.Tag as Dictionary<string, object>;
      if (dict == null)
        throw new ArgumentException(String.Format("Asking for animation {0} in model {1} with no animations.", name, name_));
      object aso;
      AnimationSet aset;
      if (!dict.TryGetValue("AnimationSet", out aso) || ((aset = aso as AnimationSet) == null))
        if (throwIfNotFound)
          throw new ArgumentException(String.Format("Asking for animation {0} in model {1} with no animations.", name, name_));
        else
          return null;
      Animation an = aset.AnimationByName(name);
      if (an == null && throwIfNotFound)
        throw new ArgumentException(String.Format("Asking for animation {0} in model {1} where it is missing.", name, name_));
      return an;
    }
    
    public override string ToString() { return String.Format("ModelDraw: Name = {0}", name_); }

    IScene scene_;
    BoundsInfo boundsInfo_;
    string name_;
    //  the name is used for debugging purposes
    public string Name { get { return name_; } }
    Model model_;
    //  get the original Model instance back
    public Model Model { get { return model_; } }
    public Model TargetModel { get { return model_; } }
    IAnimationInstance instance_;
    //  the playing animation
    public IAnimationInstance CurrentAnimation { get { return instance_; } set { instance_ = value; } }
    Matrix world_;
    //  the scene renderable transform
    public Matrix Transform { get { return world_; } set { world_ = value; } }
    Matrix[] matrices_;
    SkinnedBone[] inverseBindPose_;
    Vector4[] pose_;
    BoundingSphere boundingSphere_;
    //  the bounds for culling
    public BoundingSphere Bounds { get { return boundingSphere_; } }
    Chunk[] chunks_;

    //  information needed to draw each piece, even if it's deferred for transparency
    class Chunk
    {
      public EffectConfig Fx;
      public ModelMesh Mesh;
      public ModelMeshPart Part;
      public bool Deferred;
      public uint Bitmask;
    }

    //  calculate the bounding sphere
    internal BoundingSphere CalcBoundingSphere()
    {
      Vector3 a = Vector3.Zero;
      Vector3 b = Vector3.Zero;
      Vector3 center = Vector3.Zero;
      float longest = 0;
      //  use the bind pose that comes in at the start
      model_.CopyAbsoluteBoneTransformsTo(matrices_);
      foreach (ModelMesh m1 in model_.Meshes)
      {
        Vector3 c1 = Vector3.Transform(m1.BoundingSphere.Center, matrices_[m1.ParentBone.Index]);
        foreach (ModelMesh m2 in model_.Meshes)
        {
          //  calculate the pose of each bone, and the geometry that goes to that bone
          Vector3 c2 = Vector3.Transform(m2.BoundingSphere.Center, matrices_[m2.ParentBone.Index]);
          float l = (c2 - c1).Length();
          float d = m1.BoundingSphere.Radius + m2.BoundingSphere.Radius + l;
          if (d > longest)
          {
            //  merge the two bounding spheres
            longest = d;
            Vector3 q = (l < 1e-6) ? Vector3.Zero : (c2 - c1) * (1.0f / l);
            center = (c1 + c2) * 0.5f + q * ((m2.BoundingSphere.Radius - m1.BoundingSphere.Radius) * 0.5f);
          }
        }
      }
      //  using the bounding box calculated, return a new bounding sphere
      boundingSphere_ = new BoundingSphere(center, longest * 0.5f * boundsInfo_.MaxScale + boundsInfo_.MaxOffset);
      return boundingSphere_;
    }

    public void ScenePrepare(DrawDetails dd)
    {
      if (PoseSource != null)
        PoseSource.Get(out world_);
      //  if animating, then pose it
      if (instance_ != null)
      {
        Matrix temp;
        Keyframe[] kfs = instance_.CurrentPose;
        unchecked
        {
          int i = 0, n = matrices_.Length;
          foreach (Keyframe kf in kfs)
          {
            if (i == n)
              break;
            if (kf != null)
            {
              kf.ToMatrix(out temp);
              //  set up the model in parent-relative pose
              model_.Bones[i].Transform = temp;
            }
            ++i;
          }
        }
      }
      else
      {
      }
      //  get the object-relative matrices (object->world is separate)
      model_.CopyAbsoluteBoneTransformsTo(matrices_);
#if DRAW_SKELETON
      //  draw the skeleton, but only in debug mode
      foreach (ModelBone mb in model_.Bones)
      {
        if (mb.Parent != null)
        {
          Matrix m = matrices_[mb.Index];
          Vector3 c = m.Translation;
          DebugLines.Global.AddLine(
              c,
              matrices_[mb.Parent.Index].Translation,
              Color.White);
          DebugLines.Global.AddLine(
              c,
              c + m.Right * 0.5f,
              Color.Red);
          DebugLines.Global.AddLine(
              c,
              c + m.Up * 0.5f,
              Color.Green);
          DebugLines.Global.AddLine(
              c,
              c + m.Backward * 0.5f,
              Color.Blue);
        }
      }
#endif
      //  If I have a 3x4 matrix pose, then generate that for skinning
      if (pose_ != null)
        GeneratePose();
    }

    //  Only call Draw() once per object instance per frame. Else 
    //  transparently sorted pieces won't draw correctly, as there is 
    //  only one set of state per ModelDraw. Use multiple ModelDraw
    //  instances for multiple object instances.
    //  Immediately draw the parts that do not require transparency.
    //  put parts that need transparency on a deferred list, to be 
    //  drawn later (z sorted) using DrawDeferred().
    public bool SceneDraw(DrawDetails dd, int pass)
    {
      //  chain to an internal helper
      return Draw(dd, false, (1U << pass));
    }

    void GeneratePose()
    {
      unchecked
      {
        Matrix mat;
        for (int i = 0, n = inverseBindPose_.Length, j = 0; i != n; ++i)
        {
          //  todo: I really could hoist this to the animation keyframes themselves,
          //  and pre-transform the geometry by the bone matrices
          SkinnedBone sb = inverseBindPose_[i];
          Matrix.Multiply(ref sb.InverseBindTransform, ref matrices_[sb.Index], out mat);
          pose_[j++] = new Vector4(mat.M11, mat.M21, mat.M31, mat.M41);
          pose_[j++] = new Vector4(mat.M12, mat.M22, mat.M32, mat.M42);
          pose_[j++] = new Vector4(mat.M13, mat.M23, mat.M33, mat.M43);
        }
      }
    }

    //  callback for transparent drawing
    public void SceneDrawTransparent(DrawDetails dd, int technique)
    {
      Draw(dd, true, (1U << technique));
    }

    //  Draw helper that actually issues geometry, or defers for later.
    internal bool Draw(DrawDetails dd, bool asDeferred, uint mask)
    {
      //  keep a copy of world, because I override it in the DrawDetails
      bool added = false;
      //  each chunk is drawn separately, as it represents a different 
      //  shader set-up (world transform, texture, etc).
      foreach (Chunk ch in chunks_)
      {
        if (((ch.Bitmask & mask) == 0) && (mask != 0))
          continue;
        if (asDeferred)
        {
          //  If I'm called back to draw deferred pieces, don't draw if this 
          //  piece is not deferred.
          if (!ch.Deferred)
            continue;
        }
        else if (ch.Deferred)
        {
          added = true;
          continue;
        }
        DrawChunk(dd, ch);
      }
      return added;
    }

    //  do the device and effect magic to render a given chunk of geometry
    private void DrawChunk(DrawDetails dd, Chunk ch)
    {
      //  configure the device and actually draw
      dd.dev.Indices = ch.Part.IndexBuffer;
      dd.dev.SetVertexBuffer(ch.Part.VertexBuffer, ch.Part.VertexOffset);
      //  note: calculating the world matrix overrides the previous value, hence the use
      //  of the saved copy of the world transform
      Matrix.Multiply(ref matrices_[ch.Mesh.ParentBone.Index], ref world_, out dd.world);
      ch.Fx.Setup(dd);
      EffectTechnique et = ch.Fx.FX.CurrentTechnique;
      //  most my effects are single-pass, but at least transparency is multi-pass
      for (int i = 0, n = et.Passes.Count; i != n; ++i)
      {
        EffectPass ep = et.Passes[i];
        ep.Apply();
        dd.dev.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0,
            0, ch.Part.NumVertices, ch.Part.StartIndex, ch.Part.PrimitiveCount);
      }
    }
  }
}
