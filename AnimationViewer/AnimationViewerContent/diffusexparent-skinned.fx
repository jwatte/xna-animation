
// This is used by 3dsmax to load the correct parser
string ParamID = "0x003";

// using 192 vertex constant registers, 58 bones is what I can fit
#define MaxBones 58

float Script : STANDARDSGLOBAL <
	string UIWidget = "none";
	string ScriptClass = "object";
	string ScriptOrder = "standard";
	string ScriptOutput = "color";
	string Script = "Technique=Default;";
> = 0.8; // version #


// light direction (world space)
float3 LightDirection : Direction<  
	string UIName = "Light Position"; 
	string Object = "DirectionalLight";
	string Space = "World";
	int refID = 0;
> = {-0.577, 0.577, 0.577};

float4 LightColor : LightColor
<
	int LightRef = 0;
	string UIWidget = "None";
> = float4(0.8f, 0.8f, 0.8f, 0.0f);

float4 LightAmbient : LightAmbient
<
  int LightRef = 0;
  string UIWidget = "None";
> = float4(0.2f, 0.2f, 0.2f, 1.0f);

int SpecularPower <
	string UIName = "Specular Power";
	string UIType = "IntSpinner";
	float UIMin = 0.0f;
	float UIMax = 200.0f;	
>  = 40;

texture DiffuseTexture : DiffuseMap< 
	string UIName = "Diffuse Map ";
	int Texcoord = 0;
	int MapChannel = 1;
>;

float FogDistance <
  string UIName = "Fog Distance";
  float UIMin = 10;
  float UIMax = 10000;
>;

float4 FogColor <
  string UIName = "Fog Color";
  string UIType = "ColorSwatch";
> = float4(0.7, 0.7, 0.7, 0);


float4x4 ViewProjection         : VIEWPROJECTION;
float4x4 World                  : WORLD;
float4x4 ViewInverse            : VIEWINVERSE;

float4   Pose[MaxBones*3]       : SKINPOSE;

sampler2D DiffuseSampler =
sampler_state {
  Texture = <DiffuseTexture>;
  MipFilter = LINEAR;
  MinFilter = ANISOTROPIC;
  MagFilter = LINEAR;
  MaxAnisotropy = 8;
  AddressU = WRAP;
  AddressV = WRAP;
};


struct VSIn
{
	float4 Position		: POSITION;
	float3 Normal     : NORMAL;
	float2 UV0        : TEXCOORD0;
  float4 Weights    : BLENDWEIGHT;
  float4 Indices    : BLENDINDICES;
};


struct VSOut
{
	float4 Position       : POSITION;
 	float2 UV0            : TEXCOORD0;
 	float3 Normal         : TEXCOORD1;
 	float3 ViewDirection  : TEXCOORD2;
  float  Fog            : COLOR0;
};

float4x4 CalcBlendMatrix(VSIn data)
{
  int4 ix = int4(data.Indices * 3);

  float4x3 fx;
  fx._m00_m10_m20_m30 = Pose[ix.x];
  fx._m01_m11_m21_m31 = Pose[ix.x+1];
  fx._m02_m12_m22_m32 = Pose[ix.x+2];

  float4x3 fy;
  fy._m00_m10_m20_m30 = Pose[ix.y];
  fy._m01_m11_m21_m31 = Pose[ix.y+1];
  fy._m02_m12_m22_m32 = Pose[ix.y+2];

  float4x3 fz;
  fz._m00_m10_m20_m30 = Pose[ix.z];
  fz._m01_m11_m21_m31 = Pose[ix.z+1];
  fz._m02_m12_m22_m32 = Pose[ix.z+2];

  float4x3 fw;
  fw._m00_m10_m20_m30 = Pose[ix.w];
  fw._m01_m11_m21_m31 = Pose[ix.w+1];
  fw._m02_m12_m22_m32 = Pose[ix.w+2];

  float4x3 ret = fx * data.Weights.x;
  ret += fy * data.Weights.y;
  ret += fz * data.Weights.z;
  ret += fw * data.Weights.w;

  float4x4 mat;
  mat._m00_m10_m20_m30 = ret._m00_m10_m20_m30;
  mat._m01_m11_m21_m31 = ret._m01_m11_m21_m31;
  mat._m02_m12_m22_m32 = ret._m02_m12_m22_m32;
  mat._m03_m13_m23_m33 = float4(0, 0, 0, 1);

  return mat;
}

VSOut VS(VSIn data)
{
  VSOut ret;

  float4x4 SkinPose = CalcBlendMatrix(data);
  float4x4 PoseWorld = mul(SkinPose, World);
  ret.Position = mul(data.Position, mul(PoseWorld, ViewProjection));
  ret.UV0 = data.UV0;
  ret.Normal = mul(normalize(data.Normal), (float3x3)PoseWorld);
  ret.ViewDirection = mul(data.Position, PoseWorld) - ViewInverse[3].xyz;
  ret.Fog = 1 - saturate(length(ret.ViewDirection) / FogDistance);

  return ret;
}


struct PSIn
{
 	float4 UV0		  : TEXCOORD0;
 	float3 Normal		: TEXCOORD1;
 	float3 ViewDir  : TEXCOORD2;
	float Fog       : COLOR0;
};

struct PSOut
{
	float4 Color		: COLOR;
};

PSOut PS(PSIn data)
{
  PSOut ret;

  float4 diffuse = tex2D(DiffuseSampler, data.UV0);

  float3 LightDirN = normalize(LightDirection);

  // Normal
  float3 N = normalize(data.Normal);

  // Diffuse term
  float NdotL = saturate(dot(N, LightDirN));

  float3 vd = data.ViewDir;

  // Reflected angle
  float3 R = normalize(vd - 2 * dot(N, vd) * N);

  float specularMult = pow(saturate(dot(R, LightDirN)), 2 * SpecularPower) * 0.5;

  float4 temp = float4(LightColor.xyz * specularMult, 0)
      + NdotL * diffuse * LightColor
      + diffuse * LightAmbient;

  ret.Color = lerp(FogColor, temp, data.Fog);

  return ret;
}

technique Default
<
    string script = "Pass=p0;";
    bool transparent = true;
> 
{
    pass p1 <
		  string script = "Draw=Geometry;";
    >
    {		
  		AlphaBlendEnable	= TRUE;
        ZEnable           = TRUE;
        ZWriteEnable      = FALSE;
        CullMode          = CW;
  		VertexShader	    = compile vs_2_0 VS();
  		PixelShader		    = compile ps_2_0 PS();
    }
}
