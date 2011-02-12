using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace KiloWatt.Animation.Animation
{
  public delegate void AnimationInstanceNotify(AnimationInstance instance);

  /// <summary>
  /// An AnimationInstance is what actually "plays" an animation on a model.
  /// </summary>
  public class AnimationInstance : IAnimationInstance
  {
    //  
    public AnimationInstance(Animation anim)
    {
      animation_ = anim;
      frameRate_ = anim.FrameRate;
      //  The time of the last frame is less than the duration of the animation,
      //  as the last frame has some duration itself.
      lastFrameTime_ = (anim.NumFrames - 1) / frameRate_;
      invFrameRate_ = 1.0f / frameRate_;
      duration_ = anim.NumFrames * invFrameRate_;
      int max = 0;
      //  calculate max bone index
      //  todo: do in content pipeline
      foreach (KeyValuePair<int, AnimationTrack> kvp in anim.Tracks)
      {
        if (kvp.Key >= max)
          max = kvp.Key + 1;
      }
      //  Allocate animation keyframes (for lerping between times).
      keyframes_ = new Keyframe[max];
      //  Load all the tracks (one track per bone).
      tracks_ = new AnimationTrack[max];
      foreach (int i in anim.Tracks.Keys)
      {
        keyframes_[i] = new Keyframe();
        tracks_[i] = anim.Tracks[i];
      }
    }

    /// <summary>
    /// Advance the animation to some new point in time, if the animation is playing.
    /// </summary>
    /// <param name="dt">How much real time to advance by -- will be scaled by "Speed"</param>
    public void Advance(float dt)
    {
      if (!Playing)
        return;
      //  Todo: this seems to also wrap looping animations...
      if (Wrap(time_ + dt * speed_, 0, duration_, out time_))
        OnEndReached();
      else if (!looping_ && speed_ > 0 && time_ >= lastFrameTime_)
        OnEndReached();
    }

    /// <summary>
    /// Read the animation state out of the animation instance, at 
    /// whatever the current time is.
    /// </summary>
    public Keyframe[] CurrentPose
    {
      get
      {
        if (appliedTime_ != time_)
          CalculateKeyframes();
        return keyframes_;
      }
    }

    //  Return the name of the animation (like "walk")
    public string Name { get { return Animation.Name; } } 

    public override string ToString()
    {
      return String.Format("{0}@{1:.2}/{2:.2}", Name, Time, looping_ ? duration_ : lastFrameTime_);
    }

    /// <summary>
    /// Extract the pose as a number of parent-relative matrices.
    /// Note that this is not typically what you want to use for 
    /// rendering, as you then want object-relative matrices.
    /// </summary>
    /// <param name="output">Each bones parent relative matrix will be stored 
    /// here, if the bone is animated. Else that matrix will be untouched.</param>
    public void CopyPoseTo(Matrix[] output)
    {
      if (appliedTime_ != time_)
        CalculateKeyframes();
      int n = output.Length;
      if (n > keyframes_.Length)
        n = keyframes_.Length;
      unchecked
      {
        for (int i = 0; i != n; ++i)
        {
          Keyframe kf = keyframes_[i];
          if (kf != null)
            kf.ToMatrix(out output[i]);
        }
      }
    }
    
    /// <summary>
    /// An ultra convenient function that will copy all the keyframes 
    /// to a given Model bone array. Only the bones that are actually 
    /// animated will be affected.
    /// </summary>
    /// <param name="model">The model to apply the animation to.</param>
    public void CopyPoseTo(Model model)
    {
      if (appliedTime_ != time_)
        CalculateKeyframes();
      int n = model.Bones.Count;
      if (n > keyframes_.Length)
        n = keyframes_.Length;
      Matrix mat;
      unchecked
      {
        for (int i = 0; i != n; ++i)
        {
          Keyframe kf = keyframes_[i];
          if (kf != null)
          {
            kf.ToMatrix(out mat);
            model.Bones[i].Transform = mat;
          }
        }
      }
    }

    /// <summary>
    /// If you change animation data, Invalidate() will make the pose get 
    /// re-calculated, even if Advance() doesn't change the instance time.
    /// </summary>
    public void Invalidate()
    {
      appliedTime_ = -1;
    }

    //  Internally, calculate all the keyframe values for a given time value.
    void CalculateKeyframes()
    {
      appliedTime_ = time_;
      float fracOrig;
      int frame;
      //  figure out which keyframe index it should be
      FrameFromTime(time_, out frame, out fracOrig);
      unchecked
      {
        for (int i = 0, n = tracks_.Length; i != n; ++i)
        {
          Keyframe kf = keyframes_[i];
          if (kf != null)
          {
            //  if I'm animating this bone
            Keyframe[] kfs = tracks_[i].Keyframes;
            //  now, find the matching begin/end frames
            int f1 = frame;
            int f2 = frame+1;
            int nprev = 0;
            int npost = 0;
            //  wrap onto the available frames (the last frame may not be at the end?)
            if (f1 >= kfs.Length)
            {
              nprev = (f1 - kfs.Length + 1);
              f1 = kfs.Length-1;
            }
            if (f2 >= kfs.Length)
            {
              npost = f2 - kfs.Length;
              f2 = 0;
            }
            //  find the bracketing keyframes, because frame data is sparse
            while (kfs[f1] == null)
            {
              //  first and last slots always have keyframes
              System.Diagnostics.Debug.Assert(f1 > 0 && f1 != kfs.Length-1);
              --f1;
              ++nprev;
            }
            //  If I'm not looping, I don't want to blend with the 0 frame
            if (!looping_ && f2 == 0)
              f2 = kfs.Length - 1;
            while (kfs[f2] == null)
            {
              //  first and last slots always have keyframes
              System.Diagnostics.Debug.Assert(f2 < kfs.Length-1 && f2 != 0);
              ++f2;
              ++npost;
            }
            //  adjust "frac" to be interpolation between f1 and f2 frames
            float frac = fracOrig;
            if (nprev != 0 || npost != 0)
              frac = (nprev + frac) / (nprev + npost + 1);
            //  actually extract the appropriate animation value
            Keyframe.Interpolate(kfs[f1], kfs[f2], frac, keyframes_[i]);
          }
        }
      }
    }
    
    /// <summary>
    /// Convert a given timestamp to a frame number and fractional blend factor.
    /// Does not do any wrapping of times outide the animation duration.
    /// </summary>
    /// <param name="time">The time to convert.</param>
    /// <param name="frame">Return the frame preceeding the time.</param>
    /// <param name="frac">Return the blend between the frame (0) and the next (1).</param>
    public void FrameFromTime(float time, out int frame, out float frac)
    {
      float f = time * frameRate_;
      frame = (int)f;
      frac = f - frame;
      System.Diagnostics.Debug.Assert(frac >= 0 && frac <= 1);
    }

    /// <summary>
    /// Wrap a given floating point number into a given interval. Returns true 
    /// if the floating point number was changed, and false if it's already in 
    /// the interval.
    /// </summary>
    /// <param name="val">The value to wrap.</param>
    /// <param name="min">The minimum of the interval.</param>
    /// <param name="max">The maximum of the interval; must be greater than min.</param>
    /// <param name="oot">The wrapped (or copied) value is returned here.</param>
    /// <returns>true if oot is made different from val; false if oot is made a copy of val</returns>
    public static bool Wrap(float val, float min, float max, out float oot)
    {
      //  This is on the inner path of animation, and can't afford to test to 
      //  throw an exception at runtime.
      System.Diagnostics.Debug.Assert(max > min);
      if (val < min) {
        oot = (val - min) % (max - min) + min;
        if (oot < min)
          oot += (max - min);
        return true;
      }
      if (val > max) {
        oot = (val - min) % (max - min) + min;
        return true;
      }
      oot = val;
      return false;
    }

    Animation animation_;
    public Animation Animation { get { return animation_; } }
    float duration_;
    //  duration in seconds, including the time of the last frame
    public float Duration { get { return duration_; } }
    bool looping_ = true;
    public bool Looping { get { return looping_; } set { looping_ = value; } }
    bool playing_ = true;
    public bool Playing { get { return playing_; } set { playing_ = value; } }
    float time_;
    //  return the current time position, in the range 0..duration
    public float Time { get { return time_; } set { Wrap(value, 0, Duration, out time_); } }
    float speed_ = 1;
    //  you can play animations at different speeds, including backwards!
    public float Speed { get { return speed_; } set { speed_ = value; } }
    float lastFrameTime_;
    //  the time of the last frame is somewhat before the duration of the animation
    public float LastFrameTime { get { return lastFrameTime_; } }
    /// <summary>
    /// This even fires when the animation plays to the end, and either 
    /// stops or loops (depending on settings).
    /// </summary>
    public event AnimationInstanceNotify EndReached;

    float frameRate_;
    float invFrameRate_;
    Keyframe[] keyframes_;
    /// <summary>
    /// tracks_ is not entirely necessary, but a convenient cache for
    /// finding each track. It could also conceivably be moved into the 
    /// animation instance itself.
    /// </summary>
    AnimationTrack[] tracks_;
    float appliedTime_ = -1;

    public void OnEndReached()
    {
      //  don't reset to the beginning -- 
      //  the object should stay in the final state
      if (!looping_)
        playing_ = false;
      if (EndReached != null)
        EndReached(this);
    }

    /// <summary>
    /// Stop playback and set the time to the logical start of the 
    /// animation (which is the end, if playing backwards and not looping).
    /// </summary>
    public void Reset()
    {
      playing_ = false;
      if (speed_ < 0)
        time_ = lastFrameTime_;
      else
        time_ = 0;
    }

    public bool Complete { get { return !playing_; } }
  }
}
