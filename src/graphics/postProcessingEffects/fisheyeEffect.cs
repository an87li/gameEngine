using System;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

using Util;

namespace Graphics
{
   public class FishEyeEffect : PostEffect
   {
      ShaderProgram myShader;

      public FishEyeEffect()
         : base()
      {
         name = "fisheye";

         ShaderProgramDescriptor sd = new ShaderProgramDescriptor("Graphics.shaders.post-vs.glsl", "Graphics.shaders.fisheye-ps.glsl");
         myShader = Renderer.resourceManager.getResource(sd) as ShaderProgram;
      }

      public override List<RenderCommand> getCommands()
      {
         List<RenderCommand> ret = new List<RenderCommand>();
         StatelessRenderCommand cmd;

         ret.Add(new PushDebugMarkerCommand("fisheye effect"));

         RenderTarget target1;
         target1 = postPass.nextRenderTarget();

         //target a new render target
         ret.Add(new SetRenderTargetCommand(target1));

         //blur the input image in the horizontal direction
         cmd = new PostEffectRenderCommand(myShader, postPass.view.viewport.width, postPass.view.viewport.height);
         cmd.renderState.setTexture(postPass.previousEffectOutput(this).id(), 0, TextureTarget.Texture2D);
         cmd.renderState.setUniform(new UniformData(20, Uniform.UniformType.Int, 0));
         cmd.renderState.setUniform(new UniformData(21, Uniform.UniformType.Float, 180.0f));
         ret.Add(cmd);

         output = target1.buffers[FramebufferAttachment.ColorAttachment0];

         ret.Add(new PopDebugMarkerCommand());

         return ret;
      }
   }
}