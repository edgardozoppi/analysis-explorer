using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace Explorer
{
	public class WrapPanelLastChildFill : WrapPanel
	{
		public bool LastChildFill
		{
			get { return (bool)GetValue(LastChildFillProperty); }
			set { SetValue(LastChildFillProperty, value); }
		}

		public static readonly DependencyProperty LastChildFillProperty =
			DependencyProperty.Register("LastChildFill", typeof(bool), typeof(WrapPanelLastChildFill), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.AffectsMeasure));

		protected override Size MeasureOverride(Size constraint)
		{
			var width = double.IsPositiveInfinity(constraint.Width) ? 0 : constraint.Width;
			Size panelSize = new Size(width, 0);
			Size curLineSize = new Size();

			UIElementCollection children = this.InternalChildren;

			for (int i = 0; i < children.Count; i++)
			{
				UIElement child = children[i] as UIElement;

				if (child == null) // when clear ItemsSource collection
					continue;

				// Flow passes its own constraint to children
				child.Measure(constraint);
				Size sz = child.DesiredSize;

				if (curLineSize.Width + sz.Width > constraint.Width) //need to switch to another line
				{
					panelSize.Width = Math.Max(curLineSize.Width, panelSize.Width);
					panelSize.Height += curLineSize.Height;

					curLineSize = sz;

					if (curLineSize.Width > constraint.Width) // if the element is wider than the constraint - give it a separate line                    
					{
						panelSize.Width = Math.Max(curLineSize.Width, panelSize.Width);
						panelSize.Height += curLineSize.Height;
						curLineSize = new Size();
					}
				}
				else //continue to accumulate a line
				{
					curLineSize.Width += sz.Width;
					curLineSize.Height = Math.Max(sz.Height, curLineSize.Height);
				}
			}
			// the last line size, if any need to be added
			panelSize.Width = Math.Max(curLineSize.Width, panelSize.Width);
			panelSize.Height += curLineSize.Height;

			return panelSize;
		}

		protected override Size ArrangeOverride(Size arrangeBounds)
		{
			int firstChildInLine = 0;
			Size curLineSize = new Size();
			double accumulatedHeight = 0;
			UIElementCollection children = this.InternalChildren;

			for (int i = 0; i < children.Count; i++)
			{
				UIElement child = children[i] as UIElement;

				if (child == null)
					continue;

				Size sz = child.DesiredSize;

				if (curLineSize.Width + sz.Width > arrangeBounds.Width) //need to switch to another line
				{
					ArrangeLine(accumulatedHeight, curLineSize, arrangeBounds.Width, firstChildInLine, i);

					accumulatedHeight += curLineSize.Height;
					curLineSize = sz;

					if (sz.Width > arrangeBounds.Width) //the element is wider then the constraint - give it a separate line                    
					{
						ArrangeLine(accumulatedHeight, sz, arrangeBounds.Width, i, ++i);
						accumulatedHeight += sz.Height;
						curLineSize = new Size();
					}
					firstChildInLine = i;
				}
				else //continue to accumulate a line
				{
					curLineSize.Width += sz.Width;
					curLineSize.Height = Math.Max(sz.Height, curLineSize.Height);
				}
			}

			if (firstChildInLine < children.Count)
				ArrangeLine(accumulatedHeight, curLineSize, arrangeBounds.Width, firstChildInLine, children.Count);

			return arrangeBounds;
		}

		private void ArrangeLine(double y, Size lineSize, double boundsWidth, int start, int end)
		{
			double x = 0;
			UIElementCollection children = this.InternalChildren;
			for (int i = start; i < end; i++)
			{
				UIElement child = children[i];

				if (child == null)
					continue;

				var w = child.DesiredSize.Width;
				if (LastChildFill && i == end - 1) // last сhild fills remaining space
				{
					w = boundsWidth - x;
				}
				child.Arrange(new Rect(x, y, w, lineSize.Height));
				x += w;
			}
		}
	}
}
