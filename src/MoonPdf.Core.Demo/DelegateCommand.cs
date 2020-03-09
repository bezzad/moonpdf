using System;
using System.Windows.Input;

namespace MoonPdf
{
	public class DelegateCommand : BaseCommand
	{
		private Action<object> executeAction;
		private Predicate<object> canExecuteFunc;

		public DelegateCommand(string name, Action<object> executeAction, Predicate<object> canExecuteFunc, InputGesture inputGesture)
			: base(name, inputGesture)
		{
			this.executeAction = executeAction;
			this.canExecuteFunc = canExecuteFunc;
		}

		public override bool CanExecute(object parameter)
		{
			return canExecuteFunc?.Invoke(parameter) ?? true;
		}

		public override void Execute(object parameter)
		{
			executeAction(parameter);
		}
	}
}
