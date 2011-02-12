using System;
using System.Collections.Generic;
using System.Text;

namespace KiloWatt.Animation.Animation
{
  public enum BlendType
  {
    //  NormalizedBlend means that the blended animations will all be 
    //  composed to a 1.0 total scale. Useful for blending between 
    //  walk and run, say.
    NormalizedBlend,
    //  Compose means that the animations are "added" to each other. 
    //  Useful to add a "wave" animation to an "idle" animation, say.
    Compose
  }

  //  IBlendedAnimation is returned by the AnimationBlender, to let you control 
  //  the blended animation as it is playing (and test for completeness).
  public interface IBlendedAnimation
  {
    /// <summary>
    /// Blend weight relative to other playing animations. Must be > 0.
    /// All blend weights are normalized to 1 by the blender.
    /// </summary>
    float Weight { get; set; }
    /// <summary>
    /// Whether the animation has played out. Always false for looping underlying animations
    /// unless the animation has been removed from the blender.
    /// </summary>
    bool Complete { get; }
    /// <summary>
    /// The kind of blending that happens (normalized blend, or composition).
    /// </summary>
    BlendType BlendType { get; }
    /// <summary>
    /// Stop applying the blended animation.
    /// </summary>
    void Remove();
  }
}
