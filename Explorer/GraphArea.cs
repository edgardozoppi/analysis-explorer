using GraphX.PCL.Common.Enums;
using GraphX.PCL.Common.Interfaces;
using GraphX.PCL.Logic.Algorithms.EdgeRouting;
using GraphX.PCL.Logic.Algorithms.LayoutAlgorithms;
using GraphX.PCL.Logic.Algorithms.OverlapRemoval;
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
		public GraphLogic()
		{
			this.EdgeCurvingEnabled = true;
			this.EnableParallelEdges = true;

			SetOverlapRemovalAlgorithmType(OverlapRemovalAlgorithmTypeEnum.FSA);
			SetEdgeRoutingAlgorithmType(EdgeRoutingAlgorithmTypeEnum.SimpleER);
		}

		public GraphLogic(Graph graph, LayoutAlgorithmTypeEnum layoutAlgorithmType)
			: this()
		{
			this.Graph = graph;
			SetLayoutAlgorithmType(layoutAlgorithmType);
		}

		public void SetOverlapRemovalAlgorithmType(OverlapRemovalAlgorithmTypeEnum overlapRemovalAlgorithmType)
		{
			IOverlapRemovalParameters overlapRemovalAlgorithmParameters = null;

			switch (overlapRemovalAlgorithmType)
			{
				case OverlapRemovalAlgorithmTypeEnum.FSA:
					overlapRemovalAlgorithmParameters = new OverlapRemovalParameters()
					{
						HorizontalGap = 50,
						VerticalGap = 50
					};
					break;

				default:
					overlapRemovalAlgorithmParameters = this.AlgorithmFactory.CreateOverlapRemovalParameters(overlapRemovalAlgorithmType);
					break;
			}

			this.DefaultOverlapRemovalAlgorithmParams = overlapRemovalAlgorithmParameters;
			this.DefaultOverlapRemovalAlgorithm = overlapRemovalAlgorithmType;
		}

		public void SetEdgeRoutingAlgorithmType(EdgeRoutingAlgorithmTypeEnum edgeRoutingAlgorithmType)
		{
			IEdgeRoutingParameters edgeRoutingAlgorithmParameters = null;

			switch (edgeRoutingAlgorithmType)
			{
				case EdgeRoutingAlgorithmTypeEnum.SimpleER:
					edgeRoutingAlgorithmParameters = new SimpleERParameters()
					{
						SideStep = 1,
						BackStep = 1
					};
					break;

				default:
					edgeRoutingAlgorithmParameters = this.AlgorithmFactory.CreateEdgeRoutingParameters(edgeRoutingAlgorithmType);
					break;
			}

			this.DefaultEdgeRoutingAlgorithmParams = edgeRoutingAlgorithmParameters;
			this.DefaultEdgeRoutingAlgorithm = edgeRoutingAlgorithmType;
		}

		public void SetLayoutAlgorithmType(LayoutAlgorithmTypeEnum layoutAlgorithmType)
		{
			ILayoutParameters layoutAlgorithmParameters = null;

			switch (layoutAlgorithmType)
			{
				case LayoutAlgorithmTypeEnum.Tree:
					layoutAlgorithmParameters = new SimpleTreeLayoutParameters()
					{
						Direction = LayoutDirection.LeftToRight,
						LayerGap = 50,
						VertexGap = 50,
						SpanningTreeGeneration = SpanningTreeGeneration.BFS
					};
					break;

				case LayoutAlgorithmTypeEnum.EfficientSugiyama:
					layoutAlgorithmParameters = new EfficientSugiyamaLayoutParameters()
					{
						Direction = LayoutDirection.LeftToRight,
						LayerDistance = 50,
						VertexDistance = 50,
					};
					break;

				case LayoutAlgorithmTypeEnum.Sugiyama:
					layoutAlgorithmParameters = new SugiyamaLayoutParameters()
					{
						HorizontalGap = 50,
						VerticalGap = 50,
						Simplify = true
					};
					break;

				case LayoutAlgorithmTypeEnum.SimpleRandom:
					{
						var k = 25 * this.Graph.VertexCount;
						layoutAlgorithmParameters = new RandomLayoutAlgorithmParams()
						{
							Bounds = new GraphX.Measure.Rect(0, 0, k, k)
						};
						break;
					}

				default:
					layoutAlgorithmParameters = this.AlgorithmFactory.CreateLayoutParameters(layoutAlgorithmType);
					break;
			}

			this.DefaultLayoutAlgorithmParams = layoutAlgorithmParameters;
			this.DefaultLayoutAlgorithm = layoutAlgorithmType;
		}
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

			SetVerticesDrag(true, true);
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
					//this.LogicCore.DefaultLayoutAlgorithmParams = null;
					//this.LogicCore.DefaultLayoutAlgorithm = value.Value;
					var logicCore = this.LogicCore as GraphLogic;
					logicCore.SetLayoutAlgorithmType(value.Value);
					this.GenerateGraph();
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
