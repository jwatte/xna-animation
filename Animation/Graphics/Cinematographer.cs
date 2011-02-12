using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Xna.Framework;

namespace KiloWatt.Animation.Graphics
{
  public interface Cinematographer : CameraBase
  {
    void AddCameraTarget(CameraTarget target, float priority);
    void RemoveCameraTarget(CameraTarget target);
    void AddPriorityShot(CameraTarget target, float priority, float duration);
    void Update(float dt);
    CameraBase Camera { get; }
  }
}
