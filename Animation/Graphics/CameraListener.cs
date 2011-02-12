using System;
using Microsoft.Xna.Framework;

namespace KiloWatt.Animation.Graphics
{
  public interface CameraListener
  {
    void SetCameraParameters(Vector3 pos, Vector3 front, Vector3 up, Vector2 fov, Matrix wvp);
  }

  public interface CameraTarget
  {
    void GetCamera(out Matrix ori, out Vector3 pos, out float distance);
  }
}
