using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AnimKit.Core;

namespace AnimKit.UI
{
    /// <summary>
    /// Event arguments for input event.
    /// </summary>
    public class ListWindowEventArgs : EventArgs
    {
        public int Value { get; set; } = -1;
    }

    /// <summary>
    /// Event handler for input events.
    /// </summary>
    /// <param name="sender">Event sender</param>
    public delegate void ListWindowEventHandler(object sender, ListWindowEventArgs args);

    /// <summary>
    /// Interaction logic for ListWindow.xaml
    /// </summary>
    public partial class ListWindow : Window
    {
        [Browsable(true)]
        [Category("Action")]
        [Description("Invoked when user press on apply button")]
        public event ListWindowEventHandler? SelectionApply;

        public int SelectedIndex { get; set; } = -1;

        public ListWindow(string title, List<string> itemList)
        {
            InitializeComponent();
            DataContext = this;

            ListBoxControl.Items.Clear();
            itemList.ForEach(_ => ListBoxControl.Items.Add(_));

            Title = $"{Constants.BrandingName} / {title}";
        }

        private void ListBoxControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedIndex = ListBoxControl.SelectedIndex;
        }

        private void ListBoxControl_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (SelectedIndex != -1)
            {
                SelectionApply?.Invoke(this, new ListWindowEventArgs { Value = SelectedIndex });
                Close();
            }
        }

        private void ApplyButton_Click(object sender, MouseButtonEventArgs e)
        {
            SelectionApply?.Invoke(this, new ListWindowEventArgs { Value = SelectedIndex });
            Close();
        }
    }
}
