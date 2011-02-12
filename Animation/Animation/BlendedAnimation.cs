using System;
using System.Collections.Generic;
using System.Text;

namespace KiloWatt.Animation.Animation
{
  //  Implementation class for AnimationBlender. Keeps state about 
  //  the current blend state of a playing animation. This represents 
  //  an animation instance.
  internal class BlendedAnimation : IBlendedAnimation
  {
    internal BlendedAnimation(IAnimationInstance instance, BlendType blendType)
    {
      System.Diagnostics.Debug.Assert(instance != null);
      Instance = instance;
      blendType_ = blendType;
    }

    public override string ToString()
    {
      return Instance.Name + " " + weight_.ToString();
    }    

    internal IAnimationInstance Instance;
    internal AnimationBlender Owner;
    internal float weight_;
    internal BlendType blendType_;

    //  How much of this animation is used? (may get normalized)
    public float Weight { get { return weight_; } set { weight_ = value; } }

    //  Has the animation played out? (one-shot only)
    public bool Complete { get { return Instance.Complete; } }

    //  What kind of blend type? (normalized blend or composite)
    public BlendType BlendType { get { return blendType_; } }

    //  Remove from the animation blender -- stops playing this animation
    public void Remove()
    {
      if (Owner != null)
      {
        AnimationBlender ab = Owner;
        Owner = null;
        ab.RemoveAnimation(this);
      }
    }

    //  Owner has detected that this animation should be removed
    internal void Detach()
    {
      Owner = null;
    }

    //  Animation instance has been attached to a given blender. Can only be 
    //  attached to one blender at a time.    
    internal void Attach(AnimationBlender ab)
    {
      if (Owner != null)
        Owner.RemoveAnimation(this);
      Owner = ab;
    }
  }
}
