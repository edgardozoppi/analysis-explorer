using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Explorer
{
	public static class TreeViewHelper
	{
		private static ISet<TreeView> registeredControls = new HashSet<TreeView>();
		private static bool fromEventHandler = false;

		// Using a DependencyProperty as the backing store for SelectedItem. This enables animation, styling, binding, etc...
		public static readonly DependencyProperty SelectedItemProperty =
			DependencyProperty.RegisterAttached("SelectedItem", typeof(object), typeof(TreeViewHelper), new PropertyMetadata(OnSelectedItemChanged));

		public static object GetSelectedItem(DependencyObject obj)
		{
			return obj.GetValue(SelectedItemProperty);
		}

		public static void SetSelectedItem(DependencyObject obj, object value)
		{
			obj.SetValue(SelectedItemProperty, value);
		}

		private static void OnSelectedItemChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
		{
			var treeView = obj as TreeView;
			if (treeView == null || fromEventHandler) return;

			var ok = registeredControls.Add(treeView);

			if (ok)
			{
				treeView.SelectedItemChanged += (sender, ev) =>
				{
					fromEventHandler = true;
					SetSelectedItem(treeView, ev.NewValue);
					fromEventHandler = false;
				};
			}

			var item = (TreeViewItem)treeView.ItemContainerGenerator.ContainerFromItem(e.NewValue);
			item.IsSelected = true;
			item.Focus();
		}
	}
}
