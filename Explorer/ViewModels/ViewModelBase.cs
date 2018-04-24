using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Explorer
{
	abstract class ViewModelBase : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		public void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			if (this.PropertyChanged != null)
			{
				var args = new PropertyChangedEventArgs(propertyName);
				this.PropertyChanged(this, args);
			}
		}

		protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = "")
		{
			var result = false;

			if (!EqualityComparer<T>.Default.Equals(field, value))
			{
				field = value;
				OnPropertyChanged(propertyName);
				result = true;
			}

			return result;
		}
	}
}
