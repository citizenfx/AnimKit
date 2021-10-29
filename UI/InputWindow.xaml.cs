using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;
using AnimKit.Core;

namespace AnimKit.UI
{
    /// <summary>
    /// Event arguments for input event.
    /// </summary>
    public class InputWindowEventArgs : EventArgs
    {
        public string Value { get; set; } = string.Empty;
    }

    /// <summary>
    /// Event handler for input event.
    /// </summary>
    /// <param name="sender">Event sender</param>
    public delegate void InputWindowEventHandler(object sender, InputWindowEventArgs args);

    /// <summary>
    /// Interaction logic for InputWindow.xaml
    /// </summary>
    public partial class InputWindow : Window
    {
        public string Text
        {
            get => InputBoxItem.Value?.ToString() ?? "";
            set => InputBoxItem.Value = value;
        }

        public string Rules
        {
            get => RulesLabel.Content?.ToString() ?? "";
            set => RulesLabel.Content = value;
        }

        [Browsable(true)]
        [Category("Action")]
        [Description("Invoked when user press on apply button")]
        public event InputWindowEventHandler? InputApplied;

        public InputWindow(string title, string text = "", string rules = "")
        {
            InitializeComponent();

            Text = text;
            Rules = rules;
            Title = $"{Constants.BrandingName} / {title}";
        }

        private void ApplyButton_Click(object sender, MouseButtonEventArgs e)
        {
            InputApplied?.Invoke(this, new InputWindowEventArgs { Value = Text });
        }

        private void InputBoxItem_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                InputApplied?.Invoke(this, new InputWindowEventArgs { Value = Text });
            }
        }
    }
}
