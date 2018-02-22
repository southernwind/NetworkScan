using System;
using System.IO;
using System.Windows;

using Livet;

using SandBeige.NetworkScan.DataBase;
using SandBeige.NetworkScan.ViewModels;
using SandBeige.NetworkScan.Views;

namespace SandBeige.NetworkScan {
	/// <summary>
	/// App.xaml の相互作用ロジック
	/// </summary>
	public partial class App : Application {
		protected override void OnStartup(StartupEventArgs e) {
			DispatcherHelper.UIDispatcher = this.Dispatcher;
			AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);

			var vendorDbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "VendorList.db");
			var macAddressUrl = "https://standards.ieee.org/develop/regauth/oui/oui.csv";

			using (var dataContext = new VendorDbContext(vendorDbPath)) {
				dataContext.Database.EnsureCreated();
			}

			this.MainWindow = new MainWindow() {
				DataContext = new MainWindowViewModel(vendorDbPath, macAddressUrl)
			};
			this.MainWindow.ShowDialog();
		}

		//集約エラーハンドラ
		private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e) {
			//TODO:ロギング処理など
			MessageBox.Show(
				"不明なエラーが発生しました。アプリケーションを終了します。",
				"エラー",
				MessageBoxButton.OK,
				MessageBoxImage.Error);

			Environment.Exit(1);
		}
	}
}
