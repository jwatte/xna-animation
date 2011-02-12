using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace KiloWatt.Animation.Animation
{
  //  SkinnedBone contains information needed for each bone in a skinned mesh pose.
  //  This is information that is additional to that of a ModelBone.
  public struct SkinnedBone
  {
    //  The name -- used for linking up later.
    public string     Name;
    //  The inverse of the bind transform of this bone (go from vertex to bone space).
    public Matrix     InverseBindTransform;
    //  Index of this bone in the Model.Bones collection.
    public int        Index;
  }


  //  I/O on SkinnedBone
  public class SkinnedBoneReader : ContentTypeReader<SkinnedBone>
  {
    public SkinnedBoneReader()
    {
    }

    protected override SkinnedBone Read(ContentReader input, SkinnedBone existingInstance)
    {
      existingInstance.Name = input.ReadString();
      existingInstance.InverseBindTransform = input.ReadMatrix();
      existingInstance.Index = input.ReadInt32();
      return existingInstance;
    }
  }
}
