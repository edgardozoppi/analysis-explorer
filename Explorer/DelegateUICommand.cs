using System;
using System.Globalization;
using System.Windows.Input;

namespace Explorer
{
	class DelegateUICommand : DelegateCommand
	{
		public DelegateUICommand(string name, Action<object> OnClose, Func<object, bool> OnCanExecute = null)
			: base(OnClose, OnCanExecute)
		{
			this.Name = name;
		}

		public DelegateUICommand(string name, KeyGesture shortcut, Action<object> OnClose, Func<object, bool> OnCanExecute = null)
			: this(name, OnClose, OnCanExecute)
		{
			this.Shortcut = shortcut;
		}

		public string Name { get; private set; }
		public KeyGesture Shortcut { get; private set; }

		public string ShortcutText
		{
			get { return this.Shortcut.GetDisplayStringForCulture(CultureInfo.CurrentUICulture); }
		}
	}
}