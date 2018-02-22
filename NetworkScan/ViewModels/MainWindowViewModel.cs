using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Text.RegularExpressions;
using System.Windows.Data;

using Livet;

using Reactive.Bindings;
using Reactive.Bindings.Extensions;

using SandBeige.NetworkScan.DataBase;
using SandBeige.NetworkScan.Models;

namespace SandBeige.NetworkScan.ViewModels {
	class MainWindowViewModel : ViewModel {

		/// <summary>
		/// デザイン用コンストラクタ
		/// </summary>
		[Obsolete]
		public MainWindowViewModel() : this("", "") {
			// デザイン用データ
			this.ScanDevice.Devices.Add(new Device(new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0xff }, new CalculatableIPAddress(new byte[] { 192, 169, 120, 255 }), new Vendor("FFFFFF", "Test1 inc", "TestAddr1")));
			this.ScanDevice.Devices.Add(new Device(new byte[] { 0xff, 0xff, 0xff, 0xff, 0xff, 0 }, new CalculatableIPAddress(new byte[] { 192, 168, 111, 2 }), new Vendor("FFFFFF", "Test2 inc", "TestAddr2")));
			this.ScanDevice.Devices.Add(new Device(new byte[] { 0xff, 0xff, 0xff, 0xff, 0, 0 }, new CalculatableIPAddress(new byte[] { 192, 168, 100, 133 }), new Vendor("FFFFFF", "Test3 Corp", "TestAddr3")));
			this.ScanDevice.Devices.Add(new Device(new byte[] { 0xff, 0xff, 0xff, 0, 0, 0 }, new CalculatableIPAddress(new byte[] { 192, 168, 155, 121 }), new Vendor("FFFFFF", "Test4", "TestAddr4")));
			this.ScanDevice.Devices.Add(new Device(new byte[] { 0xff, 0xff, 0xff, 0, 0, 2 }, new CalculatableIPAddress(new byte[] { 192, 168, 33, 5 }), new Vendor("FFFFFF", "Test5", "TestAddr5")));
		}

		public MainWindowViewModel(string vendorDbPath, string macAddressUrl) {

			// VendorListUpdate
			this.VendorListMaintenance = new VendorListMaintenance(vendorDbPath, macAddressUrl);
			this.ScanDevice = new ScanDevice(vendorDbPath);

			// Property
			this.VendorListUpdateProgressRate = this.VendorListMaintenance.ToReactivePropertyAsSynchronized(x => x.ProgressRate);
			this.VendorUpdating = this.VendorListMaintenance.ObserveProperty(x => x.IsBusy).ToReactiveProperty();

			this.NicList = this.ScanDevice.NicList.ToReadOnlyReactiveCollection();
			this.SelectedInterface = this.ScanDevice.ToReactivePropertyAsSynchronized(x => x.TargetNic);
			this.DeviceList = this.ScanDevice.Devices.ToReadOnlyReactiveCollection(this.ScanDevice.Devices.ToCollectionChanged<Device>());
			this.ScanProgressRate = this.ScanDevice.ToReactivePropertyAsSynchronized(x => x.ProgressRate);
			this.DeviceScanning = this.ScanDevice.ToReactivePropertyAsSynchronized(x => x.IsBusy).ToReactiveProperty();
			this.ScanAddressFrom = this.ScanDevice.ToReactivePropertyAsSynchronized(x => x.ScanAddressFrom);
			this.ScanAddressTo = this.ScanDevice.ToReactivePropertyAsSynchronized(x => x.ScanAddressTo);
			this.RequestInterval = this.ScanDevice.ToReactivePropertyAsSynchronized(x => x.ArpIntervalMilliSeconds);

			// Command
			this.VendorListUpdateCommand.Subscribe(async () => await this.VendorListMaintenance.UpdateVendors());
			this.ScanCommand.Subscribe(async () => await this.ScanDevice.ScanAsync());
			this.ClearCommand.Subscribe(() => this.ScanDevice.Clear());

			// Sort
			this.SortedDeviceList = new ListCollectionView(this.DeviceList);
			this.SortedDeviceList.SortDescriptions.Add(new SortDescription(nameof(Device.IPAddress), ListSortDirection.Descending));
		}

		#region ベンダリスト更新
		private VendorListMaintenance VendorListMaintenance {
			get;
			set;

		}

		private ReactiveCommand _vendorListUpdateCommand;
		/// <summary>
		/// ベンダリストアップデートコマンド
		/// </summary>
		public ReactiveCommand VendorListUpdateCommand {
			get {
				return this._vendorListUpdateCommand ?? (this._vendorListUpdateCommand = new[] { this.VendorUpdating, this.DeviceScanning }.CombineLatest(x => x.All(y => !y)).ToReactiveCommand());
			}
		}

		/// <summary>
		/// ベンダリスト更新処理進捗率
		/// </summary>
		public ReactiveProperty<int> VendorListUpdateProgressRate {
			get;
		}
		
		/// <summary>
		/// ベンダリストアップデート中フラグ
		/// </summary>
		public ReactiveProperty<bool> VendorUpdating {
			get;
		}

		#endregion


		#region スキャン
		private ScanDevice ScanDevice {
			get;
			set;
		}

		private ReactiveCommand _scanCommand;
		/// <summary>
		/// ネットワーク内スキャンコマンド
		/// </summary>
		public ReactiveCommand ScanCommand {
			get {
				return this._scanCommand ?? (this._scanCommand = new[] { this.VendorUpdating, this.DeviceScanning }.CombineLatest(x => x.All(y => !y)).ToReactiveCommand());
			}
		}

		private ReactiveCommand _clearCommand;
		/// <summary>
		/// ARPテーブルロードコマンド
		/// </summary>
		public ReactiveCommand ClearCommand {
			get {
				return this._clearCommand ?? (this._clearCommand = new ReactiveCommand());
			}
		}

		/// <summary>
		/// ネットワーク内デバイス一覧
		/// </summary>
		public ReadOnlyReactiveCollection<Device> DeviceList {
			get;
		}

		/// <summary>
		/// ソート済みネットワーク内デバイス一覧
		/// </summary>
		public ListCollectionView SortedDeviceList {
			get;
		}

		/// <summary>
		/// スキャン範囲From
		/// </summary>
		public ReactiveProperty<CalculatableIPAddress> ScanAddressFrom {
			get;
		}

		/// <summary>
		/// スキャン範囲To
		/// </summary>
		public ReactiveProperty<CalculatableIPAddress> ScanAddressTo {
			get;
		}

		/// <summary>
		/// 要求間隔
		/// </summary>
		public ReactiveProperty<int> RequestInterval {
			get;
		}

		/// <summary>
		/// スキャン処理進捗率
		/// </summary>
		public ReactiveProperty<int> ScanProgressRate {
			get;
		}

		/// <summary>
		/// デバイスリストアップデート中フラグ
		/// </summary>
		public ReactiveProperty<bool> DeviceScanning {
			get;
		}

		/// <summary>
		/// NIC一覧
		/// </summary>
		public ReadOnlyReactiveCollection<ScanDevice.NicInformation> NicList {
			get;
		}

		/// <summary>
		/// 選択中NIC
		/// </summary>
		public ReactiveProperty<ScanDevice.NicInformation> SelectedInterface {
			get;
		}

		#endregion
	}
}
