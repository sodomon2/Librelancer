﻿/* The contents of this file are subject to the Mozilla Public License
 * Version 1.1 (the "License"); you may not use this file except in
 * compliance with the License. You may obtain a copy of the License at
 * http://www.mozilla.org/MPL/
 * 
 * Software distributed under the License is distributed on an "AS IS"
 * basis, WITHOUT WARRANTY OF ANY KIND, either express or implied. See the
 * License for the specific language governing rights and limitations
 * under the License.
 * 
 * 
 * The Initial Developer of the Original Code is Callum McGing (mailto:callum.mcging@gmail.com).
 * Portions created by the Initial Developer are Copyright (C) 2013-2018
 * the Initial Developer. All Rights Reserved.
 */
using System;
using LibreLancer.Utf.Mat;
using LibreLancer.Utf.Cmp;
using LibreLancer.Utf.Dfm;
namespace LibreLancer.Utf
{
	public class UtfLoader : UtfFile
	{
		public static IDrawable LoadDrawable(string file, ILibFile resources)
		{
			var root = parseFile(file);
			bool cmpnd = false;
			bool multilevel = false;
			foreach (var node in root)
			{
				var l = node.Name.ToLowerInvariant();
				if (l == "sphere") return new SphFile(file, resources);
				if (l == "vmeshpart") return new ModelFile(file, resources);
				if (l == "cmpnd") cmpnd = true;
				if (l == "multilevel") multilevel = true;
				if (l == "skeleton") return new DfmFile(file, resources);
			}
			if (cmpnd)
				return new CmpFile(file, resources);
			if (multilevel)
				return new ModelFile(file, resources);
			throw new Exception("Not a drawable file");
		}
		public static void LoadResourceFile(string file, ILibFile library, out MatFile materials, out TxmFile textures, out Vms.VmsFile vms)
		{
			materials = null;
			textures = null;
			vms = null;
			var root = parseFile(file);
			foreach (Node node in root)
			{
				switch (node.Name.ToLowerInvariant())
				{
					case "material library":
						IntermediateNode materialLibraryNode = node as IntermediateNode;
						materials = new MatFile(materialLibraryNode, library);
						break;
					case "texture library":
						IntermediateNode textureLibraryNode = node as IntermediateNode;
						textures = new TxmFile(textureLibraryNode);
						break;
					case "vmeshlibrary":
						IntermediateNode vmsnode = node as IntermediateNode;
						vms = new Vms.VmsFile(vmsnode, library);
						break;
				}
			}
		}
	}
}
