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
	/// 文件打包工具
	/// </summary>
	class PackFileTool:EditorWindow
	{
		#region 打包设定
		/// <summary>
		/// 打包版本
		/// </summary>
		private static int PackVersion = 1;
		/// <summary>
		/// 打包类型
		/// </summary>
		private static string PackType = "FilePack";

		/// <summary>
		/// 文件内容在资源包中按多少字节对齐
		/// </summary>
		private static uint MAlignBytesSize = 8;
		#endregion


		//要过滤掉的文件后缀名
		public static string MFilterFileExtention = ".meta";
		/// <summary>
		/// 打包后的资源包扩展名
		/// </summary>
		public static string MPackageExtention = ".bin";

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
		private static string PackageName = string.Empty;
		/// <summary>
		/// 打包后资源包名称的本地key
		/// </summary>
		private static string PackageNameKey = "PackageName";


		#region 数据
		private static List<PackedFileInfo> resInfoList = null;
		#endregion



		#region 窗口显示
		[MenuItem("工具/文件打包")]  
		static void Init()  
		{  
			EditorWindow.GetWindow<PackFileTool>("文件打包");

			InitPath ();
		}

		/// <summary>
		/// 初始化打包设定
		/// </summary>
		private static bool InitPath ()
		{
			GetLocalString (PackFromFolderKey, ref PackFromFolderPath);
			GetLocalString (PackToFolderKey, ref PackToFolderPath);
			GetLocalString (PackageNameKey, ref PackageName);

			return false;
		}

		void OnGUI()  
		{  
			GUILayout.BeginVertical ();

			DrawLine ();
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
			DrawLine ();

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
			DrawLine ();

			//3.打包后的资源包名
			GUILayout.BeginHorizontal ();
			GUILayout.Label("打包后的资源包名:", EditorStyles.boldLabel, GUILayout.Width(100));
			PackageName = GUILayout.TextField(PackageName, EditorStyles.boldLabel); 
			GUILayout.EndHorizontal ();
			GUILayout.Space (-10);
			DrawLine ();
			GUILayout.Space (30);

			//4.打包
			if (GUILayout.Button ("打包", GUILayout.Width(150)))
			{
				Pack ();
			}
			GUILayout.EndVertical ();
		}  

		private void DrawLine()
		{
			GUILayout.Label("______________________________________________________________________________" +
				"______________________________________________________", EditorStyles.boldLabel);
		}

		/// <summary>
		/// 获取本地已经保存了的字符串
		/// </summary>
		private static void GetLocalString(string localKey, ref string str)
		{
			str = PlayerPrefs.HasKey (localKey) ? PlayerPrefs.GetString (localKey) : string.Empty;
		}

		/// <summary>
		/// 保存字符串到本地
		/// </summary>
		private static void SaveStringToLocal(string localKey, string str)
		{
			PlayerPrefs.SetString (localKey, str);
			PlayerPrefs.Save ();
		}

		/// <summary>
		/// 打包
		/// </summary>
		private void Pack()
		{
			if (string.IsNullOrEmpty (PackFromFolderPath) || string.IsNullOrEmpty (PackToFolderPath) || string.IsNullOrEmpty (PackageName)) 
			{
				EditorUtility.DisplayDialog ("错误", "未设定好打包路径!", "确定");
				return;
			}

			PackageName = PackageName.ToLower ();
			string packagePath = PackToFolderPath + @"/" + PackageName + MPackageExtention;
			string txtPath = (new FileInfo(packagePath)).Directory.Parent + @"/" + PackageName + ".txt";
			PackFilesToPackage(PackFromFolderPath, packagePath, txtPath);

			SaveStringToLocal (PackFromFolderKey, PackFromFolderPath);
			SaveStringToLocal (PackToFolderKey, PackToFolderPath);
			SaveStringToLocal (PackageNameKey, PackageName);

			EditorUtility.DisplayDialog ("提示", "文件打包完成", "确定");
		}
		#endregion



		#region 打包文件到资源包中
		/// <summary>
		/// 打包某个文件夹中的所有文件到资源包中
		/// </summary>
		public static void PackFilesToPackage (string srcFolderPath, string packagePath, string txtPath)
		{
			int frontRegionsSize = 0;

			if (Directory.Exists (srcFolderPath)) 
			{
				string[] filePaths = Directory.GetFiles (srcFolderPath, "*", SearchOption.AllDirectories);
				if (filePaths.Length < 1)
					return;

				resInfoList = new List<PackedFileInfo> ();

				GetFilesInitInfo (filePaths);
				WriteAllDatas (packagePath, filePaths, ref frontRegionsSize);
			}

			Test (txtPath, resInfoList, frontRegionsSize);

			if(resInfoList != null)
			{
				resInfoList.Clear ();
				resInfoList = null;
			}
		}

		/// <summary>
		/// 写入所有数据到资源包文件中
		/// </summary>
		/// <param name="packagePath">打包完成后资源包的路径</param>
		/// <param name="filePaths">要打包的文件路径集合</param>
		/// <param name="frontRegionsSize">第一块区域与第二块区域所占的字节大小和</param>
		private static void WriteAllDatas(string packagePath, string[] filePaths, ref int frontRegionsSize)
		{
			BinaryWriter totalWriter = null;
			FileStream totalStream = null;
			//初始化FileStream文件流
			//totalStream = new FileStream(desFilePath, FileMode.Append);
			totalStream = new FileStream (packagePath, FileMode.Create);
			//以FileStream文件流来初始化BinaryWriter书写器，此用以合并分割的文件
			totalWriter = new BinaryWriter (totalStream);

			//1.资源包信息区域
			totalWriter.Write(MyConverter.Int2Bytes(PackVersion));								//1.1资源包版本(int)
			byte[] resTypeNameData = MyConverter.String2Bytes (PackType);
			ushort resTypeNameSize = (ushort)resTypeNameData.Length;
			totalWriter.Write (MyConverter.Ushort2Bytes (resTypeNameSize));						//1.2资源包类型名字节大小(ushort)
			totalWriter.Write(resTypeNameData);													//1.3资源包类型名(UTF8)
			//获取第一块区域（资源包信息区域）的字节大小
			int packageInfoRegionSize = GetPackageInfoRegionSize ();
			//获取第二块区域（文件信息集合区域）的字节大小
			int fileInfosRegionSize = GetFileInfosRegionSize ();
			//第一块及第二区域所占字节和
			frontRegionsSize = packageInfoRegionSize + fileInfosRegionSize;
			//获取文件在资源包中的位置信息及打包后整个资源包的字节大小
			uint totalSize = GetFilePositionInfosAndTotalSize (frontRegionsSize);
			totalWriter.Write (MyConverter.Uint2Bytes (totalSize));								//1.4资源包大小(uint)


			//2.文件信息集合区域
			totalWriter.Write (MyConverter.Int2Bytes(fileInfosRegionSize));						//2.1文件信息集合所占字节大小(int)
			totalWriter.Write(MyConverter.Int2Bytes(resInfoList.Count));						//2.2文件信息集合里的文件信息个数（int)
			byte [] infoDatas = null;
			for (int n = 0; n < resInfoList.Count; n++) 										//2.3各文件信息组合
			{
				byte[] strDatas = MyConverter.String2Bytes (resInfoList [n].fileName);
				ushort strLength = (ushort)strDatas.Length;								

				AddToEnd (ref infoDatas, MyConverter.Ushort2Bytes (strLength));					//2.3.1文件名字节长度
				AddToEnd (ref infoDatas, strDatas);												//2.3.2文件名
				AddToEnd (ref infoDatas, MyConverter.Uint2Bytes (resInfoList [n].startPos));	//2.3.3文件起始位置
				AddToEnd (ref infoDatas, MyConverter.Int2Bytes (resInfoList [n].size));			//2.3.4文件大小
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
				if (0 == tempFileLength) 
				{
					Debug.LogError ("错误:" + filePaths[i] + "这个文件内容为空");
				} 
				else 
				{
					//读取分割文件中的数据，并生成合并后文件
					totalWriter.Write (tempReader.ReadBytes (tempFileLength));
				}

				//关闭BinaryReader文件阅读器
				tempReader.Close();
				//关闭FileStream文件流
				tempStream.Close();
			}

			//关闭BinaryWriter文件书写器
			totalWriter.Close ();
			//关闭FileStream文件流
			totalStream.Close ();
		}

		/// <summary>
		/// 写入N个空的字节内容
		/// </summary>
		/// <param name="totalWriter">Total writer.</param>
		/// <param name="emptySize">Empty size.</param>
		private static void WriteEmptyBytes(ref BinaryWriter totalWriter, int emptySize)
		{
			byte[] emptyDatas = new byte[emptySize];
			totalWriter.Write (emptyDatas);
		}

		/// <summary>
		/// 将一个字节数组里面的内容添加到另一个字节数组的末尾
		/// </summary>
		/// <param name="srcData">Source data.</param>
		/// <param name="addData">Add data.</param>
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

		/// <summary>
		/// 仅作测试用的方法（用来确认数据组织的正确性与否）
		/// </summary>
		/// <param name="txtPath">Text path.</param>
		/// <param name="resInfoList">Res info list.</param>
		/// <param name="beforeLength">Before length.</param>
		private static void Test(string txtPath, List<PackedFileInfo> resInfoList, int beforeLength)
		{
			StringBuilder fileInfoes = new StringBuilder ();
			fileInfoes.Append (beforeLength.ToString ()).Append("\r\n");

			for(int i = 0; i < resInfoList.Count; i++)
			{
				string fileName = resInfoList [i].fileName + "\t";
				string beforeSpace = "beforeSpace:" + resInfoList [i].beforeSpace.ToString() + "\t";
				string startPos = "startPos:" + resInfoList [i].startPos.ToString () + "\t";
				string fileSize = "fileSize:" + resInfoList [i].size.ToString () + "\t";
				fileInfoes.Append(fileName).Append(beforeSpace).Append(startPos).Append(fileSize).Append("\r\n");
			}

			FileStream fs = new FileStream(txtPath, FileMode.Create);
			byte[] resInfo = new UTF8Encoding().GetBytes(fileInfoes.ToString());
			fs.Write(resInfo, 0, resInfo.Length);
			fs.Flush();
			fs.Close();
		}
		#endregion


		#region 准备好写入数据到资源包中的一些步骤
		/// <summary>
		/// 1.获取到所有要打包文件的初始信息
		/// </summary>
		private static void GetFilesInitInfo(string[] filePaths)
		{
			FileStream tempStream = null;
			BinaryReader tempReader = null;
			DirectoryInfo packFolderParent = (new DirectoryInfo (PackFromFolderPath)).Parent;
			int rootFolderPathLength = packFolderParent.FullName.Length + 1;

			//1.获取文件初始信息
			for (int i = 0; i < filePaths.Length; i++) 
			{
				FileInfo tempInfo = new FileInfo (filePaths [i]);
				if (!tempInfo.Extension.ToLower ().Equals (MFilterFileExtention.ToLower ())) 
				{
					tempStream = new FileStream (filePaths [i], FileMode.Open);
					tempReader = new BinaryReader (tempStream);
					int tempFileLength = (int)tempStream.Length;

					//获取文件相对路径(包括其当前所属目录);
					string relativePath = filePaths [i].Substring (rootFolderPathLength);
					PackedFileInfo tempFileInfo = new PackedFileInfo ();
					tempFileInfo.fullPath = filePaths [i];
					tempFileInfo.fileName = relativePath;
					tempFileInfo.size = tempFileLength;
					resInfoList.Add (tempFileInfo);

					//关闭BinaryReader文件阅读器
					tempReader.Close();
					//关闭FileStream文件流
					tempStream.Close();
				}
			}
		}

		/// <summary>
		/// 2.获取第一块区域（资源包信息区域）的字节大小
		/// </summary>
		/// <returns> 资源包信息区域的字节大小 </returns>
		private static int GetPackageInfoRegionSize()
		{
			//计算资源包类型名字符串所占字节
			int packageTypeNameSize = MyConverter.String2Bytes (PackType).Length;
			//资源包版本（uint) + 资源包类型名字节大小(ushort) + 资源包类型名(UTF8) + 资源包大小(uint)
			int packageRegionSize = 4 + 2 + packageTypeNameSize + 4;

			return packageRegionSize;
		}

		/// <summary>
		/// 3.获取第二块区域（文件信息集合区域）的字节大小
		/// </summary>
		/// <returns>文件信息集合区域的字节大小</returns>
		private static int GetFileInfosRegionSize()
		{
			//文件信息集合所占字节大小(int) + 文件信息集合里的文件信息个数（int)
			int size = 4 + 4;
			//单个文件信息的组合

			//2.获取到信息块的字节长度
			byte[] infoDatas = null;
			for (int n = 0; n < resInfoList.Count; n++) 
			{
				byte[] strDatas = MyConverter.String2Bytes (resInfoList [n].fileName);
				ushort strLength = (ushort)strDatas.Length;

				AddToEnd (ref infoDatas, MyConverter.Ushort2Bytes (strLength));						//文件名字节大小（ushort)
				AddToEnd (ref infoDatas, strDatas);													//文件名(UTF8)
				AddToEnd (ref infoDatas, MyConverter.Uint2Bytes (resInfoList [n].startPos));		//文件起始位置(uint)
				AddToEnd (ref infoDatas, MyConverter.Int2Bytes (resInfoList [n].size));				//文件大小(int)
			}
			size += infoDatas.Length;
			infoDatas = null;

			return size;
		}

		/// <summary>
		/// 4.获取到文件在资源包中的位置信息及打包后整个资源包的字节大小
		/// </summary>
		/// <returns>打包后整个资源包的字节大小</returns>
		/// <param name="frontRegionsSize">文件数据集合区域前面所有区域占的字节大小</param>
		private static uint GetFilePositionInfosAndTotalSize(int frontRegionsSize)
		{
			uint totalSize = (uint)frontRegionsSize;
			for (int n = 0; n < resInfoList.Count; n++) 
			{
				int beforeSpace = 0;
				uint curFileStartPos = CountFileBeginPosAndBeforeSpace (totalSize, ref beforeSpace);
				resInfoList [n].startPos = curFileStartPos;
				resInfoList [n].beforeSpace = beforeSpace;

				totalSize += (uint)(beforeSpace + resInfoList [n].size);
			}

			return totalSize;
		}

		/// <summary>
		/// 计算文件在资源包中的起始位置及其前置空白间隙（需按MAlignBytesSize字节对齐）
		/// </summary>
		/// <returns>文件在资源包中的起始位置</returns>
		/// <param name="curLength">当前已统计了的长度</param>
		/// <param name="beforeSpace">文件的前置空白间隙字节大小</param>
		private static uint CountFileBeginPosAndBeforeSpace(uint curLength, ref int beforeSpace)
		{
			if (0 == curLength % MAlignBytesSize) 
			{
				beforeSpace = 0;
				return curLength;
			} 
			else 
			{
				uint beginPos = curLength - curLength % MAlignBytesSize + MAlignBytesSize;
				beforeSpace = (int)(MAlignBytesSize - curLength % MAlignBytesSize);

				return beginPos;
			}
		}

		/// <summary>
		/// 计算最后一个文件的末尾空白间隙占多少字节
		/// </summary>
		/// <returns>The last file end space.</returns>
		private static int CountLastFileEndSpace(uint curLength)
		{
			return (0 == (int)(curLength % MAlignBytesSize)) ? 0 : (int)(MAlignBytesSize - curLength % MAlignBytesSize);
		}
		#endregion


		public class PackedFileInfo
		{
			public string fullPath = string.Empty;			//要打包文件的完整路径
			public string fileName = string.Empty;			//文件名（相对路径名，包括后缀）
			public uint startPos = 0;						//文件内容在资源包中的起始位置
			public int size = 0;							//文件内容大小
			public int beforeSpace = 0;						//文件内容在资源包中前置的空白间隙（主要是为了按MAlignBytesSize字节对齐）
		}
	}
}