using Backend.Utils;
using Microsoft.Win32;
using Model;
using Model.Types;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;

namespace Explorer
{
	class MainViewModel : ViewModelBase
	{
		private Host host;
		private ILoader loader;

		private DocumentViewModelBase activeDocument;

		public ObservableCollection<AssemblyViewModel> Assemblies { get; private set; }
		public ObservableCollection<DocumentViewModelBase> Documents { get; private set; }
		public IList<DelegateUICommand> Commands { get; private set; }
		public ProgramAnalysisInfo ProgramAnalysisInfo { get; private set; }

		public DocumentViewModelBase ActiveDocument
		{
			get { return activeDocument; }
			set { SetProperty(ref activeDocument, value); }
		}

		public DelegateUICommand OpenCommand { get; private set; }
		public DelegateUICommand ExitCommand { get; private set; }

		public MainViewModel()
		{
			this.Assemblies = new ObservableCollection<AssemblyViewModel>();
			this.Documents = new ObservableCollection<DocumentViewModelBase>();
			this.ProgramAnalysisInfo = new ProgramAnalysisInfo();

			this.Commands = new List<DelegateUICommand>();
			this.OpenCommand = AddCommand("_Open", ModifierKeys.Control, Key.O, OnOpen);
			this.ExitCommand = AddCommand("_Exit", ModifierKeys.Alt, Key.F4, OnExit);

			host = new Host();
			loader = new CCIProvider.Loader(host);
			PlatformTypes.Resolve(host);
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
		}

		private DelegateUICommand AddCommand(string name, ModifierKeys modifiers, Key key, Action<object> action, Func<object, bool> enabled = null)
		{
			var shortcut = new KeyGesture(key, modifiers);
			var command = new DelegateUICommand(name, shortcut, action, enabled);
			this.Commands.Add(command);
			return command;
		}
	}
}