using GraphX.PCL.Common.Enums;
using QuickGraph;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Windows;
using System.ComponentModel;

namespace Explorer
{
	class GraphDocumentViewModel : DocumentViewModelBase
	{
		private string name;

		public string Kind { get; private set; }
		public string DGML { get; private set; }
		public GraphLogic LogicCore { get; private set; }

		public GraphDocumentViewModel(MainViewModel main, string kind, string name, string dgml, LayoutAlgorithmTypeEnum layoutType)
			: base(main)
		{
			this.name = name;
			this.Kind = kind;
			this.DGML = dgml;

			var graph = Extensions.CreateGraphFromDGML(dgml);
			this.LogicCore = new GraphLogic(graph, layoutType);
		}

		public override string Name
		{
			get { return name; }
		}
	}

	public class EdgeViewModelBase : GraphX.PCL.Common.Models.EdgeBase<VertexViewModelBase>
	{
		public string Label { get; set; }

		public EdgeViewModelBase(VertexViewModelBase source, VertexViewModelBase target)
			: base(source, target)
		{
		}
	}

	public class VertexViewModelBase : GraphX.PCL.Common.Models.VertexBase
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
}
