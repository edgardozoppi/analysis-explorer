using System;
using System.Windows.Input;

namespace Explorer
{
	class DelegateCommand : ICommand
	{
		private Func<object, bool> OnCanExecute;
		private Action<object> OnClose;

		public DelegateCommand(Action<object> OnClose, Func<object, bool> OnCanExecute = null)
		{
			this.OnClose = OnClose;
			this.OnCanExecute = OnCanExecute;
		}

		public event EventHandler CanExecuteChanged;

		public bool CanExecute(object parameter)
		{
			var result = true;

			if (OnCanExecute != null)
			{
				result = OnCanExecute(parameter);
			}

			return result;
		}

		public void Execute(object parameter)
		{
			OnClose(parameter);
		}
	}
}