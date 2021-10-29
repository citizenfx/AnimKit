using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace AnimKit.UI
{
    public enum QuickActionType
    {
        Invalid = -1,
        CreateAsset,
        Convert
    }

    public class QuickActionEntry
    {
        /// <summary>
        /// Quick action type.
        /// </summary>
        public QuickActionType Type { get; set; } = QuickActionType.Invalid;

        /// <summary>
        /// Action title.
        /// </summary>
        public string Title { get; set; } = "";

        /// <summary>
        /// Action description.
        /// </summary>
        public string Description { get; set; } = "";

        public QuickActionEntry(QuickActionType type, string title, string description)
        {
            Type = type;
            Title = title;
            Description = description;
        }
    }

    public class QuickActionsData
    {
        /// <summary>
        /// Actions title.
        /// </summary>
        public string ActionsTitle { get; set; } = string.Empty;

        /// <summary>
        /// Action file path.
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Left action entry.
        /// </summary>
        public QuickActionEntry? Left { get; set; }

        /// <summary>
        /// Right action entry.
        /// </summary>
        public QuickActionEntry? Right { get; set; }

        public QuickActionsData(string title, string path)
        {
            ActionsTitle = title;
            FilePath = path;
        }
    }

    /// <summary>
    /// Event arguments for input event.
    /// </summary>
    public class QuickActionEventArgs : EventArgs
    {
        public QuickActionType Type { get; set; } = QuickActionType.Invalid;
        public string Path { get; set; } = string.Empty;
    }

    /// <summary>
    /// Event handler for input event.
    /// </summary>
    /// <param name="sender">Event sender</param>
    public delegate void QuickActionEventHandler(object sender, QuickActionEventArgs args);

    /// <summary>
    /// Interaction logic for QuickActions.xaml
    /// </summary>
    public partial class QuickActions : UserControl
    {
        [Browsable(true)]
        [Category("Action")]
        [Description("Invoked when user drop files on action")]
        public event QuickActionEventHandler? ActionDrop;

        [Browsable(true)]
        [Category("Action")]
        [Description("Invoked when user closing drag and drop panel")]
        public event EventHandler? Closing;

        private QuickActionsData? Current { get; set; }

        public QuickActions()
        {
            InitializeComponent();
            DataContext = this;
        }

        /// <summary>
        /// Set quick actions data.
        /// </summary>
        /// <param name="data">Data struct</param>
        public void SetQuickActionsData(QuickActionsData data)
        {
            bool isValid = data.Left != null & data.Right != null;

            LeftActionDropButton.Visibility = isValid ? Visibility.Visible : Visibility.Hidden;
            RightActionDropButton.Visibility = isValid ? Visibility.Visible : Visibility.Hidden;
            DelimiterTitle.Visibility = isValid ? Visibility.Visible : Visibility.Hidden;
            UnknownDescription.Visibility = isValid ? Visibility.Hidden : Visibility.Visible;

            if (isValid)
            {
                HeaderTitle.Title = data.ActionsTitle;
                LeftActionDropButton.Title = data.Left?.Title ?? "";
                LeftActionDropButton.Description = data.Left?.Description ?? "";
                RightActionDropButton.Title = data.Right?.Title ?? "";
                RightActionDropButton.Description = data.Right?.Description ?? "";
            }

            // Toggle visibility and story.
            Visibility = Visibility.Visible;
            VisibilityOnStory.Storyboard.Begin();

            // Save current actions data.
            Current = data;
        }

        /// <summary>
        /// Closing fade effect.
        /// </summary>
        private void DoClosingFade()
        {
            VisibilityOffStory.Storyboard.Begin();
        }

        private void LeftAction_Drop(object sender, DragEventArgs e)
        {
            if (Current?.Left != null)
            {
                ActionDrop?.Invoke(this, new QuickActionEventArgs { Path = Current.FilePath, Type = Current.Left.Type });
            }

            DoClosingFade();
        }

        private void RightAction_Drop(object sender, DragEventArgs e)
        {
            if (Current?.Right != null)
            {
                ActionDrop?.Invoke(this, new QuickActionEventArgs { Path = Current.FilePath, Type = Current.Right.Type });
            }

            DoClosingFade();
        }

        private void CloseButtonRegion_Click(object sender, MouseButtonEventArgs e)
        {
            Closing?.Invoke(this, e);
            DoClosingFade();
        }

        private void VisibilityOffStory_Completed(object sender, EventArgs e)
        {
            Visibility = Visibility.Hidden;
        }
    }
}
