using System.Globalization;
using System.Linq;
using System.Windows.Controls;

namespace SandBeige.NetworkScan.Views.ValidationRules {
	class IPAddressValidationRule : ValidationRule {
		public override ValidationResult Validate(object value, CultureInfo cultureInfo) {
			if (value is string v) {
				var octets = v.Split('.').SelectMany(x => byte.TryParse(x, out var b) ? new[] { b } : new byte[] { });
				if (octets.Count() == 4 &&
					!v.StartsWith(".") &&
					!v.EndsWith(".")) {
					return ValidationResult.ValidResult;
				}
			}
			return new ValidationResult(false, "Invalid Format");

		}
	}
}
