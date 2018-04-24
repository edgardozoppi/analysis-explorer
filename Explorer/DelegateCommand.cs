using System;
using System.Windows.Input;

namespace Explorer
{
	class DelegateCommand : ICommand
	{
		private bool lastCanExecute;
		private Func<object, bool> OnCanExecute;
		private Action<object> OnExecute;

		public event EventHandler CanExecuteChanged;

		public DelegateCommand(Action<object> OnExecute, Func<object, bool> OnCanExecute = null)
		{
			this.OnExecute = OnExecute;
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
			OnExecute(parameter);
		}
	}
}