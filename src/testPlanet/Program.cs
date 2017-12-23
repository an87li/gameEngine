﻿using System;
using System.Collections.Generic;
using System.Windows.Forms;

using OpenTK;
using OpenTK.Input;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

using Util;
using Graphics;
using UI;
using GpuNoise;


namespace testNoise
{
   public class TestHarness : GameWindow
   {
      public static int theWidth = 1600;
      public static int theHeight = 900;

      Viewport myViewport;
      Camera myCamera;
      GameWindowCameraEventHandler myCameraEventHandler;
      UI.GuiEventHandler myUiEventHandler;

      //GpuNoise.GpuNoise2dMap myNoise;
      GpuNoise.GpuNoiseCubeMap myNoise;

      public TestHarness()
         : base(theWidth, theHeight, new GraphicsMode(32, 24, 0, 0), "Test Noise", GameWindowFlags.Default, DisplayDevice.Default, 4, 5,
#if DEBUG
         GraphicsContextFlags.Debug)
#else
         GraphicsContextFlags.Default)
#endif
      {
         myViewport = new Viewport(0, 0, theWidth, theHeight);
         myCamera = new Camera(myViewport, 60.0f, 0.1f, 2000f);

         Keyboard.KeyUp += new EventHandler<KeyboardKeyEventArgs>(handleKeyboardUp);

         this.VSync = VSyncMode.Off;
      }

#region boilerplate
      public void handleKeyboardUp(object sender, KeyboardKeyEventArgs e)
      {
         if (e.Key == Key.Escape)
         {
            DialogResult res = MessageBox.Show("Are you sure", "Quit", MessageBoxButtons.OKCancel);
            if (res == DialogResult.OK)
            {
               Exit();
            }
         }
      }

      protected override void OnLoad(EventArgs e)
      {
         base.OnLoad(e);
         string version = GL.GetString(StringName.Version);
         int major = System.Convert.ToInt32(version[0].ToString());
         int minor = System.Convert.ToInt32(version[2].ToString());
         if (major < 4 && minor < 4)
         {
            MessageBox.Show("You need at least OpenGL 4.4 to run this example. Aborting.", "Ooops", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
            this.Exit();
         }
         System.Console.WriteLine("Found OpenGL Version: {0}.{1}", major, minor);

         initRenderer();
      }

      protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
      {
         base.OnClosing(e);
      }

      protected override void OnResize(EventArgs e)
      {
         base.OnResize(e);
         myViewport.width = Width;
         myViewport.height = Height;
         myViewport.apply();
      }

      #endregion

      public void initRenderer()
      {
         double fps = TimeSource.fps();
         myUiEventHandler = new GuiEventHandler(this);
         ImGui.displaySize = new Vector2(theWidth, theHeight);

         int size = 1024 * 1;
			//myNoise = new GpuNoise.GpuNoise2dMap(size, size);
			myNoise = new GpuNoise.GpuNoiseCubeMap(size, size);
      }

      protected override void OnUpdateFrame(FrameEventArgs e)
      {
         base.OnUpdateFrame(e);
			myCamera.updateCameraUniformBuffer();
		}
      protected override void OnRenderFrame(FrameEventArgs e)
      {
         base.OnRenderFrame(e);

         //update the timers
         TimeSource.frameStep();

         myNoise.update();

			//clear renderstate (especially scissor)
			RenderState rs = new RenderState();
			rs.apply();

         GL.ClearColor(0.2f, 0.2f, 0.2f, 1.0f);
         GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

         int size = 275;
         int posX = 100;
         int posY = 300;
         RenderTexture2dCommand cmd;

//          cmd = new RenderTexture2DCommand(new Vector2(posX, posY), new Vector2(posX + size, posY + size), myNoise.texture);
//          cmd.execute();

         //-x left
         cmd = new RenderTexture2dCommand(new Vector2(posX, posY), new Vector2(posX + size, posY + size), myNoise.myTextures[1]);
			cmd.renderState.setUniformBuffer(myCamera.uniformBufferId(), 0);
         cmd.execute();

         //-z front
         posX += size;
         cmd = new RenderTexture2dCommand(new Vector2(posX, posY), new Vector2(posX + size, posY + size), myNoise.myTextures[4]);
			cmd.renderState.setUniformBuffer(myCamera.uniformBufferId(), 0);
			cmd.execute();

         //+Y  top
         posY += size;
         cmd = new RenderTexture2dCommand(new Vector2(posX, posY), new Vector2(posX + size, posY + size), myNoise.myTextures[3]);
			cmd.renderState.setUniformBuffer(myCamera.uniformBufferId(), 0);
			cmd.execute();

         //-Y  bottom
         posY -= size * 2;
         cmd = new RenderTexture2dCommand(new Vector2(posX, posY), new Vector2(posX + size, posY + size), myNoise.myTextures[2]);
			cmd.renderState.setUniformBuffer(myCamera.uniformBufferId(), 0);
			cmd.execute();

         //+x  right
         posX += size;
         posY += size;
         cmd = new RenderTexture2dCommand(new Vector2(posX, posY), new Vector2(posX + size, posY + size), myNoise.myTextures[0]);
			cmd.renderState.setUniformBuffer(myCamera.uniformBufferId(), 0);
			cmd.execute();

         //+z  back
         posX += size;
         cmd = new RenderTexture2dCommand(new Vector2(posX, posY), new Vector2(posX + size, posY + size), myNoise.myTextures[5]);
			cmd.renderState.setUniformBuffer(myCamera.uniformBufferId(), 0);
			cmd.execute();

         renderUi();

			RenderCubemapSphere cmd2 = new RenderCubemapSphere(new Vector3(0, 0, -5), 2.0f,  myNoise.myCubemap, true);
			cmd2.renderState.setUniformBuffer(myCamera.uniformBufferId(), 0);
			cmd2.execute();

			SwapBuffers();
      }

		float avgFps = 0;
		private void renderUi()
      {
			avgFps = (0.99f * avgFps) + (0.01f * (float)TimeSource.fps());
			ImGui.beginFrame();
         ImGui.label("FPS: {0:0.00}", avgFps);

         ImGui.beginWindow("Noise Editor");
         ImGui.setWindowSize(new Vector2(300, 400), SetCondition.Always);
         ImGui.setWindowPosition(new Vector2(1250, 200), SetCondition.FirstUseEver);

         ImGui.slider("Function", ref myNoise.function);
         ImGui.slider("Octaves", ref myNoise.octaves, 1, 10);
         ImGui.slider("Frequency", ref myNoise.frequency, 0.1f, 10.0f);
         ImGui.slider("lacunarity", ref myNoise.lacunarity, 1.0f, 3.0f);
         ImGui.slider("Gain", ref myNoise.gain, 0.01f, 2.0f);
         ImGui.slider("Offset", ref myNoise.offset, 0.0f, 10.0f);
         ImGui.slider("H", ref myNoise.H, 0.1f, 2.0f);

         ImGui.endWindow();

//          ImGui.beginWindow("Planet Preview");
//          ImGui.setWindowSize(new Vector2(500, 500), SetCondition.Always);
//          ImGui.setWindowPosition(new Vector2(50, 50), SetCondition.FirstUseEver);
// 
//          RenderTexturedCubeCommand cmd = new RenderTexturedCubeCommand(new Vector3(0, 0, -5), 1.0f, true);
//          //cmd.renderState.shaderProgram = cubePlanetShader;
// 
//          ImGui.currentWindow.canvas.addCustomRenderCommand(cmd);
// 
//          ImGui.endWindow();

         ImGui.endFrame();

         List<RenderCommand> cmds = ImGui.getRenderCommands();
         foreach (RenderCommand rc in cmds)
         {
				StatelessRenderCommand src = rc as StatelessRenderCommand;
				if(src != null)
					src.renderState.setUniformBuffer(myCamera.uniformBufferId(), 0);

				rc.execute();
         }
      }
   }

   class Program
   {
      static void Main(string[] args)
      {
         using (TestHarness example = new TestHarness())
         {
            example.Title = "Test Noise";
            example.Run(30.0, 0.0);
         }

      }
   }
}