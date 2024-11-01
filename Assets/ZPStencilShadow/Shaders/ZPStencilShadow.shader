Shader "ZPStencilShadow/ShadowVolume"
{
    Properties
    {
        _ShadowColor ("Shadow Color", Color) = (0, 0, 0, 0.5)
        _ExcludeLength ("Exclude Length", float) = 100
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderQueue"="Transparent" "Queue"="Transparent" }

        CGINCLUDE

        #include "UnityCG.cginc"

        struct appdata
        {
            float4 vertex : POSITION;
        };

        struct v2f
        {
            float4 vertex : SV_POSITION;
        };

        struct v2g
        {
            float4 pos : SV_POSITION;
        };

        uniform float _ExcludeLength;

        // Geometry shader to generate shadow volume faces
        [maxvertexcount(24)]
        void geom(triangle appdata input[3], inout TriangleStream<v2g> triStream)
        {
            // FIXME: Calculate if the triangle is a silhouette.(now all vertexes are processed)

            v2g o0;
            v2g o1;
            v2g o2;
            v2g e0;
            v2g e1;
            v2g e2;

            o0.pos = input[0].vertex;
            o1.pos = input[1].vertex;
            o2.pos = input[2].vertex;
            
            float3 excludeDir = _WorldSpaceLightPos0.xyz * _ExcludeLength;
            // Perform extrusion and emit vertices...
            e0 = o0;
            e1 = o1;
            e2 = o2;

            e0.pos.xyz += excludeDir;
            e1.pos.xyz += excludeDir;
            e2.pos.xyz += excludeDir;

            o0.pos = UnityWorldToClipPos(o0.pos);
            o1.pos = UnityWorldToClipPos(o1.pos);
            o2.pos = UnityWorldToClipPos(o2.pos);
            e0.pos = UnityWorldToClipPos(e0.pos);
            e1.pos = UnityWorldToClipPos(e1.pos);
            e2.pos = UnityWorldToClipPos(e2.pos);

            // cover
            triStream.Append(o0);
            triStream.Append(o1);
            triStream.Append(o2);
            triStream.RestartStrip();

            // side0
            triStream.Append(o0);
            triStream.Append(e0);
            triStream.Append(e1);
            triStream.RestartStrip();
            triStream.Append(o1);
            triStream.Append(o0);
            triStream.Append(e1);
            triStream.RestartStrip();

            // side1
            triStream.Append(o1);
            triStream.Append(e1);
            triStream.Append(e2);
            triStream.RestartStrip();
            triStream.Append(o2);
            triStream.Append(o1);
            triStream.Append(e2);
            triStream.RestartStrip();

            // side2
            triStream.Append(o2);
            triStream.Append(e2);
            triStream.Append(e0);
            triStream.RestartStrip();
            triStream.Append(o0);
            triStream.Append(o2);
            triStream.Append(e0);
            triStream.RestartStrip();

            // bottom
            triStream.Append(e1);
            triStream.Append(e2);
            triStream.Append(e0);
            triStream.RestartStrip();
        }

        v2f vert (appdata v)
        {
            v2f o;
            o.vertex = mul(unity_ObjectToWorld, v.vertex);
            return o;
        }

        fixed4 frag(v2f i) : SV_Target
        {
            return fixed4(0, 0, 0, 0);
        }
        ENDCG

        Pass
        {
            Name "ShadowVolumeShadowPass"
			Tags{ "LightMode" = "ShadowVolumeShadowPass" }

			ZWrite Off // No need to write to the depth buffer
			ColorMask RGB // Only write RGB, typically shadows don't write to the alpha channel
            ZTest Less
			Blend SrcAlpha OneMinusSrcAlpha // Blending setup that works well for semi-transparent shadows
            
			Stencil
			{
				Ref 0 // Ref is set based on how the stencil buffer was filled, usually 0 for shadows
                Pass Zero
                // FIXME: ChatGPT told me it should be Comp Greater, what's wrong with my faces setup?
				Comp Less // Only pass the stencil test and shade pixels with stencil value greater than "Ref"
			}
			
			CGPROGRAM
			// Add necessary pragmas, includes, and unity macros
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment fragVolume
            // FIXME: not working
            #pragma multi_compile_instancing
                
            uniform half4 _ShadowColor;
            // Include files, structure, and uniform declarations here

            float4 fragVolume(v2f i) : SV_Target
			{
				// Check if the fragment is in shadow and set the color accordingly.
				// The following is a simple example that applies a flat shadow color.
				// This can be replaced with more sophisticated shadow computations.
				return _ShadowColor;
			}
			ENDCG
		}

        Pass
        {
            Name "ShadowVolumeFrontFaces"
            Tags { "LightMode" = "ShadowVolumeFrontFaces" }

            Cull Back // Cull back faces; we want to process front faces for this pass
            ZWrite Off // We don't want to write to the depth buffer
            //ZTest Less
            Stencil
            {
                Ref 1
                Comp Always // Always pass the stencil test
                Pass IncrSat // Increment stencil buffer value on pass (saturate at max value)
                Fail Keep // Keep the current value if the stencil test fails
                ZFail Keep // Keep the current value if the depth test fails
            }
            ColorMask 0

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma geometry geom
            #pragma multi_compile_instancing
            ENDCG
        }

        Pass
        {
            Name "ShadowVolumeBackFaces"
            Tags { "LightMode" = "ShadowVolumeBackFaces" }

            Cull Front // Cull front faces; we want to process back faces for this pass
            ZWrite Off
            //ZTest Less
            Stencil
            {
                Ref 1
                Comp Always
                Pass DecrSat // Decrement stencil buffer value on pass (saturate at zero)
                Fail Keep
                ZFail Keep
            }
            ColorMask 0

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma geometry geom
            #pragma multi_compile_instancing
            ENDCG
        }

    }
}
