using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;

//  Define MATRIXFRAMES if you want to use matrices for keyframes 
//  instead of SRT transforms. Doing so will cause collapsing when 
//  interpolating, and will cause the file size to explode.
namespace KiloWatt.Animation.Animation
{
  //  Keyframe represents one particular bone's pose at one point in time.
  //  It includes position, orientation and scale.
  public class Keyframe
  {
#if !MATRIXFRAMES
    public Keyframe()
    {
      ori_ = Quaternion.Identity;
      scale_ = Vector3.One;
    }
    
    public Keyframe(Vector3 pos, Quaternion ori)
      : this(pos, ori, Vector3.One)
    {
    }
    
    public Keyframe(Vector3 pos, Quaternion ori, Vector3 scale)
    {
      pos_ = pos;
      ori_ = ori;
      scale_ = scale;
    }

    //  Offset from parent, in parent space
    public Vector3 Pos { get { return pos_; } set { pos_ = value; } }
    Vector3 pos_;

    //  Orientation relative to parent
    public Quaternion Ori { get { return ori_; } set { ori_ = value; } }
    Quaternion ori_;

    //  Scale relative to parent
    public Vector3 Scale { get { return scale_; } set { scale_ = value; } }
    Vector3 scale_;

#else
    public Keyframe()
    {
      transform_ = Matrix.Identity;
    }

    Matrix transform_;
    public Matrix Transform { get { return transform_; } set { transform_ = value; } }
#endif

    //  A simple unit test makes sure some keyframe functions work right.
    public static bool Tested = Keyframe.Test();

    static bool Test()
    {
#if DEBUG && !MATRIXFRAMES
      Keyframe kf = new Keyframe();
      Matrix mx = Matrix.CreateScale(0.15f, 0.20f, 0.25f) * 
          Matrix.CreateRotationY((float)(Math.PI/6)) *
          Matrix.CreateRotationX(0.1f) *
          Matrix.CreateRotationZ(-0.1f) *
          Matrix.CreateTranslation(2, 0, 1);
      Keyframe.CreateFromMatrix(ref mx, kf);
      Matrix two;
      kf.ToMatrix(out two);
      Matrix three = Matrix.Invert(mx) * two;
      //  verify that forward and back is transparent
      for (int i = 0; i != 16; ++i)
      {
        float f = MatrixElement(ref three, i);
        if ((i % 5) == 0)
          System.Diagnostics.Debug.Assert(Math.Abs(f - 1) < 1e-4);
        else
          System.Diagnostics.Debug.Assert(Math.Abs(f) < 1e-4);
      }
#endif
      return true;
    }

#if !MATRIXFRAMES
    //  load from reading
    internal void Load(Vector3 pos, Quaternion ori, Vector3 scale)
    {
      pos_ = pos;
      ori_ = ori;
      scale_ = scale;
    }
#endif

    /// <summary>
    /// Evaluate the difference between two keyframes as a floating point number.
    /// </summary>
    /// <param name="o">The keyframe to compare to.</param>
    /// <returns>A measurement of how different two keyframes are. 0.01 is intended to be
    /// largely imperceptible; 1.0 is a big difference (but it could be even bigger). A 
    /// value below 0 is never returned, and 0 is returned for identity.</returns>
    public float DifferenceFrom(Keyframe o)
    {
#if !MATRIXFRAMES
      float dp = (pos_ - o.pos_).Length();
      float ds = (scale_ - o.scale_).Length();
      Matrix m1 = Matrix.CreateFromQuaternion(ori_);
      Matrix m2 = Matrix.CreateFromQuaternion(o.ori_);
      float dr = (float)Math.Pow(((m1.Right - m2.Right).Length() 
          + (m1.Up - m2.Up).Length() + (m1.Backward - m2.Backward).Length()), 4) * 100000;
      return (dp + ds + dr);
#else
      Matrix diff = Matrix.Invert(transform_) * o.transform_;
      float diff = 0;
      for (int i = 0; i < 16; ++i)
      {
        diff = diff + Math.Abs(MatrixElement(ref transform_, i) - MatrixElement(ref o.transform_, i));
      }
#endif
    }

    /// <summary>
    ///  Convert to matrix, in order scale, rotation, translation
    /// </summary>
    /// <returns>A matrix representing the keyframe transform.</returns>
    public Matrix ToMatrix()
    {
#if !MATRIXFRAMES
      Matrix ret;
      ToMatrix(out ret);
      return ret;
#else
      return transform_;
#endif
    }

#if !MATRIXFRAMES
    //  Convert to matrix, not taking scale into account
    public Matrix ToMatrixNoScale()
    {
      Matrix ret;
      ToMatrixNoScale(out ret);
      return ret;
    }
#endif

    //  Convert to matrix, in order scale, rotation, translation
    public void ToMatrix(out Matrix m)
    {
#if !MATRIXFRAMES
      m = Matrix.CreateFromQuaternion(ori_);
      m.M11 *= scale_.X;
      m.M12 *= scale_.X;
      m.M13 *= scale_.X;
      m.M21 *= scale_.Y;
      m.M22 *= scale_.Y;
      m.M23 *= scale_.Y;
      m.M31 *= scale_.Z;
      m.M32 *= scale_.Z;
      m.M33 *= scale_.Z;
      m.Translation = pos_;
#else
      m = transform_;
#endif
    }

#if !MATRIXFRAMES
    //  Convert to matrix, in order rotation, translation
    public void ToMatrixNoScale(out Matrix m)
    {
      m = Matrix.CreateFromQuaternion(ori_);
      m.Translation = pos_;
    }
#endif

    /// <summary>
    /// Interpolate between two keyframes into a provided storage keyframe.
    /// Interpolation of quaternions uses Lerp and normalization. Interpolation 
    /// is always done the short way around.
    /// </summary>
    /// <param name="left">The value to use at t == 0.</param>
    /// <param name="right">The value to use at t == 1.</param>
    /// <param name="t">The "time" factor between 0 and 1.</param>
    /// <param name="result">The destination for the interpolation.</param>
    /// <returns>result (for convenience).</returns>
    public static Keyframe Interpolate(Keyframe left, Keyframe right, float t, Keyframe result)
    {
#if DEBUG
      if (left == null)
        throw new ArgumentNullException("left");
      if (right == null)
        throw new ArgumentNullException("right");
      if (result == null)
        throw new ArgumentNullException("result");
#endif
#if !MATRIXFRAMES
      result.pos_ = left.pos_ * (1 - t) + right.pos_ * t;
      result.scale_ = left.scale_ * (1 - t) + right.scale_ * t;
      Quaternion b = right.ori_;
      if (Quaternion.Dot(left.ori_, b) < 0)
        b = -b;
      //  I'm doing lerp, rather than slerp. The idea is that keyframes will be dense enough 
      //  that the difference in angular velocity is very small, and lerp is a lot cheaper 
      //  than slerp.
      result.ori_ = Quaternion.Normalize(left.ori_ * (1 - t) + b * t);
#else
      result.transform_ = left.transform_ * (1 - t) + right.transform_ * t;
#endif
      return result;
    }

    /// <summary>
    /// Copy the values of a first keyframe into myself.
    /// </summary>
    /// <param name="o">The other keyframe to copy from.</param>
    /// <return>this</return>
    public Keyframe CopyFrom(Keyframe o)
    {
#if !MATRIXFRAMES
      pos_ = o.pos_;
      scale_ = o.scale_;
      ori_ = o.ori_;
#else
      transform_ = o.transform_;
#endif
      return this;
    }

    //  A Keyframe that does no scale, rotation or translation
    public static Keyframe Identity { get { return identity_; } }
    static Keyframe identity_ = new Keyframe();

    /// <summary>
    /// Given a base keyframe, and a "delta" keyframe, construct a keyframe that 
    /// represents the first transformation followed by a weighted amount of the 
    /// second keyframe.
    /// </summary>
    /// <param name="first">The base keyframe. Cannot be the same object as "result."</param>
    /// <param name="second">The delta keyframe. Can be the same object as "result."</param>
    /// <param name="weight">How much of the delta keyframe to apply (typically 0 .. 1)</param>
    /// <param name="result">The composed keyframe data will be stored here.</param>
    /// <returns>result (for convenience)</returns>
    public static Keyframe Compose(Keyframe first, Keyframe second, float weight, Keyframe result)
    {
#if DEBUG
      //  "first" and "result" can't be the same objects, although 
      //  "second" and "result" can be.
      System.Diagnostics.Debug.Assert(first != result);
      if (first == null)
        throw new ArgumentNullException("left");
      if (second == null)
        throw new ArgumentNullException("right");
      if (result == null)
        throw new ArgumentNullException("result");
#endif
#if !MATRIXFRAMES
      Interpolate(Identity, second, weight, result);
      Vector3 pos;
      Vector3.Transform(ref result.pos_, ref first.ori_, out pos);
      result.pos_ = first.pos_ + pos;
      result.scale_ = first.scale_ * second.scale_;
      //  Yes, XNA does quaternions in a different order from matrices. Blech.
      result.ori_ = second.ori_ * first.ori_;
#else
      result.transform_ = first.transform_ * (Matrix.Identity * (1 - weight) + second.transform_ * weight);
#endif
      return result;
    }

    //  Given a matrix, decompose that matrix into a Keyframe.
    //  This works for matrices that don't contain shear or off-axis scale.
    public static Keyframe CreateFromMatrix(Matrix mat)
    {
      return CreateFromMatrix(ref mat);
    }

    //  Given a matrix, decompose that matrix into a Keyframe.
    //  This works for matrices that don't contain shear or off-axis scale.
    public static Keyframe CreateFromMatrix(ref Matrix mat)
    {
      Keyframe k = new Keyframe();
      return CreateFromMatrix(ref mat, k);
    }

    /// <summary>
    /// Decompose the given matrix into a scale/rotation/translation 
    /// keyframe. The method used is not well behaved when the scale 
    /// along one axis is very close to 0, so don't scale down by more 
    /// than about 1/10,000.
    ///  This works for matrices that don't contain shear or off-axis scale.
    /// </summary>
    /// <param name="xform">The transform to decompose.</param>
    /// <param name="ret">The keyframe to decompose into.</param>
    /// <returns>ret (for convenience)</returns>
    public static Keyframe CreateFromMatrix(ref Matrix xform, Keyframe ret)
    {
#if DEBUG
      if (ret == null)
        throw new ArgumentNullException("ret");
#endif
#if !MATRIXFRAMES
      //  decompose the matrix into scale, rotation and translaction
      ret.Scale = new Vector3(
          (new Vector3(xform.M11, xform.M12, xform.M13)).Length(),
          (new Vector3(xform.M21, xform.M22, xform.M23)).Length(),
          (new Vector3(xform.M31, xform.M32, xform.M33)).Length());
      ret.Pos = xform.Translation;
      //  I can't extract rotation if one of the axes is zero scale.
      //  That's unfortunate, as someone might want to animation an object 
      //  becoming pancaked and spinning. Just don't make it THAT much 
      //  of a pancake then.
      Matrix mat = Matrix.Identity;
      if (Math.Abs(ret.Scale.X) > 1e-6 && Math.Abs(ret.Scale.Y) > 1e-6
          && Math.Abs(ret.Scale.Z) > 1e-6)
      {
        Vector3 right = Vector3.Normalize(xform.Right);
        Vector3 up = Vector3.Normalize(xform.Up - right * Vector3.Dot(xform.Up, right));
        Vector3 backward = Vector3.Cross(right, up);
        if (Vector3.Dot(xform.Backward, backward) < 0) {
          //  matrix is mirrored
          ret.Scale = ret.Scale * new Vector3(1, 1, -1);
        }
        mat.Right = right;
        mat.Up = up;
        mat.Backward = backward;
        ret.Ori = Quaternion.CreateFromRotationMatrix(mat);
      }
      ret.ToMatrix(out mat);
      for (int i = 0; i != 16; ++i)
      {
        //  Make sure that the matrix that comes out of the keyframe is a good decomposition
        //  of the original matrix.
        if(Math.Abs(MatrixElement(ref mat, i) - MatrixElement(ref xform, i)) > 0.02f)
        {
          Console.WriteLine("Matrix could not be properly decomposed into TRS keyframe.");
        }
      }
#else
      ret.transform_ = xform;
#endif
      return ret;
    }

    //  Given a matrix and an index, return the n-th value in that 
    //  matrix, assuming row-major ordering of a matrix assuming 
    //  row vertices on the left (so translation lives in 12, 13, 14).
    public static float MatrixElement(ref Matrix mat, int ix)
    {
      switch (ix)
      {
        case 0: return mat.M11;
        case 1: return mat.M12;
        case 2: return mat.M13;
        case 3: return mat.M14;
        case 4: return mat.M21;
        case 5: return mat.M22;
        case 6: return mat.M23;
        case 7: return mat.M24;
        case 8: return mat.M31;
        case 9: return mat.M32;
        case 10: return mat.M33;
        case 11: return mat.M34;
        case 12: return mat.M41;
        case 13: return mat.M42;
        case 14: return mat.M43;
        case 15: return mat.M44;
        default:
          throw new System.ArgumentException(String.Format("Index must be 0-15. Is {0}.", ix), "ix");
      }
    }
  }

  //  read from disk
  public class KeyframeReader : ContentTypeReader<Keyframe>
  {
    public KeyframeReader()
    {
    }

    protected override Keyframe Read(ContentReader input, Keyframe existingInstance)
    {
      if (existingInstance == null)
        existingInstance = new Keyframe();
#if !MATRIXFRAMES
      Vector3 pos = input.ReadVector3();
      Quaternion ori = input.ReadQuaternion();
      Vector3 scale = input.ReadVector3();
      existingInstance.Load(pos, ori, scale);
#else
      existingInstance.Transform = input.ReadMatrix();
#endif
      return existingInstance;
    }
  }
}
