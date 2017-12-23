using System;
using System.IO;
using System.Collections.Generic;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

using Util;

namespace Graphics
{
   public class SkyBoxDescriptor : ResourceDescriptor
   {
      public SkyBoxDescriptor(string descriptorFilename)
         : base()
      {
         type = "skybox";
         path = Path.GetDirectoryName(descriptorFilename);
         JsonObject definition = JsonObject.loadFile(descriptorFilename);
         descriptor = definition["skybox"];
         name = (string)descriptor["name"];
      }

      public override IResource create(ResourceManager mgr)
      {
         SkyBox m = new SkyBox();
         JsonObject cubemap = descriptor["cubemap"];
         List<String> faces = new List<string>();

         faces.Add(Path.Combine(path, (string)cubemap["+x"]));
         faces.Add(Path.Combine(path, (string)cubemap["-x"]));
         faces.Add(Path.Combine(path, (string)cubemap["+y"]));
         faces.Add(Path.Combine(path, (string)cubemap["-y"]));
         faces.Add(Path.Combine(path, (string)cubemap["+z"]));
         faces.Add(Path.Combine(path, (string)cubemap["-z"]));

         CubemapTextureDescriptor td = new CubemapTextureDescriptor(faces);
         CubemapTexture tex = mgr.getResource(td) as CubemapTexture;
         if(tex == null)
         {
            return null;
         }
			
			m.mesh.material = new Material(name);
			m.mesh.material.myFeatures |= Material.Feature.Skybox;
			m.mesh.material.addAttribute(new TextureAttribute("cubemap", tex));

			return m;
      }
   }

   public class SkyBox : IResource
   {
		static public VertexBufferObject<V3> theVbo = new VertexBufferObject<V3>(BufferUsageHint.StaticDraw);
		static public IndexBufferObject theIbo = new IndexBufferObject(BufferUsageHint.StaticDraw);
		static public VertexArrayObject theVao = new VertexArrayObject();

		static SkyBox()
		{
			Vector3[] verts = new Vector3[] {
				new Vector3(-1,-1,-1),
				new Vector3(1,-1,-1),
				new Vector3(-1,1,-1),
				new Vector3(1,1,-1),
				new Vector3(-1,-1,1),
				new Vector3(1,-1,1),
				new Vector3(-1,1,1),
				new Vector3(1,1,1)
			};

			ushort[] index ={
						5,1,7, 7,1,3,    //+X  RIGHT
                  0,4,2, 2,4,6,    //-X  LEFT
                  6,7,2, 2,7,3,    //+Y  TOP
                  0,1,4, 4,1,5,   //-Y  BOTTOM
                  4,5,6, 6,5,7,   //+Z  BACK
                  1,0,3, 3,0,2     //-Z  FRONT
                         };

			theVbo.setData(verts);
			theIbo.setData(index);
			theVao = new VertexArrayObject();
		}

		public Mesh mesh;

      public SkyBox()
      {
			mesh = new Mesh();
			mesh.primativeType = PrimitiveType.Triangles;
			mesh.indexBase = 0;
			mesh.indexCount = 36;
		}

		public void Dispose()
		{
			//clean up the cubemap here
		}
	}
}