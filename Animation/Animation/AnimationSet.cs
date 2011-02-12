using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework.Content;

namespace KiloWatt.Animation.Animation
{
  //  The AnimationSet is a dictionary of animations, which you 
  //  can find by name (or by iteration). It is mostly used as a 
  //  nice wrapper over AnimationDictionary and for file I/O.
  public class AnimationSet
  {
    public AnimationSet()
    {
    }

    public AnimationSet(AnimationDictionary animations)
    {
      animations_ = animations;
    }

    AnimationDictionary animations_ = new AnimationDictionary();
    public AnimationDictionary AnimationDictionary { get { return animations_; } }

    internal void Load(AnimationDictionary animations)
    {
      animations_ = animations;
    }

    //  Given a name, return the animation of that name, or null.
    public Animation AnimationByName(string name)
    {
      Animation oot;
      if (animations_.TryGetValue(name, out oot))
        return oot;
      return null;
    }

    //  Return the names of all animations.
    public IEnumerable<string> AnimationNames { get { return animations_.Keys; } }

    //  Return all animations.
    public IEnumerable<Animation> Animations { get { return animations_.Values; } }

    //  Return the number of animations.
    public int NumAnimations { get { return animations_.Count; } }

    //  Add an animation to the set. The name must be unique.
    public void AddAnimation(Animation anim)
    {
      Animation oot;
      if (animations_.TryGetValue(anim.Name, out oot))
        throw new System.ArgumentException(
            String.Format("An animation with the name {0} already exists in AnimationSet.AddAnimation()",
            anim.Name), "anim");
      animations_.Add(anim.Name, anim);
    }

    //  Remove a given animation (which must already be in the set).
    public void RemoveAnimation(Animation anim)
    {
      Animation oot;
      if (!animations_.TryGetValue(anim.Name, out oot))
        throw new System.ArgumentException(
            String.Format("An animation with the name {0} doesn't exist in AnimationSet.RemoveAnimation()",
            anim.Name), "anim");
      animations_.Remove(anim.Name);
    }
  }

  //  AnimationDictionary is the underlying storage for 
  public class AnimationDictionary : Dictionary<string, Animation>
  {
  }

  //  File I/O
  public class AnimationSetReader : ContentTypeReader<AnimationSet>
  {
    public AnimationSetReader()
    {
    }

    protected override AnimationSet Read(ContentReader input, AnimationSet existingInstance)
    {
      if (existingInstance == null)
        existingInstance = new AnimationSet();
      AnimationDictionary animations = input.ReadObject<AnimationDictionary>();
      existingInstance.Load(animations);
      return existingInstance;
    }
  }

  //  File I/O
  public class AnimationDictionaryReader : ContentTypeReader<AnimationDictionary>
  {
    public AnimationDictionaryReader()
    {
    }

    protected override AnimationDictionary Read(ContentReader input, AnimationDictionary existingInstance)
    {
      if (existingInstance == null)
        existingInstance = new AnimationDictionary();
      int num = input.ReadInt32();
      for (int i = 0; i != num; ++i)
      {
        string name = input.ReadString();
        Animation anim = input.ReadObject<Animation>();
        existingInstance.Add(name, anim);
      }
      return existingInstance;
    }
  }
}
