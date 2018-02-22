using System;
using System.Linq;
using System.Windows.Data;

namespace SandBeige.NetworkScan.Views.Converters {
	class CalculatableIPAddressConverter : IValueConverter {

		public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
			if (value is CalculatableIPAddress ip) {
				return ip.ToString();
			}
			return "";
		}

		public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture) {
			if(value is string ipText) {
				return new CalculatableIPAddress(ipText.Split('.').Select(x=>byte.Parse(x)).ToArray());
			}
			return null;
		}

	}
}
