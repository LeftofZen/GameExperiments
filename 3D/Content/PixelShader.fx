// Standard MonoGame uniforms
cbuffer CBuffer : register(b0)
{
    float2 ViewportSize;
    float4 iMouse;
    float iTime;
};

// These are typically set up by the SpriteBatch effect automatically
SamplerState TextureSampler : register(s0);
//Texture2D Texture : register(t0);

// ShaderToy-like Texture Inputs
Texture2D iChannel0 : register(t0); // For texID 0

// Set by SpriteBatch
//float2 ViewportSize;

matrix WorldViewProjection;

#define COLOR_STEP 6.0
#define PIXEL_SIZE 4.0

#define BUMPSTRENGTH 10.0

struct Material
{
    float3 color;
    bool useTexture;
    int texID;
    float bumpStrength;
    float texScale;
    float specular;
};

struct Sphere
{

    Material material;
    float3 center;
    float radius;
};

struct Ray
{

    float3 origin;
    float3 normal;
};

struct Record
{

    Ray ray;
    bool hit;
    Material material;
    float3 normal;
    float3 intersect;
    float dist;
    float3 offset;
    float3 tangent;
    float3 bitangent;
};

float2x2 rot2(float a)
{
    float2 v = sin(float2(1.570796, 0) + a);
    return float2x2(v, -v.y, v.x);
}

// Intersects a ray with a sphere
uint raySphere(in Sphere sph, inout Record rec)
{
    float3 offset = rec.ray.origin - sph.center;
    float a = 2.0 * dot(offset, rec.ray.normal);
    float b = dot(offset, offset) - sph.radius * sph.radius;
    float disc = a * a - 4.0 * b;
    
    if (disc > 0.0)
    {
        float s = sqrt(disc);
        float dstNear = max(0.0, (-a - s) / 2.0);
        float dstFar = (-a + s) / 2.0;
        
        if (dstNear < rec.dist)
        {
            if (dstNear > 0.0)
            {
                rec.intersect = (rec.ray.normal * dstNear) + rec.ray.origin;
                rec.normal = normalize(rec.intersect - sph.center);
                rec.dist = dstNear;
                rec.material = sph.material;
                rec.hit = true;
                rec.offset = (rec.intersect - sph.center - sph.radius) / sph.radius - 0.5;
                rec.tangent = normalize(float3(rec.normal.z, 0.0, -rec.normal.x));
                rec.bitangent = normalize(cross(rec.normal, rec.tangent));
                return 1;
            }
        }
    }
    return 0;
}

// Sets up the scene's spheres
//void distances(inout Record rec)
//{
//    raySphere(Sphere(Material(float3(0.0, 0.5, 1.0), true, 0, 10.0, 0.42, 0.75), float3(200.0, 0.0, 0.0), 100.0), rec);
//    raySphere(Sphere(Material(float3(0.0, 0.5, 1.0), true, 1, 40.0, 0.42, 0.40), float3(0.0, 0.0, 200.0), 100.0), rec);
//    raySphere(Sphere(Material(float3(0.0, 0.5, 1.0), true, 2, 10.0, 0.42, 0.80), float3(-200.0, 0.0, 0.0), 100.0), rec);
//    raySphere(Sphere(Material(float3(0.0, 0.5, 1.0), true, 3, 10.0, 0.42, 0.60), float3(0.0, 0.0, -200.0), 100.0), rec);
//}

// Initializes the ray
void initRay(inout Ray ray, in float2 fragCoord)
{
    ray.origin = float3(fragCoord, -500.0);
    ray.normal = float3(0.0, 0.0, 1.0);
    float2x2 rot = rot2(-0.392699081699);
    ray.origin.yz = mul(rot, ray.origin.yz);
    ray.normal.yz = mul(rot, ray.normal.yz);
}

// Initializes the record
void initRecord(inout Record rec, in Ray ray)
{
    rec.ray = ray;
    rec.hit = false;
    rec.dist = 100000.0;
}

// Main ray-tracing function
//float4 Trace(in float2 fragCoord, float3 iChannelResolution[4], float iTime)
//{
//    Ray ray;
//    initRay(ray, fragCoord);
//    Record rec;
//    initRecord(rec, ray);
//    //distances(rec);
//    if(rec.hit) {
//        float3 matColor;
//        float3 nMap;
//        if(rec.material.useTexture) {
//            matColor = texturize(rec.material.texID, rec.offset, rec.normal, rec.material.texScale, float2(0.0, 0.0)).xyz;
//            nMap = normal(rec.material.texID, rec.offset, rec.normal, rec.material.texScale, iChannelResolution[3].xy, rec.material.bumpStrength);
//            rec.normal = worldSpace(nMap, rec.tangent, rec.bitangent, rec.normal);
//        } else {
//            matColor = rec.material.color;
//        }
//        float2 mPos = iMouse.xy - ViewportSize / 2.0;
//        float3 lightNormal = normalize((float3(mPos.x, 400.0, mPos.y)) - rec.intersect);
//        float shade = clamp(dot(lightNormal, rec.normal), 0.0, 1.0);
//        float specang = acos(shade);
//        float specexp = specang / (1.0 - rec.material.specular);
//        float spec = exp(-specexp * specexp) * rec.material.specular;
//        float3 result = matColor * lerp(float3(0.2, 0.2, 0.2), float3(1.0, 1.0, 1.0), shade) + float3(spec, spec, spec);
//        return float4(result, 1.0);
//    }
//    return float4(0.0, 0.0, 0.0, 0.0);
//}

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
};

// A simple passthrough for vertex data, required by the compiler.
struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float2 TexCoords : TEXCOORD0;
};

VertexShaderOutput MainVS(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput) 0;

    output.Position = mul(input.Position, WorldViewProjection);
    output.Color = input.Color;

    return output;
}

// --- Pixel Shader ---
// This is the core logic. It is self-contained.
float4 PixelShaderFunction(VertexShaderOutput input) : SV_TARGET
{
	// Recreate fragCoord from UVs and ViewportSize
    float2 fragCoord = input.TexCoords * ViewportSize;

	// Pixel Sizing logic
    float ratio = ViewportSize.y / ViewportSize.x;
    float2 pixel = round(fragCoord / (PIXEL_SIZE * ratio)) * PIXEL_SIZE * ratio;
    float2 uv = pixel / ViewportSize.xy;
	
	// Sample the texture
    float4 textureColor = input.Color; //Texture.Sample(TextureSampler, uv);
	
	// Color quantization logic
    float3 nCol = normalize(textureColor.rgb);
    float nLen = length(textureColor.rgb);
    float4 finalColor = float4(nCol * round(nLen * COLOR_STEP) / COLOR_STEP, textureColor.w);
	
	// Combine with vertex color (tint)
    return finalColor; //input.Color;
}

// --- Techniques and Passes ---
// This block tells MonoGame how to compile the shader.
// We're using a more modern shader model which is more flexible.
// If this still fails, try changing `4_0` to `3_0`.
technique
{
    pass MyPass
    {
        VertexShader = compile vs_3_0 MainVS();
        PixelShader = compile ps_3_0 PixelShaderFunction();
    }
}