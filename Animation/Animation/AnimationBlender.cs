using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace KiloWatt.Animation.Animation
{
  /// <summary>
  /// You will typically create one AnimationBlender per character you want to 
  /// animation, and blend/compose all the animations you want to play into that 
  /// blender. For each animation you want to blend or compose, you get the 
  /// Animation from the AnimationSet, and then create an IBlendedAnimation
  /// which you can then add to and remove from the AnimationBlender.
  /// Each time through your Update() loop, you call Advance() on the AnimationBlender 
  /// instance. Each time through your Draw() loop, you get the CurrentPose and apply 
  /// that to your bone array.
  /// </summary>
  public class AnimationBlender : IAnimationInstance
  {
    /// <summary>
    /// Create an AnimationBlender instance, intended to target the given model.
    /// The Model is just used to get the initial pose for each bone, and the count of bones.
    /// </summary>
    /// <param name="target">The model to animate using this blender.</param>
    /// <param name="name">The name of this blender (for diagnostics).</param>
    public AnimationBlender(Model target, string name)
    {
      target_ = target;
      name_ = name;
      Reset();
    }
    
    Keyframe[] keyframes_;
    Model target_;
    string name_;
    bool dirty_;
    bool inAdvance_;
    Keyframe workItem_ = new Keyframe();

    /// <summary>
    /// Advance all attached animations by a given amount of time.
    /// </summary>
    /// <param name="dt">How much to advance animations by.</param>
    public void Advance(float dt)
    {
      dirty_ = true;
      try
      {
        unchecked
        {
          //  update transitions
          for (int ti = 0, tn = transitions_.Count; ti != tn; ++ti)
          {
            if (transitions_[ti].Update(dt))
            {
              transitions_.RemoveAt(ti);
              --ti;
              --tn;
            }
          }

          inAdvance_ = true;
          //  update blends
          float runningWeight = 0;
          for (int bi = 0, bn = blendedAnimations_.Count; bi != bn; ++bi)
          {
            BlendedAnimation ba = blendedAnimations_[bi];
            ba.Instance.Advance(dt);
            float wt = ba.Weight;
            if (wt == 0)
              continue;
            Keyframe[] kfs = ba.Instance.CurrentPose;
            if (runningWeight == 0)
            {
              runningWeight = Math.Abs(wt);
              for (int i = 0, n = Math.Min(kfs.Length, keyframes_.Length); i != n; ++i)
              {
                Keyframe k = kfs[i];
                if (k != null)
                  keyframes_[i].CopyFrom(k);
              }
            }
            else
            {
              runningWeight += Math.Abs(wt);
              float t = wt / runningWeight;
              for (int i = 0, n = Math.Min(kfs.Length, keyframes_.Length); i != n; ++i)
              {
                Keyframe k = kfs[i];
                Keyframe d = keyframes_[i];
                if (k != null)
                  Keyframe.Interpolate(d, k, wt, d);
              }
            }
          }

          //  update composes
          //  TODO: sort from smallest to biggest weight?
          for (int ci = 0, cn = composedAnimations_.Count; ci != cn; ++ci)
          {
            BlendedAnimation ca = composedAnimations_[ci];
            ca.Instance.Advance(dt);
            float w = ca.Weight;
            if (w == 0)
              continue;
            Keyframe[] kfs = ca.Instance.CurrentPose;
            for (int i = 0, n = Math.Min(kfs.Length, keyframes_.Length); i != n; ++i)
            {
              Keyframe k = kfs[i];
              Keyframe d = keyframes_[i];
              if (k != null)
              {
                Keyframe.Compose(d, k, w, workItem_);
                d.CopyFrom(workItem_);
              }
            }
          }
        }
      }
      finally
      {
        inAdvance_ = false;
      }
    }

    /// <summary>
    /// Calculate (if necessary) and return the current pose of all the 
    /// blended and composed animations playing.
    /// </summary>
    public Keyframe[] CurrentPose { get { return MaybeUpdateKeyframes(); } }

    /// <summary>
    /// The name of this animation blender, specified during construction.
    /// </summary>
    public string Name { get { return name_; } }
    
    /// <summary>
    /// Return true if there are no currently playing animations (blended or composed).
    /// </summary>
    public bool Complete { get { return composedAnimations_.Count == 0 && blendedAnimations_.Count == 0; } }

    Keyframe[] MaybeUpdateKeyframes()
    {
      if (dirty_)
      {
      }
      return keyframes_;
    }

    List<BlendedAnimation> composedAnimations_ = new List<BlendedAnimation>();
    List<BlendedAnimation> blendedAnimations_ = new List<BlendedAnimation>();
    List<Transition> transitions_ = new List<Transition>();

    /// <summary>
    /// Create an IBlendedAnimation given an underlying animation instance.
    /// That composed animation can be added to an AnimationBlender. Note that 
    /// you shouldn't re-use the same instance on multiple AnimationBlenders 
    /// simultaneously. Composed animations are all multiplied together, given 
    /// their respective influencing weights.
    /// </summary>
    /// <param name="anim">The animation instance to compose.</param>
    /// <returns>The composed animation that you should add to an AnimationBlender.</returns>
    public static IBlendedAnimation CreateComposedAnimation(IAnimationInstance anim)
    {
      return new BlendedAnimation(anim, BlendType.Compose);
    }

    /// <summary>
    /// Create an IBlendedAnimation given an underlying animation instance.
    /// That blended animation can be added to an AnimationBlender. Note that 
    /// you shouldn't re-use the same instance on multiple AnimationBlenders 
    /// simultaneously. Blended animations are weighted through interpolation 
    /// to a total weight of 1.0.
    /// </summary>
    /// <param name="anim">The animation instance to blend.</param>
    /// <returns>The blended animation that you should add to an AnimationBlender.</returns>
    public static IBlendedAnimation CreateBlendedAnimation(IAnimationInstance anim)
    {
      return new BlendedAnimation(anim, BlendType.NormalizedBlend);
    }

    /// <summary>
    /// Remove all currently playing animations and set a clean slate.
    /// </summary>
    public void Reset()
    {
      keyframes_ = new Keyframe[target_.Bones.Count];
      for (int i = 0, n = target_.Bones.Count; i != n; ++i)
        keyframes_[i] = Keyframe.CreateFromMatrix(target_.Bones[i].Transform);
      dirty_ = true;
      foreach (IBlendedAnimation ba in blendedAnimations_)
        (ba as BlendedAnimation).Detach();
      blendedAnimations_.Clear();
      foreach (IBlendedAnimation ca in composedAnimations_)
        (ca as BlendedAnimation).Detach();
      composedAnimations_.Clear();
      transitions_.Clear();
    }

    /// <summary>
    /// Over the given amount of time, transition from using some previous animation, 
    /// to using the currently specified animation. The previous animation will be 
    /// ramped to 0 weight and removed; the new animation will be ramped from 0 weight 
    /// to 1 over the time of the duration. The arguments must have been created with 
    /// CreateBlendedAnimation().
    /// You probably want to create a short stationary animation to use for things that 
    /// don't move, because blending from an animation to null (when that's the last 
    /// running animation) will result in the last animation playing fully until the 
    /// blend duration runs out, because of the re-normalization to 1.0 weight.
    /// Unfortunately, this results in a small amount of garbage when complete.
    /// </summary>
    /// <param name="from">Previous animation to transition out, or null.</param>
    /// <param name="to">New animation to transition in, or null.</param>
    /// <param name="duration">How long to run the transition for.</param>
    public void TransitionAnimations(IBlendedAnimation from, IBlendedAnimation to, float duration)
    {
      if (to != null)
        AddAnimation(to, 0);
      transitions_.Add(new Transition(from, (from == null) ? 0 : from.Weight, to, 1, duration));
    }

    /// <summary>
    /// Add a new blended animation, that will keep playing until you remove it or it 
    /// completes. The animation must have been created with CreateBlendedAnimation()
    /// or CreateComposedAnimation().
    /// </summary>
    /// <param name="nu">The animation to start blending.</param>
    /// <param name="weight">The relative weight of this animation, compared to other 
    /// blended animations.</param>
    public void AddAnimation(IBlendedAnimation nu, float weight)
    {
      BlendedAnimation ba = (BlendedAnimation)nu;
      if (nu.BlendType == BlendType.NormalizedBlend)
        blendedAnimations_.Add(ba);
      else
        composedAnimations_.Add(ba);
      ba.Attach(this);
      nu.Weight = weight;
    }

    internal class Transition
    {
      internal Transition(IBlendedAnimation from, float fromWeight, IBlendedAnimation to, float toWeight, float duration)
      {
        From = from;
        FromWeight = fromWeight;
        To = to;
        ToWeight = toWeight;
        Duration = duration;
        Elapsed = 0;
      }
      
      internal void OnRemove(IBlendedAnimation anim)
      {
        if (anim == From)
        {
          //  make sure it completes in the next step
          From = null;
          Elapsed = Duration;
          if (To != null)
            To.Weight = 1;
        }
        if (anim == To)
        {
          To = null;
          //  keep fading out "From" here, or just set to 0 ?
        }
      }
      
      IBlendedAnimation From;
      IBlendedAnimation To;
      float FromWeight;
      float ToWeight;
      float Duration;
      float Elapsed;

      internal bool Update(float dt)
      {
        Elapsed += dt;
        if (Elapsed >= Duration || (From != null && From.Complete) || (To != null && To.Complete))
        {
          if (From != null)
            From.Remove();
          if (To != null)
            To.Weight = 1;
          return true;
        }
        else
        {
          float delta = Elapsed / Duration;
          if (From != null)
            From.Weight = FromWeight * (1 - delta);
          if (To != null)
            To.Weight = ToWeight * delta;
          return false;
        }
      }
    }

    internal void RemoveAnimation(BlendedAnimation anim)
    {
      System.Diagnostics.Debug.Assert(!inAdvance_);
      System.Diagnostics.Debug.Assert(
          (anim.BlendType == BlendType.NormalizedBlend && blendedAnimations_.Contains(anim))
          || (anim.BlendType == BlendType.Compose && composedAnimations_.Contains(anim)));
      if (anim.BlendType == BlendType.NormalizedBlend)
        blendedAnimations_.Remove(anim);
      else
        composedAnimations_.Remove(anim);
      unchecked
      {
        for (int ti = 0, tn = transitions_.Count; ti != tn; ++ti)
          transitions_[ti].OnRemove(anim);
      }
    }
  }
}
