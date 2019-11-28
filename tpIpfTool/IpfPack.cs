using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace tpIpfTool {
	class IpfPack {

		public delegate void DlgPrint(string s);
		DlgPrint Print;

		public IpfPack(DlgPrint dlg) {
			Print = dlg;
		}

		void ExPrint(string t, StreamWriter sw) {
			Print(t);
			if (sw!=null) {
				sw.WriteLine(t);
				sw.Flush();
			}
		}

		string ipfTgtName = null;

		public int PacIpf(string[] files, uint tgtver, uint pkgver, string tgtName) {
			try {
				ipfTgtVer = tgtver;
				ipfPkgVer = pkgver;
				ipfTgtName = tgtName;
				lstFileTab.Clear();
				foreach (var x in files) {
					string dPath = Path.GetFullPath(x+"\\");
					var dFiles = Directory.EnumerateFiles(dPath, "*", SearchOption.AllDirectories);
					foreach (var fPath in dFiles) {
						var fti=new FileTableInf();
						fti.filePath = fPath;
						fti.archNm = Path.GetDirectoryName(dPath).Remove(0, Path.GetDirectoryName(dPath).LastIndexOf("\\")+1);
						fti.fileNm = fPath.Remove(0, dPath.Length).Replace("\\", "/");
						//Print(fti.archNm + " | " +fti.fileNm);
						lstFileTab.Add(fti);
					}
				}
				if (lstFileTab.Count <1) {
					Print("構成対象のファイルが存在しません");
					return -1;
				}
				return Packing();
			} catch (Exception ex) {
				Print("例外発生!!");
				Print(ex.ToString());
			}
			return -9999;
		}

		public int PacAddon(string[] files, uint tgtver, uint pkgver, string tgtName) {
			try {
				ipfTgtVer = tgtver;
				ipfPkgVer = pkgver;
				ipfTgtName = tgtName;
				lstFileTab.Clear();
				foreach (var x in files) {
					string dPath = Path.GetDirectoryName(x)+"\\";
					var dFiles = Directory.EnumerateFiles(x, "*", SearchOption.AllDirectories);
					foreach (var fPath in dFiles) {
						var fti=new FileTableInf();
						fti.filePath = fPath;
						fti.archNm = "addon_d.ipf";
						fti.fileNm = fPath.Remove(0, dPath.Length == 1 ? 0 : dPath.Length).Replace("\\", "/");
						//Print(fti.archNm + " | " +fti.fileNm);
						lstFileTab.Add(fti);
					}
				}
				if (lstFileTab.Count <1) {
					Print("構成対象のファイルが存在しません");
					return -1;
				}
				return Packing();
			} catch (Exception ex) {
				Print("例外発生!!");
				Print(ex.ToString());
			}
			return -9999;
		}


		public int Packing() {
			Print("ファイル構成開始");
			string fileName = string.IsNullOrEmpty(ipfTgtName) ? "_p" + DateTime.Now.ToString("yyMMddhhmmss") : ipfTgtName;
			using (FileStream fw = new FileStream(Directory.GetCurrentDirectory()+"/"+fileName+".ipf", FileMode.Create, FileAccess.ReadWrite)) {
				foreach (var fti in lstFileTab) {
					using (FileStream fr = new FileStream(fti.filePath, FileMode.Open, FileAccess.Read)) {
						IpfCrypt ic = new IpfCrypt();
						fti.dataPos = (int)fw.Position;
						fti.deplLen = (int)fr.Length;
						string ext = Path.GetExtension(fti.filePath.ToLower());
						if ((ext == ".jpg") || (ext == ".fsb") || (ext == ".mp3")) {
							byte[] readBuf = new byte[4096];
							for (int readSize = 0; ; ) {
								readSize= fr.Read(readBuf, 0, readBuf.Length);
								if (readSize==0) {
									break;
								}
								fw.Write(readBuf, 0, readSize);
							}
						} else {
							using (var memStrm =  new MemoryStream()) {
								using (var deflStrm =  new DeflateStream(memStrm, CompressionMode.Compress, true)) {
									byte[] readBuf = new byte[4096];
									for (int readSize =0, readPos =0; ; readPos+=readSize) {
										Array.Resize(ref readBuf, readPos+4096);
										readSize = fr.Read(readBuf, readPos, 4096);
										if (readSize==0) {
											Array.Resize(ref readBuf, readPos);
											break;
										}
									}
									deflStrm.Write(readBuf, 0, readBuf.Length);
								}
								var tmpBuf = memStrm.ToArray();
								//DumpBuf(tmpBuf, 32);
								ic.EncryptBuf(tmpBuf);
								//DumpBuf(tmpBuf, 32);
								fw.Write(tmpBuf, 0, tmpBuf.Length);
							}
						}
						fti.compLen = (int)fw.Position - fti.dataPos;
						fr.Seek(0, SeekOrigin.Begin);
						fti.fileCrc = ic.ComputeHash(fr);
					}
				}
				ipfFileCnt = lstFileTab.Count;
				ipfFileTblPos = (int)fw.Position;
                int lastDataPos = 0;
				foreach (var fti in lstFileTab) {
					byte[] archNmB	 = Encoding.UTF8.GetBytes(fti.archNm);
					byte[] fileNmB	 = Encoding.UTF8.GetBytes(fti.fileNm);
					byte[] tmpBuf = new byte[20];
					tmpBuf[0] = (byte)(fileNmB.Length); tmpBuf[1] = (byte)(fileNmB.Length>>8);
					tmpBuf[18] = (byte)(archNmB.Length); tmpBuf[19] = (byte)(archNmB.Length>>8);
					tmpBuf[2] = (byte)(fti.fileCrc); tmpBuf[3] = (byte)(fti.fileCrc>>8); tmpBuf[4] = (byte)(fti.fileCrc>>16); tmpBuf[5] = (byte)(fti.fileCrc>>24);
					tmpBuf[6] = (byte)(fti.compLen); tmpBuf[7] = (byte)(fti.compLen>>8); tmpBuf[8] = (byte)(fti.compLen>>16); tmpBuf[9] = (byte)(fti.compLen>>24);
					tmpBuf[10] = (byte)(fti.deplLen); tmpBuf[11] = (byte)(fti.deplLen>>8); tmpBuf[12] = (byte)(fti.deplLen>>16); tmpBuf[13] = (byte)(fti.deplLen>>24);
					tmpBuf[14] = (byte)(fti.dataPos); tmpBuf[15] = (byte)(fti.dataPos>>8); tmpBuf[16] = (byte)(fti.dataPos>>16); tmpBuf[17] = (byte)(fti.dataPos>>24);
                    lastDataPos = fti.dataPos;

                    fw.Write(tmpBuf, 0, tmpBuf.Length);
					fw.Write(archNmB, 0, archNmB.Length);
					fw.Write(fileNmB, 0, fileNmB.Length);
				}
				ipfFileFtrPos = (int)lastDataPos;
				{
					byte[] tmpBuf = new byte[24];
					tmpBuf[0] = (byte)(ipfFileCnt); tmpBuf[1] = (byte)(ipfFileCnt>>8);
					tmpBuf[2] = (byte)(ipfFileTblPos); tmpBuf[3] = (byte)(ipfFileTblPos>>8); tmpBuf[4] = (byte)(ipfFileTblPos>>16); tmpBuf[5] = (byte)(ipfFileTblPos>>24);
					tmpBuf[8] = (byte)(ipfFileFtrPos); tmpBuf[9] = (byte)(ipfFileFtrPos>>8); tmpBuf[10] = (byte)(ipfFileFtrPos>>16); tmpBuf[11] = (byte)(ipfFileFtrPos>>24);
					tmpBuf[12] = 0x50; tmpBuf[13] = 0x4B; tmpBuf[14] = 0x05; tmpBuf[15] = 0x06;
					tmpBuf[16] = (byte)(ipfTgtVer); tmpBuf[17] = (byte)(ipfTgtVer>>8); tmpBuf[18] = (byte)(ipfTgtVer>>16); tmpBuf[19] = (byte)(ipfTgtVer>>24);
					tmpBuf[20] = (byte)(ipfPkgVer); tmpBuf[21] = (byte)(ipfPkgVer>>8); tmpBuf[22] = (byte)(ipfPkgVer>>16); tmpBuf[23] = (byte)(ipfPkgVer>>24);
					fw.Write(tmpBuf, 0, tmpBuf.Length);
				}
			}
			Print("ファイル構成完了");
			Print("----");
			return 0;
		}

		public int ExtIpf(string[] files) {
			StreamWriter sw = null;
			try {
				foreach (var filePath in files) {
					lstFileTab.Clear();
					ExPrint("ファイル展開開始", sw);
					ExPrint("File:"+filePath, sw);
					string tgtDir = Directory.GetCurrentDirectory()+"/"+Path.GetFileName(filePath)+"_e"+DateTime.Now.ToString("yyMMddhhmmss");
					Directory.CreateDirectory(tgtDir);
					//sw = new StreamWriter(Path.Combine(tgtDir, "log.txt"), true);
					int sucCnt = 0;
					int errCnt = 0;
					using (FileStream fr = new FileStream(filePath, FileMode.Open, FileAccess.Read)) {
						if (CheckIpf(fr, sw)<0) {
							Print("ファイル展開失敗");
							sw.WriteLine("ファイル展開失敗");
							Print("----");
							sw.WriteLine("----");
							continue;
						}

						foreach (var fti in lstFileTab) {
							if (Ext1File(fr, fti, tgtDir) < 0) {
								errCnt++;
							} else {
								sucCnt++;
							}
						}
					}
					ExPrint("ファイル展開完了", sw);
					//Print("　成功："+sucCnt);
					//Print("　失敗："+errCnt);
					ExPrint("----", sw);

					//foreach (var s in nonCompExtnt) {
					//	Print("非圧縮:"+s);
					//}
					if (sw!=null) {
						sw.Close();
						sw = null;
					}
				}
				return 0;
			} catch (Exception ex) {
				Print("例外発生!!");
				Print(ex.ToString());
			} finally {
				if (sw!=null) {
					sw.Close();
					sw = null;
				}
			}
			return -9999;
		}

		private int Ext1File(FileStream fs, FileTableInf fti, string tgtDir) {
			//Print("生成:"+fti.archNm+"/"+System.IO.Path.GetDirectoryName(fti.fileNm));
			if (!Directory.Exists(tgtDir+"/"+fti.archNm+"/"+Path.GetDirectoryName(fti.fileNm))) {
				Directory.CreateDirectory(tgtDir+"/"+fti.archNm+"/"+Path.GetDirectoryName(fti.fileNm));
			}

			using (FileStream fw = File.Create(tgtDir+"/"+fti.archNm+"/"+fti.fileNm)) {
				byte[] tmpBuf = new byte[fti.compLen];
				ReadFile(fs, tmpBuf, fti.dataPos, fti.compLen);
				if (fti.compLen == fti.deplLen) {
					nonCompExtnt.Add(Path.GetExtension(fti.fileNm));
					fw.Write(tmpBuf, 0, fti.compLen);
					return 0;
				}
				IpfCrypt ic = new IpfCrypt();
				//DumpBuf(tmpBuf, 32);
				ic.DecryptBuf(tmpBuf);
				//DumpBuf(tmpBuf, 32);
				using (Stream memStrm = new MemoryStream(tmpBuf)) {
					using (var deflStrm =  new DeflateStream(memStrm, CompressionMode.Decompress)) {

						byte[] readBuf = new byte[4096];
						int readSize =0;
						for (int readPos = 0; ; readPos+=readSize) {
							readSize= deflStrm.Read(readBuf, 0, readBuf.Length);
							if (readSize==0) {
								break;
							}
							fw.Write(readBuf, 0, readSize);
						}
					}
				}
			}
			return 0;
		}

		private int CheckIpf(FileStream fs, StreamWriter sw) {
			if (fs.Length<44) {
				ExPrint("IPFサイズ不足　(最低44バイト)", sw);
				return -1;
			}
			if (fs.Length>Int32.MaxValue) {
				ExPrint("IPFサイズ超過　(最大"+Int32.MaxValue+"バイト)", sw);
				return -2;
			}
			ipfLen = (int)fs.Length; // ファイルのサイズ

			ExPrint("IPFフッタ解析", sw);

			byte[] tmpBuf = new byte[24];
			ReadFile(fs, tmpBuf, ipfLen-24, 24);
			if ((tmpBuf[12]!=0x50)||(tmpBuf[13]!=0x4B)||(tmpBuf[14]!=0x05)||(tmpBuf[15]!=0x06)) {
				ExPrint("IPFフッタ不正", sw);
				return -3;
			}
			ipfFileCnt		= tmpBuf[0] + (tmpBuf[1]*0x100);
			ipfFileTblPos	= tmpBuf[2] + (tmpBuf[3]*0x100) + (tmpBuf[4]*0x10000) + (tmpBuf[5]*0x1000000);
			ipfFileFtrPos	= tmpBuf[8] + (tmpBuf[9]*0x100) + (tmpBuf[10]*0x10000) + (tmpBuf[11]*0x1000000);
			ipfTgtVer		= tmpBuf[16] + (tmpBuf[17]*0x100U) + (tmpBuf[18]*0x10000U) + (tmpBuf[19]*0x1000000U);
			ipfPkgVer		= tmpBuf[20] + (tmpBuf[21]*0x100U) + (tmpBuf[22]*0x10000U) + (tmpBuf[23]*0x1000000U);

			ExPrint("  ファイル数:"+ipfFileCnt+" [0x"+ipfFileCnt.ToString("X4")+"]", sw);
			ExPrint("  TargetVer:["+ipfTgtVer+"]  PackageVer:["+ipfPkgVer+"]", sw);
			if (ipfFileTblPos>ipfLen-24 || ipfFileTblPos<0) {
				ExPrint("IPFフッタ不正　IPF内テーブル始", sw);
				return -4;
			}
			if (ipfFileFtrPos>ipfLen-24 || ipfFileFtrPos<0) {
				ExPrint("IPFフッタ不正　IPF内フッタ位置", sw);
				return -5;
			}

			int tmpPos = ipfFileTblPos;
			for (int i =0; i<ipfFileCnt; i++) {
				//Print("IPFテーブル解析 "+(i+1));
				if (tmpPos<0) {
					ExPrint("IPFテーブル不正　開始位置", sw);
					return -6;
				}
				FileTableInf fti = new FileTableInf();

				tmpBuf = new byte[24];
				ReadFile(fs, tmpBuf, tmpPos, 20);
				int archNmLen	= tmpBuf[18] + (tmpBuf[19]*0x100);
				int fileNmLen	= tmpBuf[0] + (tmpBuf[1]*0x100);
				fti.fileCrc		= (uint)(tmpBuf[2] + (tmpBuf[3]*0x100) + (tmpBuf[4]*0x10000)) + ((uint)tmpBuf[5]*0x1000000U);
				fti.compLen		= tmpBuf[6] + (tmpBuf[7]*0x100) + (tmpBuf[8]*0x10000) + (tmpBuf[9]*0x1000000);
				fti.deplLen		= tmpBuf[10] + (tmpBuf[11]*0x100) + (tmpBuf[12]*0x10000) + (tmpBuf[13]*0x1000000);
				fti.dataPos		= tmpBuf[14] + (tmpBuf[15]*0x100) + (tmpBuf[16]*0x10000) + (tmpBuf[17]*0x1000000);
				tmpBuf = new byte[archNmLen];
				ReadFile(fs, tmpBuf, tmpPos+20, archNmLen);
				fti.archNm = Encoding.UTF8.GetString(tmpBuf);
				tmpBuf = new byte[fileNmLen];
				ReadFile(fs, tmpBuf, tmpPos+20+archNmLen, fileNmLen);
				fti.fileNm = Encoding.UTF8.GetString(tmpBuf);
				//Print("  ファイル名:"+fti.archNm+" | "+fti.fileNm+"  ファイル位置:"+fti.dataPos+" [0x"+fti.dataPos.ToString("X8")+"]  圧縮サイズ　:"+fti.compLen+" [0x"+fti.compLen.ToString("X8")+"]");
				//Print("  CRC:"+fti.fileCrc.ToString("X8"));

				lstFileTab.Add(fti);
				tmpPos += 20+fileNmLen+archNmLen;
			}

			return 0;
		}

		class FileTableInf {
			public string archNm;
			public string fileNm;
			public uint fileCrc;
			public int compLen;
			public int deplLen;
			public int dataPos;

			public string filePath;//解凍時は使わない
		}

		private static void ReadFile(FileStream fs, byte[] buf, int seek, int size) {
			fs.Seek(seek, SeekOrigin.Begin);
			int readSize =0;
			while (size > readSize) {
				readSize += fs.Read(buf, readSize, size-readSize);
			}
		}

		private void DumpBuf(byte[] buf, int size) {
			StringBuilder sb =new StringBuilder();
			sb.Append("  DUMP["+size.ToString("X4")+ "]\n");
			for (int i =0; i<size; i++) {
				if ((i%16 == 0)) {
					if ((i!=0)) {
						sb.Append("\n");
					}
					sb.Append("   ");
				}
				sb.Append(" ");
				sb.Append(buf[i].ToString("X2"));
			}
			Print(sb.ToString());
		}


		HashSet<string> nonCompExtnt = new HashSet<string>();

		List<FileTableInf> lstFileTab = new List<FileTableInf>();
		//byte[] ipfBuf;
		int ipfLen;
		int ipfFileCnt;
		int ipfFileTblPos;
		int ipfFileFtrPos;
		uint ipfTgtVer;
		uint ipfPkgVer;

	}
}
