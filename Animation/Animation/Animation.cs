using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework.Content;

namespace KiloWatt.Animation.Animation
{
  //  An animation is a collection of animation tracks targeting different bones, 
  //  and some information about duration and frame rate.
  public class Animation
  {
    public Animation()
    {
    }

    public Animation(string name, AnimationTrackDictionary tracks, float frameRate)
      : this(name, tracks, frameRate, -1)
    {
    }

    public Animation(string name, AnimationTrackDictionary tracks, float frameRate, int durationFrames)
    {
      Load(name, frameRate, durationFrames, tracks);
      if (durationFrames < 0)
        CalcNumFrames();
    }

    //  the number of frames is the maximum of all the tracks
    void CalcNumFrames()
    {
      int nf = 0;
      foreach (AnimationTrack at in tracks_.Values)
      {
        if (at.NumFrames > nf)
          nf = at.NumFrames;
      }
      numFrames_ = nf;
    }

    //  because bone index (into Model.Bones) may be sparse and large, 
    //  it is stored in a dictionary.
    public AnimationTrack AnimationTrackByBoneIndex(int index)
    {
      AnimationTrack ret;
      if (tracks_.TryGetValue(index, out ret))
        return ret;
      return null;
    }

    string name_;
    public string Name { get { return name_; } set { name_ = value; } }
    AnimationTrackDictionary tracks_;
    public AnimationTrackDictionary Tracks { get { return tracks_; } }
    int numFrames_;
    public int NumFrames { get { return numFrames_; } }
    float frameRate_;
    public float FrameRate { get { return frameRate_; } }

    internal void Load(string name, float frameRate, int numFrames, AnimationTrackDictionary tracks)
    {
      name_ = name;
      frameRate_ = frameRate;
      numFrames_ = numFrames;
      tracks_ = tracks;
    }
  }

  //  I/O
  public class AnimationTrackDictionary : Dictionary<int, AnimationTrack>
  {
  }

  public class AnimationReader : ContentTypeReader<Animation>
  {
    public AnimationReader()
    {
    }

    protected override Animation Read(ContentReader input, Animation existingInstance)
    {
      if (existingInstance == null)
        existingInstance = new Animation();
      string name = input.ReadString();
      float frameRate = input.ReadSingle();
      int numFrames = input.ReadInt32();
      AnimationTrackDictionary tracks = input.ReadObject<AnimationTrackDictionary>();
      existingInstance.Load(name, frameRate, numFrames, tracks);
      return existingInstance;
    }
  }

  public class AnimationTrackDictionaryReader : ContentTypeReader<AnimationTrackDictionary>
  {
    public AnimationTrackDictionaryReader()
    {
    }

    protected override AnimationTrackDictionary Read(ContentReader input, AnimationTrackDictionary existingInstance)
    {
      if (existingInstance == null)
        existingInstance = new AnimationTrackDictionary();
      int num = input.ReadInt32();
      for (int i = 0; i != num; ++i)
      {
        int index = input.ReadInt32();
        AnimationTrack track = input.ReadObject<AnimationTrack>();
        existingInstance.Add(index, track);
      }
      return existingInstance;
    }
  }
}
