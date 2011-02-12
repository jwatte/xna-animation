using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;

namespace KiloWatt.Animation.Graphics
{
  public interface Constraint<T>
  {
    void Constrain(ref T t);
  }

  public class PositionConstraint : Constraint<Vector3>
  {
    public PositionConstraint(Vector3 minBox, Vector3 maxBox, Constraint<Vector3> chain)
    {
      chain_ = chain;
      minBox_ = minBox;
      maxBox_ = maxBox;
    }
    public Vector3 Min { get { return minBox_; } set { minBox_ = value; } }
    public Vector3 Max { get { return maxBox_; } set { maxBox_ = value; } }
    Vector3 minBox_;
    Vector3 maxBox_;
    Constraint<Vector3> chain_;
    public void Constrain(ref Vector3 t)
    {
      if (t.X < minBox_.X) t.X = minBox_.X;
      if (t.Y < minBox_.Y) t.Y = minBox_.Y;
      if (t.Z < minBox_.Z) t.Z = minBox_.Z;
      if (t.X > maxBox_.X) t.X = maxBox_.X;
      if (t.Y > maxBox_.Y) t.Y = maxBox_.Y;
      if (t.Z > maxBox_.Z) t.Z = maxBox_.Z;
      if (chain_ != null)
        chain_.Constrain(ref t);
    }
  }
}
