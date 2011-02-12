using System;

namespace KiloWatt.Animation.Animation
{
  /// <summary>
  /// There may be implementations of AnimationInstance that do 
  /// animation blending, feathering or composition. Those implementations 
  /// all implement IAnimationInstance.
  /// </summary>
  public interface IAnimationInstance
  {
    //  What is the current pose? Will calculate it if not known.
    Keyframe[] CurrentPose { get; }
    //  Name of the animation.
    string Name { get; }
    //  True when the animation has played out, if it's not looping.
    bool Complete { get; }
    //  Advance the animation by some given amount of time.
    void Advance(float dt);
  }
}
