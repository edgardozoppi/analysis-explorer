using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Explorer
{
	public class MenuTemplateSelector : ItemContainerTemplateSelector
	{
		public override DataTemplate SelectTemplate(object item, ItemsControl parentItemsControl)
		{
			DataTemplate result;

			if (item == null)
			{
				result = parentItemsControl.FindResource("SeparatorMenuTemplate") as DataTemplate;
			}
			else
			{
				result = base.SelectTemplate(item, parentItemsControl);
			}

			return result;
		}
	}
}
