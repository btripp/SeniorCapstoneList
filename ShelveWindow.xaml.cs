using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace AugustaStateUniversity.SeniorCapstoneIgnoreList
{
    /// <summary>
    /// Interaction logic for ShelveWindow.xaml
    /// </summary>
    public partial class ShelveWindow : Window
    {
        public ShelveWindow()
        {
            InitializeComponent();
        }
        public ShelveWindow(ObservableCollection<changeItem> changeCollection, Workspace activeWorkspace)
        {
            InitializeComponent();
            // this is what makes sure the data bindings are pointing to this class... so i can do path=shelveCollection
            // TODO can i just have this point to the control so i dont have to pass the collection? or would we rather pass it. 
            this.DataContext = this;
            shelveCollection = changeCollection;
            this.activeWorkspace = activeWorkspace;
        }

        public Workspace activeWorkspace { get; set; }
        public ObservableCollection<changeItem> shelveCollection { get { return _shelveCollection; } set { _shelveCollection = value; } }

        private ObservableCollection<changeItem> _shelveCollection;

        private void cancelShelve_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void shelve_Click(object sender, RoutedEventArgs e)
        {
            // checks to see if the shelveset already exists, if it does you have to re-enter
            int count = activeWorkspace.VersionControlServer.QueryShelvesets(shelvesetName.Text, activeWorkspace.OwnerName).Count();
            if (count > 0)
            {
                MessageBox.Show("The shelveset name already exists. Please rename the shelveset.", "Already exists", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            else
                DialogResult = true;
        }
    }
}
