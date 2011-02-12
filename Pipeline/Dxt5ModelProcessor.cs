using System;
using System.Collections.Generic;
using System.ComponentModel;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline;
using Microsoft.Xna.Framework.Content.Pipeline.Serialization.Compiler;
using Microsoft.Xna.Framework.Content.Pipeline.Graphics;
using Microsoft.Xna.Framework.Content.Pipeline.Processors;

namespace LevelProcessor
{
  /// <summary>
  /// This ModelProcessor subclass fixes some problems with DXT and transparency 
  /// usage in the original ModelProcessor. Specifically, if there is full 
  /// transparency, but with color, in the source texture, the stock ModelProcessor
  /// will generate DXT1 transparent data, which will be all black. This processor 
  /// just forces all textures to DXT5, which is generally better behavior.
  ///
  /// Additionally, this processor can apply new shaders to geometry based on whether 
  /// it is skinned (has blend weights) or not.
  /// </summary>
  [ContentProcessor(DisplayName = "Dxt5 Model Processor - KiloWatt")]
  public class Dxt5ModelProcessor : ModelProcessor
  {
    public Dxt5ModelProcessor()
    {
      //  default to ColorKeyEnabled off
      this.ColorKeyEnabled = false;
      foundSkinning_ = false;
    }

    /// <summary>
    /// If set, force all textures to be DXT5 format.
    /// </summary>
    [DefaultValue(true)]
    [Description("Force texture compression DXT5 to be used.")]
    public bool ForceDXT5 { get { return forceDxt5_; } set { forceDxt5_ = value; } }
    bool forceDxt5_ = true;

    bool foundSkinning_;
    protected bool FoundSkinning { get { return foundSkinning_; } }

    /// <summary>
    /// If non-empty, set all materials for all non-skinned geometry to use the given shader.
    /// If the string starts with a "+," use the base name of the existing shader and add the 
    /// name of this shader.
    /// </summary>
    [DefaultValue("")]
    [Description("Shader name to force on materials; + to append.")]
    public string ForceShader { get { return forceShader_; } set { forceShader_ = value; } }
    string forceShader_ = "";

    /// <summary>
    /// If non-empty, set all materials for all skinned geometry to use the given shader.
    /// If the string starts with a "+," use the base name of the existing shader and add the 
    /// name of this shader.
    /// </summary>
    [DefaultValue("")]
    [Description("Shader name to force on skinned geometry; + to append.")]
    public string ForceSkinnedShader { get { return forceSkinnedShader_; } set { forceSkinnedShader_ = value; } }
    string forceSkinnedShader_ = "";

    public override ModelContent Process(NodeContent input, ContentProcessorContext context)
    {
      foundSkinning_ = false;
      if (!forceShader_.Equals("") || !forceSkinnedShader_.Equals(""))
        ReplaceShaders(input, context, input.Identity);
      ModelContent ret = base.Process(input, context);
      if (ret.Tag == null)
        ret.Tag = new Dictionary<string, object>();
      SetTexturePaths(ret);
      CalculateBoundingBoxes(ret, context.TargetPlatform);
      return ret;
    }

    public static void CalculateBoundingBoxes(ModelContent output, TargetPlatform platform)
    {
      BoundingBox bb = new BoundingBox();
      bool bbFirst = true;
      foreach (var mesh in output.Meshes)
      {
        Vector3 min = Vector3.Zero;
        Vector3 max = Vector3.Zero;
        bool first = true;
        foreach (var part in mesh.MeshParts)
        {
          unsafe
          {
            fixed (byte* b = part.VertexBuffer.VertexData)
            {
              int offset = 0;
              int stride = 0;
              foreach (VertexElement ve in part.VertexBuffer.VertexDeclaration.VertexElements)
              {
                if (ve.UsageIndex == 0 && ve.VertexElementUsage == VertexElementUsage.Position)
                {
                  if (ve.VertexElementFormat != VertexElementFormat.Vector3)
                  {
                    throw new InvalidContentException(
                        String.Format("Can't deal with vertex element format {0} for Position0!",
                            ve.VertexElementFormat));
                  }
                  offset = ve.Offset;
                }
                int s = KiloWatt.Animation.Graphics.VertexDeclarationContent.FormatSize(ve.VertexElementFormat)
                    + ve.Offset;
                if (s > stride)
                  stride = s;
              }
              offset += part.VertexOffset;

              for (int i = 0; i < part.NumVertices; ++i)
              {
                Vector3 v = *(Vector3*)(b + offset + i * stride);
                if (platform == TargetPlatform.Xbox360)
                {
                  Vector3 *v3 = &v;
                  {
                    //  swap the data
                    byte *c = (byte *)v3;
                    byte x = c[0];
                    c[0] = c[3];
                    c[3] = x;
                    x = c[1];
                    c[1] = c[2];
                    c[2] = x;
                    c += 4;
                    x = c[0];
                    c[0] = c[3];
                    c[3] = x;
                    x = c[1];
                    c[1] = c[2];
                    c[2] = x;
                    c += 4;
                    x = c[0];
                    c[0] = c[3];
                    c[3] = x;
                    x = c[1];
                    c[1] = c[2];
                    c[2] = x;
                  }
                }
                if (first)
                {
                  min = v;
                  max = v;
                  first = false;
                }
                else
                {
                  min = Vector3.Min(min, v);
                  max = Vector3.Max(max, v);
                }
              }
            }
          }
        }
        if (mesh.Tag == null)
        {
          mesh.Tag = new Dictionary<string, object>();
        }
        BoundingBox bbx = new BoundingBox(min, max);
        ((Dictionary<string, object>)mesh.Tag).Add("BoundingBox", bbx);
        Transform(ref bbx, mesh.ParentBone);
        if (bbFirst)
        {
          bb = bbx;
          bbFirst = false;
        }
        else
        {
          bb = BoundingBox.CreateMerged(bb, bbx);
        }
      }
      ((Dictionary<string, object>)output.Tag).Add("BoundingBox", bb);
    }

    static Vector3[] corners = new Vector3[8];

    static Matrix WorldTransform(ModelBoneContent bone)
    {
      Matrix m = bone.Transform;
      while (bone.Parent != null && bone.Parent != bone)
      {
        bone = bone.Parent;
        m = m * WorldTransform(bone);
      }
      return m;
    }

    static void Transform(ref BoundingBox bbx, ModelBoneContent bone)
    {
      bbx.GetCorners(corners);
      Matrix m = WorldTransform(bone);
      Vector3.Transform(corners, ref m, corners);
      bbx = BoundingBox.CreateFromPoints(corners);
    }

    
    /// <summary>
    /// Recurse the entire node structure, looking for materials that may need its 
    /// shader replacing.
    /// </summary>
    /// <param name="input">The node hierarchy to replace shaders in.</param>
    /// <param name="context">To generate errors.</param>
    protected virtual void ReplaceShaders(NodeContent input, ContentProcessorContext context,
        ContentIdentity identity)
    {
      MeshContent mc = input as MeshContent;
      if (mc != null)
        ReplaceShaders(mc, context, identity);
      foreach (NodeContent child in input.Children)
        ReplaceShaders(child, context, identity);
    }
    
    protected virtual void SetTexturePaths(ModelContent model)
    {
      foreach (ModelMeshContent mmc in model.Meshes)
      {
        foreach (ModelMeshPartContent mmpc in mmc.MeshParts)
        {
          Dictionary<string, object> tag = mmpc.Tag as Dictionary<string, object>;
          if (tag == null)
          {
            tag = new Dictionary<string,object>();
            mmpc.Tag = tag;
          }
          foreach (KeyValuePair<string, ExternalReference<TextureContent>> kvp
              in mmpc.Material.Textures)
          {
            string s = kvp.Value.Filename;
            if (s.Contains("ontent\\"))
              s = s.Substring(s.IndexOf("ontent\\") + 7);
            if (s.EndsWith(".xnb"))
              s = s.Substring(0, s.Length-4);
            tag.Add(kvp.Key, s);
          }
        }
      }
    }

    /// <summary>
    /// Examine each geometry, deciding whether it should have its shader replaced, 
    /// and if so to replace with a skinned or non-skinned shader.
    /// </summary>
    /// <param name="mc">The mesh to perhaps replace shaders on.</param>
    /// <param name="context">For reporting errors.</param>
    protected virtual void ReplaceShaders(MeshContent mc, ContentProcessorContext context,
        ContentIdentity identity)
    {
      foreach (GeometryContent gc in mc.Geometry)
      {
        //  figure out whether the geometry is skinned or not
        if (gc.Vertices == null)
          continue;
        bool isSkinned = VerticesAreSkinned(gc.Vertices, context);
        if (isSkinned)
          foundSkinning_ = true;
        MaybeReplaceMaterial(gc, context, isSkinned ? forceSkinnedShader_ : forceShader_, identity);
      }
    }
    
    /// <summary>
    /// If the replacement shader string is not empty, replace the shader of the 
    /// given material with the given replacement shader. Copy all shader parameters across.
    /// </summary>
    /// <param name="gc">The geometry content to replace the shader in.</param>
    /// <param name="context">For generating errors.</param>
    /// <param name="shader">The shader to replace to (or empty string, to do nothing).</param>
    protected virtual void MaybeReplaceMaterial(GeometryContent gc, ContentProcessorContext context, 
        string shader, ContentIdentity identity)
    {
      if (shader.Equals(""))
        return;
      EffectMaterialContent emc = new EffectMaterialContent();
      string prevEffect = "";
      foreach (KeyValuePair<string, object> kvp in gc.Material.OpaqueData)
      {
#if DEBUG
        context.Logger.LogMessage("param {0}: value {1}", kvp.Key, kvp.Value);
#endif
        if (kvp.Key.Equals("Effect"))
        {
          prevEffect = (kvp.Value as ExternalReference<EffectContent>).Filename;
        }
        else if (kvp.Key.Equals("CompiledEffect"))
        {
          throw new System.ArgumentException("Dxt5ModelProcessor cannot do effect substitution of already compiled effects.");
        }
        else
        {
          emc.OpaqueData.Add(kvp.Key, kvp.Value);
        }
      }
      foreach (KeyValuePair<string, ExternalReference<TextureContent>> tr in gc.Material.Textures)
      {
        emc.Textures.Add(tr.Key, tr.Value);
      }
      string path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(identity.SourceFilename), shader);
      if (shader[0] == '+')
      {
        path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(prevEffect),
            System.IO.Path.GetFileNameWithoutExtension(prevEffect) + shader.Substring(1) + ".fx");
        if (!System.IO.File.Exists(path))
        {
          path = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(identity.SourceFilename),
            System.IO.Path.GetFileNameWithoutExtension(prevEffect) + shader.Substring(1) + ".fx");
        }
      }
      context.Logger.LogImportantMessage("{2}: Replacing shader {0} to {1} for {3}", prevEffect, path, identity.SourceFilename, gc.Name);
      emc.OpaqueData.Add("Effect", new ExternalReference<EffectContent>(path));
      gc.Material = emc;
    }

    /// <summary>
    /// Return true if the vertex content contains skinning blend indices.
    /// </summary>
    /// <param name="vc">The vertex content to check.</param>
    /// <param name="context">For reporting errors.</param>
    /// <returns>true if skinning should be applied to these vertices</returns>
    protected virtual bool VerticesAreSkinned(VertexContent vc, ContentProcessorContext context)
    {
      return vc.Channels.Contains(VertexChannelNames.Weights());
    }

    /// <summary>
    /// Given the provided material, convert it, possibly forcing texture format to DXT5.
    /// </summary>
    /// <param name="material">The material to convert.</param>
    /// <param name="context">To generate errors.</param>
    /// <returns>The converted material using the Dxt5MaterialProcessor.</returns>
    protected override MaterialContent ConvertMaterial(MaterialContent material, ContentProcessorContext context)
    {
      OpaqueDataDictionary processorParameters = new OpaqueDataDictionary();
      processorParameters["ColorKeyColor"] = this.ColorKeyColor;
      processorParameters["ColorKeyEnabled"] = this.ColorKeyEnabled;
      processorParameters["TextureFormat"] = this.TextureFormat;
      processorParameters["GenerateMipmaps"] = this.GenerateMipmaps;
      processorParameters["ResizeTexturesToPowerOfTwo"] = this.ResizeTexturesToPowerOfTwo;
      processorParameters["ForceDXT5"] = this.ForceDXT5;
      return context.Convert<MaterialContent, MaterialContent>(material, typeof(Dxt5MaterialProcessor).Name, processorParameters);
    }
  }

  [ContentProcessor(DisplayName = "Dxt5 Material Processor - KiloWatt"), DesignTimeVisible(false)]
  public class Dxt5MaterialProcessor : MaterialProcessor
  {
    public Dxt5MaterialProcessor()
    {
    }

    [DefaultValue(true)]
    public bool ForceDXT5 { get { return forceDxt5_; } set { forceDxt5_ = value; } }
    bool forceDxt5_ = true;

    protected override ExternalReference<TextureContent> BuildTexture(string textureName, ExternalReference<TextureContent> texture, ContentProcessorContext context)
    {
      OpaqueDataDictionary processorParameters = new OpaqueDataDictionary();
      processorParameters.Add("ColorKeyColor", this.ColorKeyColor);
      processorParameters.Add("ColorKeyEnabled", this.ColorKeyEnabled);
      processorParameters.Add("TextureFormat", this.TextureFormat);
      processorParameters.Add("GenerateMipmaps", this.GenerateMipmaps);
      processorParameters.Add("ResizeToPowerOfTwo", this.ResizeTexturesToPowerOfTwo);
      processorParameters.Add("ForceDXT5", this.ForceDXT5);
      return context.BuildAsset<TextureContent, TextureContent>(texture, typeof(Dxt5TextureProcessor).Name, processorParameters, null, null);
    }
  }

  [ContentProcessor(DisplayName = "Dxt5 Texture Processor - KiloWatt")]
  public class Dxt5TextureProcessor : TextureProcessor
  {
    public Dxt5TextureProcessor()
    {
    }

    [DefaultValue(true)]
    public bool ForceDXT5 { get { return forceDxt5_; } set { forceDxt5_ = value; } }
    bool forceDxt5_ = true;

    public override TextureContent Process(TextureContent input, ContentProcessorContext context)
    {
      if (input == null)
        throw new ArgumentNullException("input");
      if (context == null)
        throw new ArgumentNullException("context");

      if (!this.ForceDXT5)
        return base.Process(input, context);

      TextureContent ret = null;
      TextureProcessorOutputFormat fmt = this.TextureFormat;
      this.TextureFormat = TextureProcessorOutputFormat.NoChange;
      try
      {
        ret = base.Process(input, context);
        Type originalType = ret.Faces[0][0].GetType();
        if (originalType != typeof(Dxt5BitmapContent))
          ret.ConvertBitmapType(typeof(Dxt5BitmapContent));
      }
      finally
      {
        this.TextureFormat = fmt;
      }
      return ret;
    }
  }

  public class AnimatedTextureContent
  {
    public AnimatedTextureContent(float frameTime)
    {
      frameTime_ = frameTime;
      coll_ = new List<TextureContent>();
    }
    public float frameTime_;
    public List<TextureContent> coll_;
    public List<TextureContent> Array { get { return coll_; } }
  }
  
  [ContentProcessor(DisplayName = "Animated Texture Processor - KiloWatt")]
  public class AnimatedTextureProcessor : ContentProcessor<TextureContent, AnimatedTextureContent>
  {
    public AnimatedTextureProcessor()
    {
    }

    [DefaultValue(1.0f)]
    public float SecondsPerFrame { get { return secondsPerFrame_; } set { secondsPerFrame_ = value; } }
    float secondsPerFrame_ = 1.0f;

    public override AnimatedTextureContent Process(TextureContent input, ContentProcessorContext context)
    {
      string inputName = input.Identity.SourceFilename;
      int offset = inputName.LastIndexOf('.');
      if (offset < 0) offset = inputName.Length;
      string extension = inputName.Substring(offset);
      int ndig = 0;
      while (offset > 0 && inputName[offset-1] >= '0' && inputName[offset-1] <= '9')
      {
        --offset;
        ++ndig;
      }
      string digPart = inputName.Substring(offset, ndig);
      int firstVal = digPart.Length > 0 ? Int32.Parse(digPart) : 0;
      string baseName = inputName.Substring(0, offset);
      string format;
      switch (ndig)
      {
        case 0:
          format = "{0}{1}{2}";
          break;
        case 1:
          format = "{0}{1:0}{2}";
          break;
        case 2:
          format = "{0}{1:00}{2}";
          break;
        case 3:
          format = "{0}{1:000}{2}";
          break;
        case 4:
          format = "{0}{1:0000}{2}";
          break;
        default:
          throw new System.Exception("Animated textures can't be named with more than 4 digits (and even that is ludicrous!)");
      }
      AnimatedTextureContent atc = new AnimatedTextureContent(SecondsPerFrame);
      for (int i = firstVal; true; ++i)
      {
        string path = String.Format(format, baseName, i, extension);
        if (!System.IO.File.Exists(path))
        {
          context.Logger.LogImportantMessage("Didn't find {0}; {1} animated textures total.\n", path, i - firstVal);
          break;
        }
        TextureContent tc = context.BuildAndLoadAsset<TextureContent, TextureContent>(
            new ExternalReference<TextureContent>(path, input.Identity),
            "Dxt5TextureProcessor");
        context.Logger.LogMessage("Adding texture {0}.\n", path);
        atc.Array.Add(tc);
      }
      return atc;
    }
  }

  [ContentTypeWriter]
  public class AnimatedTextureWriter : ContentTypeWriter<AnimatedTextureContent>
  {
    protected override void Write(ContentWriter output, AnimatedTextureContent atc)
    {
      output.Write(atc.frameTime_);
      output.Write(atc.Array.Count);
      foreach (TextureContent tc in atc.Array)
        output.WriteObject<TextureContent>(tc);
    }

    public override string GetRuntimeReader(TargetPlatform targetPlatform)
    {
      string str = "KiloWatt.Runtime.Assets.AnimatedTextureReader, KiloWatt.Runtime";
      return str;
    }
  }
}
