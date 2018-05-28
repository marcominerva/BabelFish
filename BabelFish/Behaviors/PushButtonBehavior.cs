using Microsoft.Xaml.Interactivity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Windows.Devices.Gpio;
using Windows.UI.Xaml;

namespace BabelFish.Behaviors
{
    /// <summary>
    /// A behavior that listens for a specified event on its source and executes its actions when that event is fired.
    /// </summary>
    public sealed class PushButtonBehavior : Behavior
    {
        /// <summary>
        /// Get/Sets the direction of the PushButton pin number
        /// </summary>
        public int PinNumber { get; set; }

        public ButtonType ButtonType { get; set; }

        public PushButton Button { get; private set; }

        public static readonly DependencyProperty ClickCommandProperty = DependencyProperty.Register("ClickCommand", typeof(ICommand),
            typeof(PushButtonBehavior), new PropertyMetadata(null, OnClickCommandPropertyChanged));

        public static readonly DependencyProperty LongClickCommandProperty = DependencyProperty.Register("LongClickCommand", typeof(ICommand),
            typeof(PushButtonBehavior), new PropertyMetadata(null, OnLongClickCommandPropertyChanged));

        public static readonly DependencyProperty PressedCommandProperty = DependencyProperty.Register("PressedCommand", typeof(ICommand),
            typeof(PushButtonBehavior), new PropertyMetadata(null, OnPressedCommandPropertyChanged));

        public static readonly DependencyProperty ReleasedCommandProperty = DependencyProperty.Register("ReleasedCommand", typeof(ICommand),
            typeof(PushButtonBehavior), new PropertyMetadata(null, OnReleasedCommandPropertyChanged));

        public static void SetClickCommand(DependencyObject d, ICommand value) => d.SetValue(ClickCommandProperty, value);

        public static ICommand GetClickCommand(DependencyObject d) => (ICommand)d.GetValue(ClickCommandProperty);

        public static void SetLongClickCommand(DependencyObject d, ICommand value) => d.SetValue(LongClickCommandProperty, value);

        public static ICommand GetLongClickCommand(DependencyObject d) => (ICommand)d.GetValue(LongClickCommandProperty);

        public static void SetPressedCommand(DependencyObject d, ICommand value) => d.SetValue(PressedCommandProperty, value);

        public static ICommand GetPressedCommand(DependencyObject d) => (ICommand)d.GetValue(PressedCommandProperty);

        public static void SetReleasedCommand(DependencyObject d, ICommand value) => d.SetValue(ReleasedCommandProperty, value);

        public static ICommand GetReleasedCommand(DependencyObject d) => (ICommand)d.GetValue(ReleasedCommandProperty);

        private static void OnClickCommandPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var isTypePresent = (GpioController.GetDefault() != null);
            if (isTypePresent)
            {
                var control = d as PushButtonBehavior;
                if (control != null)
                {
                    control.Button.Click += Button_Click;
                }
                else
                {
                    control.Button.Click -= Button_Click;
                }
            }
        }

        private static void OnLongClickCommandPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var isTypePresent = (GpioController.GetDefault() != null);
            if (isTypePresent)
            {
                var control = d as PushButtonBehavior;
                if (control != null)
                {
                    control.Button.LongClick += Button_LongClick;
                }
                else
                {
                    control.Button.LongClick -= Button_LongClick;
                }
            }
        }

        private static void OnPressedCommandPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var isTypePresent = (GpioController.GetDefault() != null);
            if (isTypePresent)
            {
                var control = d as PushButtonBehavior;
                if (control != null)
                {
                    control.Button.Pressed += Button_Pressed;
                }
                else
                {
                    control.Button.Pressed -= Button_Pressed;
                }
            }
        }

        private static void OnReleasedCommandPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var isTypePresent = (GpioController.GetDefault() != null);
            if (isTypePresent)
            {
                var control = d as PushButtonBehavior;
                if (control != null)
                {
                    control.Button.Released += Button_Released;
                }
                else
                {
                    control.Button.Released -= Button_Released;
                }
            }
        }

        private readonly bool isTypePresent;

        public PushButtonBehavior()
        {
            isTypePresent = (GpioController.GetDefault() != null);
        }

        /// <summary>
        /// Called after the behavior is attached to the <see cref="Behavior.AssociatedObject"/>.
        /// </summary>
        protected override void OnAttached()
        {
            base.OnAttached();

            if (isTypePresent)
            {
                Button = new PushButton(PinNumber, ButtonType, this);
            }
        }

        /// <summary>
        /// Called when the behavior is being detached from its <see cref="Behavior.AssociatedObject"/>.
        /// </summary>
        protected override void OnDetaching()
        {
            base.OnDetaching();

            if (isTypePresent)
            {
                Button.Click -= Button_Click;
                Button.LongClick -= Button_LongClick;
            }
        }

        private static void Button_Click(object sender, EventArgs e)
        {
            var control = (sender as PushButton).Behavior;
            GetClickCommand(control)?.Execute(null);
        }

        private static void Button_LongClick(object sender, EventArgs e)
        {
            var control = (sender as PushButton).Behavior;
            GetLongClickCommand(control)?.Execute(null);
        }

        private static void Button_Pressed(object sender, EventArgs e)
        {
            var control = (sender as PushButton).Behavior;
            GetPressedCommand(control)?.Execute(null);
        }

        private static void Button_Released(object sender, EventArgs e)
        {
            var control = (sender as PushButton).Behavior;
            GetReleasedCommand(control)?.Execute(null);
        }
    }
}
