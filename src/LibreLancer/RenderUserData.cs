﻿using System;
namespace LibreLancer
{
	public struct RenderUserData
	{
		public ICamera Camera;
		public Matrix4 Matrix2;
		public Color4 Color;
		public Color4 Color2;
		//public Vector3 Vector;
		public Texture Texture;
		public float Float;
		//public float Float2;
		public ShaderAction UserFunction;
		//public Lighting Lighting;
        public object Object;
		//public int Integer;
	}
}

