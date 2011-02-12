using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Storage;
using Microsoft.Xna.Framework.Content;

namespace KiloWatt.Animation.Input
{
  /// <summary>
  /// This is a game component that implements IUpdateable.
  /// </summary>
  public class InputState : Microsoft.Xna.Framework.GameComponent
  {
    public InputState(Microsoft.Xna.Framework.Game game, PlayerIndex index, bool editor)
      : base(game)
    {
      UpdateOrder = -10000;
      index_ = index;
      gamePad_ = GamePad.GetState(index);
      everConnected_ = false;
      nowConnected_ = false;
#if !XBOX360
      editor_ = editor;
      if (index == PlayerIndex.One)
      {
        nowConnected_ = true;
        ReadMouse();
      }
#endif
    }
    
    bool everConnected_;
    bool nowConnected_;
#if !XBOX360
    bool editor_;
#endif

    public bool Connected { get { return nowConnected_; } }

    public InputState(Microsoft.Xna.Framework.Game game, bool editor)
      : this(game, PlayerIndex.One, editor)
    {
    }

    GamePadState gamePad_;
    KeyboardState keyboard_;
    KeyboardState oldKeyboard_;
    bool navButtonPressed_;
    bool okButtonPressed_;
    bool backButtonPressed_;

    public bool CommandKeyHit(Keys k) { return keyboard_.IsKeyDown(k) && !oldKeyboard_.IsKeyDown(k); }
    public bool IsKeyPressed(Keys k) { return keyboard_.IsKeyDown(k); }

    public int GetNewPressedKeys(Keys[] oKeys)
    {
      int n = 0;
      Keys[] old = oldKeyboard_.GetPressedKeys();
      Keys[] nu = keyboard_.GetPressedKeys();
      for (int i = 0; i != nu.Length && n != oKeys.Length; ++i)
      {
        bool ok = true;
        Keys key = nu[i];
        for (int j = 0; j != old.Length; ++j)
        {
          if (old[j] == key)
            ok = false;
        }
        if (ok)
          oKeys[n++] = key;
      }
      return n;
    }

#if !XBOX360
    MouseState mouse_;
    ButtonState oldLeftButton_ = ButtonState.Released;
    ButtonState oldRightButton_ = ButtonState.Released;
    Vector2 centerPos_ = new Vector2();
    int oldScrollWheelValue_;

    public bool LeftDown { get { return mouse_.LeftButton == ButtonState.Pressed; } }
    public bool RightDown { get { return mouse_.RightButton == ButtonState.Pressed; } }
    public bool LeftClicked { get { return LeftDown && oldLeftButton_ == ButtonState.Released; } }
    public bool RightClicked { get { return RightDown && oldRightButton_ == ButtonState.Released; } }
    public float MouseDeltaX { get { return mouse_.X - centerPos_.X; } }
    public float MouseDeltaY { get { return mouse_.Y - centerPos_.Y; } }
    public float MouseX { get { return mouse_.X; } }
    public float MouseY { get { return mouse_.Y; } }
    public float MouseWheelDelta { get { return mouse_.ScrollWheelValue - oldScrollWheelValue_; } }
    public bool PrecisionDown { get { return keyboard_.IsKeyDown(Keys.LeftShift) || keyboard_.IsKeyDown(Keys.RightShift)
        || (gamePad_.IsConnected && gamePad_.IsButtonDown(Buttons.RightShoulder)); } }

    void ReadMouse()
    {
      MouseState newMouse = Game.IsActive ? Mouse.GetState() : new MouseState();
      MouseState oldMouse = newMouse;
      if (!editor_) return;
      centerPos_ = new Vector2(oldMouse.X, oldMouse.Y);
      oldScrollWheelValue_ = mouse_.ScrollWheelValue;
      if (!Game.IsMouseVisible && Game.IsActive)
      {
        Mouse.SetPosition(Game.Window.ClientBounds.Width / 2, Game.Window.ClientBounds.Height / 2);
        centerPos_.X = Game.Window.ClientBounds.Width / 2;
        centerPos_.Y = Game.Window.ClientBounds.Height / 2;
      }
      //  make sure to only detect clicks on actual scan
      //  oldMouse is two scans ago at this point
      oldLeftButton_ = oldMouse.LeftButton;
      oldRightButton_ = oldMouse.RightButton;
      //  mouse_ is the previous value
      if (mouse_.LeftButton == ButtonState.Released)
        oldLeftButton_ = ButtonState.Released;
      if (mouse_.RightButton == ButtonState.Released)
        oldRightButton_ = ButtonState.Released;
      //  update mouse_ to current value
      mouse_ = newMouse;
    }
#endif
    PlayerIndex index_;
    static int nextPeriod_ = 1;
    int counter_ = (nextPeriod_ += 19);

    /// <summary>
    /// Allows the game component to perform any initialization it needs to before starting
    /// to run.  This is where it can query for any required services and load content.
    /// </summary>
    public override void Initialize()
    {
      base.Initialize();
    }

    /// <summary>
    /// Allows the game component to update itself.
    /// </summary>
    /// <param name="gameTime">Provides a snapshot of timing values.</param>
    public override void Update(GameTime gameTime)
    {
      base.Update(gameTime);
      gamePad_ = GamePad.GetState(index_);
      bool con = gamePad_.IsConnected;
      if (!Game.IsActive)
        gamePad_ = new GamePadState();
#if !XBOX360
      if (index_ == PlayerIndex.One)
        con = true;
#endif
      if (con)
        everConnected_ = true;
      if (everConnected_)
        nowConnected_ = con;
      oldKeyboard_ = keyboard_;
      keyboard_ = Game.IsActive ? Keyboard.GetState(index_) : new KeyboardState();
#if !XBOX360
      ReadMouse();
#endif
      UpdateDerivedState((float)gameTime.ElapsedGameTime.TotalSeconds);
    }
    
    public bool IsDisconnected { get { return everConnected_ && !nowConnected_; } }

    static float KeyboardSteerScale = 2.0f; 

    class MenuButtons
    {
      internal const uint M_R = 1;
      internal const uint M_L = 2;
      internal const uint M_U = 4;
      internal const uint M_D = 8;
      internal const uint M_A = 16;
      internal const uint M_B = 32;
      internal const uint M_Start = 64;
      internal const uint M_RT = 128;
      internal const uint M_Back = 256;
      internal const uint M_RS = 512;
      internal const uint M_LT = 1024;
      internal const uint M_LS = 2048;
      internal const uint M_X = 4096;
      internal const uint M_Y = 8192;
      //  virtual buttons (synthesized)
      internal const uint MB_LEFT = 0x10000U;
      internal const uint MB_RIGHT = 0x20000U;
      internal const uint MB_UP = 0x40000U;
      internal const uint MB_DOWN = 0x80000;
    }
    
    public uint MakeButtonMask(GamePadState st)
    {
      uint ret = 0;
      if (st.Buttons.A == ButtonState.Pressed)
        ret |= MenuButtons.M_A;
      if (st.Buttons.B == ButtonState.Pressed)
        ret |= MenuButtons.M_B;
      if (st.DPad.Left == ButtonState.Pressed)
        ret |= MenuButtons.M_L | MenuButtons.MB_LEFT;
      if (st.DPad.Right == ButtonState.Pressed)
        ret |= MenuButtons.M_R | MenuButtons.MB_RIGHT;
      if (st.DPad.Down == ButtonState.Pressed)
        ret |= MenuButtons.M_D | MenuButtons.MB_DOWN;
      if (st.DPad.Up == ButtonState.Pressed)
        ret |= MenuButtons.M_U | MenuButtons.MB_UP;
      if (st.Buttons.Start == ButtonState.Pressed)
        ret |= MenuButtons.M_Start;
      if (st.Triggers.Right > 0.4f)
        ret |= MenuButtons.M_RT;
      if (st.Buttons.RightShoulder == ButtonState.Pressed)
        ret |= MenuButtons.M_RS;
      if (st.Buttons.Back == ButtonState.Pressed)
        ret |= MenuButtons.M_Back;
      if (st.Triggers.Left > 0.4f)
        ret |= MenuButtons.M_LT;
      if (st.Buttons.LeftShoulder == ButtonState.Pressed)
        ret |= MenuButtons.M_LS;
      if (st.Buttons.X == ButtonState.Pressed)
        ret |= MenuButtons.M_X;
      if (st.Buttons.Y == ButtonState.Pressed)
        ret |= MenuButtons.M_Y;
      if (st.ThumbSticks.Left.X > 0.5)
        ret |= MenuButtons.MB_RIGHT;
      if (st.ThumbSticks.Left.X < -0.5)
        ret |= MenuButtons.MB_LEFT;
      if (st.ThumbSticks.Left.Y > 0.5)
        ret |= MenuButtons.MB_UP;
      if (st.ThumbSticks.Left.Y < -0.5)
        ret |= MenuButtons.MB_DOWN;
      return ret;
    }

    public uint MakeButtonMask(KeyboardState kb)
    {
      uint ret = 0;
      if (kb.IsKeyDown(Keys.Up)) ret |= MenuButtons.MB_UP;
      if (kb.IsKeyDown(Keys.Left)) ret |= MenuButtons.MB_LEFT;
      if (kb.IsKeyDown(Keys.Down)) ret |= MenuButtons.MB_DOWN;
      if (kb.IsKeyDown(Keys.Right)) ret |= MenuButtons.MB_RIGHT;
      if (kb.IsKeyDown(Keys.I)) ret |= MenuButtons.MB_UP | MenuButtons.M_U;
      if (kb.IsKeyDown(Keys.J)) ret |= MenuButtons.MB_LEFT | MenuButtons.M_L;
      if (kb.IsKeyDown(Keys.K)) ret |= MenuButtons.MB_DOWN | MenuButtons.M_D;
      if (kb.IsKeyDown(Keys.L)) ret |= MenuButtons.MB_RIGHT | MenuButtons.M_R;
      if (kb.IsKeyDown(Keys.Enter)) ret |= MenuButtons.M_A;
      if (kb.IsKeyDown(Keys.Back)) ret |= MenuButtons.M_B;
      if (kb.IsKeyDown(Keys.Tab)) ret |= MenuButtons.M_Start;
      if (kb.IsKeyDown(Keys.Escape)) ret |= MenuButtons.M_Back;
      if (kb.IsKeyDown(Keys.Space)) ret |= MenuButtons.M_RT;
      if (kb.IsKeyDown(Keys.OemOpenBrackets)) ret |= MenuButtons.M_LS;
      if (kb.IsKeyDown(Keys.OemCloseBrackets)) ret |= MenuButtons.M_RS;
      if (kb.IsKeyDown(Keys.OemMinus)) ret |= MenuButtons.M_X;
      if (kb.IsKeyDown(Keys.OemPlus)) ret |= MenuButtons.M_Y;
      if (kb.IsKeyDown(Keys.LeftAlt)) ret |= MenuButtons.M_LT;
      return ret;
    }

    public void UpdateDerivedState(float dt)
    {
      bool touchedKeyboardLeftY = false;
      bool touchedKeyboardRightY = false;
      bool touchedKeyboardLeftX = false;
      bool touchedKeyboardRightX = false;
      rightX_ = gamePad_.ThumbSticks.Right.X;
      if (keyboard_.IsKeyDown(Keys.A))
      {
        touchedKeyboardRightX = true;
        keyboardRightX_ -= dt * KeyboardSteerScale;
      }
      if (keyboard_.IsKeyDown(Keys.D))
      {
        touchedKeyboardRightX = true;
        keyboardRightX_ += dt * KeyboardSteerScale;
      }
      if (keyboardRightX_ < -1) keyboardRightX_ = -1;
      if (keyboardRightX_ > 1) keyboardRightX_ = 1;
      rightX_ += keyboardRightX_;
      if (rightX_ < -1) rightX_ = -1;
      if (rightX_ > 1) rightX_ = 1;
      if (rightX_ > -0.1f && rightX_ < 0.1f) rightX_ = 0;

      rightY_ = gamePad_.ThumbSticks.Right.Y;
      if (keyboard_.IsKeyDown(Keys.Up))
      {
        touchedKeyboardRightY = true;
        keyboardRightY_ += dt * KeyboardSteerScale;
      }
      if (keyboard_.IsKeyDown(Keys.Down))
      {
        touchedKeyboardRightY = true;
        keyboardRightY_ -= dt * KeyboardSteerScale;
      }
      if (keyboardRightY_ < -1) keyboardRightY_ = -1;
      if (keyboardRightY_ > 1) keyboardRightY_ = 1;
      rightY_ += keyboardRightY_;
      if (rightY_ < -1) rightY_ = -1;
      if (rightY_ > 1) rightY_ = 1;
      if (rightY_ > -0.1f && rightY_ < 0.1f) rightY_ = 0;

      leftX_ = gamePad_.ThumbSticks.Left.X;
      if (keyboard_.IsKeyDown(Keys.Right))
      {
        touchedKeyboardLeftX = true;
        keyboardLeftX_ += dt * KeyboardSteerScale;
      }
      if (keyboard_.IsKeyDown(Keys.Left))
      {
        touchedKeyboardLeftX = true;
        keyboardLeftX_ -= dt * KeyboardSteerScale;
      }
      if (keyboardLeftX_ < -1) keyboardLeftX_ = -1;
      if (keyboardLeftX_ > 1) keyboardLeftX_ = 1;
      leftX_ += keyboardLeftX_;
      if (leftX_ < -1) leftX_ = -1;
      if (leftX_ > 1) leftX_ = 1;
      if (leftX_ > -0.1f && leftX_ < 0.1f) leftX_ = 0;

      float l = leftX_*leftX_ + rightY_*rightY_;
      if (l > 1)
      {
        l = 1.0f / (float)Math.Sqrt(l);
        leftX_ *= l;
        rightY_ *= l;
      }

      leftY_ = gamePad_.ThumbSticks.Left.Y;
      if (keyboard_.IsKeyDown(Keys.W))
      {
        touchedKeyboardLeftY = true;
        keyboardLeftY_ += dt * KeyboardSteerScale;
      }
      if (keyboard_.IsKeyDown(Keys.S))
      {
        touchedKeyboardLeftY = true;
        keyboardLeftY_ -= dt * KeyboardSteerScale;
      }
      if (keyboardLeftY_ < -1) keyboardLeftY_ = -1;
      if (keyboardLeftY_ > 1) keyboardLeftY_ = 1;
      leftY_ += keyboardLeftY_;
      if (leftY_ < -1) leftY_ = -1;
      if (leftY_ > 1) leftY_ = 1;
      if (leftY_ > -0.1f && leftY_ < 0.1f) leftY_ = 0;

      if (keyboardLeftY_ != 0 && !touchedKeyboardLeftY)
      {
        float d = dt * KeyboardSteerScale;
        if (Math.Abs(keyboardLeftY_) < d)
          keyboardLeftY_ = 0;
        else if (keyboardLeftY_ > 0)
          keyboardLeftY_ -= d;
        else
          keyboardLeftY_ += d;
      }
      if (keyboardRightY_ != 0 && !touchedKeyboardRightY)
      {
        float d = dt * KeyboardSteerScale;
        if (Math.Abs(keyboardRightY_) < d)
          keyboardRightY_ = 0;
        else if (keyboardRightY_ > 0)
          keyboardRightY_ -= d;
        else
          keyboardRightY_ += d;
      }
      if (keyboardLeftX_ != 0 && !touchedKeyboardLeftX)
      {
        float d = dt * KeyboardSteerScale;
        if (Math.Abs(keyboardLeftX_) < d)
          keyboardLeftX_ = 0;
        else if (keyboardLeftX_ > 0)
          keyboardLeftX_ -= d;
        else
          keyboardLeftX_ += d;
      }
      if (keyboardRightX_ != 0 && !touchedKeyboardRightX)
      {
        float d = dt * KeyboardSteerScale;
        if (Math.Abs(keyboardRightX_) < d)
          keyboardRightX_ = 0;
        else if (keyboardRightX_ > 0)
          keyboardRightX_ -= d;
        else
          keyboardRightX_ += d;
      }

      if (keyboard_.IsKeyDown(Keys.Q))
        keyboardTriggerLeft_ += dt * KeyboardSteerScale;
      else
        keyboardTriggerLeft_ -= dt * KeyboardSteerScale;
      if (keyboardTriggerLeft_ > 1) keyboardTriggerLeft_ = 1;
      if (keyboardTriggerLeft_ < 0) keyboardTriggerLeft_ = 0;

      if (keyboard_.IsKeyDown(Keys.E))
        keyboardTriggerRight_ += dt * KeyboardSteerScale;
      else
        keyboardTriggerRight_ -= dt * KeyboardSteerScale;
      if (keyboardTriggerRight_ > 1) keyboardTriggerRight_ = 1;
      if (keyboardTriggerRight_ < 0) keyboardTriggerRight_ = 0;

      gasBrake_ = gamePad_.Triggers.Right - gamePad_.Triggers.Left + keyboardTriggerRight_ - keyboardTriggerLeft_;
      if (gasBrake_ < -1) gasBrake_ = -1;
      if (gasBrake_ > 1) gasBrake_ = 1;

      uint buttons = MakeButtonMask(gamePad_) | MakeButtonMask(keyboard_);
      curButtons_ = buttons & ~oldButtons_;
      oldButtons_ = buttons;
      if ((curButtons_ & MenuButtons.M_Start) != 0)
        Console.WriteLine("Got start button for {0}", index_);

      okButtonPressed_ = (curButtons_ & (uint)(MenuButtons.M_A | MenuButtons.M_X | MenuButtons.M_Start)) != 0;
      backButtonPressed_ = (curButtons_ & (uint)(MenuButtons.M_B | MenuButtons.M_Y | MenuButtons.M_Back)) != 0;
      navButtonPressed_ = ((curButtons_ != 0) && !okButtonPressed_ && !backButtonPressed_);
    }
    
    public bool NavButtonPressed { get { return navButtonPressed_; } }
    public bool OkButtonPressed { get { return okButtonPressed_; } }
    public bool BackButtonPressed { get { return backButtonPressed_; } }

    /// <summary>
    /// Used only for menu navigation (different on Xbox and Windows)
    /// </summary>
    public bool MenuLeft { get { return (curButtons_ & MenuButtons.MB_LEFT) != 0; } }
    /// <summary>
    /// Used only for menu navigation (different on Xbox and Windows)
    /// </summary>
    public bool MenuRight { get { return (curButtons_ & MenuButtons.MB_RIGHT) != 0; } }
    /// <summary>
    /// Used only for menu navigation (different on Xbox and Windows)
    /// </summary>
    public bool MenuUp { get { return (curButtons_ & MenuButtons.MB_UP) != 0; } }
    /// <summary>
    /// Used only for menu navigation (different on Xbox and Windows)
    /// </summary>
    public bool MenuDown { get { return (curButtons_ & MenuButtons.MB_DOWN) != 0; } }
    public bool MenuUpPressed { get { return (oldButtons_ & MenuButtons.MB_UP) != 0; } }
    public bool MenuDownPressed { get { return (oldButtons_ & MenuButtons.MB_DOWN) != 0; } }
    public bool MenuSelect { get { return (curButtons_ & (MenuButtons.M_A | MenuButtons.M_Start)) != 0; } }
    public bool MenuCancel { get { return (curButtons_ & (MenuButtons.M_B | MenuButtons.M_Back)) != 0; } }
    public bool GameQuit { get { return (curButtons_ & MenuButtons.M_Back) != 0; } }
    public bool GameStart { get { return (curButtons_ & MenuButtons.M_Start) != 0; } }
    public bool Trigger { get { return (curButtons_ & MenuButtons.M_RT) != 0; } }
    public bool AltTrigger { get { return (curButtons_ & MenuButtons.M_LT) != 0; } }
    public bool SwitchLeft { get { return (curButtons_ & MenuButtons.M_LS) != 0; } }
    public bool SwitchLeftContinuous { get { return (oldButtons_ & MenuButtons.M_LS) != 0; } }
    public bool SwitchRight { get { return (curButtons_ & MenuButtons.M_RS) != 0; } }
    public bool SwitchRightContinuous { get { return (oldButtons_ & MenuButtons.M_RS) != 0; } }
    public bool Activate { get { return (curButtons_ & MenuButtons.M_A) != 0; } }
    public bool Deactivate { get { return (curButtons_ & MenuButtons.M_B) != 0; } }
    public bool TriggerContinuous { get { return (oldButtons_ & MenuButtons.M_RT) != 0; } }
    public bool AltTriggerContinuous { get { return (oldButtons_ & MenuButtons.M_LT) != 0; } }
    public bool YellowButton { get { return (curButtons_ & MenuButtons.M_Y) != 0; } }
    public bool YellowButtonContinuous { get { return (oldButtons_ & MenuButtons.M_Y) != 0; } }
    public bool BlueButton { get { return (curButtons_ & MenuButtons.M_X) != 0; } }
    public bool BlueButtonContinuous { get { return (oldButtons_ & MenuButtons.M_X) != 0; } }

    uint oldButtons_;
    uint curButtons_;
    float keyboardRightX_ = 0;
    float rightX_ = 0;
    float keyboardRightY_ = 0;
    float rightY_ = 0;
    float keyboardLeftX_ = 0;
    float leftX_ = 0;
    float keyboardLeftY_ = 0;
    float leftY_ = 0;
    float gasBrake_ = 0;
    float keyboardTriggerLeft_ = 0;
    float keyboardTriggerRight_ = 0;
    /// <summary>
    /// RightX is for looking or turning. LeftX is for strafing.
    /// </summary>
    public float RightX { get { return -rightX_; } set { rightX_ = -value; } }
    /// <summary>
    /// RightY is for looking.
    /// </summary>
    public float RightY { get { return rightY_; } set { rightY_ = value; } }
    /// <summary>
    /// LeftY is for movement.
    /// </summary>
    public float LeftY { get { return leftY_; } set { leftY_ = value; } }
    /// <summary>
    /// GasBrake is for vehicle controls (warning: the triggers are used!)
    /// </summary>
    public float GasBrake { get { return gasBrake_; } set { gasBrake_ = value; } }
    /// <summary>
    /// LeftX is for strafing; RightX is for turning. Sadness.
    /// </summary>
    public float LeftX { get { return leftX_; } set { leftX_ = value; } }
  }
}
