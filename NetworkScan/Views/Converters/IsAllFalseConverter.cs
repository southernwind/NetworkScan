using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace SandBeige.NetworkScan.Views.Converters {
	public class IsAllFalseConverter : IMultiValueConverter {
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture) {
			return values.OfType<bool>().All(x => !x);
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) {
			throw new NotImplementedException();
		}
	}
}