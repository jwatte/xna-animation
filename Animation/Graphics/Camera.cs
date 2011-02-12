using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;

using KiloWatt.Animation.Input;

namespace KiloWatt.Animation.Graphics
{
  public interface Camera : CameraBase
  {
    void SetArgument(int ix, string value);
    void SetListener(CameraListener l);
    void SetPositionConstraint(Constraint<Vector3> pc);
    void Update(InputState state, float dt);
    Vector2 Fov { get; }
    Matrix ViewProjection { get; }
    Matrix ViewInverse { get; }
    Matrix View { get; }
    Matrix Projection { get; }
    bool Flying { get; set; }
    CameraTarget Target { get; set; }
  }

  /// <summary>
  /// CameraBase is shared with Cinematographer (who just delegates).
  /// That way, we can interface entirely with the Cinematographer, and 
  /// don't have to hand out a writable camera object to people who just 
  /// want to know what the camera is up to.
  /// </summary>
  public interface CameraBase
  {
    Vector3 Front { get; }
    Vector3 Left { get; }
    Vector3 Up { get; }
    Vector3 Position { get; }
  }
}
