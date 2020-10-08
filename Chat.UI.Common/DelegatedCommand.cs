using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Chat.UI.Common
{
    public class DelegatedCommand : DependencyObject, ICommand
    {
        public static event Func<Exception, bool> OnPropogateError;

        #region bool CanBeExecuted

        public bool CanBeExecuted
        {
            get { return (bool)GetValue(CanBeExecutedProperty); }
            set { SetValue(CanBeExecutedProperty, value); }
        }

        // Using a DependencyProperty as the backing store for CanBeExecuted.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty CanBeExecutedProperty =
            DependencyProperty.Register("CanBeExecuted", typeof(bool), typeof(DelegatedCommand), new UIPropertyMetadata(true));

        #endregion

        public event EventHandler CanExecuteChanged;

        public event EventHandler Accomplished;

        Func<object, Task> _action;
        Func<object, bool> _checker;

        public DelegatedCommand(Func<object, Task> action, bool defaultAvailability = true, Func<object, bool> checker = null)
        {
            if (action == null)
                throw new ArgumentNullException("Argument 'action' cannot be null!");

            _action = action;
            _checker = checker;
            this.CanBeExecuted = defaultAvailability;
        }

        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            if (e.Property == CanBeExecutedProperty)
            {
                var oldValue = (bool)e.OldValue;
                var newValue = (bool)e.NewValue;

                if (newValue != oldValue)
                {
                    this.RaizeCanExecuteChanged();
                }
            }

            base.OnPropertyChanged(e);
        }

        public void RaizeCanExecuteChanged()
        {
            var ev = this.CanExecuteChanged;
            if (ev != null)
                ev(this, EventArgs.Empty);
        }

        public bool CanExecute(object parameter)
        {
            return this.CanBeExecuted && (_checker != null ? _checker(parameter) : true);
        }

        public void Execute(object parameter)
        {
            try
            {
                if (!this.CanExecute(parameter))
                    throw new InvalidOperationException("Command cannot be executed now!");

                var task = _action(parameter);
                task.ContinueWith(t => {
                    this.Accomplished?.Invoke(this, EventArgs.Empty);
                    if (t.IsFaulted)
                        this.PropogateError(t.Exception);
                });
            }
            catch (Exception ex)
            {
                this.PropogateError(ex);
            }
        }

        private void PropogateError(Exception ex)
        {
            if (OnPropogateError?.Invoke(ex) ?? false)
            {
                Util.ShowError(ex);
            }
            else
            {
                throw new ApplicationException("Exception during command execution", ex);
            }
        }
    }
}
