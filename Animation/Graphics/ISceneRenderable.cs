using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using KiloWatt.Animation.Animation;

namespace KiloWatt.Animation.Graphics
{
  public interface ISceneRenderable
  {
    void Attach(IScene scene);
    void Detach();
    //  TODO: perhaps return bit mask of passes it wants to draw in?
    void ScenePrepare(DrawDetails dd);
    /// <summary>
    /// Draw your opaque stuff to the device for the given technique (RenderScene or Shadow).
    /// The technique ID is looked up ahead of time through IScene.TechniqueIndex().
    /// </summary>
    /// <param name="dd">Details about drawing</param>
    /// <param name="technique">The specific technique to issue for</param>
    /// <returns>TRUE if you have transparent data for this technique</returns>
    bool SceneDraw(DrawDetails dd, int technique);
    void SceneDrawTransparent(DrawDetails dd, int technique);
    Matrix Transform { get; set; }
    BoundingSphere Bounds { get; }
  }

}
