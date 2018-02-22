using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

using CsvHelper;

using Livet;

using Microsoft.EntityFrameworkCore;

using SandBeige.NetworkScan.DataBase;

namespace SandBeige.NetworkScan.Models {
	public class VendorListMaintenance : NotificationObject {

		/// <summary>
		/// MACアドレス取得先URL
		/// </summary>
		private readonly Uri _macAddressUrl;

		/// <summary>
		/// ベンダ一覧DB保管フルパス
		/// </summary>
		private readonly string _vendorDbPath;

		/// <summary>
		/// コンストラクタ
		/// </summary>
		/// <param name="vendorDbPath">ベンダ一覧DB保管フルパス</param>
		/// <param name="macAddressUrl">MACアドレス取得先URL</param>
		public VendorListMaintenance(string vendorDbPath, string macAddressUrl) {
			this._vendorDbPath = vendorDbPath;
			this._macAddressUrl = new Uri(macAddressUrl);
		}

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

		/// <summary>
		/// ベンダリストアップデート
		/// </summary>
		/// <returns></returns>
		public async Task UpdateVendors() {
			//完全に判定するならばMA-L,MA-M,MA-Sをそれぞれ更新する必要がある
			//MA-SをMA-Mにも登録
			//MA-MをMA-Lにも登録する？
			this.IsBusy = true;
			var hc = new HttpClient();
			var res = await hc.GetAsync(this._macAddressUrl, HttpCompletionOption.ResponseHeadersRead);

			//全体のファイルサイズ
			var fullsize = (long)res.Content.Headers.ContentLength;

			using (var httpStream = await res.Content.ReadAsStreamAsync())
			using (var sr = new StreamReader(httpStream))
			using (var csvReader = new CsvReader(sr))
			using (var dataContext = new VendorDbContext(this._vendorDbPath)) {
				using (var transaction = dataContext.Database.BeginTransaction()) {

					// ベンダIDが被ってるものがある。詳細は不明なため、2件目以降はスキップする。
					// 登録済みのベンダIDを退避しておく。
					var insertedAssignments = new List<string>();

					// 現在データ全件DELETE
					await dataContext.Database.ExecuteSqlCommandAsync("DELETE FROM Vendors;");

					csvReader.Configuration.HasHeaderRecord = true;
					csvReader.Configuration.RegisterClassMap<VendorMapper>();

					await Task.Run(() => {
						foreach (var vendor in csvReader.GetRecords<Vendor>()) {
							if (insertedAssignments.All(x => x != vendor.Assignment)) {
								dataContext.Vendors.Add(vendor);
								insertedAssignments.Add(vendor.Assignment);
							}
							this.ProgressRate = (int)(((double)csvReader.Context.CharPosition / fullsize) * 100);
						}
					});
					await dataContext.SaveChangesAsync();
					transaction.Commit();
				}
				this.IsBusy = false;
			}
		}

		private class VendorMapper : CsvHelper.Configuration.ClassMap<Vendor> {
			public VendorMapper() {
				Map(x => x.Assignment).Index(1);
				Map(x => x.OrganizationName).Index(2).Name("Organization Name");
				Map(x => x.OrganizationAddress).Index(3).Name("Organization Address");
			}
		}
	}
}
