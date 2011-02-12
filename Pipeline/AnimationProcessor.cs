using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;

using NodeContent = Microsoft.Xna.Framework.Content.Pipeline.Graphics.NodeContent;
using ModelContent = Microsoft.Xna.Framework.Content.Pipeline.Processors.ModelContent;

using KiloWatt.Animation.Animation;
using System.Diagnostics;

//  The problem is this:
//  The indices in the mesh are based on the flattened skeleton hierarchy.
//  This skeleton hierarchy excludes certain elements of the "bones" array
//  of the model.
//  Thus, a different index list is used for the skin palette, than for all 
//  the hierarchically animated data.

namespace AnimationProcessor
{
  /// <summary>
  /// This model processor includes all the fixes from the Dxt5ModelProcessor, as well 
  /// as adds support for baking animations into the Tag dictionary.
  /// </summary>
  [ContentProcessor(DisplayName = "Animation Processor - KiloWatt")]
  public class AnimationProcessor : LevelProcessor.Dxt5ModelProcessor
  {
    public override ModelContent Process(NodeContent input, ContentProcessorContext context)
    {
      CompileRegularExpressions();
      context.Logger.LogMessage("Output Platform: {0}", context.TargetPlatform);

      maxScale_ = 0;
      maxOffset_ = 0;
      BoneContent skeleton = MeshHelper.FindSkeleton(input);
      FlattenTransforms(input, skeleton, context);
      SkinnedBone[] inverseBindPose = GetInverseBindPose(input, context, skeleton);
      context.Logger.LogMessage("Found {0} skinned bones in skeleton.", (inverseBindPose == null) ? 0 : inverseBindPose.Length);

      ModelContent output = base.Process(input, context);
      if (output.Tag == null)
        output.Tag = new Dictionary<string, object>();

      if (FoundSkinning)
      {
#if DEBUG
        StringBuilder strb = new StringBuilder();
#endif
        if (inverseBindPose == null)
          throw new System.Exception("Could not find skeleton although there is skinned data.");
        for (int i = 0; i != inverseBindPose.Length; ++i)
        {
          SkinnedBone sb = inverseBindPose[i];
          int q = 0;
          sb.Index = -1;
          foreach (ModelBoneContent mbc in output.Bones)
          {
            if (mbc.Name == sb.Name)
            {
              sb.Index = mbc.Index;
              break;
            }
            ++q;
          }
          if (sb.Index == -1)
            throw new System.ArgumentException(
                String.Format("Can't find the index for animated bone named {0}.", sb.Name));
          inverseBindPose[i] = sb;
        }
        ((Dictionary<string, object>)output.Tag).Add("InverseBindPose", inverseBindPose);
      }

      ((Dictionary<string, object>)output.Tag).Add("AnimationSet", 
          BuildAnimationSet(input, ref output, context));

      ((Dictionary<string, object>)output.Tag).Add("BoundsInfo",
          new BoundsInfo(maxScale_, maxOffset_));

      return output;
    }
    
    float maxScale_;
    float maxOffset_;

    protected virtual void CompileRegularExpressions()
    {
      excludeAnimationsExpressions_ = MakeExclusion(excludeAnimations_);
      excludeBonesExpressions_ = MakeExclusion(excludeBones_);
      trimAnimationExpressions_  = MakeExclusion(trimAnimations_);
    }
    
    Regex[] MakeExclusion(string str)
    {
      if (String.IsNullOrEmpty(str))
        return null;
      string[] exprs = str.Split(';');
      Regex[] ret = new Regex[exprs.Length];
      for (int i = 0; i != exprs.Length; ++i)
      {
        ret[i] = new Regex(exprs[i], RegexOptions.IgnoreCase);
      }
      return ret;
    }

    protected virtual void FlattenTransforms(NodeContent node, NodeContent stopper, ContentProcessorContext context)
    {
      if (node == stopper)
      {
        BakeTransformsToTop(node.Parent);
        return;
      }
      MeshContent mc = node as MeshContent;
      if (mc != null)
      {
        foreach (GeometryContent gc in mc.Geometry)
        {
          if (VerticesAreSkinned(gc.Vertices, context))
          {
            BakeTransformsToTop(node);
            break;
          }
        }
      }
      foreach (NodeContent child in node.Children)
        FlattenTransforms(child, stopper, context);
    }
    
    //  If there are articulating bones above the skinned mesh or skeleton in the mesh, 
    //  those will be flattened. Put them further down in the hierarchy if you care.
    protected virtual void BakeTransformsToTop(NodeContent node)
    {
      while (node != null)
      {
        if (!node.Transform.Equals(Matrix.Identity))
        {
          MeshHelper.TransformScene(node, node.Transform);
          node.Transform = Matrix.Identity;
          //  because I baked this node, I can't animate it!
          node.Animations.Clear();
        }
        node = node.Parent;
      }
    }

    protected virtual SkinnedBone[] GetInverseBindPose(NodeContent input, ContentProcessorContext context, BoneContent skeleton)
    {
      if (skeleton == null)
        return null;
      IList<BoneContent> original = MeshHelper.FlattenSkeleton(skeleton);
      if (original.Count > maxNumBones_)
        throw new System.ArgumentException(String.Format(
            "The animation processor found {0} bones in the skeleton; a maximum of {1} is allowed.",
            original.Count, maxNumBones_));
      List<SkinnedBone> inversePose = new List<SkinnedBone>();
      foreach (BoneContent bc in original)
      {
        SkinnedBone sb = new SkinnedBone();
        sb.Name = bc.Name;
        if (sb.Name == null)
          throw new System.ArgumentNullException("Bone with null name found.");
        sb.InverseBindTransform = Matrix.Invert(GetAbsoluteTransform(bc, null));
        inversePose.Add(sb);
      }
      return inversePose.ToArray();
    }

    protected int nBonesGenerated_;
    protected Dictionary<ModelBoneContent, string> boneNames_ = new Dictionary<ModelBoneContent, string>();

    protected virtual string GetBoneName(ModelBoneContent mbc)
    {
      if (mbc.Name != null)
        return mbc.Name;
      string ret;
      if (!boneNames_.TryGetValue(mbc, out ret))
      {
        ret = String.Format("_GenBone{0}", ++nBonesGenerated_);
        boneNames_.Add(mbc, ret);
      }
      return ret;
    }

    /// <summary>
    /// The workhorse of the animation processor. It loops through all 
    /// animations, all tracks, and all keyframes, and converts to the format
    /// expected by the runtime animation classes.
    /// </summary>
    /// <param name="input">The NodeContent to process. Comes from the base ModelProcessor.</param>
    /// <param name="output">The ModelContent that was produced. You don't typically change this.</param>
    /// <param name="context">The build context (logger, etc).</param>
    /// <returns>An allocated AnimationSet with the animations to include.</returns>
    public virtual AnimationSet BuildAnimationSet(NodeContent input, ref ModelContent output, 
        ContentProcessorContext context)
    {
      AnimationSet ret = new AnimationSet();
      if (!DoAnimations)
      {
        context.Logger.LogImportantMessage("DoAnimation is set to false for {0}; not generating animations.", input.Name);
        return ret;
      }

      //  go from name to index
      Dictionary<string, ModelBoneContent> nameToIndex = new Dictionary<string, ModelBoneContent>();
      foreach (ModelBoneContent mbc in output.Bones)
        nameToIndex.Add(GetBoneName(mbc), mbc);

      AnimationContentDictionary adict = MergeAnimatedBones(input);
      if (adict == null || adict.Count == 0)
      {
        context.Logger.LogWarning("http://kwxport.sourceforge.net/", input.Identity, 
            "Model processed with AnimationProcessor has no animations.");
        return ret;
      }

      foreach (AnimationContent ac in adict.Values)
      {
        if (!IncludeAnimation(ac))
        {
          context.Logger.LogImportantMessage(String.Format("Not including animation named {0}.", ac.Name));
          continue;
        }
        context.Logger.LogImportantMessage(
            "Processing animation {0} duration {1} sample rate {2} reduction tolerance {3}.",
            ac.Name, ac.Duration, SampleRate, Tolerance);
        AnimationChannelDictionary acdict = ac.Channels;
        AnimationTrackDictionary tracks = new AnimationTrackDictionary();
        TimeSpan longestUniqueDuration = new TimeSpan(0);
        foreach (string name in acdict.Keys)
        {
          if (!IncludeTrack(name))
          {
            context.Logger.LogImportantMessage(String.Format("Not including track named {0}.", name));
            continue;
          }

          int ix = 0;
          AnimationChannel achan = acdict[name];
          int bix = nameToIndex[name].Index;
          context.Logger.LogMessage("Processing bone {0}:{1}.", name, bix);
          AnimationTrack at;
          if (tracks.TryGetValue(bix, out at))
          {
            throw new System.ArgumentException(
                String.Format("Bone index {0} is used by multiple animations in the same clip (name {1}).",
                bix, name));
          }

          //  Sample at given frame rate from 0 .. Duration
          List<Keyframe> kfl = new List<Keyframe>();
          int nFrames = (int)Math.Floor(ac.Duration.TotalSeconds * SampleRate + 0.5);
          for (int i = 0; i < nFrames; ++i)
          {
            Keyframe k = SampleChannel(achan, i / SampleRate, ref ix);
            kfl.Add(k);
          }

          //  Run keyframe elimitation
          Keyframe[] frames = kfl.ToArray();
          int nReduced = 0;
          if (tolerance_ > 0)
            nReduced = ReduceKeyframes(frames, tolerance_);
          if (nReduced > 0)
            context.Logger.LogMessage("Reduced '{2}' from {0} to {1} frames.", 
                frames.Length, frames.Length - nReduced, name);

          //  Create an AnimationTrack
          at = new AnimationTrack(bix, frames);
          Debug.Assert(name != null);
          at.Name = name;
          tracks.Add(bix, at);
        }
        if (ShouldTrimAnimation(ac))
        {
          TrimAnimationTracks(ac.Name, tracks, context);
        }

        Animation a = new Animation(ac.Name, tracks, SampleRate);
        ret.AddAnimation(a);
      }

      //  build the special "identity" and "bind pose" animations
      AnimationTrackDictionary atd_id = new AnimationTrackDictionary();
      AnimationTrackDictionary atd_bind = new AnimationTrackDictionary();
      foreach (KeyValuePair<string, ModelBoneContent> nip in nameToIndex)
      {
        if (!IncludeTrack(nip.Key))
          continue;
        Keyframe[] frames_id = new Keyframe[2];
        frames_id[0] = new Keyframe();
        frames_id[1] = new Keyframe();
        AnimationTrack at_id = new AnimationTrack(nip.Value.Index, frames_id);
        at_id.Name = nip.Key;
        atd_id.Add(nip.Value.Index, at_id);

        Keyframe[] frames_bind = new Keyframe[2];
        Matrix mat = nip.Value.Transform;
        frames_bind[0] = Keyframe.CreateFromMatrix(mat);
        frames_bind[1] = new Keyframe();
        frames_bind[1].CopyFrom(frames_bind[0]);
        AnimationTrack at_bind = new AnimationTrack(nip.Value.Index, frames_bind);
        at_bind.Name = nip.Key;
        atd_bind.Add(nip.Value.Index, at_bind);
      }
      ret.AddAnimation(new Animation("$id$", atd_id, 1.0f));
      ret.AddAnimation(new Animation("$bind$", atd_bind, 1.0f));

      return ret;
    }

    /// <summary>
    /// Given the animation tracks in the dictionary (for an animation with the given 
    /// name), figure out what the latest frame is that contains unique data for any 
    /// track, and trim the animation from the end to that length.
    /// </summary>
    /// <param name="name">name of the animation to trim</param>
    /// <param name="tracks">the tracks of the animation to trim</param>
    /// <param name="context">for logging etc</param>
    protected virtual void TrimAnimationTracks(string name, AnimationTrackDictionary tracks,
        ContentProcessorContext context)
    {
      int latestUnique = 1;
      int latestFrame = 0;
      foreach (AnimationTrack at in tracks.Values)
      {
        Keyframe last = at.Keyframes[0];
        int latestCurrent = 0;
        int index = 0;
        if (at.NumFrames > latestFrame)
        {
          latestFrame = at.NumFrames;
        }
        foreach (Keyframe kf in at.Keyframes)
        {
          if (kf != null && last.DifferenceFrom(kf) >= tolerance_)
          {
            latestCurrent = index;
            last = kf;
          }
          ++index;
        }
        if (latestCurrent > latestUnique)
        {
          latestUnique = latestCurrent;
        }
      }
      if (latestUnique + 1 < latestFrame)
      {
        context.Logger.LogMessage("Trimming animation {0} from {1} to {2} frames.",
            name, latestFrame, latestUnique + 1);
        foreach (AnimationTrack at in tracks.Values)
        {
          at.ChopToLength(latestUnique + 1);
        }
      }
    }

    protected static Matrix GetAbsoluteTransform(NodeContent mbc, NodeContent relativeTo)
    {
      Matrix mat = Matrix.Identity;
      //  avoid recursion
      while (mbc != null && mbc != relativeTo)
      {
        mat = mat * mbc.Transform;
        mbc = mbc.Parent;
      }
      return mat;
    }

    protected virtual AnimationContentDictionary MergeAnimatedBones(NodeContent root)
    {
      AnimationContentDictionary ret = new AnimationContentDictionary();
      CollectAnimatedBones(ret, root);
      return ret;
    }
    
    protected virtual void CollectAnimatedBones(AnimationContentDictionary dict, NodeContent bone)
    {
      AnimationContentDictionary acd = bone.Animations;
      if (acd != null)
      {
        foreach (string name in acd.Keys)
        {
          //  merge each animation into the dictionary
          AnimationContent ac = acd[name];
          AnimationContent xac;
          if (!dict.TryGetValue(name, out xac))
          {
            //  create it if we haven't already seen it, and there's something there
            if (ac.Channels.Count > 0)
            {
              xac = ac;
              dict.Add(name, xac);
            }
          }
          else
          {
            //  merge the animation content
            foreach (KeyValuePair<string, AnimationChannel> kvp in ac.Channels)
            {
              AnimationChannel ov;
              if (xac.Channels.TryGetValue(kvp.Key, out ov))
              {
                throw new System.ArgumentException(
                    String.Format("The animation {0} has multiple channels named {1}.",
                        name, kvp.Key));
              }
              xac.Channels.Add(kvp.Key, kvp.Value);
            }
            xac.Duration = new TimeSpan((long)
                (Math.Max(xac.Duration.TotalSeconds, ac.Duration.TotalSeconds) * 1e7));
          }
        }
      }
      foreach (NodeContent nc in bone.Children)
        CollectAnimatedBones(dict, nc);
    }

    /// <summary>
    /// Return true if you want to trim the given animation, removing identical frames 
    /// from the end. This is necessay for short animations imported through the .X 
    /// importer, which somehow manages to add blank padding to the duration.
    /// </summary>
    /// <param name="ac">The animation to test against the list of animation name patterns</param>
    /// <returns></returns>
    protected virtual bool ShouldTrimAnimation(AnimationContent ac)
    {
      if (trimAnimationExpressions_ != null)
        foreach (Regex re in trimAnimationExpressions_)
          if (re.IsMatch(ac.Name))
            return true;
      return false;
    }

    /// <summary>
    /// Return true if you want to include the specific animation in the output animation set.
    /// </summary>
    /// <param name="ac">The animation to check.</param>
    /// <returns>true unless the animation name is in a semicolon-separated list called ExcludeAnimations</returns>
    protected virtual bool IncludeAnimation(AnimationContent ac)
    {
      if (excludeAnimationsExpressions_ != null)
        foreach (Regex re in excludeAnimationsExpressions_)
          if (re.IsMatch(ac.Name))
            return false;
      return true;
    }

    /// <summary>
    /// Return true if you want to include the specific track (bone) in the output animation set.
    /// </summary>
    /// <param name="ac">The animation to check.</param>
    /// <returns>true unless the bone name is in a semicolon-separated list called ExcludeBones</returns>
    protected virtual bool IncludeTrack(string name)
    {
      if (excludeBonesExpressions_ != null)
        foreach (Regex re in excludeBonesExpressions_)
          if (re.IsMatch(name))
            return false;
      return true;
    }

    [DefaultValue("")]
    [Description("List of regex for animation names to filter out (; separates)")]
    public string ExcludeAnimations { get { return excludeAnimations_; } set { excludeAnimations_ = value; } }
    string excludeAnimations_ = "";
    Regex[] excludeAnimationsExpressions_;

    [DefaultValue("")]
    [Description("List of regex for animation bones to filter out (; separates)")]
    public string ExcludeBones { get { return excludeBones_; } set { excludeBones_ = value; } }
    string excludeBones_ = "";
    Regex[] excludeBonesExpressions_;

    [DefaultValue("")]
    [Description("List of regex for animation animations to trim static frames from the end of (; separates)")]
    public string TrimAnimations { get { return trimAnimations_; } set { trimAnimations_ = value; } }
    string trimAnimations_ = "";
    Regex[] trimAnimationExpressions_;

    /// <summary>
    /// SampleChannel will be called to sample the transformation of a given channel 
    /// at a given Clock. The given index parameter is for use by the sample function, 
    /// to avoid having to look for the "base" frame each call. It will start out as 
    /// zero in the first call for a given channel. "Clock" will be monotonically 
    /// increasing for a given channel.
    /// </summary>
    /// <param name="achan">The channel to sample from.</param>
    /// <param name="Clock">The Clock to sample at (monotonically increasing).</param>
    /// <param name="ix">For use by SampleChannel (starts at 0 for each new channel).</param>
    /// <returns>The sampled keyframe output (allocated by this function).</returns>
    protected virtual Keyframe SampleChannel(AnimationChannel achan, float time, ref int ix)
    {
      Keyframe ret = new Keyframe();
      AnimationKeyframe akf0 = achan[ix];
      float scale = CalcTransformScale(akf0.Transform);
      //todo:  really should be done in world space, but I'm giving up now
      float offset = akf0.Transform.Translation.Length();
      if (scale > maxScale_)
        maxScale_ = scale;
      if (offset > maxOffset_)
        maxOffset_ = offset;
    again:
      if (ix == achan.Count - 1)
        return KeyframeFromMatrix(akf0.Transform, ret);
      AnimationKeyframe akf1 = achan[ix+1];
      if (akf1.Time.TotalSeconds <= time)
      {
        akf0 = akf1;
        ++ix;
        goto again;
      }
      KeyframeFromMatrix(akf0.Transform, tmpA_);
      KeyframeFromMatrix(akf1.Transform, tmpB_);
      Keyframe.Interpolate(tmpA_, tmpB_, 
          (float)((time - akf0.Time.TotalSeconds) / (akf1.Time.TotalSeconds - akf0.Time.TotalSeconds)), 
          ret);
      return ret;
    }

    protected virtual float CalcTransformScale(Matrix mat)
    {
      return (float)Math.Pow(
          mat.Right.Length() *
          mat.Up.Length() *
          mat.Backward.Length(),
          1.0 / 3);
    }

    protected Keyframe tmpA_ = new Keyframe();
    protected Keyframe tmpB_ = new Keyframe();
    
    /// <summary>
    /// Decompose the given matrix into a scale/rotation/translation 
    /// keyframe.
    /// </summary>
    /// <param name="xform">The transform to decompose.</param>
    /// <param name="ret">The keyframe to decompose into.</param>
    /// <returns>ret (for convenience)</returns>
    protected Keyframe KeyframeFromMatrix(Matrix xform, Keyframe ret)
    {
      return Keyframe.CreateFromMatrix(ref xform, ret);
    }

    protected virtual int ReduceKeyframes(Keyframe[] frames, float tolerance)
    {
#if DEBUG
      if (frames == null)
        throw new ArgumentNullException("frames");
#endif
      int nReduced = 0;
      Keyframe lerp = new Keyframe();
      int nFrames = frames.Length;
      int prevFrame = 0;
      Keyframe prevData = frames[0];
      for (int curFrame = 1; curFrame < nFrames-1; ++curFrame)
      {
        Keyframe curData = frames[curFrame];
        int nextFrame = curFrame+1;
        Keyframe nextData = frames[nextFrame];
        Keyframe.Interpolate(prevData, nextData, 
            (float)(curFrame - prevFrame)/(float)(nextFrame - prevFrame),
            lerp);
        if (lerp.DifferenceFrom(curData) < tolerance)
        {
          frames[curFrame] = null;
          ++nReduced;
        }
        else
        {
          prevFrame = curFrame;
          prevData = curData;
        }
      }
      return nReduced;
    }

    bool doAnimations_ = true;
    [DefaultValue(true)]
    public bool DoAnimations { get { return doAnimations_; } set { doAnimations_ = value; } }

    float sampleRate_ = 30.0f;
    [DefaultValue(30.0f)]
    public float SampleRate { get { return sampleRate_; } set { sampleRate_ = value; } }

    float tolerance_ = 0.001f;
    [DefaultValue(0.001f)]
    public float Tolerance { get { return tolerance_; } set { tolerance_ = value; } }

    int maxNumBones_ = 72;
    [DefaultValue(72)]
    public int MaxNumBones { get { return maxNumBones_; } set { maxNumBones_ = value; } }
  }
}
