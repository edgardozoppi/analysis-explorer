using System;
using System.Globalization;
using System.Windows.Input;

namespace Explorer
{
	interface IUICommand
	{
		string Category { get; }
		bool IsSeparator { get; }
	}

	class UICommandSeparator : IUICommand
	{
		public string Category { get; private set; }

		public UICommandSeparator(string category)
		{
			this.Category = category;
		}

		public bool IsSeparator
		{
			get { return true; }
		}
	}

	class MenuCommand : DelegateCommand, IUICommand
	{
		public string Name { get; private set; }
		public KeyGesture Shortcut { get; private set; }
		public string Category { get; private set; }
		public string IconPath { get; private set; }

		public MenuCommand(string category, string name, string iconPath, KeyGesture shortcut, Action<object> OnExecute, Func<object, bool> OnCanExecute = null)
			: base(OnExecute, OnCanExecute)
		{
			this.Name = name;
			this.IconPath = iconPath;
			this.Shortcut = shortcut;
			this.Category = category;
		}

		public bool IsSeparator
		{
			get { return false; }
		}

		public string DisplayName
		{
			get { return this.Name.Replace("_", string.Empty); }
		}

		public string ShortcutText
		{
			get { return this.Shortcut.GetDisplayStringForCulture(CultureInfo.CurrentUICulture); }
		}
	}

	class ToolBarCommand : MenuCommand
	{
		public ToolBarCommand(string category, string name, string iconPath, KeyGesture shortcut, Action<object> OnExecute, Func<object, bool> OnCanExecute = null)
			: base(category, name, iconPath, shortcut, OnExecute, OnCanExecute)
		{
		}

		public string ToolTip
		{
			get { return string.Format("{0} ({1})", this.DisplayName, this.ShortcutText); }
		}
	}
}