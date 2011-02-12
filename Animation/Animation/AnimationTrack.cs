using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework.Content;

namespace KiloWatt.Animation.Animation
{
  //  One track of animation data, which means a set of keyframes over time
  //  for a given bone.
  public class AnimationTrack
  {
    public AnimationTrack()
    {
    }

    public AnimationTrack(int boneIndex, Keyframe[] kfs)
    {
      Load(boneIndex, kfs);
    }
    
    Keyframe[] keyframes_;
    public void ChopToLength(int length)
    {
      if (length < keyframes_.Length)
      {
        Keyframe[] old = keyframes_;
        keyframes_ = new Keyframe[length];
        Array.Copy(old, keyframes_, length);
        if (keyframes_[length-1] == null)
        {
          for (int i = length; i < old.Length; ++i)
          {
            if (old[i] != null)
            {
              keyframes_[length-1] = old[i];
              break;
            }
          }
          Debug.Assert(keyframes_[length-1] != null);
        }
      }
    }
    public Keyframe[] Keyframes { get { return keyframes_; } }
    public int NumFrames { get { return keyframes_.Length; } }
    int boneIndex_;
    public int BoneIndex { get { return boneIndex_; } }
    public string Name { get; set; }

    internal void Load(int boneIndex, Keyframe[] kfs)
    {
      keyframes_ = kfs;
      boneIndex_ = boneIndex;
    }
  }

  //  File I/O
  public class AnimationTrackReader : ContentTypeReader<AnimationTrack>
  {
    public AnimationTrackReader()
    {
    }

    protected override AnimationTrack Read(ContentReader input, AnimationTrack existingInstance)
    {
      if (existingInstance == null)
        existingInstance = new AnimationTrack();
      int ix = input.ReadInt32();
      Keyframe[] kfs = input.ReadObject<Keyframe[]>();
      existingInstance.Load(ix, kfs);
      existingInstance.Name = input.ReadString();
      return existingInstance;
    }
  }
}
