//-----------------------------------------------------------------------------
// Global Variables
//-----------------------------------------------------------------------------

// World, View, and Projection matrices
float4x4 World;
float4x4 View;
float4x4 Projection;
float4x4 WorldViewProjection;

// Light properties
float3 LightDirection = normalize(float3(1, -1, 1)); // Example light direction
float4 AmbientColor = float4(0.1, 0.1, 0.1, 1.0); // Ambient light color
float4 DiffuseColor = float4(1.0, 1.0, 1.0, 1.0); // Diffuse light color

//-----------------------------------------------------------------------------
// Structs
//-----------------------------------------------------------------------------

// This is the input vertex data structure from Monogame
// In our case, the color is ignored as we calculate a new flat color
struct VertexShaderInput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float3 Normal : NORMAL0;
};

// This is the output from the vertex shader and input to the pixel shader
// The normal is now marked with "flat" to ensure flat shading
struct VertexShaderOutput
{
    float4 Position : SV_POSITION;  // The transformed position
    float3 Normal : NORMAL0; // The non-interpolated normal
};

//-----------------------------------------------------------------------------
// Vertex Shader
//-----------------------------------------------------------------------------

VertexShaderOutput VertexShaderFunction(VertexShaderInput input)
{
    VertexShaderOutput output;

    // Transform the vertex position into clip space for rendering
    output.Position = mul(input.Position, WorldViewProjection);

    // Transform the normal into world space and pass it without interpolation
    output.Normal = mul(input.Normal, (float3x3)World);

    return output;
}

//-----------------------------------------------------------------------------
// Pixel Shader
//-----------------------------------------------------------------------------

float4 PixelShaderFunction(VertexShaderOutput input) : SV_TARGET
{
    // The normal is now a single, non-interpolated value for the whole triangle.
    float3 faceNormal = normalize(input.Normal);
    
    // Calculate the lighting value based on the face normal and the light direction
    float lightIntensity = saturate(dot(faceNormal, LightDirection));

    // Combine ambient and diffuse lighting
    float4 finalColor = AmbientColor + (DiffuseColor * lightIntensity);
    
    // Return the final, flat-shaded color for the pixel
    return finalColor;
}

//-----------------------------------------------------------------------------
// Technique
//-----------------------------------------------------------------------------

technique FlatShading
{
    pass P0
    {
        VertexShader = compile vs_4_0 VertexShaderFunction();
        PixelShader = compile ps_4_0 PixelShaderFunction();
    }
}
