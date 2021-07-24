#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0
	#define PS_SHADERMODEL ps_4_0
#endif

matrix View, World, Projection;
float3 campos;

struct VertexShaderInput
{
	float4 Position : SV_POSITION;
	float4 Normal : NORMAL0;
	float4 Color : COLOR0;
};

struct VertexShaderOutput
{
	float4 Position : SV_POSITION;
	float3 WorldPos : TEXCOORD0;
	float3 Normal : NORMAL0;
	float4 Color : COLOR0;
};
struct PixelShaderOutput
{
	float4 OUT1;
	float OUT2;
};


VertexShaderOutput MainVS(in VertexShaderInput input)
{
	VertexShaderOutput output = (VertexShaderOutput)0;

	float4 worldPosition = mul(input.Position, World);
	float4 viewPosition = mul(worldPosition, View);
	output.Position = mul(viewPosition, Projection);
	output.Color = float4(abs(input.Normal.x), abs(input.Normal.y), abs(input.Normal.z), 1);// input.Color;
	output.WorldPos = worldPosition;

	output.Normal = input.Normal;
	output.Color.g = 1 - output.Color.g;
	output.Color = pow(output.Color, 0.5f);

	return output;
}

PixelShaderOutput MainPS(VertexShaderOutput input) : COLOR
{
	PixelShaderOutput OUT;
	OUT.OUT1 = abs(input.Color);
	OUT.OUT2 = length(input.WorldPos - campos);
	return OUT;
}

technique BasicColorDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};