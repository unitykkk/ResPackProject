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
	class ResPackTool:EditorWindow
	{
		#region 打包设定
		private static string PackVersionStr = "1";
		private static int PackVersion = 1;
		private static string PackType = "Resource";
		#endregion


		//要过滤掉的文件后缀名
		public static string MFilterFileExtention = ".meta";
		/// <summary>
		/// 打包后的资源包后缀名
		/// </summary>
		public static string MPackedFileExtention = ".bin";

		/// <summary>
		/// 需要打包的文件夹
		/// </summary>
		private static string PackFromFolderPath = string.Empty;
		/// <summary>
		/// 需要打包的文件夹本地key
		/// </summary>
		private static string PackFromFolderKey = "PackFromFolder";

		/// <summary>
		/// 打完包后资源包所存放的文件夹
		/// </summary>
		private static string PackToFolderPath = string.Empty;
		/// <summary>
		/// 打完包后资源包所存放的文件夹本地key
		/// </summary>
		private static string PackToFolderKey = "PackToFolder";
		/// <summary>
		/// 打包后的资源包名称
		/// </summary>
		private static string PackedFileName = string.Empty;
		/// <summary>
		/// 打包后资源包名称的本地key
		/// </summary>
		private static string PackedFileNameKey = "PackedFileName";


		#region 数据
		private static List<ResPackInfo> resInfoList = null;
		#endregion
		#region 窗口显示



		[MenuItem("工具/文件打包")]  
		static void Init()  
		{  
			EditorWindow.GetWindow<ResPackTool>("文件打包");

			InitPath ();
		}

		/// <summary>
		/// 初始化打包设定
		/// </summary>
		private static bool InitPath ()
		{
			GetLocalString (PackFromFolderKey, ref PackFromFolderPath);
			GetLocalString (PackToFolderKey, ref PackToFolderPath);
			GetLocalString (PackedFileNameKey, ref PackedFileName);

			return false;
		}

		void OnGUI()  
		{  
			GUILayout.BeginVertical ();

			GUILayout.Label("______________________________________________________________________________" +
				"______________________________________________________", EditorStyles.boldLabel);
			GUILayout.Space (0);

			//1.要打包文件夹
			GUILayout.BeginHorizontal ();
			GUILayout.Label("要打包的文件夹:", EditorStyles.boldLabel, GUILayout.Width(100));
			if (GUILayout.Button ("设定", GUILayout.Width(50))) 
			{
				string srcPath = EditorUtility.OpenFolderPanel ("选择要打包的文件夹", "", "");
				if (string.IsNullOrEmpty (srcPath)) 
				{
//					Debug.Log ("已取消选择目录");
				} 
				else 
				{
					PackFromFolderPath = srcPath;
				}
			}
			GUILayout.Label(PackFromFolderPath, EditorStyles.boldLabel);  
			GUILayout.EndHorizontal ();
			GUILayout.Space (-10);
			GUILayout.Label("______________________________________________________________________________" +
				"______________________________________________________", EditorStyles.boldLabel);

			//2.资源包存放文件夹
			GUILayout.BeginHorizontal ();
			GUILayout.Label("资源包存放文件夹:", EditorStyles.boldLabel, GUILayout.Width(100));
			if (GUILayout.Button ("设定", GUILayout.Width(50))) 
			{
				string desPath = EditorUtility.OpenFolderPanel ("选择资源包存放的文件夹", "", "");
				if (string.IsNullOrEmpty (desPath)) 
				{
//					Debug.Log ("已取消选择目录");
				} 
				else 
				{
					PackToFolderPath = desPath;
				}
			}
			GUILayout.Label(PackToFolderPath, EditorStyles.boldLabel);  
			GUILayout.EndHorizontal ();
			GUILayout.Space (-10);
			GUILayout.Label("______________________________________________________________________________" +
				"______________________________________________________", EditorStyles.boldLabel);

			//3.
			GUILayout.BeginHorizontal ();
			GUILayout.Label("打包后的资源包名:", EditorStyles.boldLabel, GUILayout.Width(100));
			PackedFileName = GUILayout.TextField(PackedFileName, EditorStyles.boldLabel); 
			GUILayout.EndHorizontal ();
			GUILayout.Space (-10);
			GUILayout.Label("______________________________________________________________________________" +
				"______________________________________________________", EditorStyles.boldLabel);

			//4.
			GUILayout.BeginHorizontal ();
			GUILayout.Label("资源包版本号:", EditorStyles.boldLabel, GUILayout.Width(100));
			PackVersionStr = GUILayout.TextField(PackVersionStr, EditorStyles.boldLabel); 
			GUILayout.EndHorizontal ();
			GUILayout.Space (-10);
			GUILayout.Label("______________________________________________________________________________" +
				"______________________________________________________", EditorStyles.boldLabel);

			//5.
			GUILayout.BeginHorizontal ();
			GUILayout.Label("资源包的类型:", EditorStyles.boldLabel, GUILayout.Width(100));
			PackType = GUILayout.TextField(PackType, EditorStyles.boldLabel); 
			GUILayout.EndHorizontal ();
			GUILayout.Space (-10);
			GUILayout.Label("______________________________________________________________________________" +
				"______________________________________________________", EditorStyles.boldLabel);
			GUILayout.Space (30);

			//6.打包
			if (GUILayout.Button ("打包", GUILayout.Width(150)))
			{
				if (int.TryParse (PackVersionStr, out PackVersion)) 
				{
					Pack ();
				} 
				else 
				{
					EditorUtility.DisplayDialog ("错误", "资源包的版本号必须为整数", "确定");
				}
			}
			GUILayout.EndVertical ();
		}  


		/// <summary>
		/// 获取本地已经保存了的字符串
		/// </summary>
		private static void GetLocalString(string localKey, ref string str)
		{
			str = PlayerPrefs.HasKey (localKey) ? PlayerPrefs.GetString (localKey) : string.Empty;
		}

		/// <summary>
		/// 保存文件夹路径到本地
		/// </summary>
		private static void SaveStringToLocal(string localKey, string str)
		{
			PlayerPrefs.SetString (localKey, str);
			PlayerPrefs.Save ();
		}

		private void Pack()
		{
			if (string.IsNullOrEmpty (PackFromFolderPath) || string.IsNullOrEmpty (PackToFolderPath) || string.IsNullOrEmpty (PackedFileName)) 
			{
				Debug.LogError ("错误，未设定好打包路径！");
				return;
			}


			PackedFileName = PackedFileName.ToLower ();
			string desFilePath = PackToFolderPath + @"/" + PackedFileName + MPackedFileExtention;
			string txtPath = (new FileInfo(desFilePath)).Directory.Parent + @"/" + PackedFileName + ".txt";
			CombineFile(PackFromFolderPath, desFilePath, txtPath);

			SaveStringToLocal (PackFromFolderKey, PackFromFolderPath);
			SaveStringToLocal (PackToFolderKey, PackToFolderPath);
			SaveStringToLocal (PackedFileNameKey, PackedFileName);

			EditorUtility.DisplayDialog ("提示", "文件打包完成", "确定");
		}
		#endregion



		#region 合并文件
		/// <summary>
		/// 合并文件
		/// </summary>
		public static void CombineFile (string srcFolderPath, string desFilePath, string txtPath)
		{
			int frontRegionsSize = 0;

			if (Directory.Exists (srcFolderPath)) 
			{
				string[] filePaths = Directory.GetFiles (srcFolderPath, "*", SearchOption.AllDirectories);
				if (filePaths.Length < 1)
					return;

				resInfoList = new List<ResPackInfo> ();

				GetFilesInitInfo (filePaths);
				WriteAllDatas (desFilePath, filePaths, ref frontRegionsSize);
			}

			Test (txtPath, resInfoList, frontRegionsSize);

			if(resInfoList != null)
			{
				resInfoList.Clear ();
				resInfoList = null;
			}
		}

		private static void WriteAllDatas(string desFilePath, string[] filePaths, ref int frontRegionsSize)
		{
			BinaryWriter totalWriter = null;
			FileStream totalStream = null;
			//初始化FileStream文件流
			//totalStream = new FileStream(desFilePath, FileMode.Append);
			totalStream = new FileStream (desFilePath, FileMode.Create);
			//以FileStream文件流来初始化BinaryWriter书写器，此用以合并分割的文件
			totalWriter = new BinaryWriter (totalStream);

			//1.资源包信息区域
			totalWriter.Write(MyConverter.Int2Bytes(PackVersion));						//1.1资源包版本(int)
			byte[] resTypeNameData = MyConverter.String2Bytes (PackType);
			ushort resTypeNameSize = (ushort)resTypeNameData.Length;
			totalWriter.Write (MyConverter.Ushort2Bytes (resTypeNameSize));				//1.2资源包类型名字节大小(ushort)
			totalWriter.Write(resTypeNameData);											//1.3资源包类型名(UTF8)
			int fileInfosRegionSize = 0;
			uint totalSize = CountFilesStartPos (ref fileInfosRegionSize, ref frontRegionsSize);
			totalWriter.Write (MyConverter.Uint2Bytes (totalSize));						//1.4资源包大小(uint)


			//2.文件信息集合区域
			totalWriter.Write (MyConverter.Int2Bytes(fileInfosRegionSize));			//2.1文件信息集合所占字节大小(int)
			totalWriter.Write(MyConverter.Int2Bytes(resInfoList.Count));				//2.2文件信息集合里的文件信息个数（int)
			byte [] infoDatas = null;
			for (int n = 0; n < resInfoList.Count; n++) 								//2.3各文件信息组合
			{
				byte[] strDatas = MyConverter.String2Bytes (resInfoList [n].resName);
				ushort strLength = (ushort)strDatas.Length;			//字节长度

				AddToEnd (ref infoDatas, MyConverter.Ushort2Bytes (strLength));
				AddToEnd (ref infoDatas, strDatas);
				AddToEnd (ref infoDatas, MyConverter.Uint2Bytes (resInfoList [n].beginPos));
				AddToEnd (ref infoDatas, MyConverter.Int2Bytes (resInfoList [n].size));
			}
			totalWriter.Write (infoDatas);

			//3.文件数据集合
			FileStream tempStream = null;
			BinaryReader tempReader = null;
			for (int i = 0; i < resInfoList.Count; i++) 
			{
				//1.写入文件头部间隙
				if(resInfoList[i].beforeSpace > 0)
				{
					WriteEmptyBytes (ref totalWriter, resInfoList [i].beforeSpace);
				}

				//2.写入文件内容
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

				//3.写入文件尾部间隙
				if(resInfoList[i].endSpace > 0)
				{
					WriteEmptyBytes (ref totalWriter, resInfoList [i].endSpace);
				}
			}

			//关闭BinaryWriter文件书写器
			totalWriter.Close ();
			//关闭FileStream文件流
			totalStream.Close ();
		}

		private static void WriteEmptyBytes(ref BinaryWriter totalWriter, int emptySize)
		{
			byte[] emptyDatas = new byte[emptySize];
			totalWriter.Write (emptyDatas);
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

		private static void Test(string txtPath, List<ResPackInfo> resInfoList, int beforeLength)
		{
			StringBuilder fileInfoes = new StringBuilder ();
			fileInfoes.Append (beforeLength.ToString ()).Append("\r\n");

			for(int i = 0; i < resInfoList.Count; i++)
			{
				string fileName = resInfoList [i].resName + "\t";
				string beforeSpace = "beforeSpace:" + resInfoList [i].beforeSpace.ToString() + "\t";
				string startPos = "startPos:" + resInfoList [i].beginPos.ToString () + "\t";
				string fileSize = "fileSize:" + resInfoList [i].size.ToString () + "\t";
				string endSpace = "endSpace" + resInfoList [i].endSpace.ToString ();
				fileInfoes.Append(fileName).Append(beforeSpace).Append(startPos).Append(fileSize).Append(endSpace).Append("\r\n");
			}

			FileStream fs = new FileStream(txtPath, FileMode.Create);
			byte[] resInfo = new UTF8Encoding().GetBytes(fileInfoes.ToString());
			fs.Write(resInfo, 0, resInfo.Length);
			fs.Flush();
			fs.Close();
		}
		#endregion


		#region Steps
		/// <summary>
		/// 1.获取到所有文件的初始信息
		/// </summary>
		private static void GetFilesInitInfo(string[] filePaths)
		{
			FileStream tempStream = null;
			BinaryReader tempReader = null;
			int rootFolderPathLength = PackFromFolderPath.Length + 1;

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
					tempResInfo.fullPath = filePaths [i];
					tempResInfo.resName = relativePath;
					tempResInfo.size = tempFileLength;
					resInfoList.Add (tempResInfo);

					//关闭BinaryReader文件阅读器
					tempReader.Close();
					//关闭FileStream文件流
					tempStream.Close();
				}
			}
		}

		/// <summary>
		/// 第2步，获取文件信息集合区域所占的字节大小
		/// </summary>
		/// <returns>The file infos region size.</returns>
		private static int GetFileInfosRegionSize()
		{
			//文件信息集合所占字节大小(int) + 文件信息集合里的文件信息个数（int)
			int size = 4 + 4;
			//单个文件信息的组合

			//2.获取到信息块的字节长度
			byte[] infoDatas = null;
			for (int n = 0; n < resInfoList.Count; n++) 
			{
				byte[] strDatas = MyConverter.String2Bytes (resInfoList [n].resName);
				ushort strLength = (ushort)strDatas.Length;

				AddToEnd (ref infoDatas, MyConverter.Ushort2Bytes (strLength));						//文件名字节大小（ushort)
				AddToEnd (ref infoDatas, strDatas);													//文件名(UTF8)
				AddToEnd (ref infoDatas, MyConverter.Uint2Bytes (resInfoList [n].beginPos));		//文件起始位置(uint)
				AddToEnd (ref infoDatas, MyConverter.Int2Bytes (resInfoList [n].size));				//文件大小(int)
			}
			size += infoDatas.Length;

			return size;
		}

		/// <summary>
		/// 3.计算每个文件的起始位置
		/// </summary>
		private static uint CountFilesStartPos(ref int fileInfosRegionSize, ref int frontRegionsSize)
		{
			//1.计算资源包信息区域所占字节长度
			//计算资源包类型名字符串所占字节
			int resTypeNameSize = MyConverter.String2Bytes (PackType).Length;
			//资源包版本（uint) + 资源包类型名字节大小(ushort) + 资源包类型名(UTF8) + 资源包大小(uint)
			int resRegionSize = 4 + 2 + resTypeNameSize + 4;
			fileInfosRegionSize = GetFileInfosRegionSize ();
			frontRegionsSize = resRegionSize + fileInfosRegionSize;

			uint totalSize = (uint)frontRegionsSize;
			for (int n = 0; n < resInfoList.Count; n++) 
			{
				int beforeSpace = 0;
				uint curFileStartPos = CountNextBeginPos (totalSize, ref beforeSpace);
				resInfoList [n].beginPos = curFileStartPos;
				resInfoList [n].beforeSpace = beforeSpace;

				totalSize += (uint)(beforeSpace + resInfoList [n].size);

				if (n == resInfoList.Count - 1) 
				{
					resInfoList [n].endSpace = (int)((0 == totalSize % 8) ? 0 : (8 - totalSize % 8));
					totalSize += (uint)resInfoList [n].endSpace;
				}
			}

			return totalSize;
		}

		/// <summary>
		/// 计算下一个文件的起始位置（需要按8字节对齐）
		/// </summary>
		/// <returns>The next begin position.</returns>
		/// <param name="curLength">Current length.</param>
		private static uint CountNextBeginPos(uint curLength, ref int beforeSpace)
		{
			if (0 == curLength % 8) 
			{
				beforeSpace = 0;
				return curLength;
			} 
			else 
			{
				uint nextBeginPos = curLength - curLength % 8 + 8;
				beforeSpace = (int)(8 - curLength % 8);

				return nextBeginPos;
			}
		}
		#endregion


		[Serializable]
		public class ResPackInfo
		{
			public string fullPath = string.Empty;
			public string resName = string.Empty;
			public uint beginPos = 0;
			public int size = 0;
			public int beforeSpace = 0;
			public int endSpace = 0;
		}
	}
}