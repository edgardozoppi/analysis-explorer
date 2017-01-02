using GraphX.PCL.Common.Enums;
using QuickGraph;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Explorer
{
	public class Graph : BidirectionalGraph<VertexViewModelBase, EdgeViewModelBase>
	{
	}

	public class GraphLogic : GraphX.PCL.Logic.Models.GXLogicCore<VertexViewModelBase, EdgeViewModelBase, Graph>
	{
	}

	public class GraphArea : GraphX.Controls.GraphArea<VertexViewModelBase, EdgeViewModelBase, Graph>, INotifyPropertyChanged
	{
		private DependencyPropertyDescriptor descriptor;

		public event EventHandler LayoutAlgorithmTypeChanged;
		public event PropertyChangedEventHandler PropertyChanged;

		public GraphArea()
		{
			descriptor = DependencyPropertyDescriptor.FromProperty(LogicCoreProperty, typeof(GraphArea));
			descriptor.AddValueChanged(this, OnLogicCoreChanged);
		}

		protected override void OnDispose()
		{
			descriptor.RemoveValueChanged(this, OnLogicCoreChanged);
			base.OnDispose();
		}

		public IEnumerable<LayoutAlgorithmTypeEnum> LayoutAlgorithmTypes
		{
			get
			{
				return Enum.GetValues(typeof(LayoutAlgorithmTypeEnum))
					.Cast<LayoutAlgorithmTypeEnum>()
					.Where(v => v != LayoutAlgorithmTypeEnum.Custom);
			}
		}

		public LayoutAlgorithmTypeEnum? LayoutAlgorithmType
		{
			get
			{
				if (this.LogicCore == null) return null;
				return this.LogicCore.DefaultLayoutAlgorithm;
			}
			set
			{
				if (this.LogicCore != null && value.HasValue)
				{
					this.LogicCore.DefaultLayoutAlgorithmParams = null;
					this.LogicCore.DefaultLayoutAlgorithm = value.Value;
					this.GenerateGraph();
					this.SetVerticesDrag(true, true);
				}

				OnPropertyChanged();

				if (this.LayoutAlgorithmTypeChanged != null)
				{
					this.LayoutAlgorithmTypeChanged(this, new EventArgs());
				}
			}
		}

		private void OnLogicCoreChanged(object sender, EventArgs e)
		{
			if (this.LogicCore != null)
			{
				this.GenerateGraph();
				this.SetVerticesDrag(true, true);
			}

			OnPropertyChanged(nameof(this.LayoutAlgorithmType));

			if (this.LayoutAlgorithmTypeChanged != null)
			{
				this.LayoutAlgorithmTypeChanged(this, new EventArgs());
			}
		}

		private void OnPropertyChanged([CallerMemberName] string propertyName = null)
		{
			if (this.PropertyChanged != null)
			{
				var args = new PropertyChangedEventArgs(propertyName);
				this.PropertyChanged(this, args);
			}
		}
	}
}
