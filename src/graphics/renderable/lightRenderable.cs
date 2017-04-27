using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;

namespace Graphics
{
	public class LightRenderable : Renderable
	{
		public enum Type { DIRECTIONAL, POINT, SPOT};
		public Type myLightType;
		public float size;
		public int index;
		public Color4 color;
		public Vector4 direction;
		public float constantAttenuation;
		public float linearAttenuation;
		public float quadraticAttenuation;
		public float spotAngle;
		public float spotExponential;

		public bool isShadowCasting;

		public LightRenderable()
		 : base()
		{
			myType = "light";
		}

		public override bool isVisible(Camera c)
		{
			if (myLightType == Type.DIRECTIONAL)
				return true;

			DebugRenderer.addSphere(position, 0.1f, color, Fill.TRANSPARENT, false, 0);

			return c.containsSphere(position, size);
		}
	}
}