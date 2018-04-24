using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Explorer
{
	public class ButtonHelper
	{
		// Boilerplate code to register attached property "bool? DialogResult"
		public static bool? GetDialogResult(DependencyObject obj) { return (bool?)obj.GetValue(DialogResultProperty); }
		public static void SetDialogResult(DependencyObject obj, bool? value) { obj.SetValue(DialogResultProperty, value); }
		public static readonly DependencyProperty DialogResultProperty = DependencyProperty.RegisterAttached("DialogResult", typeof(bool?), typeof(ButtonHelper), new UIPropertyMetadata
		{
			PropertyChangedCallback = (obj, e) =>
			{
				var button = obj as Button;

				if (button == null)
				{
					throw new InvalidOperationException("Can only use ButtonHelper.DialogResult on a Button control");
				}

				button.Click += (sender, e2) =>
				{
					var window = Window.GetWindow(button);
					var result = GetDialogResult(button);

					window.DialogResult = result;
					window.Close();
				};
			}
		});
	}
}
