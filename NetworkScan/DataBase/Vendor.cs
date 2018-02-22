using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace SandBeige.NetworkScan.DataBase {
	public class Vendor {
		public Vendor() {

		}

		public Vendor(string assignment, string organizationName, string organizationAddress) {
			this.Assignment = assignment;
			this.OrganizationName = organizationName;
			this.OrganizationAddress = organizationAddress;
		}

		[Key, Column(Order = 0)]
		public string Assignment {
			get;
			set;
		}

		[Column(Order = 1)]
		public string OrganizationName {
			get;
			set;
		}

		[Column(Order = 2)]
		public string OrganizationAddress {
			get;
			set;
		}
	}
}
