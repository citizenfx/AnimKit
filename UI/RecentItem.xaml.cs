using System;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Input;

namespace AnimKit.UI
{
    /// <summary>
    /// Interaction logic for RecentItem.xaml
    /// </summary>
    public partial class RecentItem : UserControl
    {
        public string Title { get; set; } = "";

        public string Description { get; set; } = "";

        public string AssetPath { get; set; } = "";

        public DateTime DateTimestamp { get; set; }

        public string DisplayDate
        {
            get => DateTimestamp.ToString("dd MMM yyyy HH:mm");
            set => DateTimestamp = DateTime.Parse(value);
        }

        [Browsable(true)]
        [Category("Action")]
        [Description("Invoked when user select item")]
        public event MouseEventHandler? ItemSelected;

        [Browsable(true)]
        [Category("Action")]
        [Description("Invoked when user remove item")]
        public event MouseEventHandler? ItemRemoved;

        public RecentItem()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void ActionRegion_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ItemSelected?.Invoke(this, e);
        }

        private void RemoveRegion_MouseDown(object sender, MouseButtonEventArgs e)
        {
            ItemRemoved?.Invoke(this, e);
        }
    }
}
