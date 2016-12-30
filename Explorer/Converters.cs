using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Explorer
{
	[ValueConversion(typeof(bool), typeof(GridLength))]
	public class BoolToGridLengthConverter : IValueConverter
	{
		private static GridLength star = new GridLength(1, GridUnitType.Star);
		private static GridLength zero = new GridLength(0);

		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return ((bool)value == true) ? star : zero;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}

	public class BooleanAndConverter : IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			return values.OfType<IConvertible>().All(System.Convert.ToBoolean);
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}

	public class BooleanOrConverter : IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			return values.OfType<IConvertible>().Any(System.Convert.ToBoolean);
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}

	public class AggregateConverter : List<IValueConverter>, IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return this.Aggregate(value, (current, converter) => converter.Convert(current, targetType, parameter, culture));
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}

	public class MapReduceConverter : IMultiValueConverter
	{
		public IMultiValueConverter Map { get; set; }
		public IValueConverter Reduce { get; set; }

		public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
		{
			var convertedValue = this.Map.Convert(values, targetType, parameter, culture);
			return this.Reduce.Convert(convertedValue, targetType, parameter, culture);
		}

		public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}
