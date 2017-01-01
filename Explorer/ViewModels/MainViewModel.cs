using Backend.Utils;
using Microsoft.Win32;
using Model;
using Model.Types;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Input;

namespace Explorer
{
	class MainViewModel : ViewModelBase
	{
		private Host host;
		private ILoader loader;

		private ItemViewModelBase activeItem;
		private DocumentViewModelBase activeDocument;

		public ObservableCollection<AssemblyViewModel> Assemblies { get; private set; }
		public ObservableCollection<DocumentViewModelBase> Documents { get; private set; }
		public IList<UIDelegateCommand> Commands { get; private set; }
		public ProgramAnalysisInfo ProgramAnalysisInfo { get; private set; }

		public MainViewModel()
		{
			this.Assemblies = new ObservableCollection<AssemblyViewModel>();
			this.Documents = new ObservableCollection<DocumentViewModelBase>();
			this.ProgramAnalysisInfo = new ProgramAnalysisInfo();

			this.Commands = new List<UIDelegateCommand>();
			AddCommand("File", "_Open", ModifierKeys.Control, Key.O, OnOpen);
			AddSeparator();
			AddCommand("File", "_Exit", ModifierKeys.Alt, Key.F4, OnExit);

			host = new Host();
			loader = new CCIProvider.Loader(host);
			PlatformTypes.Resolve(host);
		}

		//public IEnumerable<ICommand> FileCommands
		//{
		//	get { return this.Commands.OfType<UIDelegateCommand>().Where(c => c.Category == "File"); }
		//}

		public IEnumerable<ICommand> FileCommands
		{
			get
			{
				return this.Commands.SkipWhile(c => c == null || c.Category != "File")
									.TakeWhile(c => c == null || c.Category == "File");
			}
		}

		public Host Host
		{
			get { return host; }
		}

		public ItemViewModelBase ActiveItem
		{
			get { return activeItem; }
			set { SetProperty(ref activeItem, value); }
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

		private void OnOpen(object obj)
		{
			LoadAssembly(@"C:\Users\Edgar\Projects\Consume-Net\Tool2\Input\Samples.exe");
			return;

			var dialog = new OpenFileDialog()
			{
				Multiselect = true,
				Filter = "Executable files (*.exe; *.dll)|*.exe; *.dll|Assembly files (*.dll)|*.dll|All files (*.*)|*.*",
				InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
			};

			var ok = dialog.ShowDialog();

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

		private void LoadAssembly(string fileName)
		{
			var assembly = loader.LoadAssembly(fileName);
			var vm = new AssemblyViewModel(this, assembly);
			this.Assemblies.Add(vm);
			this.ActiveItem = vm;
		}

		protected void AddSeparator()
		{
			this.Commands.Add(null);
		}

		private UIDelegateCommand AddCommand(string category, string name, ModifierKeys modifiers, Key key, Action<object> action, Func<object, bool> enabled = null)
		{
			var shortcut = new KeyGesture(key, modifiers);
			var command = new UIDelegateCommand(category, name, shortcut, action, enabled);
			this.Commands.Add(command);
			return command;
		}
	}
}