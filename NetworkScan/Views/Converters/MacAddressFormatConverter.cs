using System;
using System.Linq;
using System.Net.NetworkInformation;
using System.Windows.Data;

namespace SandBeige.NetworkScan.Views.Converters {
	class MacAddressFormatConverter : IValueConverter {

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
			if (value is PhysicalAddress mac) {
				return string.Join("-", mac.GetAddressBytes().Select(x => $"{x:X2}"));
			} else if (value is byte[] bytes) {
				return string.Join("-", bytes.Take(6).Select(x => $"{x:X2}"));
			}
			return value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
			throw new NotImplementedException();
		}

	}
}
