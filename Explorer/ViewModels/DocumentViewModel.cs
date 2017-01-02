using Backend.Analyses;
using Backend.Model;
using Backend.Serialization;
using Backend.Transformations;
using Backend.Utils;
using GraphX.PCL.Common.Enums;
using Model;
using Model.Types;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace Explorer
{
	abstract class DocumentViewModelBase : ViewModelBase
	{
		public MainViewModel Main { get; private set; }
		public abstract string Name { get; }

		public ICommand CloseCommand { get; private set; }

		public DocumentViewModelBase(MainViewModel main)
		{
			this.Main = main;
			this.CloseCommand = new DelegateCommand(OnClose);
		}

		private void OnClose(object obj)
		{
			this.Main.Documents.Remove(this);
		}
	}

	class TextDocumentViewModel : DocumentViewModelBase
	{
		private string name;

		public string Kind { get; private set; }
		public string Text { get; private set; }

		public TextDocumentViewModel(MainViewModel main, string kind, string name, string text)
			: base(main)
		{
			this.Kind = kind;
			this.name = name;
			this.Text = text;
		}

		public override string Name
		{
			get { return name; }
		}
	}

	class MethodDocumentViewModel : DocumentViewModelBase
	{
		private MethodDefinition method;
		private IList<MethodBodyViewModel> bodies;

		public MethodDocumentViewModel(MainViewModel main, MethodDefinition method)
			: base(main)
		{
			this.method = method;
			this.bodies = new List<MethodBodyViewModel>();

			Analyze();
		}

		public override string Name
		{
			get { return method.ToFullDisplayName(); }
		}

		public string Kind
		{
			get { return "All"; }
		}

		public IEnumerable<MethodBodyViewModel> Bodies
		{
			get { return bodies; }
		}

		public IEnumerable<object> VisibleBodies
		{
			get { return bodies.Where(vm => vm.IsVisible); }
		}

		private void Analyze()
		{
			var methodInfo = this.Main.ProgramAnalysisInfo.GetOrAdd(method);

			//GenerateIL(methodInfo);
			//GenerateTAC(methodInfo);
			//GenerateCFG(methodInfo);
			//GenerateWebs(methodInfo);
			//GenerateSSA(methodInfo);
			GeneratePTG(methodInfo);

			var text = methodInfo.Get<string>("IL_TEXT");
			var vm = new MethodBodyViewModel(this, "IL Bytecode", text);
			bodies.Add(vm);

			text = methodInfo.Get<string>("TAC_TEXT");
			vm = new MethodBodyViewModel(this, "Three Address Code", text);
			bodies.Add(vm);

			text = methodInfo.Get<string>("WEBS_TEXT");
			vm = new MethodBodyViewModel(this, "Webbed Three Address Code", text);
			bodies.Add(vm);

			text = methodInfo.Get<string>("SSA_TEXT");
			vm = new MethodBodyViewModel(this, "Static Single Assignment", text);
			bodies.Add(vm);

			text = methodInfo.Get<string>("CFG_TEXT");
			vm = new MethodGraphViewModel(this, "Control-Flow Graph", text, LayoutAlgorithmTypeEnum.EfficientSugiyama);
			bodies.Add(vm);

			text = methodInfo.Get<string>("PTG_TEXT");
			vm = new MethodGraphViewModel(this, "Points-To Graph", text, LayoutAlgorithmTypeEnum.LinLog);
			bodies.Add(vm);
		}

		private void GenerateIL(MethodAnalysisInfo methodInfo)
		{
			if (!methodInfo.Contains("IL_TEXT"))
			{
				var text = method.Body.ToString();

				methodInfo.Add("IL_TEXT", text);
			}
		}

		private void GenerateTAC(MethodAnalysisInfo methodInfo)
		{
			GenerateIL(methodInfo);

			if (!methodInfo.Contains("TAC"))
			{
				var dissasembler = new Disassembler(method);
				var body = dissasembler.Execute();
				var text = body.ToString();

				methodInfo.Add("TAC", body);
				methodInfo.Add("TAC_TEXT", text);
			}
		}

		private void GenerateCFG(MethodAnalysisInfo methodInfo)
		{
			//GenerateIL(methodInfo);
			GenerateTAC(methodInfo);

			if (!methodInfo.Contains("CFG"))
			{
				var body = methodInfo.Get<MethodBody>("TAC");

				// Control-flow
				var cfAnalysis = new ControlFlowAnalysis(body);
				var cfg = cfAnalysis.GenerateNormalControlFlow();
				//var cfg = cfAnalysis.GenerateExceptionalControlFlow();

				var domAnalysis = new DominanceAnalysis(cfg);
				domAnalysis.Analyze();
				domAnalysis.GenerateDominanceTree();

				var loopAnalysis = new NaturalLoopAnalysis(cfg);
				loopAnalysis.Analyze();

				var domFrontierAnalysis = new DominanceFrontierAnalysis(cfg);
				domFrontierAnalysis.Analyze();

				//var text = DGMLSerializer.Serialize(cfg);

				methodInfo.Add("CFG", cfg);
				//methodInfo.Add("CFG_TEXT", text);
			}
		}

		private void GenerateWebs(MethodAnalysisInfo methodInfo)
		{
			//GenerateIL(methodInfo);
			//GenerateTAC(methodInfo);
			GenerateCFG(methodInfo);

			if (!methodInfo.Contains("WEBS_TEXT"))
			{
				var body = methodInfo.Get<MethodBody>("TAC");
				var cfg = methodInfo.Get<ControlFlowGraph>("CFG");

				// Webs
				var splitter = new WebAnalysis(cfg);
				splitter.Analyze();
				splitter.Transform();

				body.UpdateVariables();

				var typeAnalysis = new TypeInferenceAnalysis(cfg);
				typeAnalysis.Analyze();

				var text = body.ToString();
				methodInfo.Add("WEBS_TEXT", text);

				//text = DGMLSerializer.Serialize(cfg);
				//methodInfo.Set("CFG_TEXT", text);
			}
		}

		private void GenerateSSA(MethodAnalysisInfo methodInfo)
		{
			//GenerateIL(methodInfo);
			//GenerateTAC(methodInfo);
			//GenerateCFG(methodInfo);
			GenerateWebs(methodInfo);

			if (!methodInfo.Contains("SSA_TEXT"))
			{
				var body = methodInfo.Get<MethodBody>("TAC");
				var cfg = methodInfo.Get<ControlFlowGraph>("CFG");

				// Live Variables
				var liveVariables = new LiveVariablesAnalysis(cfg);
				liveVariables.Analyze();

				// SSA
				var ssa = new StaticSingleAssignment(body, cfg);
				ssa.Transform();
				ssa.Prune(liveVariables);

				body.UpdateVariables();

				var text = body.ToString();
				methodInfo.Add("SSA_TEXT", text);

				text = DGMLSerializer.Serialize(cfg);
				methodInfo.Set("CFG_TEXT", text);
			}
		}

		private void GeneratePTG(MethodAnalysisInfo methodInfo)
		{
			//GenerateIL(methodInfo);
			//GenerateTAC(methodInfo);
			//GenerateCFG(methodInfo);
			//GenerateWebs(methodInfo);
			GenerateSSA(methodInfo);

			if (!methodInfo.Contains("PTG_TEXT"))
			{
				var cfg = methodInfo.Get<ControlFlowGraph>("CFG");

				// Points-to
				var pointsTo = new PointsToAnalysis(cfg, method);
				var result = pointsTo.Analyze();

				var ptg = result[cfg.Exit.Id].Output;
				//ptg.RemoveVariablesExceptParameters();
				//ptg.RemoveTemporalVariables();

				var text = DGMLSerializer.Serialize(ptg);

				methodInfo.Add("PTG", ptg);
				methodInfo.Set("PTG_TEXT", text);
			}
		}
	}

	class MethodBodyViewModel : ViewModelBase
	{
		private MethodDocumentViewModel parent;
		private bool isVisible;

		public string Name { get; private set; }
		public string Text { get; private set; }

		public MethodBodyViewModel(MethodDocumentViewModel parent, string name, string text)
		{
			this.parent = parent;
			this.Name = name;
			this.Text = text;
			this.isVisible = true;
		}

		public bool IsVisible
		{
			get { return isVisible; }
			set
			{
				SetProperty(ref isVisible, value);
				parent.OnPropertyChanged(nameof(parent.VisibleBodies));
			}
		}
	}

	class MethodGraphViewModel : MethodBodyViewModel
	{
		public GraphLogic LogicCore { get; private set; }

		public MethodGraphViewModel(MethodDocumentViewModel parent, string name, string text, LayoutAlgorithmTypeEnum layoutType)
			: base(parent, name, text)
		{
			this.LogicCore = new GraphLogic()
			{
				DefaultLayoutAlgorithm = layoutType,
				DefaultOverlapRemovalAlgorithm = OverlapRemovalAlgorithmTypeEnum.FSA,
				DefaultEdgeRoutingAlgorithm = EdgeRoutingAlgorithmTypeEnum.SimpleER,
				EdgeCurvingEnabled = true,
				EnableParallelEdges = true,
				Graph = Extensions.CreateGraphFromDGML(text)
			};
		}
	}
}