using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;


using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

using Graphics;
using Util;

namespace Terrain
{
	public class PerPixelSolidEffect : Effect
	{
		LightVisualizer myLightVisualizer;

		public PerPixelSolidEffect(ShaderProgram sp) : base(sp)
		{
			myFeatures = (Graphics.Material.Feature)1; // 1 means solid effect 
		}

		public override void updateRenderState(Graphics.Material m, RenderState state)
		{
			if (myLightVisualizer == null)
			{
				myLightVisualizer = Renderer.visualizers["light"] as LightVisualizer;
			}

			//setup diffuse map, it should exists
			ArrayTexture tex = (m.findAttribute("texArray") as TextureAttribute).value() as ArrayTexture;
			state.setTexture((int)tex.id(), 0, TextureTarget.Texture2DArray);
			state.setUniform(new UniformData(20, Uniform.UniformType.Int, 0));

			//setup the lights that influence this terrain
			state.setUniformBuffer(myLightVisualizer.myLightUniforBuffer.id, 1);
		}

		public override PipelineState getPipeline(Graphics.Material m)
		{
			PipelineState state = new PipelineState();
			state.culling.enabled = true;
			state.blending.enabled = false;
			state.depthWrite = true;

			state.shaderProgram = myShader;
			state.generateId();
			return state;
		}
	}
}