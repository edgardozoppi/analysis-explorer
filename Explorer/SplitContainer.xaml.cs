using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Explorer
{
	/// <summary>
	/// Interaction logic for SplitContainer.xaml
	/// </summary>
	public partial class SplitContainer : UserControl
	{
		public static readonly DependencyProperty ItemsSourceProperty =
			DependencyProperty.Register("ItemsSource", typeof(IEnumerable), typeof(SplitContainer), new PropertyMetadata(OnItemsSourceChanged));

		private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
		{
			var self = d as SplitContainer;
			self.OnItemsSourceChanged(e.OldValue as IEnumerable, e.NewValue as IEnumerable);
		}

		public SplitContainer()
		{
			InitializeComponent();

			this.Loaded += SplitContainer_Loaded;
		}

		public IEnumerable ItemsSource
		{
			get { return (IEnumerable)GetValue(ItemsSourceProperty); }
			set { SetValue(ItemsSourceProperty, value); }
		}

		private void SplitContainer_Loaded(object sender, RoutedEventArgs e)
		{
			Refresh();
		}

		protected void OnItemsSourceChanged(IEnumerable oldValue, IEnumerable newValue)
		{
			Refresh();

			if (oldValue is INotifyCollectionChanged)
			{
				var source = oldValue as INotifyCollectionChanged;
				source.CollectionChanged -= Source_CollectionChanged;
			}

			if (newValue is INotifyCollectionChanged)
			{
				var source = newValue as INotifyCollectionChanged;
				source.CollectionChanged += Source_CollectionChanged;
			}
		}

		private void Source_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
		{
			Refresh();
		}

		private void Refresh()
		{
			var grid = this.MainGrid;

			grid.Children.Clear();
			grid.ColumnDefinitions.Clear();

			if (this.ItemsSource == null) return;
			var i = 0;

			foreach (var item in this.ItemsSource)
			{
				ColumnDefinition column;

				// Adding splitter
				if (i > 0)
				{
					column = new ColumnDefinition()
					{
						Width = new GridLength(5)
					};

					grid.ColumnDefinitions.Add(column);

					var splitter = new GridSplitter()
					{
						HorizontalAlignment = HorizontalAlignment.Stretch
					};

					grid.Children.Add(splitter);

					Grid.SetColumn(splitter, i);
					i++;
				}

				column = new ColumnDefinition();
				grid.ColumnDefinitions.Add(column);

				UIElement container;

				if (item is UIElement)
				{
					container = item as UIElement;
				}
				else
				{
					container = new ContentPresenter()
					{
						Content = item
					};
				}

				grid.Children.Add(container);

				Grid.SetColumn(container, i);
				i++;
			}
		}
	}
}
