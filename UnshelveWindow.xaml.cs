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
        public UnshelveWindow()
        {
            InitializeComponent();
        }
        public UnshelveWindow(ObservableCollection<Shelveset> shelveSet, Workspace activeWorkspace)
        {
            InitializeComponent();
            this.DataContext = this;
            // TODO i dont want to do it this way... but this splits the Workspace\owner and returns just the owner name
            string ownerName = activeWorkspace.OwnerName.Split('\\')[1];
            Owner.Text = ownerName;
            // TODO 
            // This is going to have to filter based on the name entered
            // What is bad about this is that if you want to get a shelveset from a different 
            // workspace (not sure if thats something that you would do or not) then the way im stripping the workspace name 
            // out of the text box wont work... what i should do is keep the ownername in its entirety but only show the stripped version.
            // but then when you ENTER in a different name i need to make sure i either tack on the workspace\ if they only enter
            // in the user name... but then be able to accept workspace\username if they wish to get at another workspace
            // and there-in lies another problem because i only pass the active workspace to work with...
            // this can be fixed but will take a little work. will leave this way for now until we know more about how this will be used
            foreach (Shelveset set in shelveSet)
            {
                string setOwner = set.OwnerName.Split('\\')[1];
                MessageBox.Show(setOwner);
                if(setOwner.Equals(Owner.Text))
                {
                    shelveSetCollection.Add(set);
                }
            }
            // commented out because i need to filter based on owner name
            //shelveSetCollection = shelveSet;
        }

        private ObservableCollection<Shelveset> _shelveSetCollection = new ObservableCollection<Shelveset>();
        public ObservableCollection<Shelveset> shelveSetCollection { get { return _shelveSetCollection; } set { _shelveSetCollection = value; } }
        public Shelveset selectedSet { get; set; }
        private void cancelShelve_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void shelve_Click(object sender, RoutedEventArgs e)
        {
            Object test = shelveSetList.SelectedItem;
            // take the selected item and assign it to selectedSet so the control can have access to it and then use that in the
            // unshelve method
            DialogResult = true;
        }
    }
}
