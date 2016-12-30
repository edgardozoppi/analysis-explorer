using Backend.Analyses;
using Backend.Model;
using Backend.Transformations;
using Backend.Utils;
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

	class DocumentViewModel : DocumentViewModelBase
	{
		private string name;

		public string Kind { get; private set; }
		public string Text { get; private set; }

		public DocumentViewModel(MainViewModel main, string kind, string name, string text)
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
		private IList<MethodBodyDocumentViewModel> bodies;

		public MethodDocumentViewModel(MainViewModel main, MethodDefinition method)
			: base(main)
		{
			this.method = method;
			this.bodies = new List<MethodBodyDocumentViewModel>();

			Analyze();
		}

		public override string Name
		{
			get { return method.ToFullDisplayName(); }
		}

		public IEnumerable<MethodBodyDocumentViewModel> Bodies
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
			GenerateSSA(methodInfo);

			var text = methodInfo.Get<string>("IL_TEXT");
			var vm = new MethodBodyDocumentViewModel(this, "IL Bytecode", text);
			bodies.Add(vm);

			text = methodInfo.Get<string>("TAC_TEXT");
			vm = new MethodBodyDocumentViewModel(this, "Three Address Code", text);
			bodies.Add(vm);

			text = methodInfo.Get<string>("WEBS_TEXT");
			vm = new MethodBodyDocumentViewModel(this, "Webbed Three Address Code", text);
			bodies.Add(vm);

			text = methodInfo.Get<string>("SSA_TEXT");
			vm = new MethodBodyDocumentViewModel(this, "Static Single Assignment", text);
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

				methodInfo.Add("CFG", cfg);
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
			}
		}
	}

	class MethodBodyDocumentViewModel : ViewModelBase
	{
		private MethodDocumentViewModel parent;
		private bool isVisible;

		public string Name { get; private set; }
		public string Text { get; private set; }

		public MethodBodyDocumentViewModel(MethodDocumentViewModel parent, string name, string text)
		{
			this.Name = name;
			this.Text = text;
			this.parent = parent;
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
}