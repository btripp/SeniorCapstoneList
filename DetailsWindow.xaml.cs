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
    /// Interaction logic for DetailsWindow.xaml
    /// </summary>
    public partial class DetailsWindow : Window
    {
    #region constructors
        public DetailsWindow()
        {
            InitializeComponent();
        }
        public DetailsWindow(Shelveset selectedShelveSet)
        {
            InitializeComponent();
            this.DataContext = this;
            shelveSet = selectedShelveSet;
            // strange way of finding the pendingsets for the selected shelve set in order to get its pending changes
            // there is always one pendingset because we query for the specific shelveset
            PendingSet[] pendingSets = selectedShelveSet.VersionControlServer.QueryShelvedChanges(selectedShelveSet);
            PendingChange[] pendingChanges = pendingSets[0].PendingChanges;
            foreach (PendingChange change in pendingChanges)
            {
                changeItem item = new changeItem(change);
                shelvedChanges.Add(item);
            }
            shelvesetName.Text = shelveSet.Name;
            owner.Text = shelveSet.OwnerName;
            date.Text = shelveSet.CreationDate.ToString();
            comment.Text = shelveSet.Comment;

        }
    #endregion
    #region properties
        public ObservableCollection<changeItem> shelvedChanges { get { return _shelvedChanges; } set { _shelvedChanges = value; } }
        public Shelveset shelveSet { get { return _shelveSet; } set { _shelveSet = value; } }
    #endregion
    #region private vars
        private Shelveset _shelveSet;
        private ObservableCollection<changeItem> _shelvedChanges = new ObservableCollection<changeItem>();
    #endregion
        
        

        private void unshelve_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void cancelunshelve_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        public bool anyChecked()
        {
            // TODO where is this used?
            bool returnValue = false;
            foreach (var item in shelvedChanges)
            {
                if(item.selected == true)
                {
                    returnValue = true;
                    break;
                }                
            }
            return returnValue;
        }
    }
}
