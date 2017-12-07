using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;



namespace PackTool
{
	public class MyConverter
	{
		#region GetBytes
		public static byte[] Ushort2Bytes(ushort value)
		{
			return BitConverter.GetBytes (value);
		}

		public static byte[] String2Bytes(string str)
		{
			return new UTF8Encoding ().GetBytes (str);
		}

		public static byte[] Uint2Bytes(uint value)
		{
			return BitConverter.GetBytes (value);
		}

		public static byte[] Int2Bytes(int value)
		{
			return BitConverter.GetBytes (value);
		}
		#endregion
	}
}

