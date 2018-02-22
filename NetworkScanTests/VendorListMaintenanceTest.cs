using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using NUnit.Framework;

using SandBeige.NetworkScan.DataBase;
using SandBeige.NetworkScan.Models;

namespace SandBeige.NetworkScanTests {
	[TestFixture]
	public class VendorListMaintenanceTest {
		[SetUp]
		public void Init() {

		}

		[Test]
		public async Task DownloadOuiCsv() {
			var dbPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "testDb.db");
			var vlm = new VendorListMaintenance(dbPath, "http://localhost/ieee/mac.csv");
			await vlm.UpdateVendors();

			using (var dataContext = new VendorDbContext(dbPath)) {
				var google = dataContext.Vendors.First(x => x.Assignment == "001A11");
				Assert.AreEqual(google.OrganizationName, "Google, Inc.");
				Assert.AreEqual(google.OrganizationAddress, "1600 Amphitheater Parkway Mountain View CA US 94043 ");
			}

		}
	}
}
