using System;
using System.Globalization;
using System.Windows.Input;

namespace Explorer
{
	class UIDelegateCommand : DelegateCommand
	{
		public string Name { get; private set; }
		public KeyGesture Shortcut { get; private set; }
		public string Category { get; private set; }

		public UIDelegateCommand(string name, Action<object> OnClose, Func<object, bool> OnCanExecute = null)
			: base(OnClose, OnCanExecute)
		{
			this.Name = name;
		}

		public UIDelegateCommand(string name, KeyGesture shortcut, Action<object> OnClose, Func<object, bool> OnCanExecute = null)
			: this(name, OnClose, OnCanExecute)
		{
			this.Shortcut = shortcut;
		}

		public UIDelegateCommand(string category, string name, KeyGesture shortcut, Action<object> OnClose, Func<object, bool> OnCanExecute = null)
			: this(name, shortcut, OnClose, OnCanExecute)
		{
			this.Category = category;
		}

		public string ShortcutText
		{
			get { return this.Shortcut.GetDisplayStringForCulture(CultureInfo.CurrentUICulture); }
		}
	}
}