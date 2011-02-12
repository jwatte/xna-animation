using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

namespace KiloWatt.Animation.Animation
{
  //  information about the bounds of a skinned object (including scale)
  public class BoundsInfo
  {
    public BoundsInfo(float scale, float offset)
    {
      MaxScale = scale;
      MaxOffset = offset;
    }
    
    public float MaxScale;
    public float MaxOffset;
  }

  public class BoundsInfoReader : ContentTypeReader<BoundsInfo>
  {
    public BoundsInfoReader()
    {
    }

    protected override BoundsInfo Read(ContentReader input, BoundsInfo existingInstance)
    {
      if (existingInstance == null)
        existingInstance = new BoundsInfo(1, 0);
      existingInstance.MaxScale = input.ReadSingle();
      existingInstance.MaxOffset = input.ReadSingle();
      return existingInstance;
    }
  }
}
