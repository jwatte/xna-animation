using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Net;
using Microsoft.Xna.Framework.Storage;

using KiloWatt.Animation.Graphics;
using KiloWatt.Animation.Animation;

namespace AnimationViewer
{
  /// <summary>
  /// This is the main type for your game
  /// </summary>
  public class AnimationViewer : Microsoft.Xna.Framework.Game, IScene
  {
    GraphicsDeviceManager graphics;
    SpriteBatch spriteBatch;
    public static AnimationViewer Global;

    public AnimationViewer()
    {
      Global = this;
      graphics = new GraphicsDeviceManager(this);
#if XBOX360
      graphics.PreferredBackBufferWidth = 1280;
      graphics.PreferredBackBufferHeight = 720;
#else
      graphics.PreferredBackBufferWidth = 1024;
      graphics.PreferredBackBufferHeight = 576;
#endif
      graphics.SynchronizeWithVerticalRetrace = true;
      Content.RootDirectory = "Content";
    }

    /// <summary>
    /// Allows the game to perform any initialization it needs to before starting to run.
    /// This is where it can query for any required services and load any non-graphic
    /// related content.  Calling base.Initialize will enumerate through any components
    /// and initialize them as well.
    /// </summary>
    protected override void Initialize()
    {
      base.Initialize();
#if !XBOX360
      Mouse.SetPosition(GraphicsDevice.PresentationParameters.BackBufferWidth / 2,
          GraphicsDevice.PresentationParameters.BackBufferHeight / 2);
      curMouse_ = prevMouse_ = Mouse.GetState();
#endif
      debugLines_ = new DebugLines(GraphicsDevice);
    }

    DebugLines debugLines_;
    SpriteFont font_;

    /// <summary>
    /// LoadContent will be called once per game and is the place to load
    /// all of your content.
    /// </summary>
    protected override void LoadContent()
    {
      // Create a new SpriteBatch, which can be used to draw textures.
      spriteBatch = new SpriteBatch(GraphicsDevice);

      SetPath("");
      showingBrowser_ = true;
      font_ = Content.Load<SpriteFont>("Tw Cen MT");
    }

    /// <summary>
    /// UnloadContent will be called once per game and is the place to unload
    /// all content.
    /// </summary>
    protected override void UnloadContent()
    {
      // TODO: Unload any non ContentManager content here
    }

    GamePadState prevGamePad_;
    GamePadState curGamePad_;
    KeyboardState prevKeyboard_;
    KeyboardState curKeyboard_;
#if !XBOX360
    MouseState prevMouse_;
    MouseState curMouse_;
#endif

    /// <summary>
    /// Allows the game to run logic such as updating the world,
    /// checking for collisions, gathering input, and playing audio.
    /// </summary>
    /// <param name="gameTime">Provides a snapshot of timing values.</param>
    protected override void Update(GameTime gameTime)
    {
      prevGamePad_ = curGamePad_;
      curGamePad_ = GamePad.GetState(PlayerIndex.One);
      prevKeyboard_ = curKeyboard_;
      curKeyboard_ = Keyboard.GetState(PlayerIndex.One);
#if !XBOX360
      prevMouse_ = curMouse_;
      curMouse_ = Mouse.GetState();
      int wid = GraphicsDevice.PresentationParameters.BackBufferWidth / 2;
      int hei = GraphicsDevice.PresentationParameters.BackBufferHeight / 2;
      Mouse.SetPosition(wid, hei);

      if (curMouse_.RightButton == ButtonState.Pressed)
      {
        if (curMouse_.LeftButton == ButtonState.Pressed)
        {
          //  reset all
          pan_ = Vector3.Zero;
          relativeDistance_ = 0;
          Heading = 0;
          Pitch = 0;
        }
        else
        {
          Heading = Heading + (curMouse_.X - wid) * 0.025f;
          Pitch = Pitch + (curMouse_.Y - hei) * 0.025f;
        }
      }
      else if (curMouse_.LeftButton == ButtonState.Pressed)
      {
        pan_ = pan_ + viewInv_.Right * (curMouse_.X - wid) * 0.025f + 
            viewInv_.Down * (curMouse_.Y - hei) * 0.025f;
      }
      float dWheel = (curMouse_.ScrollWheelValue - prevMouse_.ScrollWheelValue) * -0.001f;
      if (dWheel != 0)
        relativeDistance_ += dWheel;
#endif

      float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
      if (dt > 0.1f) dt = 0.1f;

      Heading = Heading + curGamePad_.ThumbSticks.Left.X * dt * 5;
      if (Heading < -(float)Math.PI) Heading = Heading + 2 * (float)Math.PI;
      if (Heading > (float)Math.PI) Heading = Heading - 2 * (float)Math.PI;

      Pitch = Pitch + curGamePad_.ThumbSticks.Left.Y * dt * 5;
      if (Pitch < -1.5f) Pitch = -1.5f;
      if (Pitch > 1.5f) Pitch = 1.5f;

      relativeDistance_ = relativeDistance_ + curGamePad_.ThumbSticks.Right.Y * dt * -2;

      if (relativeDistance_ < -10f)
        relativeDistance_ = -10f;
      if (relativeDistance_ > 10f)
        relativeDistance_ = 10f;

      if (showingBrowser_)
      {
        if (ButtonPressed(Buttons.A) || ButtonPressed(Buttons.Start)
            || KeyPressed(Keys.Space) || KeyPressed(Keys.Enter))
        {
          if (CurEntryIsDir)
          {
            SetPath(System.IO.Path.Combine(CurPath, CurEntryName));
          }
          else
          {
            LoadModel(System.IO.Path.Combine(CurPath, CurEntryName));
          }
          showingBrowser_ = false;
        }
        else if (ButtonPressed(Buttons.B) || ButtonPressed(Buttons.Back)
            || KeyPressed(Keys.Escape) || KeyPressed(Keys.Back))
        {
          showingBrowser_ = false;
        }
        else if (CurEntry < NumEntries-1 &&
            (ButtonPressed(Buttons.DPadDown)
            || KeyPressed(Keys.Down)))
        {
          CurEntry = CurEntry + 1;
        }
        else if (CurEntry > 0 &&
            (ButtonPressed(Buttons.DPadUp)
            || KeyPressed(Keys.Up)))
        {
          CurEntry = CurEntry - 1;
        }
        else if (ButtonPressed(Buttons.Y)
            || KeyPressed(Keys.PageUp))
        {
          int ind = CurPath.LastIndexOf(System.IO.Path.DirectorySeparatorChar);
          if (ind > 0)
            SetPath(CurPath.Substring(0, ind));
        }
      }
      else if (ButtonPressed(Buttons.A) || ButtonPressed(Buttons.Start)
          || KeyPressed(Keys.Space) || KeyPressed(Keys.Enter))
      {
        showingBrowser_ = true;
      }
      else if (ButtonPressed(Buttons.Back)
          || KeyPressed(Keys.Escape))
      {
        Exit();
      }
      else if (instances_ != null
          && curAnimationInstance_ < instances_.Length - 1
          && (ButtonPressed(Buttons.RightShoulder)
              || KeyPressed(Keys.OemCloseBrackets)))
      {
        curAnimationInstance_ += 1;
        Message = String.Format("{0} : {1}", loadedModel_.Name, instances_[curAnimationInstance_].Animation.Name);
        if (curAnimationInstance_ > 0)
          instances_[curAnimationInstance_].Time = instances_[curAnimationInstance_-1].Time;
        blender_.TransitionAnimations(GetBlended(curAnimationInstance_-1), GetBlended(curAnimationInstance_), 1.0f);
      }
      else if (instances_ != null
          && curAnimationInstance_ > -1
          && (ButtonPressed(Buttons.LeftShoulder)
              || KeyPressed(Keys.OemOpenBrackets)))
      {
        curAnimationInstance_ -= 1;
        if (curAnimationInstance_ >= 0)
          Message = String.Format("{0} : {1}", loadedModel_.Name, instances_[curAnimationInstance_].Animation.Name);
        else
          Message = loadedModel_.Name;
        if (curAnimationInstance_ > 0)
          instances_[curAnimationInstance_].Time = instances_[curAnimationInstance_ - 1].Time;
        blender_.TransitionAnimations(GetBlended(curAnimationInstance_ + 1), GetBlended(curAnimationInstance_), 1.0f);
      }
      if (blender_ != null)
        blender_.Advance(dt);

      base.Update(gameTime);
    }
    
    IBlendedAnimation GetBlended(int ix)
    {
      unchecked
      {
        return (ix < 0) ? null : (ix >= blended_.Length) ? null : blended_[ix];
      }
    }

    public bool ButtonPressed(Buttons b)
    {
      return !prevGamePad_.IsButtonDown(b) && curGamePad_.IsButtonDown(b);
    }
    
    public bool KeyPressed(Keys k)
    {
      return !prevKeyboard_.IsKeyDown(k) && curKeyboard_.IsKeyDown(k);
    }

    DrawDetails drawDetails_ = new DrawDetails();

    /// <summary>
    /// This is called when the game should draw itself.
    /// </summary>
    /// <param name="gameTime">Provides a snapshot of timing values.</param>
    protected override void Draw(GameTime gameTime)
    {
      graphics.GraphicsDevice.Clear(Color.DarkKhaki);

      //  Set up camera parameters
      float d = baseDistance_ * (float)(Math.Pow(2, relativeDistance_ - 1));
      view_ = Matrix.CreateTranslation(pan_) * Matrix.CreateRotationY(Heading) *
          Matrix.CreateRotationX(Pitch) * Matrix.CreateTranslation(0, 0, -d);
      float near = d * 0.25f;
      projection_ = Matrix.CreatePerspective(1.6f * zoom_ * near, 0.9f * zoom_ * near, near, d * 10.0f + 10.0f);
      viewInv_ = Matrix.Invert(view_);

      //  if I have a model, set up the scene drawing parameters and draw the model
      if (loadedModel_ != null)
      {
        //  the drawdetails set-up can be re-used for all items in the scene
        DrawDetails dd = drawDetails_;
        dd.dev = GraphicsDevice;
        dd.fogColor = new Vector4(0.5f, 0.5f, 0.5f, 1);
        dd.fogDistance = 10 * baseDistance_;
        dd.lightAmbient = new Vector4(0.2f, 0.2f, 0.2f, 1.0f);
        dd.lightDiffuse = new Vector4(0.8f, 0.8f, 0.8f, 0);
        dd.lightDir = Vector3.Normalize(new Vector3(1, 3, 2));

        dd.viewInv = viewInv_;
        dd.viewProj = view_ * projection_;
        dd.world = Matrix.Identity;

        //  draw the loaded model (the only model I have)
        loadedModel_.ScenePrepare(dd);
        if (loadedModel_.SceneDraw(dd, 0))
        {
            loadedModel_.SceneDrawTransparent(dd, 0);
        }
      }
      //  when everything else is drawn, Z sort and draw the transparent parts

      //  draw any components
      base.Draw(gameTime);

      //  draw any visualization lines
      debugLines_.Draw(view_, projection_);
      debugLines_.Reset();

      //  draw text information
      spriteBatch.Begin();

      spriteBatch.DrawString(font_, Message,
          new Vector2(30, GraphicsDevice.PresentationParameters.BackBufferHeight - 40),
          Color.White);
      spriteBatch.DrawString(font_, String.Format("Pan: {0} Distance: {1}", pan_, d),
          new Vector2(30, GraphicsDevice.PresentationParameters.BackBufferHeight - 65),
          Color.White);

      //  draw the very primitive file browser
      if (ShowingBrowser)
      {
        spriteBatch.DrawString(font_, CurPath, new Vector2(30, 30), Color.LightYellow);
        float x = GraphicsDevice.PresentationParameters.BackBufferWidth / 2;
        for (int i = 0; i < entries_.Count; ++i)
        {
          spriteBatch.DrawString(font_, entries_[i].Name,
              new Vector2(x, i * 30 + 30),
              i == CurEntry ? Color.Red : entries_[i].IsDir ? Color.LightYellow : Color.DarkBlue);
        }
      }

      spriteBatch.End();
    }

    internal class CaseInsensitiveComparer : System.Collections.IComparer
    {
      public int Compare(object x, object y)
      {
        return String.Compare((string)x, (string)y, true);
      }
    }
    
    void SetPath(string path)
    {
      curPath_ = path;
      entries_.Clear();
      string[] dirs = System.IO.Directory.GetDirectories(System.IO.Path.Combine(Content.RootDirectory, path));
      string[] files = System.IO.Directory.GetFiles(System.IO.Path.Combine(Content.RootDirectory, path));
      System.Collections.IComparer cic = new CaseInsensitiveComparer();
      Array.Sort(dirs, cic);
      Array.Sort(files, cic);
      foreach (string dir in dirs)
        if (dir[0] != '.' && dir.IndexOf('%') < 0)
          entries_.Add(new FileEntry(dir, true));
      foreach (string file in files)
        if (file[0] != '.' && file.IndexOf('%') < 0)
          if (IsInstanceOf<Model>(file))
            entries_.Add(new FileEntry(file, false));
      curEntry_ = 0;
    }

    void LoadModel(string path)
    {
      try
      {
        //  the file name that comes from the browser contains .xnb
        if (path.EndsWith(".xnb"))
          path = path.Substring(0, path.Length - 4);

        //  load the model from disk, and prepare it for animation
        loadedModel_ = new ModelDraw(Content.Load<Model>(path), System.IO.Path.GetFileName(path));
        loadedModel_.Attach(this);

        //  create a blender that can compose the animations for transition
        blender_ = new AnimationBlender(loadedModel_.Model, loadedModel_.Name);
        loadedModel_.CurrentAnimation = blender_;

        //  remember things about this model
        ResetModelData(path);

        //  figure out what the animations are, if any.
        LoadAnimations();
      }
      catch (System.Exception x)
      {
        // couldn't load model
        Message = x.Message;
        System.Diagnostics.Debug.WriteLine(Message);
      }
    }
    
    void ResetModelData(string path)
    {
      Heading = 0;
      Pitch = 0;
      RelativeDistance = 0;
      modelSize_ = loadedModel_.Bounds;
      baseDistance_ = (modelSize_.Center.Length() + modelSize_.Radius) * 2;
      modelPath_ = path;
      Message = String.Format("Viewing {0}", modelPath_);
      animations_ = null;
    }

    internal struct FileEntry
    {
      public FileEntry(string name, bool isDir)
      {
        int i = name.LastIndexOf(System.IO.Path.DirectorySeparatorChar);
        if (i <= 0)
          Name = name;
        else
          Name = name.Substring(i+1);
        IsDir = isDir;
      }
      public string Name;
      public bool IsDir;
    }
    
    public static bool IsInstanceOf<T>(string name) where T : class
    {
      ContentManager mgr = new ContentManager(AnimationViewer.Global.Services);
      try
      {
        int ind = name.LastIndexOf(System.IO.Path.DirectorySeparatorChar);
        mgr.RootDirectory = name.Substring(0, ind);
        String toLoad = name.Substring(ind + 1, name.Length - (ind + 5));
        object o = mgr.Load<object>(toLoad);
        Console.WriteLine("{0} is a {1}", name, o.GetType().Name);
        if (typeof(T).IsAssignableFrom(o.GetType()))
          return true;
      }
      catch (System.Exception x)
      {
        Console.WriteLine("{0} threw {1}", name, x.Message);
      }
      finally
      {
        mgr.Dispose();
      }
      return false;
    }

    void LoadAnimations()
    {
      //  clear current state
      curAnimationInstance_ = -1;
      instances_ = null;
      blended_ = null;

      //  get the list of animations from our dictionary
      Dictionary<string, object> tag = loadedModel_.Model.Tag as Dictionary<string, object>;
      object aobj = null;
      if (tag != null)
        tag.TryGetValue("AnimationSet", out aobj);
      animations_ = aobj as AnimationSet;

      //  set up animations
      if (animations_ != null)
      {
        instances_ = new AnimationInstance[animations_.NumAnimations];
        //  I'll need a BlendedAnimation per animation, so that I can let the 
        //  blender object transition between them.
        blended_ = new IBlendedAnimation[instances_.Length];
        int ix = 0;
        foreach (Animation a in animations_.Animations)
        {
          instances_[ix] = new AnimationInstance(a);
          blended_[ix] = AnimationBlender.CreateBlendedAnimation(instances_[ix]);
          ++ix;
        }
      }
    }

    ModelDraw loadedModel_;             //  loaded geometry
    BoundingSphere modelSize_;          //  calculated size of the model
    AnimationSet animations_;           //  the animations I have to choose from
    AnimationInstance[] instances_;     //  the animation data, as loaded
    IBlendedAnimation[] blended_;       //  state about the different animations (that can change)
    AnimationBlender blender_;          //  object that blends between playing animations
    int curAnimationInstance_ = -1;     //  which animation is playing? (-1 for none)


    // camera support
    float heading_;
    public float Heading { get { return heading_; } set { heading_ = value; } }
    float pitch_;
    public float Pitch { get { return pitch_; } set { pitch_ = value; } }
    float relativeDistance_;
    public float RelativeDistance { get { return relativeDistance_; } set { relativeDistance_ = value; } }
    float zoom_ = 0.7f;
    public float Zoom { get { return zoom_; } set { zoom_ = value; } }
    Vector3 pan_;
    public Vector3 Pan { get { return pan_; } set { pan_ = value; } }
    float baseDistance_ = 1;

    Matrix view_;
    Matrix projection_;
    Matrix viewInv_;

    //  file browser support

    string modelPath_;
    public string ModelPath { get { return modelPath_; } }

    bool showingBrowser_;
    public bool ShowingBrowser { get { return showingBrowser_; } set { showingBrowser_ = value; } }

    string curPath_ = "";
    public string CurPath { get { return curPath_; } set { curPath_ = value; } }

    List<FileEntry> entries_ = new List<FileEntry>();
    int curEntry_;
    public int CurEntry { get { return curEntry_; } set { curEntry_ = value; } }

    public int NumEntries { get { return entries_.Count; } }

    public string CurEntryName { get { return entries_[curEntry_].Name; } }

    public bool CurEntryIsDir { get { return entries_[curEntry_].IsDir; } }

    //  the message displayed at the bottom
    string message_ = "Art is only licensed for inclusion with AnimationViewer.";
    public string Message { get { return message_; } set { message_ = value; } }

    #region IScene Members

    public int TechniqueIndex(string technique)
    {
        return 0;
    }

    public void AddRenderable(ISceneRenderable sr)
    {
        throw new NotImplementedException();
    }

    public void RemoveRenderable(ISceneRenderable sr)
    {
        throw new NotImplementedException();
    }

    public ISceneTexture GetSceneTexture()
    {
        throw new NotImplementedException();
    }

    public void Clear()
    {
        throw new NotImplementedException();
    }

    #endregion
  }

}
