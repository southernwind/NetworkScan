using Livet;

using SandBeige.NetworkScan.DataBase;

namespace SandBeige.NetworkScan.Models {
	public class Device : NotificationObject {

		public Device() {

		}
		public Device(byte[] macAddress, CalculatableIPAddress ipAddress, Vendor vendor) {
			this.MacAddress = macAddress;
			this.IPAddress = ipAddress;
			this.Vendor = vendor;
		}

		public byte[] MacAddress {
			get;
			set;
		}

		public CalculatableIPAddress IPAddress {
			get;
			set;
		}

		private string _hostName;
		public string HostName {
			get {
				return this._hostName ?? "";
			}
			set {
				this._hostName = value;
				RaisePropertyChanged();
			}
		}

		private Vendor _vendor;
		public Vendor Vendor {
			get {
				return this._vendor;
			}
			set {
				this._vendor = value;
				RaisePropertyChanged();
			}
		}

	}
}