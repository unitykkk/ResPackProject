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
	/// <summary>
	/// 资源打包工具
	/// </summary>
	class ResPackTool:Editor
	{
		public static string MFilterFileExtention = ".meta";
		//要过滤掉的文件后缀名
		private static String desPath = null;
		//打包后的文件
		private static string srcPath = null;
		//要打包的文件夹
		private static string txtPath = null;


		[MenuItem ("工具/资源打包")]
		static void Main ()
		{
			if (InitPath ())
				return;
		
			CombineFile (srcPath, desPath, txtPath);

			EditorUtility.DisplayDialog ("提示", "资源打包完成", "确定");
		}

		/// <summary>
		/// 初始化打包路径
		/// </summary>
		private static bool InitPath ()
		{
			srcPath = EditorUtility.OpenFolderPanel ("选择要打包的资源文件夹", "", "");
			if (string.IsNullOrEmpty (srcPath)) {
				Debug.Log ("已取消打包");
				return true;
			}

			DirectoryInfo dirInfo = new DirectoryInfo (srcPath);
			desPath = dirInfo.Parent.FullName + @"/total.bin";
			txtPath = dirInfo.Parent.FullName + @"/total.txt";

			return false;
		}


		#region 合并文件
		/// <summary>
		/// 合并文件
		/// </summary>
		public static void CombineFile (string srcFolderPath, string desFilePath, string txtPath)
		{
			int rootFolderPathLength = srcFolderPath.Length + 1;

			BinaryWriter totalWriter = null;
			FileStream totalStream = null;
			//初始化FileStream文件流
			//totalStream = new FileStream(desFilePath, FileMode.Append);
			totalStream = new FileStream (desFilePath, FileMode.Create);
			//以FileStream文件流来初始化BinaryWriter书写器，此用以合并分割的文件
			totalWriter = new BinaryWriter (totalStream);

			uint startPos = 0;

			List<ResPackInfo> resInfoList = new List<ResPackInfo> ();
			int beforeLength = 0;

			FileStream tempStream = null;
			BinaryReader tempReader = null;
			if (Directory.Exists (srcFolderPath)) 
			{
				string[] filePaths = Directory.GetFiles (srcFolderPath, "*", SearchOption.AllDirectories);
				if (filePaths.Length < 1)
					return;
				
				//1.获取文件初始信息
				for (int i = 0; i < filePaths.Length; i++) 
				{
					FileInfo tempInfo = new FileInfo (filePaths [i]);
					if (!tempInfo.Extension.ToLower ().Equals (MFilterFileExtention.ToLower ())) 
					{
						tempStream = new FileStream (filePaths [i], FileMode.Open);
						tempReader = new BinaryReader (tempStream);
						int tempFileLength = (int)tempStream.Length;

						//获取文件相对路径
						string relativePath = filePaths [i].Substring (rootFolderPathLength);
						ResPackInfo tempResInfo = new ResPackInfo ();
						tempResInfo.resName = relativePath;
						tempResInfo.beginPos = (uint)startPos;
						tempResInfo.size = tempFileLength;
						resInfoList.Add (tempResInfo);

						startPos += (uint)tempFileLength;

						//关闭BinaryReader文件阅读器
						tempReader.Close();
						//关闭FileStream文件流
						tempStream.Close();
					}
				}
				//2.获取到信息块的字节长度
				byte[] infoDatas = null;
				for (int n = 0; n < resInfoList.Count; n++) 
				{
					byte[] strDatas = MyConverter.String2Bytes (resInfoList [n].resName);
					ushort strLength = (ushort)strDatas.Length;			//字节长度

					AddToEnd (ref infoDatas, MyConverter.Ushort2Bytes (strLength));
					AddToEnd (ref infoDatas, strDatas);
					AddToEnd (ref infoDatas, MyConverter.Uint2Bytes (resInfoList [n].beginPos));
					AddToEnd (ref infoDatas, MyConverter.Int2Bytes (resInfoList [n].size));
				}
				ushort infoRegionLength = (ushort)infoDatas.Length;
				//3.重新计算文件的起始位置
				for (int n = 0; n < resInfoList.Count; n++) 
				{
					resInfoList [n].beginPos += (uint)(2 + infoRegionLength);			//ushort + inforegion + res
				}
				//4.重新抓取信息块的二进制数据
				infoDatas = null;
				for (int n = 0; n < resInfoList.Count; n++) 
				{
					byte[] strDatas = MyConverter.String2Bytes (resInfoList [n].resName);
					ushort strLength = (ushort)strDatas.Length;			//字节长度

					AddToEnd (ref infoDatas, MyConverter.Ushort2Bytes (strLength));
					AddToEnd (ref infoDatas, strDatas);
					AddToEnd (ref infoDatas, MyConverter.Uint2Bytes (resInfoList [n].beginPos));
					AddToEnd (ref infoDatas, MyConverter.Int2Bytes (resInfoList [n].size));
				}
				//5.按信息块长度、信息块内容、资源内容这3部分二进制数据依次组包
				totalWriter.Write(MyConverter.Ushort2Bytes(infoRegionLength));
				totalWriter.Write (infoDatas);
				for (int i = 0; i < filePaths.Length; i++) {
					FileInfo tempInfo = new FileInfo (filePaths [i]);
					if (!tempInfo.Extension.ToLower ().Equals (MFilterFileExtention.ToLower ())) 
					{
						//以小文件所对应的文件名称和打开模式来初始化FileStream文件流，起读取分割作用
						tempStream = new FileStream (filePaths [i], FileMode.Open);
						tempReader = new BinaryReader (tempStream);
						int tempFileLength = (int)tempStream.Length;
						//读取分割文件中的数据，并生成合并后文件
						totalWriter.Write(tempReader.ReadBytes(tempFileLength));
						//关闭BinaryReader文件阅读器
						tempReader.Close();
						//关闭FileStream文件流
						tempStream.Close();
					}
				}

				beforeLength = 2 + infoRegionLength;
			}

			Test (resInfoList, beforeLength);

			//关闭BinaryWriter文件书写器
			totalWriter.Close ();
			//关闭FileStream文件流
			totalStream.Close ();
		}

		private static void AddToEnd (ref byte[] srcData, byte[] addData)
		{
			if (srcData == null) 
			{
				srcData = addData;
			}
			else
			{
				Array.Resize (ref srcData, srcData.Length + addData.Length);
				addData.CopyTo (srcData, srcData.Length - addData.Length);
			}
		}

		private static void Test(List<ResPackInfo> resInfoList, int beforeLength)
		{
			StringBuilder fileInfoes = new StringBuilder ();
			fileInfoes.Append (beforeLength.ToString ()).Append("\r\n");

			for(int i = 0; i < resInfoList.Count; i++)
			{
				fileInfoes.Append(resInfoList[i].resName).Append(",").Append(resInfoList[i].beginPos).Append(",").Append(resInfoList[i].size).Append("\r\n");
			}

			FileStream fs = new FileStream(txtPath, FileMode.Create);
			byte[] resInfo = new UTF8Encoding().GetBytes(fileInfoes.ToString());
			fs.Write(resInfo, 0, resInfo.Length);
			fs.Flush();
			fs.Close();
		}


		#endregion

		[Serializable]
		public class ResPackInfo
		{
			public string resName = string.Empty;
			public uint beginPos = 0;
			public int size = 0;
		}
	}
}