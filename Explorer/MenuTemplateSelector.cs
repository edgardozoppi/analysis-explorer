using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Explorer
{
	public class MenuItemTemplateSelector : ItemContainerTemplateSelector
	{
		public DataTemplate SeparatorTemplate { get; set; }

		public override DataTemplate SelectTemplate(object item, ItemsControl parentItemsControl)
		{
			DataTemplate result;

			if (item is UICommandSeparator)
			{
				result = this.SeparatorTemplate;
			}
			else
			{
				result = base.SelectTemplate(item, parentItemsControl);
			}

			return result;
		}
	}

	public class ToolBarItemTemplateSelector : DataTemplateSelector
	{
		public DataTemplate SeparatorTemplate { get; set; }

		public override DataTemplate SelectTemplate(object item, DependencyObject container)
		{
			DataTemplate result;

			if (item is UICommandSeparator)
			{
				result = this.SeparatorTemplate;
			}
			else
			{
				result = base.SelectTemplate(item, container);
			}

			return result;
		}
	}
}
