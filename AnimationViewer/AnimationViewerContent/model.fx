
// This is used by 3dsmax to load the correct parser
string ParamID = "0x003";

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

bool FlipNormal <
  string UIName = "Flip Normal G Channel";
> = false;

texture DiffuseTexture : DiffuseMap< 
	string UIName = "Diffuse Map ";
	int Texcoord = 0;
	int MapChannel = 1;
>;

texture GlossTexture : SpecularMap< 
	string UIName = "Gloss Map";
	int Texcoord = 0;
	int MapChannel = 1;
>;

texture NormalTexture : NormalMap< 
	string UIName = "Normal Map";
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


float4x4 WorldViewProjection    : WORLDVIEWPROJECTION;
float4x4 World                  : WORLD;
float4x4 ViewInverse            : VIEWINVERSE;

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

sampler2D GlossSampler =
sampler_state {
  Texture = <GlossTexture>;
  MipFilter = LINEAR;
  MinFilter = LINEAR;
  MagFilter = LINEAR;
  MaxAnisotropy = 2;
};

sampler2D NormalSampler =
sampler_state {
  Texture = <NormalTexture>;
  MipFilter = LINEAR;
  MinFilter = LINEAR;
  MagFilter = LINEAR;
  MaxAnisotropy = 2;
};


struct VSIn
{
	float4 Position		: POSITION;
	float3 Normal		: NORMAL;
	float3 Tangent		: TANGENT;
	float3 BiNormal		: BINORMAL;
	float2 UV0		: TEXCOORD0;
};


struct VSOut
{
	float4 Position       : POSITION;
 	float2 UV0            : TEXCOORD0;
 	float3 Normal         : TEXCOORD1;
	float3 Tangent		    : TEXCOORD2;
	float3 BiNormal		    : TEXCOORD3;
 	float3 ViewDirection  : TEXCOORD4;
  float  Fog            : COLOR0;
};


VSOut VS(VSIn data)
{
  VSOut ret;

  ret.Position = mul(data.Position, WorldViewProjection);
  ret.UV0 = data.UV0;
  ret.Normal = mul(normalize(data.Normal), (float3x3)World);
  ret.Tangent = mul(normalize(data.Tangent), (float3x3)World);
  ret.BiNormal = mul(normalize(data.BiNormal), (float3x3)World);
  ret.ViewDirection = mul(data.Position, World) - ViewInverse[3].xyz;
  ret.Fog = 1 - saturate(length(ret.ViewDirection) / FogDistance);

  return ret;
}


struct PSIn
{
 	float4 UV0		: TEXCOORD0;
 	float3 Normal		: TEXCOORD1;
	float3 Tangent		: TEXCOORD2;
	float3 BiNormal		: TEXCOORD3;
 	float3 ViewDir		: TEXCOORD4;
	float Fog     : COLOR0;
};

struct PSOut
{
	float4 Color		: COLOR;
};

PSOut PS(PSIn data)
{
  PSOut ret;

  float4 normal = tex2D(NormalSampler, data.UV0) * 2 - 1;
  float4 diffuse = tex2D(DiffuseSampler, data.UV0);
  float4 gloss = tex2D(GlossSampler, data.UV0);

  float3 LightDirN = normalize(LightDirection);

  // Normal
  float3 N = normalize(data.Normal * normal.b + data.Tangent * normal.g + data.BiNormal * normal.r);

  // Diffuse term
  float NdotL = saturate(dot(N, LightDirN));

  float3 vd = data.ViewDir;

  // Reflected angle
  float3 R = normalize(vd - 2 * dot(N, vd) * N);

  float specularMult = pow(saturate(dot(R, LightDirN)), 2 * SpecularPower * gloss.w);

  float4 temp = float4(gloss.xyz * LightColor.xyz * specularMult, 0)
      + NdotL * diffuse * LightColor
      + diffuse * LightAmbient;

  ret.Color = lerp(FogColor, temp, data.Fog);

  return ret;
}


technique Default
<
	string script = "Pass=p0;";
> 
{
    pass p0 <
		  string script = "Draw=Geometry;";
    >
    {		
  		AlphaBlendEnable	= FALSE;
      ZEnable           = TRUE;
      ZWriteEnable      = TRUE;
      CullMode          = CW;
  		VertexShader	    = compile vs_2_0 VS();
  		PixelShader		    = compile ps_2_0 PS();
    }
}
