using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;

namespace KiloWatt.Animation.Physics
{
  public class OBB
  {
    public OBB()
      : this(Vector3.One, Vector3.Zero, Quaternion.Identity)
    {
    }
    public OBB(Vector3 halfDim, Vector3 pos, Quaternion ori)
    {
      ori_ = ori;
      Pos = pos;
      HalfDim = halfDim;
      CalcMatrix();
    }

    Quaternion ori_;
    public Quaternion Ori
    {
      get
      {
        return ori_;
      }
      set
      {
        ori_ = value;
        CalcMatrix();
      }
    }
    public Matrix PosOriMatrix;
    public Matrix InvOriMatrix;
    public Vector3 HalfDim;
    public Vector3 Pos;

    void CalcMatrix()
    {
      Matrix.CreateFromQuaternion(ref ori_, out PosOriMatrix);
      PosOriMatrix.Translation = Pos;
      Quaternion q2;
      Quaternion.Conjugate(ref ori_, out q2);
      Matrix.CreateFromQuaternion(ref q2, out InvOriMatrix);
    }

    internal bool Overlaps(ref AABB aabb, float expand)
    {
      //  test OBB in AABB space
      float dx = Math.Abs(PosOriMatrix.Right.X) * HalfDim.X +
        Math.Abs(PosOriMatrix.Up.X) * HalfDim.Y +
        Math.Abs(PosOriMatrix.Backward.X) * HalfDim.Z;
      if (Pos.X + dx < aabb.Lo.X ||
        Pos.X - dx > aabb.Hi.X)
        return false;
      float dy = Math.Abs(PosOriMatrix.Right.Y) * HalfDim.X +
        Math.Abs(PosOriMatrix.Up.Y) * HalfDim.Y +
        Math.Abs(PosOriMatrix.Backward.Y) * HalfDim.Z;
      if (Pos.Y + dy < aabb.Lo.Y ||
        Pos.Y - dy > aabb.Hi.Y)
        return false;
      float dz = Math.Abs(PosOriMatrix.Right.Z) * HalfDim.X +
        Math.Abs(PosOriMatrix.Up.Z) * HalfDim.Y +
        Math.Abs(PosOriMatrix.Backward.Z) * HalfDim.Z;
      if (Pos.Z + dz < aabb.Lo.Z ||
        Pos.Z - dz > aabb.Hi.Z)
        return false;
      //  TODO: test AABB in OBB space
      //  TODO: test 9 cross axes
      return true;
    }
  }
}
