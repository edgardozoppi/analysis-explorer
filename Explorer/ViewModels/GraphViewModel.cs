using QuickGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Explorer
{
	public class Graph : BidirectionalGraph<VertexViewModelBase, EdgeViewModelBase>
	{
	}

	public class GraphLayout : GraphSharp.Controls.GraphLayout<VertexViewModelBase, EdgeViewModelBase, Graph>
	{
	}

	class GraphDocumentViewModel : DocumentViewModelBase
	{
		private string name;
		private string layoutType;

		public string Kind { get; private set; }
		public string DGML { get; private set; }
		public Graph Graph { get; private set; }

		public GraphDocumentViewModel(MainViewModel main, string kind, string name, string dgml, string layoutType)
			: base(main)
		{
			this.Kind = kind;
			this.name = name;
			this.DGML = dgml;
			this.layoutType = layoutType;

			this.Graph = Extensions.CreateGraphFromDGML(dgml);
		}

		public string LayoutType
		{
			get { return layoutType; }
			set { SetProperty(ref layoutType, value); }
		}

		public override string Name
		{
			get { return name; }
		}
	}

	public class EdgeViewModelBase : ViewModelBase, IEdge<VertexViewModelBase>
	{
		public VertexViewModelBase Source { get; private set; }
		public VertexViewModelBase Target { get; private set; }
		public string Label { get; set; }

		public EdgeViewModelBase(VertexViewModelBase source, VertexViewModelBase target)
		{
			this.Source = source;
			this.Target = target;
		}
	}

	public class VertexViewModelBase : ViewModelBase
	{
		public string Id { get; private set; }
		public string Label { get; private set; }
		public string BackgroundColor { get; set; }

		public VertexViewModelBase(string id, string label)
		{
			this.Id = id;
			this.Label = label;
			this.BackgroundColor = "White";
		}
	}

	//class GraphViewModel<V, E> : ViewModelBase
	//	where E : IEdge<V>
	//{
	//	public IGraph<V, E> Graph { get; private set; }

	//	public GraphViewModel(IGraph<V, E> graph)
	//	{
	//		this.Graph = graph;
	//	}
	//}
}
