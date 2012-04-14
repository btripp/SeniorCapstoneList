﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using System.Text.RegularExpressions;

namespace AugustaStateUniversity.SeniorCapstoneIgnoreList
{
    public partial class MyControl : UserControl
    {
        // TODO 
        // find out how pending changes list asks for the password to connect to TFS... if we do not have that for our window it will
        // break because we try to access something we do not have access to yet... 
        public MyControl()
        {
            InitializeComponent();
            this.DataContext = this;
            mc = this;
            shelvewindow = new ShelveWindow();
            unshelvewindow = new UnshelveWindow();
        }
        #region Properties
        // TODO : does the mycontrol class need a copy of itself as a property?
        public static MyControl mc;
        public List<string> removeFromCollection { get { return _removeFromCollection; } set { _removeFromCollection = value; } }
        public List<string> listOfChanges { get { return _listOfChanges; } set { _listOfChanges = value; } }
        public ObservableCollection<changeItem> changesCollection { get { return _changesCollection; } set { _changesCollection = value; } }
        public ObservableCollection<changeItem> shelveCollection { get { return _shelveCollection; } set { _shelveCollection = value; } }
        public Workspace activeWorkspace { get; set; }
        public ShelveWindow shelvewindow { get; set; }
        public UnshelveWindow unshelvewindow { get; set; }
        #endregion
        #region Private Vars
        private List<RegisteredProjectCollection> projects;
        // do we need this?
        private List<string> _removeFromCollection = new List<string>();
        private List<string> _listOfChanges = new List<string>();
        private ObservableCollection<changeItem> _changesCollection = new ObservableCollection<changeItem>();
        private ObservableCollection<changeItem> _shelveCollection = new ObservableCollection<changeItem>();
        #endregion
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions")]
        #region Tool window functions
        public static void RegisterEventHandlers(VersionControlServer versionControl)
        {
            // DEBUG 
            //MessageBox.Show("registering event handlers");
            versionControl.OperationFinished += afterUpdate;
        }
        public static void afterUpdate(Object sender, OperationEventArgs e)
        {
            mc.loadPendingChangesList();
        }
        public void MyToolWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // DEBUG
            //MessageBox.Show("it got into the load");
            List<RegisteredProjectCollection> projectCollections;

            // get all registered project collections (previously connected to from Team Explorer)
            projectCollections = new List<RegisteredProjectCollection>((RegisteredTfsConnections.GetProjectCollections()));
            this.projects = projectCollections;

            // filter down to only those collections that are currently on-line
            var onlineCollections =
                from collection in projectCollections
                where collection.Offline == false
                select collection;

            // DEBUG
            //MessageBox.Show(onlineCollections.Count().ToString());
            // fail if there are no registered collections that are currently on-line
            if (onlineCollections.Count() < 1)
            {
                Console.Error.WriteLine("Error: There are no on-line registered project collections");
                Environment.Exit(1);
            }
            // find a project collection with at least one team project
            // is it going to act differently if there is more than one online collection?
            foreach (var registeredProjectCollection in onlineCollections)
            {
                var projectCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(registeredProjectCollection);
                try
                {
                    // DEBUG
                    //MessageBox.Show("finding workspaces");
                    var versionControl = (VersionControlServer)projectCollection.GetService(typeof(VersionControlServer));
                    var teamProjects = new List<TeamProject>(versionControl.GetAllTeamProjects(false));
                    //if there are no team projects in this collection, skip it
                    if (teamProjects.Count < 1) continue;
                    Workspace workspace = versionControl.GetWorkspace(System.Environment.MachineName, System.Environment.UserName);
                    activeWorkspace = workspace;
                    Workspace[] workspaces = versionControl.QueryWorkspaces(null, null, null);
                    loadWorkspaces(workspaces);
                    workSpaces.SelectedItem = workspace.Name;

                    RegisterEventHandlers(versionControl);
                }
                finally { }
                break;
            }
            loadPendingChangesList();
        }
        // do we use onLoad or onInitialize?
        private void MyToolWindow_Init(object sender, EventArgs e)
        {
            //this alone will not work anymore.
            //loadPendingChangesList();
        }
        #endregion
        #region pending Changes Section
        public void loadWorkspaces(Workspace[] workspaces)
        {
            foreach (Workspace workspace in workspaces)
            {
                workSpaces.Items.Add(workspace.Name);
            }
        }
        public void checkIgnoreList()
        {
            bool found;
            string[] ignoreListArray = new string[ignoreList.Items.Count];
            ignoreList.Items.CopyTo(ignoreListArray, 0);
            var filters = from f in ignoreListArray
                          where f.Contains("*")
                          select f;

            


            //build changes to be checked in.
            foreach (changeItem item in changesCollection)
            {
                found = false;
                if (filters.Count() > 0)
                {
                    foreach (var filter in filters)
                    {
                        Wildcard wildcard = new Wildcard(filter, RegexOptions.IgnoreCase);

                        // found in the filter so false
                        if (wildcard.IsMatch(item.fileName))
                        {
                            found = true;
                            item.selected = false;
                            break;
                        }
                    }
                    if (found == false)
                    {
                        // not in filter and not in the ignore list
                        if (ignoreListArray.Contains(item.fileName, StringComparer.OrdinalIgnoreCase) == false)
                        {
                            ;
                        }
                        else
                            item.selected = false;
                    }
                }
                else
                {
                    // no filters and they are not in the ignore list
                    if (ignoreListArray.Contains(item.fileName, StringComparer.OrdinalIgnoreCase) == false)
                    {
                        ;
                    }
                    else
                        item.selected = false;
                }
            }
        }
        public void loadPendingChangesList()
        {
            // because i pass the workspace... this will only have pending changes for the current workspace
            // this clear is to clear the observableCollection
            // TODO work on this
            // this.changesCollection.Clear();
            PendingChange[] pendingChanges = activeWorkspace.GetPendingChanges();
            foreach (PendingChange pendingChange in pendingChanges)
            {
                // i have this next thing there to make sure that you dont add the same file twice if we keep this
                // onload
                if (!mc.pendingChangesList.Items.Contains(pendingChange.FileName)) 
                {
                    // this is an ugly way to make sure that the same changeItem is not added a second time
                    // i dont think this works because maybe i dont provide the collection with a way to evaluate "contains"
                    // rather than look up how to change this im going to go ao different way
                    //changeItem temp = new changeItem(pendingChange);
                    //if (!changesCollection.Contains(temp))
                    //{
                    //    MessageBox.Show("adding " + pendingChange.FileName);
                    //    this.changesCollection.Add(new changeItem(pendingChange));
                    //}
                    // this checks the list of changes to see if it needs to be added to the collection or not
                    // this is a workaround for the above problem
                    if(!listOfChanges.Contains(pendingChange.FileName))
                    {
                        changesCollection.Add(new changeItem(pendingChange));
                        listOfChanges.Add(pendingChange.FileName);
                    }
                }
            }
            //checkIgnoreList();
            string message = "";
            foreach (changeItem item in changesCollection)
            {
                message += (item.fileName) + "\n";
            }
            // DEBUG - this shows what the collection contains that the list is bound to
            //MessageBox.Show(message);
        }
        public void updatePendingChangesList()
        {
            foreach (string remove in removeFromCollection)
            {
                // starting at the back because im not sure how remove works ( do all elements shift down or is it just a logical remove?) 
                // so by starting at the end it works the same and you dont have to worry about shifting elements. 
                // there might be an easier way to do it but i just wanted to get it done. 
                for (int i = changesCollection.Count-1; i >= 0; i--)
                {
                    if(changesCollection[i].fileName.Equals(remove))
                    {
                        changesCollection.RemoveAt(i);
                    }
                }
            }
        }
        //I would say that we keep this guy. I think its a nice feature to have.
        
        //added regex stuff
        //added stuff to make sure a change isnt added twice... not sure if it ever can.
        //TODO Need to add logic to make sure we dont try to check in nothing
        private PendingChange[] getSelectedChanges(ObservableCollection<changeItem> collection)
        {
            List<PendingChange> myChanges = new List<PendingChange>();
            PendingChange[] pendingChanges;

            foreach (changeItem item in collection)
            {
                // DEBUG
                //MessageBox.Show(item.selected.ToString());
                if (item.selected)
                {
                    myChanges.Add(item.change);
                    removeFromCollection.Add(item.fileName);
                }
            }
            pendingChanges = myChanges.ToArray();
            return pendingChanges;
        }
        private void checkin_Click(object sender, RoutedEventArgs e)
        {


            
            PendingChange[] arrayChanges = getSelectedChanges(changesCollection);
            if (arrayChanges.Count() > 0)
            {
                activeWorkspace.CheckIn(arrayChanges, commentBox.Text);
                MessageBox.Show(arrayChanges.Count() + " File(s) checked in.", "Files Checked in...", MessageBoxButton.OK, MessageBoxImage.Information);
                updatePendingChangesList();
            }
            else
            {
                MessageBox.Show("0 Files checked in.", "Files Checked in...", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            commentBox.Clear();
            //commentBox.Text = "";
            
        }
        private void ignore_Click(object sender, RoutedEventArgs e)
        {
            if (pendingChangesList.SelectedItem != null)
            {
                changeItem item = (changeItem)pendingChangesList.SelectedItem;
                addToIgnoreList(item.fileName);
            }
        }
        private void shelve_Click(object sender, RoutedEventArgs e)
        {
            //List<PendingChange> myChanges = new List<PendingChange>();
            //PendingChange[] pendingChanges = activeWorkspace.GetPendingChanges();
            // im going to read up about shelving.. the shelve function needs a shelvset, pendingchanges[], comment
            // it seems to pretty much the same and i think i get the idea but i want to ready up on it some before i write this
            
            // any code after show dialog will not execute until the dialog has been closed, we can still you the things that belong
            // to that window
            //shelvewindow.shelvesetName.Text
            string message = "";
            foreach (changeItem item in shelveCollection)
            {
                message += item.fileName + "\n";
            }
            message += ".";
            // DEBUG
            // MessageBox.Show(message);
            // TODO the shelvewindow cant bind to this shelveCollection because i dont have the binding path correct. shelvewindow is
            // in a different class so i need to map it to myControl class and then it should work
            shelvewindow = new ShelveWindow(changesCollection);
            shelvewindow.ShowDialog();
            if (shelvewindow.DialogResult.HasValue && shelvewindow.DialogResult.Value)
            {
                // Debug
                MessageBox.Show("User clicked OK");
                shelveCollection = shelvewindow.shelveCollection;
                Shelve();
            }
            else
                // Debug
                MessageBox.Show("User clicked Cancel");
        }
        private void Shelve()
        {
            Shelveset shelveset = new Shelveset(activeWorkspace.VersionControlServer, shelvewindow.shelvesetName.Text, activeWorkspace.OwnerName);
            shelveset.Comment = shelvewindow.comment.Text;
            PendingChange[] toShelve = getSelectedChanges(shelveCollection);
            activeWorkspace.Shelve(shelveset, toShelve , ShelvingOptions.None);
            // have to "UNDO" on the pending changes that were shelved in order to unshelve the set
            activeWorkspace.Undo(toShelve);
            // TODO is there a better way to update the list? this next thing just removes the pending changes from
            // the collection that i dont want to be there
            removeFromCollection.Clear();
            foreach (PendingChange change in toShelve)
            {
                removeFromCollection.Add(change.FileName);
            }
            updatePendingChangesList();
            // DEBUG
            //string message = "This is what would have been shelved...\n";
            //foreach (PendingChange change in getSelectedChanges(shelveCollection))
            //{
            //    message += change.FileName + "\n";
            //}
            //message += ".";
            //MessageBox.Show(message);
        }

        private void unshelve_Click(object sender, RoutedEventArgs e)
        {
            openUnshelveWindow();
            // this one will be easy once i fully understand the shelve method
        }
        private void openUnshelveWindow()
        {
            unshelvewindow = new UnshelveWindow();
            unshelvewindow.ShowDialog();
        }
        private void changeWorkspace(object sender, SelectionChangedEventArgs e)
        {
            // TODO abstract this so its not so messy
            var onlineCollections =
                from collection in projects
                where collection.Offline == false
                select collection;
            // fail if there are no registered collections that are currently on-line
            if (onlineCollections.Count() < 1)
            {
                Console.Error.WriteLine("Error: There are no on-line registered project collections");
                Environment.Exit(1);
            }
            // find a project collection with at least one team project
            // is it going to act differently if there is more than one online collection?
            // TODO is there ever going to be more than one project collection? if there is. i dont think that my way of finding the current workspace would work
            // or maybe it would work but we dont need to have a version control for each registered project collection... if we dont need to foreach through this
            // then we could have versioncontrol be a property as well.. if versioncontrol was a property we could switch between workspace much easier
            foreach (var registeredProjectCollection in onlineCollections)
            {
                var projectCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(registeredProjectCollection);
                try
                {
                    var versionControl = (VersionControlServer)projectCollection.GetService(typeof(VersionControlServer));
                    var teamProjects = new List<TeamProject>(versionControl.GetAllTeamProjects(false));
                    //if there are no team projects in this collection, skip it
                    if (teamProjects.Count < 1) continue;
                    // ----------------> all of that was just to get at this versioncontrol in order to get the workspace with the dropdowns selected items name
                    Workspace workspace = versionControl.GetWorkspace(workSpaces.SelectedItem.ToString(), System.Environment.UserName);
                    activeWorkspace = workspace;
                }
                finally { }
                break;
            }
            // once the workspace is changed, load the pending changes list for that workspace
            loadPendingChangesList();
        }
        private void toggleSelected(object sender, RoutedEventArgs e)
        {
            // This right here probably isnt needed
            // when a user unchecks the checkbox it chanes the value of the selected property on its own
            // to see that behavior in action, uncomment the loop below
            // DEBUG
            //foreach (changeItem item in changesCollection)
            //{
            //    if (item.selected == false)
            //    {
            //        MessageBox.Show("you unchecked " + item.fileName + "!!!");
            //    }
            //}
            
        }

        #endregion
        #region ignore list section
        private void ignoreListAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (ignoreTextBox.Text != null && ignoreTextBox.Text != "")
            {
                addToIgnoreList(ignoreTextBox.Text);
                ignoreTextBox.Text = "";
                checkIgnoreList();
            }
            else
            {
                MessageBox.Show("You must enter something to ignore.", "Add to list", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        private void loadButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = ""; // Default file name
            dlg.DefaultExt = ".txt"; // Default file extension
            dlg.Filter = "Text documents (.txt)|*.txt"; // Filter files by extension
            dlg.Title = "Load Ignore List";
            // Show open file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process open file dialog box results
            if (result == true)
            {
                ignoreList.Items.Clear();
                string[] ignoreListItems;
                string filename = dlg.FileName;
                ignoreListItems = System.IO.File.ReadAllLines(filename);

                foreach (string s in ignoreListItems)
                {
                    addToIgnoreList(s);
                }
            }
        }
        private void saveButton_Click(object sender, RoutedEventArgs e)
        {
            // Configure save file dialog box
            Microsoft.Win32.SaveFileDialog dlg = new Microsoft.Win32.SaveFileDialog();
            dlg.FileName = "ignoreList"; // Default file name
            dlg.DefaultExt = ".text"; // Default file extension
            dlg.Filter = "Text documents (.txt)|*.txt"; // Filter files by extension

            // Show save file dialog box
            Nullable<bool> result = dlg.ShowDialog();

            // Process save file dialog box results
            if (result == true)
            {
                // Save document
                string filename = dlg.FileName;
                string[] ignoreListItems = new string[ignoreList.Items.Count];
                ignoreList.Items.CopyTo(ignoreListItems, 0);
                System.IO.File.WriteAllLines(filename, ignoreListItems);                
            }
        }
        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (ignoreList.SelectedIndex != -1)
            {
                ignoreList.Items.RemoveAt(ignoreList.SelectedIndex);
            }
        }
        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            if (ignoreList.Items.Count > 0)
            {
                var result = MessageBox.Show("Are you sure you want to clear the ignore list?", "Clear List", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    ignoreList.Items.Clear();
                }
            }
            
        }
        public void addToIgnoreList(string fileName)
        {
            if(fileName.Contains("*")) //regex or wildcard dont ignore case
            {
                if (!ignoreList.Items.Contains(fileName))
                {
                    ignoreList.Items.Add(fileName);
                }
            }
            else   //ignore case when adding filenames
            {
                string [] ignoreListArray = new string[ignoreList.Items.Count];
                ignoreList.Items.CopyTo(ignoreListArray ,0);
                if (!ignoreListArray.Contains(fileName, StringComparer.OrdinalIgnoreCase))
                {
                    ignoreList.Items.Add(fileName);
                }
            }
        }

        private void newButton_Click(object sender, RoutedEventArgs e)
        {
            if (ignoreList.Items.Count > 0)
            {
                var result = MessageBox.Show("Do you want to save first?", "Unsaved List", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.No)
                {
                    ignoreList.Items.Clear();
                }
                else
                {
                    saveButton_Click(this, e);
                    ignoreList.Items.Clear();
                }
            }
        }

        private void hideShowComment_Click(object sender, RoutedEventArgs e)
        {
            if (commentBox.IsVisible)
            {
                commentBox.Visibility = Visibility.Collapsed;
                commentLabel.Visibility = Visibility.Collapsed;
            }
            else
            {
                commentBox.Visibility = Visibility.Visible;
                commentLabel.Visibility = Visibility.Visible;
            }
        }

        private void ignoreTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ignoreListAddButton_Click(this, e);
            }
        }

        private void hideShowIgnoreList_Click(object sender, RoutedEventArgs e)
        {
            if (ignoreListToolbar.IsVisible)
            {
                ignoreListToolbar.Visibility = Visibility.Collapsed;
                button2.Visibility = Visibility.Collapsed;
                ignoreList.Visibility = Visibility.Collapsed;
                ignoreTextBox.Visibility = Visibility.Collapsed;
                title.Visibility = Visibility.Collapsed;
            }
            else
            {
                ignoreListToolbar.Visibility = Visibility.Visible;
                button2.Visibility = Visibility.Visible;
                ignoreList.Visibility = Visibility.Visible;
                ignoreTextBox.Visibility = Visibility.Visible;
                title.Visibility = Visibility.Visible;
            }
        }
        #endregion


        #region shelve window

        #endregion
        #region unshelve window

        #endregion
    }
    
        

    public class changeItem : INotifyPropertyChanged

    {
        public changeItem()
        {

            fileName = "test name";
            changeType = "test type";
            folder = "test folder";
            selected = true;
        }
        public changeItem(PendingChange change)
        {
            this.change = change;
            fileName = change.FileName;
            changeType = change.ChangeType.ToString();
            folder = change.LocalOrServerFolder;
            selected = true;
        }
        public string fileName { get; set; }
        public string changeType { get; set; }
        public string folder { get; set; }
        public PendingChange change { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
        private bool _selected;
        public bool selected 
        {
            get
            {
                return _selected;
            }
            set
            {
                if (_selected != value)
                {
                    _selected = value;
                    if (PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs("selected"));
                    }

                }
            }
        }
    }
    public class Wildcard : Regex
    {
        public Wildcard(string pattern)
            : base(WildcardToRegex(pattern)){}

        public Wildcard(string pattern, RegexOptions options)
            : base(WildcardToRegex(pattern), options){}

        //Converts wildcard to regex
        public static string WildcardToRegex(string pattern)
        {
            return "^" + Regex.Escape(pattern).
             Replace("\\*", ".*").
             Replace("\\?", ".") + "$";
        }
    }
}
#region commented out stuff
//=============================================================================================================
//this was commented out from ---  private void ignoreListAddButton_Click(object sender, RoutedEventArgs e)
/* 
 * public static PendingChange[] GetPendingChangesInTheWorkspace(string workspaceName, string userName, string compName)
{
var tfs = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(new Uri(ConfigurationManager.AppSettings["TfsUri"]));
var service = tfs.GetService<VersionControlServer>();

Workspace workspace = service.QueryWorkspaces(string.IsNullOrEmpty(workspaceName) ? null : workspaceName, 
                                                string.IsNullOrEmpty(userName) ? null : userName, 
                                                string.IsNullOrEmpty(compName) ? null : compName).First();

var pendingchanges = workspace.GetPendingChanges();

return pendingchanges;
}*/
//var tfs = 

//if (ignoreListTextBox.Text != "")
//{
//    string file;
//    ignoreList.Items.Add(ignoreListTextBox.Text);
//    file = ignoreListTextBox.Text;
//    ignoreListTextBox.Clear();
//    MessageBox.Show("\"" + file + "\"" + " was added to the ignore list", "File Added");
//}
//else
//{
//    MessageBox.Show("You must enter something in the textbox.", "Nothing Added");
//}
//=============================================================================================================
// this was taken from the checkin method
// TODO is there someway to abstract this part where you drill down to online collections and then go through each registered collection?
// im not sure how all the collections and stuff work? can you have multiple collections in one workspace? 
// i think a workspace is just the specific computer/username that you are using
//var onlineCollections =
//    from collection in this.projects
//    where collection.Offline == false
//    select collection;
//if (onlineCollections.Count() < 1)
//{
//    Console.Error.WriteLine("Error: There are no on-line registered project collections");
//    Environment.Exit(1);
//}
//List<PendingChange> myChanges = new List<PendingChange>();
//foreach (var registeredProjectCollection in onlineCollections)
//{
//    var projectCollection =
//        TfsTeamProjectCollectionFactory.GetTeamProjectCollection(registeredProjectCollection);
//    try
//    {
//        var versionControl = (VersionControlServer)projectCollection.GetService(typeof(VersionControlServer));

//        // the following code gets the workspace based on the workspace dropwdown

//        Workspace workSpace = versionControl.GetWorkspace(workSpaces.SelectedItem.ToString(), System.Environment.UserName);
//        PendingChange[] pendingChanges = workSpace.GetPendingChanges();
//        foreach (PendingChange pendingChange in pendingChanges)
//        {
//            if (!this.ignoreList.Items.Contains(pendingChange.FileName))
//            {
//                myChanges.Add(pendingChange);
//            }
//        }
//        PendingChange[] arrayChanges = myChanges.ToArray();
//        // DEBUG
//        string message = "These are the files that are to be checked in: \n";
//        foreach (PendingChange change in arrayChanges)
//        {
//            message += change.FileName + "\n";
//        }
//        MessageBox.Show(message);
//        workSpace.GetPendingChanges();
//        workSpace.CheckIn(arrayChanges, "");

//    }
//    finally { }
//    break;
//}
//=========================================================================
// from checkin_click

// commenting this out because when i check in i am now just going to see which items are checked for checkin
// adding to the ignore list and other methods are going to make sure that nothing in the ignore list is checked
// in the pending changes list


//bool found;
//string[] ignoreListArray = new string[ignoreList.Items.Count];
//ignoreList.Items.CopyTo(ignoreListArray,0);
//var filters = from f in ignoreListArray
//              where f.Contains("*")
//              select f;
////build changes to be checked in.
//foreach (PendingChange pendingChange in pendingChanges)
//{
//    found = false;
//    if (filters.Count() > 0)
//    {
//        foreach (var filter in filters)
//        {
//            Wildcard wildcard = new Wildcard(filter, RegexOptions.IgnoreCase);

//            // found in the filter so false
//            if (wildcard.IsMatch(pendingChange.FileName))
//            {
//                found = true;

//                break;
//            }
//        }
//        if (found == false)
//        {
//            if (ignoreListArray.Contains(pendingChange.FileName,StringComparer.OrdinalIgnoreCase) == false)
//            {
//                //add if not already added
//                if (myChanges.Contains(pendingChange) == false)
//                {
//                    myChanges.Add(pendingChange);
//                }
//            }
//        }
//    }
//    else
//    {
//        if (ignoreListArray.Contains(pendingChange.FileName, StringComparer.OrdinalIgnoreCase) == false)
//        {
//            //add if not already added
//            if (myChanges.Contains(pendingChange) == false)
//            {
//                myChanges.Add(pendingChange);
//            }
//        }
//    }
//}

// rather than look through the pending changes we are looking through the observable collection for change items selected to be checked in
//foreach (PendingChange change in arrayChanges)
//{
//    message += change.FileName + "\n";
//    // TODO this is going to be change once i change it to checking in all the checked items. 
//    removeFromCollection.Add(change.FileName);
//}
//=====================================================================================
// from check  in
//List<PendingChange> myChanges = new List<PendingChange>();

//// DEBUG
//string message = "These are the files that are to be checked in: \n";

//foreach (changeItem item in changesCollection)
//{
//    // DEBUG
//    //MessageBox.Show(item.selected.ToString());
//    if (item.selected)
//    {
//        message += item.fileName + "\n";
//        myChanges.Add(item.change);
//        removeFromCollection.Add(item.fileName);
//    }
//}

//MessageBox.Show(message);
//============================================================================================
#endregion