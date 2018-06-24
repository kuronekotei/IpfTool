using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Threading;

namespace tpIpfTool {
	/// <summary>
	/// MainWindow.xaml の相互作用ロジック
	/// </summary>
	public partial class MainWindow : Window {
		public MainWindow() {
			InitializeComponent();
			DataContext = mdl;
			Print("Start");
			String[] args = App.Args;
			for (int i=0; i<args.Length; i++) {
				Print("arg["+i+"]:"+args[i]);
				modeArgs = true;
			}
			if (modeArgs) {
				Execute(args);
			}
		
		}
		bool modeArgs = false;
		private void Window_PreviewDragOver(object sender, DragEventArgs e) {
			if (e.Data.GetDataPresent(DataFormats.FileDrop, true)) {
				e.Effects = DragDropEffects.Copy;
			} else {
				e.Effects = DragDropEffects.None;
			}
			e.Handled = true;
		}

		private void Window_Drop(object sender, DragEventArgs e) {
			string[] files = e.Data.GetData(DataFormats.FileDrop) as string[];
			if (files == null || files.Length <1) {
				return;
			}
			Execute(files);
		}
		private void Execute(string[] files) {
			bool modePac =true;
			bool modePacIpf =true;
			bool modePacAddon =true;
			bool modeExt =true;
			foreach (var s in files) {
				if (Directory.Exists(s)) {
					modeExt =false;

					string tmpFile = Path.GetFileName(s).ToLower();
					if ((tmpFile.Length <5) || (tmpFile.IndexOf(".ipf", tmpFile.Length-4) <0)) {
						modePacIpf =false;
					} else {
						modePacAddon =false;
					}

					continue;
				} else {
					modePac = false;
				}
				if (!File.Exists(s)) {
					Print("指定されたファイル/ディレクトリが見つかりません:"+s);
					return;
				}
				if (Path.GetExtension(s).ToLower() != ".ipf") {
					Print("IPF以外のファイルが指定されました:"+s);
					return;
				}
			}

			if (modePac && !modePac) {
				Print("ディレクトリ、もしくはIPFのどちらかを指定してください");
				return;
			}
			if (!modeExt && !modePacIpf && !modePacAddon) {
				Print("ディレクトリは全てxxx.ipf形式か、全てが左記形式でない必要があります");
				return;
			}

			if (modeExt) {
				Task task = new Task(() => {
					IpfPack ip = new IpfPack(Print);
					int ret = ip.ExtIpf(files);
					if (modeArgs && (ret>-1)) {
						Environment.Exit(0);
					}
				});
				task.Start();
				return;
			}
			if (modePac && modePacIpf) {
				Task task = new Task(() => {
					IpfPack ip = new IpfPack(Print);
					int ret = ip.PacIpf(files, mdl.tgtver, mdl.pkgver);
					if (modeArgs && (ret>-1)) {
						Environment.Exit(0);
					}
				});
				task.Start();
				return;
			}
			if (modePac && modePacAddon) {
				Task task = new Task(() => {
					IpfPack ip = new IpfPack(Print);
					int ret = ip.PacAddon(files, mdl.tgtver, mdl.pkgver);
					if (modeArgs && (ret>-1)) {
						Environment.Exit(0);
					}
				});
				task.Start();
				return;
			}
		}





		class MainWindowModel : INotifyPropertyChanged {
			public event PropertyChangedEventHandler PropertyChanged;
			private static readonly PropertyChangedEventArgs LogtxtPropertyChangedEventArgs = new PropertyChangedEventArgs("logtxt");
			private static readonly PropertyChangedEventArgs TgtVerPropertyChangedEventArgs = new PropertyChangedEventArgs("tgtver");
			private static readonly PropertyChangedEventArgs PkgVerPropertyChangedEventArgs = new PropertyChangedEventArgs("pkgver");
			string _logtxt;
			public string logtxt {
				get { return _logtxt; }
				set {
					if (_logtxt == value) { return; }
					_logtxt = value;
					if (PropertyChanged != null) {
						PropertyChanged.Invoke(this, LogtxtPropertyChangedEventArgs);
					}
				}
			}
			uint _tgtver;
			public uint tgtver {
				get { return _tgtver; }
				set {
					if (_tgtver == value) { return; }
					_tgtver = value;
					if (PropertyChanged != null) {
						PropertyChanged.Invoke(this, TgtVerPropertyChangedEventArgs);
					}
				}
			}
			uint _pkgver;
			public uint pkgver {
				get { return _pkgver; }
				set {
					if (_pkgver == value) { return; }
					_pkgver = value;
					if (PropertyChanged != null) {
						PropertyChanged.Invoke(this, TgtVerPropertyChangedEventArgs);
					}
				}
			}
		}

		MainWindowModel mdl = new MainWindowModel();

		private void Print(string txt) {
			mdl.logtxt += txt +"\n";
		}


	}
}
