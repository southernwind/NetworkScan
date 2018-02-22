using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

using Livet;

using PcapDotNet.Base;
using PcapDotNet.Core;
using PcapDotNet.Packets;
using PcapDotNet.Packets.Arp;
using PcapDotNet.Packets.Ethernet;

using SandBeige.NetworkScan.DataBase;

namespace SandBeige.NetworkScan.Models {
	public class ScanDevice : NotificationObject {

		private object lockObj = new object();

		/// <summary>
		/// ベンダ一覧DB保管フルパス
		/// </summary>
		private readonly string _vendorDbPath;

		/// <summary>
		/// ベンダ名検索、ホスト名逆引き用タスクリスト
		/// </summary>
		private List<Task> taskList;

		/// <summary>
		/// 受信処理用タスク
		/// </summary>
		private Task _receiveTask;

		/// <summary>
		/// パケット送受信用NIC
		/// </summary>
		private PacketCommunicator _communicator;

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="vendorDbPath">ベンダ一覧DB保管フルパス</param>
		public ScanDevice(string vendorDbPath) {
			this.Devices = new ObservableSynchronizedCollection<Device>();
			this.NicList = new ObservableCollection<NicInformation>(
				NetworkInterface
					.GetAllNetworkInterfaces()
					.Select(
						x =>
							new NicInformation() {
								NetworkInterface = x,
								IPInterfaceProperties = x.GetIPProperties(),
								PhysicalAddress = x.GetPhysicalAddress(),
								UnicastAddress = x.GetIPProperties()
									.UnicastAddresses
									.FirstOrDefault(address =>
										address.PrefixOrigin != PrefixOrigin.WellKnown &&
										address.IPv4Mask != new IPAddress(0)
									)
							}
					)
			);
			this._vendorDbPath = vendorDbPath;
			this.taskList = new List<Task>();
		}


		#region ネットワーク内デバイスリスト変更通知プロパティ
		private ObservableSynchronizedCollection<Device> _devices;
		/// <summary>
		/// ネットワーク内デバイスリスト
		/// </summary>
		public ObservableSynchronizedCollection<Device> Devices {
			get {
				return this._devices;
			}
			set {
				this._devices = value;
				RaisePropertyChanged();
			}
		}
		#endregion

		#region NIC一覧
		private ObservableCollection<NicInformation> _nicList;
		/// <summary>
		/// NIC一覧
		/// </summary>
		public ObservableCollection<NicInformation> NicList {
			get {
				return this._nicList;
			}
			set {
				this._nicList = value;
				RaisePropertyChanged();
			}
		}
		#endregion

		#region ターゲットNIC
		private NicInformation _targetNic;
		/// <summary>
		/// ターゲットNIC
		/// </summary>
		public NicInformation TargetNic {
			get {
				return this._targetNic;
			}
			set {
				if(this._targetNic == value) {
					return;
				}
				this._targetNic = value;
				ChangeNic();

				this.ScanAddressFrom = this.TargetNic.NetworkAddress.ToCalculatableIPAddress();
				this.ScanAddressTo = this.ScanAddressFrom | (this.TargetNic.UnicastAddress.IPv4Mask.ToCalculatableIPAddress() ^ new CalculatableIPAddress(new byte[] { 255, 255, 255, 255 }));

				RaisePropertyChanged();
			}
		}

		#endregion

		#region 処理中フラグ変更通知プロパティ
		/// <summary>
		/// 処理中フラグ
		/// </summary>
		private bool _isBusy;
		public bool IsBusy {
			get {
				return this._isBusy;
			}
			set {
				if(this._isBusy == value) {
					return;
				}
				this._isBusy = value;
				RaisePropertyChanged();
			}
		}
		#endregion

		#region 進捗率変更通知プロパティ
		private int _progressRate;
		/// <summary>
		/// 進捗率
		/// </summary>
		public int ProgressRate {
			get {
				return this._progressRate;
			}
			set {
				if (this._progressRate == value) {
					return;
				}
				this._progressRate = value;
				RaisePropertyChanged();
			}
		}
		#endregion

		#region ARP要求送信間隔(ms)変更通知プロパティ
		private int _arpIntervalMilliSeconds;
		/// <summary>
		/// ARP要求送信間隔(ms)
		/// </summary>
		public int ArpIntervalMilliSeconds {
			get {
				return this._arpIntervalMilliSeconds;
			}
			set {
				if(this._arpIntervalMilliSeconds == value) {
					return;
				}
				this._arpIntervalMilliSeconds = value;
				RaisePropertyChanged();
			}
		}
		#endregion

		#region スキャンアドレスFrom変更通知プロパティ
		private CalculatableIPAddress _scanAddressFrom;
		public CalculatableIPAddress ScanAddressFrom {
			get {
				return this._scanAddressFrom;
			}
			set {
				if(this._scanAddressFrom == value) {
					return;
				}
				this._scanAddressFrom = value;
				RaisePropertyChanged();
			}
		}
		#endregion

		#region スキャンアドレスTo変更通知プロパティ
		private CalculatableIPAddress _scanAddressTo;
		public CalculatableIPAddress ScanAddressTo {
			get {
				return this._scanAddressTo;
			}
			set {
				if (this._scanAddressTo == value) {
					return;
				}
				this._scanAddressTo = value;
				RaisePropertyChanged();
			}
		}
		#endregion

		/// <summary>
		/// ネットワーク内機器のスキャンを行いデバイスリストプロパティのを更新をする。
		/// ネットワーク内すべてのIPアドレスにARP要求を行い、応答のあったIPアドレスをデバイスリストに登録する。
		/// </summary>
		/// <returns></returns>
		public async Task ScanAsync() {
			if (this.TargetNic == null) {
				return;
			}

			this.IsBusy = true;

			var ipCount = this.ScanAddressTo - this.ScanAddressFrom + 1;
			var countLockObj = new object();
			var completedIp = 0;

			var cancelToken = new CancellationTokenSource();
			await Task.Run( async () => {
				// ネットワークアドレスからブロードキャストアドレスまでのIPアドレスに対して処理を行う。
				foreach (var ipAddress in Enumerable.Range(0, (int)(this.ScanAddressTo - this.ScanAddressFrom)).Select(x => this.ScanAddressFrom + x)) {

					// ARP要求送信前待機
					await Task.Delay(this.ArpIntervalMilliSeconds);
					
					// ARP要求パケット作成
					var arpRequestPacket = PacketBuilder.Build(
						DateTime.Now,
						new EthernetLayer {
							Source = new MacAddress(
								string.Join(
									":",
									this.TargetNic
										.PhysicalAddress
										.GetAddressBytes()
										.Select(x => $"{x:X2}")
								)
							),
							Destination = new MacAddress(
								"FF:FF:FF:FF:FF:FF"
							),
							EtherType = EthernetType.Arp
						},
						new ArpLayer {
							ProtocolType = EthernetType.IpV4,
							SenderHardwareAddress = this.TargetNic.PhysicalAddress.GetAddressBytes().AsReadOnly(),
							SenderProtocolAddress = this.TargetNic.UnicastAddress.Address.GetAddressBytes().AsReadOnly(),
							TargetHardwareAddress = Enumerable.Repeat<byte>(0, 6).ToList().AsReadOnly(),
							TargetProtocolAddress = ipAddress.GetAddressBytes().AsReadOnly(),
							Operation = ArpOperation.Request
						}
					);

					// ARP要求パケット送信
					this._communicator.SendPacket(arpRequestPacket);

					// 進捗率更新
					lock (countLockObj) {
						this.ProgressRate = (int)(++completedIp * 100 / ipCount);
					}
				}
			});

			this.IsBusy = false;
		}

		/// <summary>
		/// デバイスリストクリア
		/// </summary>
		public void Clear() {
			this.Devices.Clear();
		}

		/// <summary>
		/// NIC変更時処理
		/// </summary>
		private void ChangeNic() {
			if (this.TargetNic == null) {
				return;
			}

			// 送受信に使用するNIC
			var nic = LivePacketDevice.AllLocalMachine.First(x => x.Name.Contains(this.TargetNic.NetworkInterface.Id));

			// NIC変更前の受信処理を終了させる。
			this._communicator?.Dispose();
			this._receiveTask?.Dispose();

			// 新しいNICをオープンする
			this._communicator = nic.Open();

			// 受信処理 communicatorが破棄されるとInvalidOperationExceptionが発生し、受信処理が終了する。
			// やり方がわからず無理やりやっている。これでいいのか？不明
			this._receiveTask = Task.Run(() => {
				try {
					this._communicator.ReceivePackets(0, this.DeviceRegister);
				} catch (InvalidOperationException) {
				}
			});
		}

		/// <summary>
		/// 機器登録
		/// </summary>
		/// <param name="packet">受信ARPパケット</param>
		private void DeviceRegister(Packet packet) {
			// イーサネットタイプがARP以外は破棄
			if (packet.Ethernet.EtherType != EthernetType.Arp) {
				return;
			}

			var arp = packet.Ethernet.Arp;

			lock (this.lockObj) {
				// 送信元MACアドレスが登録済みの場合は破棄
				if (this.Devices.Any(x => x.MacAddress.SequenceEqual(arp.SenderHardwareAddress.ToArray()))) {
					return;
				}

				this.Devices.Add(new Device() {
					IPAddress = new CalculatableIPAddress(arp.SenderProtocolAddress.ToArray()),
					MacAddress = arp.SenderHardwareAddress.ToArray()
				});

				// 時間のかかるベンダ検索、ホスト名逆引き処理をTask化
				this.taskList.Add(
					Task.Run(() => {
						using (var dataContext = new VendorDbContext(this._vendorDbPath)) {
							foreach (var device in this.Devices) {
								var vendorId = string.Join("", device.MacAddress.Take(3).Select(m => $"{m:X2}"));
								device.Vendor = dataContext.Vendors.FirstOrDefault(x => x.Assignment == vendorId);
								try {
#pragma warning disable CS0618 // 型またはメンバーが古い形式です
									// Dns.GetHostEntryは正引き出来ないと失敗となるためDns.GetHostByAddressを使用する。
									device.HostName = (Dns.GetHostByAddress(device.IPAddress)).HostName;
#pragma warning restore CS0618 // 型またはメンバーが古い形式です
								} catch (SocketException) {

								}
							}
						}
					})
				);

				// 完了済みの削除
				this.taskList.RemoveAll(task =>
					task.Status == TaskStatus.RanToCompletion ||
					task.Status == TaskStatus.Canceled ||
					task.Status == TaskStatus.Faulted);
			}
		}

		/// <summary>
		/// NIC情報保持クラス
		/// </summary>
		public class NicInformation {

			public NetworkInterface NetworkInterface {
				get;
				set;
			}

			public IPInterfaceProperties IPInterfaceProperties {
				get;
				set;
			}

			public PhysicalAddress PhysicalAddress {
				get;
				set;
			}

			public UnicastIPAddressInformation UnicastAddress {
				get;
				set;
			}

			public CalculatableIPAddress NetworkAddress {
				get {
					if (this.UnicastAddress == null) {
						return null;
					}
					return this.UnicastAddress.Address.ToCalculatableIPAddress() & this.UnicastAddress.IPv4Mask.ToCalculatableIPAddress();
				}
			}
		}
	}

}
