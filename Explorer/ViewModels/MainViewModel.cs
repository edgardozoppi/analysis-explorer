using Backend.Analyses;
using Backend.Model;
using Backend.Serialization;
using Backend.Transformations;
using Backend.Utils;
using Microsoft.Win32;
using Model;
using Model.Types;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Explorer
{
	class MainViewModel : ViewModelBase
	{
		private Host host;
		private ILoader loader;
		private ProgramAnalysisInfo programInfo;
		private IDictionary<IMethodReference, MethodBody> originalMethodBodies;

		private ItemViewModelBase activeItem;
		private DocumentViewModelBase activeDocument;
		private OptionsViewModel options;

		public ObservableCollection<AssemblyViewModel> Assemblies { get; private set; }
		public ObservableCollection<DocumentViewModelBase> Documents { get; private set; }
		public IList<IUICommand> Commands { get; private set; }

		// Options
		public bool RemoveUnusedLabels { get; private set; }
		public bool GenerateExceptionalControlFlow { get; private set; }
		public bool RunForwardCopyPropagation { get; private set; }
		public bool RunBackwardCopyPropagation { get; private set; }

		public MainViewModel()
		{
			this.Assemblies = new ObservableCollection<AssemblyViewModel>();
			this.Documents = new ObservableCollection<DocumentViewModelBase>();

			this.Commands = new List<IUICommand>();
			AddCommand("File|ToolBar", "_Open", ModifierKeys.Control, Key.O, OnOpen, icon: "Images/open.png");
			AddSeparator("File");
			AddCommand("File", "_Exit", ModifierKeys.Alt, Key.F4, OnExit);
			AddCommand("View|ToolBar", "_Options", ModifierKeys.Control, Key.Q, OnOptions, icon: "Images/options.png");
			AddSeparator("ToolBar");

			options = new OptionsViewModel(this);

			host = new Host();
			loader = new CCIProvider.Loader(host);
			programInfo = new ProgramAnalysisInfo();
			originalMethodBodies = new Dictionary<IMethodReference, MethodBody>(MethodReferenceDefinitionComparer.Default);

			PlatformTypes.Resolve(host);
		}

		public IEnumerable<IUICommand> FileCommands
		{
			get { return this.Commands.Where(c => c.Category.Contains("File")); }
		}

		public IEnumerable<IUICommand> ViewCommands
		{
			get { return this.Commands.Where(c => c.Category.Contains("View")); }
		}

		public IEnumerable<IUICommand> ToolBarCommands
		{
			get
			{
				var commands = this.Commands.Where(c => c.Category.Contains("ToolBar"));

				if (activeItem != null)
				{
					commands = commands.Concat(activeItem.Commands);
				}

				return commands;
			}
		}

		public ItemViewModelBase ActiveItem
		{
			get { return activeItem; }
			set
			{
				SetProperty(ref activeItem, value);
				OnPropertyChanged(nameof(this.ToolBarCommands));
			}
		}

		public DocumentViewModelBase ActiveDocument
		{
			get { return activeDocument; }
			set { SetProperty(ref activeDocument, value); }
		}

		public void AddDocument(DocumentViewModelBase document)
		{
			this.Documents.Add(document);
			this.ActiveDocument = document;
		}

		public T GetProgramInfo<T>(string key)
		{
			return programInfo.Get<T>(key);
		}

		public T GetMethodInfo<T>(MethodDefinition method, string key)
		{
			var methodInfo = programInfo.GetOrAdd(method);
			return methodInfo.Get<T>(key);
		}

		public void GenerateCH()
		{
			if (!programInfo.Contains("CH_TEXT"))
			{
				var ch = new ClassHierarchy();
				ch.Analyze(host);

				var text = DGMLSerializer.Serialize(ch);

				programInfo.Add("CH", ch);
				programInfo.Add("CH_TEXT", text);
			}
		}

		public void GenerateCG()
		{
			GenerateCH();

			if (!programInfo.Contains("CG_TEXT"))
			{
				var ch = programInfo.Get<ClassHierarchy>("CH");
				var cga = new ClassHierarchyAnalysis(ch);

				cga.OnReachableMethodFound = method =>
				{
					//GenerateIL(method);
					GenerateTAC(method);
				};

				var roots = host.GetRootMethods();
				var cg = cga.Analyze(host, roots);
				var text = DGMLSerializer.Serialize(cg);

				programInfo.Add("CG_TEXT", text);
			}
		}

		public void GenerateIL(MethodDefinition method)
		{
			var methodInfo = programInfo.GetOrAdd(method);

			if (!methodInfo.Contains("IL_TEXT"))
			{
				EnsureMethodHasOriginalBody(method);

				if (options.RemoveUnusedLabels)
				{
					method.Body.RemoveUnusedLabels();
				}

				var text = method.Body.ToString();

				methodInfo.Add("IL_TEXT", text);
			}
		}

		public void GenerateTAC(MethodDefinition method)
		{
			var methodInfo = programInfo.GetOrAdd(method);

			GenerateIL(method);

			if (!methodInfo.Contains("TAC_TEXT"))
			{
				var dissasembler = new Disassembler(method);
				method.Body = dissasembler.Execute();

				if (options.RemoveUnusedLabels)
				{
					method.Body.RemoveUnusedLabels();
				}

				var text = method.Body.ToString();

				methodInfo.Add("TAC_TEXT", text);
			}
		}

		public void GenerateCFG(MethodDefinition method)
		{
			var methodInfo = programInfo.GetOrAdd(method);

			//GenerateIL(method);
			GenerateTAC(method);

			if (!methodInfo.Contains("CFG"))
			{
				// Control-flow
				var cfAnalysis = new ControlFlowAnalysis(method.Body);
				ControlFlowGraph cfg;

				if (options.GenerateExceptionalControlFlow)
				{
					cfg = cfAnalysis.GenerateExceptionalControlFlow();
				}
				else
				{
					cfg = cfAnalysis.GenerateNormalControlFlow();
				}

				var domAnalysis = new DominanceAnalysis(cfg);
				domAnalysis.Analyze();
				domAnalysis.GenerateDominanceTree();

				//// Optional
				//var loopAnalysis = new NaturalLoopAnalysis(cfg);
				//loopAnalysis.Analyze();

				var domFrontierAnalysis = new DominanceFrontierAnalysis(cfg);
				domFrontierAnalysis.Analyze();

				var pdomAnalysis = new PostDominanceAnalysis(cfg);
				pdomAnalysis.Analyze();
				pdomAnalysis.GeneratePostDominanceTree();

				var pdomFrontierAnalysis = new PostDominanceFrontierAnalysis(cfg);
				pdomFrontierAnalysis.Analyze();

				var controlDependenceAnalysis = new ControlDependenceAnalysis(cfg);
				controlDependenceAnalysis.Analyze();

				var text = DGMLSerializer.Serialize(cfg);

				methodInfo.Add("CFG", cfg);
				methodInfo.Add("CFG_TEXT", text);

				text = DGMLSerializer.SerializeDominanceTree(cfg);
				methodInfo.Add("DT_TEXT", text);

				text = DGMLSerializer.SerializePostDominanceTree(cfg);
				methodInfo.Add("PDT_TEXT", text);

				text = DGMLSerializer.SerializeControlDependenceGraph(cfg);
				methodInfo.Add("CDG_TEXT", text);
			}
		}

		public void GenerateWebs(MethodDefinition method)
		{
			var methodInfo = programInfo.GetOrAdd(method);

			//GenerateIL(method);
			//GenerateTAC(method);
			GenerateCFG(method);

			if (!methodInfo.Contains("WEBS_TEXT"))
			{
				var cfg = methodInfo.Get<ControlFlowGraph>("CFG");

				// Webs
				var splitter = new WebAnalysis(cfg);
				splitter.Analyze();
				splitter.Transform();

				method.Body.UpdateVariables();

				var typeAnalysis = new TypeInferenceAnalysis(cfg);
				typeAnalysis.Analyze();

				if (options.RunForwardCopyPropagation)
				{
					var forwardCopyAnalysis = new ForwardCopyPropagationAnalysis(cfg);
					forwardCopyAnalysis.Analyze();
					forwardCopyAnalysis.Transform(method.Body);
				}

				if (options.RunBackwardCopyPropagation)
				{
					var backwardCopyAnalysis = new BackwardCopyPropagationAnalysis(cfg);
					backwardCopyAnalysis.Analyze();
					backwardCopyAnalysis.Transform(method.Body);
				}

				var text = method.Body.ToString();
				methodInfo.Add("WEBS_TEXT", text);

				text = DGMLSerializer.Serialize(cfg);
				methodInfo.Set("CFG_TEXT", text);

				text = DGMLSerializer.SerializeDominanceTree(cfg);
				methodInfo.Set("DT_TEXT", text);

				text = DGMLSerializer.SerializePostDominanceTree(cfg);
				methodInfo.Set("PDT_TEXT", text);

				text = DGMLSerializer.SerializeControlDependenceGraph(cfg);
				methodInfo.Set("CDG_TEXT", text);
			}
		}

		public void GenerateSSA(MethodDefinition method)
		{
			var methodInfo = programInfo.GetOrAdd(method);

			//GenerateIL(method);
			//GenerateTAC(method);
			//GenerateCFG(method);
			GenerateWebs(method);

			if (!methodInfo.Contains("SSA_TEXT"))
			{
				var cfg = methodInfo.Get<ControlFlowGraph>("CFG");

				// Live Variables
				var liveVariables = new LiveVariablesAnalysis(cfg);
				var livenessInfo = liveVariables.Analyze();

				// SSA
				var ssa = new StaticSingleAssignment(method.Body, cfg);
				ssa.Transform();
				ssa.Prune(livenessInfo);

				method.Body.UpdateVariables();

				var text = method.Body.ToString();
				methodInfo.Add("SSA_TEXT", text);

				text = DGMLSerializer.Serialize(cfg);
				methodInfo.Set("CFG_TEXT", text);

				text = DGMLSerializer.SerializeDominanceTree(cfg);
				methodInfo.Set("DT_TEXT", text);

				text = DGMLSerializer.SerializePostDominanceTree(cfg);
				methodInfo.Set("PDT_TEXT", text);

				text = DGMLSerializer.SerializeControlDependenceGraph(cfg);
				methodInfo.Set("CDG_TEXT", text);
			}
		}

		public void GeneratePTG(MethodDefinition method)
		{
			var methodInfo = programInfo.GetOrAdd(method);

			//GenerateIL(method);
			//GenerateTAC(method);
			//GenerateCFG(method);
			//GenerateWebs(method);
			GenerateSSA(method);

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

				methodInfo.Set("PTG_TEXT", text);
			}
		}

		private void OnOpen(object obj)
		{
//#if DEBUG
//			LoadAssembly(@"C:\Users\Edgar\Projects\Consume-Net\Tool2\Input\Samples.exe");
//			return;
//#endif
			var dialog = new OpenFileDialog()
			{
				Multiselect = true,
				Filter = "Executable files (*.exe; *.dll)|*.exe; *.dll|Assembly files (*.dll)|*.dll|All files (*.*)|*.*",
				//InitialDirectory = Environment.CurrentDirectory
				//InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
			};

			var ok = dialog.ShowDialog(Application.Current.MainWindow);

			if (ok.HasValue && ok.Value)
			{
				foreach (var fileName in dialog.FileNames)
				{
					LoadAssembly(fileName);
				}
			}
		}

		private void OnExit(object obj)
		{
			Application.Current.MainWindow.Close();
		}

		private void OnOptions(object obj)
		{
			options.LoadOptions();
			var dialog = new OptionsDialog(options);
			var ok = dialog.ShowDialog();

			if (ok.HasValue && ok.Value)
			{
				this.ApplyOptions();
			}
		}

		public void ApplyOptions()
		{
			var changed = false;

			if (this.RemoveUnusedLabels != options.RemoveUnusedLabels)
			{
				this.RemoveUnusedLabels = options.RemoveUnusedLabels;
				changed = true;
			}

			if (this.GenerateExceptionalControlFlow != options.GenerateExceptionalControlFlow)
			{
				this.GenerateExceptionalControlFlow = options.GenerateExceptionalControlFlow;
				changed = true;
			}

			if (this.RunForwardCopyPropagation != options.RunForwardCopyPropagation)
			{
				this.RunForwardCopyPropagation = options.RunForwardCopyPropagation;
				changed = true;
			}

			if (this.RunBackwardCopyPropagation != options.RunBackwardCopyPropagation)
			{
				this.RunBackwardCopyPropagation = options.RunBackwardCopyPropagation;
				changed = true;
			}

			if (changed)
			{
				programInfo.Clear();
			}
		}

		public void LoadAssembly(string fileName)
		{
			var assembly = loader.LoadAssembly(fileName);
			var vm = new AssemblyViewModel(this, assembly);
			this.Assemblies.Add(vm);
			this.ActiveItem = vm;
		}

		private void EnsureMethodHasOriginalBody(MethodDefinition method)
		{
			MethodBody body;
			var ok = originalMethodBodies.TryGetValue(method, out body);

			if (ok)
			{
				method.Body = body;
			}
			else
			{
				originalMethodBodies.Add(method, method.Body);
			}
		}

		protected UICommandSeparator AddSeparator(string category)
		{
			var command = new UICommandSeparator(category);
			this.Commands.Add(command);
			return command;
		}

		private MenuCommand AddCommand(string category, string name, ModifierKeys modifiers, Key key, Action<object> action, Func<object, bool> enabled = null, string icon = null)
		{
			MenuCommand command;
			var shortcut = new KeyGesture(key, modifiers);

			if (icon != null)
			{
				command = new ToolBarCommand(category, name, icon, shortcut, action, enabled);
			}
			else
			{
				command = new MenuCommand(category, name, icon, shortcut, action, enabled);
			}

			this.Commands.Add(command);
			return command;
		}
	}
}