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
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;

namespace AugustaStateUniversity.SeniorCapstoneIgnoreList
{
    /// <summary>
    /// Interaction logic for unshelveWindow.xaml
    /// </summary>
    public partial class UnshelveWindow : Window
    {
    #region constructors
        public UnshelveWindow()
        {
            InitializeComponent();
        }
        public UnshelveWindow(ObservableCollection<Shelveset> shelveSet, Workspace activeWorkspace)
        {
            InitializeComponent();
            this.DataContext = this;
            this.activeWorkspace = activeWorkspace;
            string owner = this.activeWorkspace.OwnerName.Split('\\')[1];
            Owner.Text = owner;
            allShelveSets = shelveSet;
            filteredSets = new List<String>();
            loadShelvesets();
            
        }
    #endregion

    #region properties
        public ObservableCollection<Shelveset> allShelveSets { get; set; }
        public ObservableCollection<Shelveset> shelveSetCollection { get { return _shelveSetCollection; } set { _shelveSetCollection = value; } }
        public List<String> filteredSets { get; set; }
        public Shelveset selectedSet { get; set; }
        public Workspace activeWorkspace { get; set; }
        public PendingChange[] changes { get; set; }
        public string owner 
        {
            get { return _owner; }

            set
            {
                if(value.Contains('\\'))
                {
                    _owner = value;
                }
                else
                {
                    _owner = activeWorkspace + "\\" + value;
                }
            }
        }
    #endregion

    #region private vars
        private ObservableCollection<Shelveset> _shelveSetCollection = new ObservableCollection<Shelveset>();
        private string _owner;
    #endregion

        private void cancelShelve_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void unshelve_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Unshelving will cause any local pending changes for files contained " +
                            "in the shelveset to be undone.\nDo you wish to continue?", "Attention",
                            MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                unshelveSet();
            }
            
        }
        private void unshelveSet()
        {
            // take the selected item and assign it to selectedSet so the control can have access to it and then use that in the
            // unshelve method
            selectedSet = (Shelveset)shelveSetList.SelectedItem;
            PendingSet[] pendingSets = activeWorkspace.VersionControlServer.QueryShelvedChanges(selectedSet);
            if (pendingSets.Length == 1)
            {
                this.changes = pendingSets[0].PendingChanges;
            }
            else
            {
                MessageBox.Show("it shouldnt have gotten here, says there are " + pendingSets.Length + " pending sets");
            }
            DialogResult = true;
        }
        private void detailsButton_Click(object sender, RoutedEventArgs e)
        {
            selectedSet = (Shelveset)shelveSetList.SelectedItem;

            DetailsWindow dw = new DetailsWindow(selectedSet);
            dw.ShowDialog();
            if (dw.DialogResult.HasValue && dw.DialogResult.Value)
            {
                unshelveSet();
            }

        }

        private void deleteButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show("Delete Shelveset \n\n Are you sure you want to delete the selected items? This operation is permanent.", "Delete Shelveset", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            
            if (result == MessageBoxResult.No)
            {
                //do what needs to be done if they select no.
            }
            else
            {
                selectedSet = (Shelveset)shelveSetList.SelectedItem;
                updateWindow();
                selectedSet.VersionControlServer.DeleteShelveset(selectedSet);
            }

        }
        private void updateWindow()
        {
            shelveSetCollection.Remove(selectedSet);
        }

        private void findButton_Click(object sender, RoutedEventArgs e)
        {
            //repopulates it based on the new owner name
            loadShelvesets();
        }
        public void loadShelvesets()
        {
            //clears the shelveset list
            shelveSetCollection.Clear();
            filteredSets.Clear();
            foreach (Shelveset set in allShelveSets)
            {
                string setOwner = set.OwnerName.Split('\\')[1];
                if (setOwner.ToLower().Equals(Owner.Text.ToLower()))
                {
                    if (!filteredSets.Contains(set.Name))
                    {
                        shelveSetCollection.Add(set);
                        filteredSets.Add(set.Name);
                    }
                }
            }
        }
    }
}
