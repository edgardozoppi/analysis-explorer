using System;
using System.Windows.Input;

namespace Explorer
{
	class DelegateCommand : ICommand
	{
		private bool lastCanExecute;
		private Func<object, bool> OnCanExecute;
		private Action<object> OnClose;

		public event EventHandler CanExecuteChanged;

		public DelegateCommand(Action<object> OnClose, Func<object, bool> OnCanExecute = null)
		{
			this.OnClose = OnClose;
			this.OnCanExecute = OnCanExecute;
		}

		public bool CanExecute(object parameter)
		{
			var result = true;

			if (OnCanExecute != null)
			{
				result = OnCanExecute(parameter);
			}

			if (result != lastCanExecute)
			{
				lastCanExecute = result;

				if (CanExecuteChanged != null)
				{
					CanExecuteChanged(this, EventArgs.Empty);
				}
			}

			return result;
		}

		public void Execute(object parameter)
		{
			OnClose(parameter);
		}
	}
}