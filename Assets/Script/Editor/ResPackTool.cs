using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;


/// <summary>
/// 资源打包工具
/// </summary>
class ResPackTool:Editor
{
	public static string MFilterFileExtention = ".meta";			//要过滤掉的文件后缀名
	private static String desPath = null;  							//打包后的文件
	private static string srcPath = null;							//要打包的文件夹
	private static string txtPath = null;							//打包完成后存有各资源信息的txt文件


	[MenuItem("工具/资源打包")]
	static void Main()
    {
		if (InitPath ())	return;
		
		CombineFile (srcPath, desPath, txtPath);

		EditorUtility.DisplayDialog("提示", "资源打包完成", "确定");
    }

	/// <summary>
	/// 初始化打包路径
	/// </summary>
	private static bool InitPath()
	{
		srcPath = EditorUtility.OpenFolderPanel ("选择要打包的资源文件夹", "", "");
		if (string.IsNullOrEmpty (srcPath)) 
		{
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
	public static void CombineFile(string srcFolderPath, string desFilePath, string txtPath)
	{
		int rootFolderPathLength = srcFolderPath.Length + 1;

		FileStream totalStream = null;
		//初始化FileStream文件流
		//totalStream = new FileStream(desFilePath, FileMode.Append);
		totalStream = new FileStream(desFilePath, FileMode.Create);
		//以FileStream文件流来初始化BinaryWriter书写器，此用以合并分割的文件
		BinaryWriter totalWriter = new BinaryWriter(totalStream);

		StringBuilder fileInfoes = new StringBuilder();

		int startPos = 0;

		FileStream tempStream = null;
		BinaryReader tempReader = null;
		if (Directory.Exists(srcFolderPath))
		{
			string[] filePaths = Directory.GetFiles(srcFolderPath, "*", SearchOption.AllDirectories);
			//string[] filePaths = Directory.GetFiles(srcFolderPath);
			for (int i = 0; i < filePaths.Length; i++)
			{
				FileInfo tempInfo = new FileInfo(filePaths[i]);
				if (!tempInfo.Extension.ToLower().Equals(MFilterFileExtention.ToLower()))
				{
					//以小文件所对应的文件名称和打开模式来初始化FileStream文件流，起读取分割作用
					tempStream = new FileStream(filePaths[i], FileMode.Open);
					tempReader = new BinaryReader(tempStream);
					int tempFileLength = (int)tempStream.Length;
					//读取分割文件中的数据，并生成合并后文件
					totalWriter.Write(tempReader.ReadBytes(tempFileLength));
					//关闭BinaryReader文件阅读器
					tempReader.Close();
					//关闭FileStream文件流
					tempStream.Close();

					//获取文件相对路径
					string relativePath = filePaths[i].Substring(rootFolderPathLength);
					fileInfoes.Append(relativePath).Append("\t").Append(startPos).Append("\t").Append(tempFileLength).Append("\r\n");

					startPos += tempFileLength;
				}
			}
		}

		FileStream fs = new FileStream(txtPath, FileMode.Create);
		byte[] data = new UTF8Encoding().GetBytes(fileInfoes.ToString());
		fs.Write(data, 0, data.Length);
		fs.Flush();
		fs.Close();

		//关闭BinaryWriter文件书写器
		totalWriter.Close();
		//关闭FileStream文件流
		totalStream.Close();
	}
	#endregion
}