using System;
using System.Configuration;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Net;
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
using Microsoft.TeamFoundation.VersionControl.Common;
using System.Text.RegularExpressions;
using System.IO;

// TODO
// what if someone calls undo pending changes or checks in stuff from a different window?
// maybe the refresh button we have shouldnt just loadpendingchanges list again but do its own
// method that really refreshes everything.. clears all the lists and querys the pending changes again?
// or maybe loadpendingchanges can be tweaked to allow for that functionality.

namespace AugustaStateUniversity.SeniorCapstoneIgnoreList
{
    public partial class MyControl : UserControl
    {
        
        public MyControl()
        {
            InitializeComponent();
            this.DataContext = this;
            // something about this makes me nervous
            mc = this;
            shelvewindow = new ShelveWindow();
            unshelvewindow = new UnshelveWindow();
        }
    #region Properties
        // Tool window properties
        //========================
        // TODO : does the mycontrol class need a copy of itself as a property?
        public static MyControl mc;

        public ShelveWindow shelvewindow { get; set; }
        public UnshelveWindow unshelvewindow { get; set; }

        //use for the override button
        public bool checkBoxChecked = false;
        //used for all checkins and to populate pendingchanges list
        public Workspace activeWorkspace { get; set; }
        // list populated by all the checked items on the pendingchanges list. list is used to remove the checked in changes from the pending changes list
        public List<string> removeFromCollection { get { return _removeFromCollection; } set { _removeFromCollection = value; } }
        // this list is used to make sure that there are no duplicates on the pending changes list. 
        public List<string> listOfChanges { get { return _listOfChanges; } set { _listOfChanges = value; } }
        // the pending changes list is bound to this collection.
        public ObservableCollection<changeItem> changesCollection { get { return _changesCollection; } set { _changesCollection = value; } }

        // Shelve/Unshelve Window properties
        //===========================
        // collection of pending changes that is passed to the shelveset window to populate what will be shelved
        public ObservableCollection<changeItem> shelveCollection { get { return _shelveCollection; } set { _shelveCollection = value; } }
        
        // used for the unshelve window to populate all shelvsets across all workspaces
        public Workspace[] allWorkSpaces { get; set; }
        // collection of shelvesets that is passed to the unshelve window
        public ObservableCollection<Shelveset> shelveSetCollection { get { return _shelveSetCollection; } set { _shelveSetCollection = value; } }
    #endregion

    #region Private Vars
        private List<RegisteredProjectCollection> projects;
        private List<string> _removeFromCollection = new List<string>();
        private List<string> _listOfChanges = new List<string>();
        private ObservableCollection<changeItem> _changesCollection = new ObservableCollection<changeItem>();
        private ObservableCollection<changeItem> _shelveCollection = new ObservableCollection<changeItem>();
        private ObservableCollection<Shelveset> _shelveSetCollection = new ObservableCollection<Shelveset>();
    #endregion

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions")]
    #region Tool window functions
        public static void RegisterEventHandlers(VersionControlServer versionControl)
        {
            // DEBUG 
            //MessageBox.Show("registering event handlers");

            //this updates pendingchanges
            versionControl.OperationFinished += afterUpdate;
        }
        public static void afterUpdate(Object sender, OperationEventArgs e)
        {
            // something about this makes me nervous
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
                var projectCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(registeredProjectCollection, new UICredentialsProvider());
                
                // this is will throw out the credential window if you are not authenticated
                // TODO
                // is there some way to pass default credentials?
                // i think the best way to get around this would be to have stuff configured
                //correctly on tfs... so having the user entercredentials is good. if they are 
                // setup correctly they wont have to
                projectCollection.EnsureAuthenticated();
                
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
                    allWorkSpaces = versionControl.QueryWorkspaces(null, null, null);
                    loadWorkspaces(allWorkSpaces);
                    workSpaces.SelectedItem = workspace.Name;

                    RegisterEventHandlers(versionControl);
                }
                finally { }
                break;
            }
            loadPendingChangesList();
        }
        // do we use onLoad or onInitialize?
        /// <summary>
        /// ran when the window is initialized. loads the last state of the ignore list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MyToolWindow_Init(object sender, EventArgs e)
        {
            // this can be used to load the ignore list from the computer
            // it is only called once
            // we have to make sure that loading the list has nothing to do with tfs though, or it will crap out
            // from not having permission
            string path = System.Reflection.Assembly.GetExecutingAssembly().Location;
            //MessageBox.Show(path);
        }
        /// <summary>
        /// whenever the toolwindow is closed the list is saved to the computer for loading next time the list is initialized
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void toolWindow_unloaded(object sender, RoutedEventArgs e)
        {
            //MessageBox.Show("unloaded");
            // this can be used to save the ignore list
            // this is called everytime the window is unloaded, if its tabbed and you close the tab for example
        }
        #region dragDrop Code
        private void myDataGrid_MouseMove(object sender, MouseEventArgs e)
        {
            // TODO 
            // make a custom move cursor so the user knows he is moving something
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                changeItem moving = (changeItem)pendingChangesList.SelectedItem;
                string name = moving.fileName;
                ListView testView = new ListView();
                object test = new ListViewItem();
                // DEBUG
                //MessageBox.Show(moving.fileName);
                object selectedItem = pendingChangesList.SelectedItem;
                if (selectedItem != null)
                {
                    DataGridRow container = (DataGridRow)pendingChangesList.ItemContainerGenerator.ContainerFromItem(selectedItem);
                    DragDropEffects finalDropEffect = DragDrop.DoDragDrop(pendingChangesList, name, DragDropEffects.Move);
                }
            }
        }
        private void DataGrid_CheckDropTarget(object sender, DragEventArgs e)
        {
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
            // im using this to add to the ignore list because i cant get drop to work
            //addToIgnoreList((string)e.Data.GetData(DataFormats.Text));
            checkIgnoreList();
        }
        private void ignoreList_Drop(object sender, DragEventArgs e)
        {
            //DOESNT WORK
            // i dont know why i couldnt get this to work.
            // is it because im trying to drop something that the list doenst like?
            // this even is never fired
            e.Effects = DragDropEffects.Move;
            e.Handled = true;
            MessageBox.Show("HELL FUCKIN YA");
            //ignoreList.Items.Add(e.Data.GetData(DataFormats.Text));
        }
        #endregion

        /// <summary>
        /// refreshes the pendingchanges list to show any changes that may have been made elsewhere
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void refresh_click(object sender, RoutedEventArgs e)
        {
            refreshPendingChangesList();
        }
        /// <summary>
        /// basically the same as loadpendingchangeslist but it clears the changescollection and listofchanges
        /// </summary>
        public void refreshPendingChangesList()
        {
            changesCollection.Clear();
            listOfChanges.Clear();
            PendingChange[] pendingChanges = activeWorkspace.GetPendingChanges();
            foreach (PendingChange pendingChange in pendingChanges)
            {
                // i have this next thing there to make sure that you dont add the same file twice if we keep this
                // onload
                if (!mc.pendingChangesList.Items.Contains(pendingChange.FileName))
                {
                    // this checks the list of changes to see if it needs to be added to the collection or not
                    if (!listOfChanges.Contains(pendingChange.FileName))
                    {
                        changesCollection.Add(new changeItem(pendingChange));
                        listOfChanges.Add(pendingChange.FileName);
                    }
                }

                checkIgnoreList();
                string message = "";
                foreach (changeItem item in changesCollection)
                {
                    message += (item.fileName) + "\n";
                }
                // DEBUG - this shows what the collection contains that the list is bound to
                //MessageBox.Show(message);
            }
        }
        /// <summary>
        /// allows user to force all pending changes to be checked in the pending changes list, overriding the ignore list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void overide_Checked(object sender, RoutedEventArgs e)
        {
            foreach (var change in changesCollection)
            {
                change.selected = true;
            }
            checkBoxChecked = true;
        }
        /// <summary>
        /// turns off the override, enabling the ignorelist again
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void overide_Unchecked(object sender, RoutedEventArgs e)
        {
            checkBoxChecked = false;
            checkIgnoreList();
        }

    #endregion

    #region pending Changes Section
        /// <summary>
        /// Used for the pending changes list workspace drop down
        /// </summary>
        /// <param name="workspaces"></param>
        public void loadWorkspaces(Workspace[] workspaces)
        {
            foreach (Workspace workspace in workspaces)
            {
                if (!workSpaces.Items.Contains(workspace.Name))
                {
                    workSpaces.Items.Add(workspace.Name);
                }
            }
        }
        /// <summary>
        /// Compares pending changes to ignore list. if pending change matches an item in ignore list, uncheck the pending change
        /// </summary>
        public void checkIgnoreList()
        {
            bool found;
            string[] ignoreListArray = new string[ignoreList.Items.Count];
            ignoreList.Items.CopyTo(ignoreListArray, 0);
            var filters = from f in ignoreListArray
                          where f.Contains("*")
                          select f;


            //build changes to be checked in.
            if (!checkBoxChecked)
            {
                foreach (changeItem item in changesCollection)
                {
                // DEBUG
                //MessageBox.Show("BEFORE\nselected = " + item.selected + "\nPreviousState = " + item.previousState);
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
                            // if the item needs to be change to false, do so and keep previous state as true
                            if (item.selected == true)
                            {
                                item.selected = false;
                                item.previousState = true;
                            }
                            // if the item is already false, do nothing
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
                        {
                            if (item.selected == true)
                            {
                                item.selected = false;
                                item.previousState = true;
                            }
                        }
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
                    {
                        if (item.selected == true)
                        {
                            item.selected = false;
                            item.previousState = true;
                        }
                    }
                    }
                // DEBUG
                //MessageBox.Show("selected = " + item.selected + "\nPreviousState = " + item.previousState);
                }
            }
        }
        /// <summary>
        /// loads pending changes for the selected workspace into the pending changes list
        /// </summary>
        public void loadPendingChangesList()
        {
            PendingChange[] pendingChanges = activeWorkspace.GetPendingChanges();
            foreach (PendingChange pendingChange in pendingChanges)
            {
                // i have this next thing there to make sure that you dont add the same file twice if we keep this
                // onload
                if (!mc.pendingChangesList.Items.Contains(pendingChange.FileName)) 
                {
                    // this checks the list of changes to see if it needs to be added to the collection or not
                    if(!listOfChanges.Contains(pendingChange.FileName))
                    {
                        changesCollection.Add(new changeItem(pendingChange));
                        listOfChanges.Add(pendingChange.FileName);
                    }
                }
            }
            checkIgnoreList();
            string message = "";
            foreach (changeItem item in changesCollection)
            {
                message += (item.fileName) + "\n";
            }
            // DEBUG - this shows what the collection contains that the list is bound to
            //MessageBox.Show(message);
        }
        /// <summary>
        /// uses the removeFromCollection in order to remove all the pending changes that were checked in from the list
        /// </summary>
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
                    // have to do this so something that was checked in can make it back onto the list
                    if(listOfChanges[i].Equals(remove))
                    {
                        listOfChanges.RemoveAt(i);
                    }

                }
            }
        }
        /// <summary>
        /// iterates through the changes collection to find all items that are checked, returns pendingChanges[] for check in
        /// </summary>
        /// <param name="collection"></param>
        /// <returns></returns>
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
        /// <summary>
        /// called from checkin_click(). will be used for conflicts
        /// </summary>
        private void checkForConflicts()
        {
            // DONT WORK
            // TODO in order to go anywhere we this we need to be able to get those first 2 strings
            // first one is local path, second is serverpath 
            // I am worried about this. if we get the working folders thats great. but is it going to check it all back in? 
            // i just dont know enough about it yet
            WorkingFolder[] Folders = activeWorkspace.Folders;
            GetStatus status = activeWorkspace.Merge(Folders[1].LocalItem,
                        Folders[1].ServerItem,
                        null,
                        null,
                        LockLevel.None,
                        RecursionType.Full,
                        MergeOptions.None);
            MessageBox.Show(status.ToString());
            Conflict[] conflicts = activeWorkspace.QueryConflicts(new string[] { Folders[0].ServerItem }, true);
            MessageBox.Show("there are " + conflicts.Length + " conflicts");
            foreach (Conflict conflict in conflicts)
            {
                MessageBox.Show(conflict.ToString());
                if (activeWorkspace.MergeContent(conflict, true))
                {
                    conflict.Resolution = Resolution.AcceptMerge;
                    activeWorkspace.ResolveConflict(conflict);
                }
                if (conflict.IsResolved)
                {
                    activeWorkspace.PendEdit(conflict.TargetLocalItem);
                    File.Copy(conflict.MergedFileName, conflict.TargetLocalItem,
                        true);
                }
            }
            string message = "";
            message += "There are " + Folders.Length + " folders in the workspace";
            for (int i = 0; i < Folders.Length; i++)
            {
                message += "folder[" + i + "] is: " + Folders[i].LocalItem;
            }
            message+="\nFolder[0] localItem : "+Folders[0].LocalItem;
            message+="\nFolder[0] serverItem : "+Folders[0].ServerItem;
            MessageBox.Show(message);
        }
        /// <summary>
        /// called from checkin_click(). it checks to see if a checked item for checkin is on the ignore list
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        private bool isIgnored(string name)
        {
            bool isIgnored = false;
            string[] ignoreListArray = new string[ignoreList.Items.Count];
            ignoreList.Items.CopyTo(ignoreListArray, 0);
            var filters = from f in ignoreListArray
                          where f.Contains("*")
                          select f;


            if (filters.Count() > 0)
            {
                foreach (var filter in filters)
                {
                    Wildcard wildcard = new Wildcard(filter, RegexOptions.IgnoreCase);

                    // found in the filter so ignored
                    if (wildcard.IsMatch(name))
                    {
                        isIgnored = true;
                        break;
                    }
                }
                if (isIgnored == false)
                {
                    // not in filter and not in the ignore list
                    if (ignoreListArray.Contains(name, StringComparer.OrdinalIgnoreCase) == false)
                    {
                        ;
                    }
                    else
                    {
                        isIgnored = true;
                    }
                }
            }
            else
            {
                // no filters and they are not in the ignore list
                if (ignoreListArray.Contains(name, StringComparer.OrdinalIgnoreCase) == false)
                {
                    ;
                }
                else
                {
                    isIgnored = true;
                }
            }

            return isIgnored;
        }
        /// <summary>
        /// check in process
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void checkin_Click(object sender, RoutedEventArgs e)
        {
            // DEBUG
            //checkForConflicts();
            // TODO 
            // check for conflicts... i say just show that there are conflicts and they can deal with them on their own if
            // VS or TFS does not have an easy way to do it automatically.
            PendingChange[] arrayChanges = getSelectedChanges(changesCollection);
            List<PendingChange> confirmChanges = new List<PendingChange>();

            // checks to see if any item to be checked in is on the ignore list
            foreach (PendingChange change in arrayChanges)
            {
                // if it is on the ignore list, add file to a list for user prompt
                if (isIgnored(change.FileName))
                {
                    confirmChanges.Add(change);
                }
            }
            //user prompt about checking in files that are ignored
            string message = "The following files are on the ignore list:\n";
            foreach (PendingChange change in confirmChanges)
            {
                message += change.FileName + "\n";
            }
            message += "\nAre you sure you want to check them in?";
            // if there is a file the user has to confirm
            if (confirmChanges.Count > 0)
            {
                if (MessageBox.Show(message, "Attention", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No) == MessageBoxResult.Yes)
                {
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
                }
                confirmChanges.Clear();
            }
            // nothing the user has to confirm so go ahead
            else
            {
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
            }
            // TODO 
            // once this is checked in... VS spits out the output in a window. do we want to do that? do we need to do that?
            
        }
        /// <summary>
        /// ignore selected item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ignore_Click(object sender, RoutedEventArgs e)
        {
            if (pendingChangesList.SelectedItem != null)
            {
                changeItem item = (changeItem)pendingChangesList.SelectedItem;
                addToIgnoreList(item.fileName);
                checkIgnoreList();
            }
        }
        /// <summary>
        /// opens the shelve dialog and calls the shelve method based on the dialog results
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void shelve_Click(object sender, RoutedEventArgs e)
        {   
            string message = "";
            foreach (changeItem item in shelveCollection)
            {
                message += item.fileName + "\n";
            }
            message += ".";
            // DEBUG
            // MessageBox.Show(message);

            // pass the shelve window the collection of pending changes so they can select what to shelve
            shelvewindow = new ShelveWindow(changesCollection, activeWorkspace);
            shelvewindow.ShowDialog();
            if (shelvewindow.DialogResult.HasValue && shelvewindow.DialogResult.Value)
            {
                // Debug
                //MessageBox.Show("User clicked OK");
                shelveCollection = shelvewindow.shelveCollection;
                // error checking is done in the shelvewindow
                Shelve();
            }
            else
            {
                // Debug
                //MessageBox.Show("User clicked Cancel");
            }
        }
        /// <summary>
        /// opens the unshelve dialog and calls the unshelve method based on the dialog results
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void unshelve_Click(object sender, RoutedEventArgs e)
        {
            // TODO
            // im not 100% this is how unshelve is supposed to work

            // this is to clear the collection each time you re-open the unshelve window. 
            shelveSetCollection.Clear();

            // passes all the shelvesets in all the workspaces
            foreach (Workspace workspace in allWorkSpaces)
            {
                Shelveset[] shelveSets = workspace.VersionControlServer.QueryShelvesets(null, null);
                foreach (Shelveset set in shelveSets)
                {
                    shelveSetCollection.Add(set);
                }
            }
            unshelvewindow = new UnshelveWindow(shelveSetCollection, activeWorkspace);
            unshelvewindow.ShowDialog();
            if (unshelvewindow.DialogResult.HasValue && unshelvewindow.DialogResult.Value)
            {
                // Debug
                //MessageBox.Show("User clicked OK");
                Unshelve();
            }
            else
            {
                // Debug
                //MessageBox.Show("User clicked Cancel");
            }
        }
        /// <summary>
        /// switches the workspace based on the dropdown, and then calls refreshPendingChangesList
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void changeWorkspace(object sender, SelectionChangedEventArgs e)
        {
            // TODO
            // not exactly sure how workspaces and versioncontrolservers work... 
            #region not sure if we need
            //// TODO abstract this so its not so messy
            //var onlineCollections =
            //    from collection in projects
            //    where collection.Offline == false
            //    select collection;
            //// fail if there are no registered collections that are currently on-line
            //if (onlineCollections.Count() < 1)
            //{
            //    Console.Error.WriteLine("Error: There are no on-line registered project collections");
            //    Environment.Exit(1);
            //}
            //// find a project collection with at least one team project
            //// is it going to act differently if there is more than one online collection?
            //// TODO is there ever going to be more than one project collection? if there is. i dont think that my way of finding the current workspace would work
            //// or maybe it would work but we dont need to have a version control for each registered project collection... if we dont need to foreach through this
            //// then we could have versioncontrol be a property as well.. if versioncontrol was a property we could switch between workspace much easier
            //foreach (var registeredProjectCollection in onlineCollections)
            //{
            //    var projectCollection = TfsTeamProjectCollectionFactory.GetTeamProjectCollection(registeredProjectCollection);
            //    try
            //    {
            //        var versionControl = (VersionControlServer)projectCollection.GetService(typeof(VersionControlServer));
            //        var teamProjects = new List<TeamProject>(versionControl.GetAllTeamProjects(false));
            //        //if there are no team projects in this collection, skip it
            //        if (teamProjects.Count < 1) continue;
            //        // ----------------> all of that was just to get at this versioncontrol in order to get the workspace with the dropdowns selected items name
                    
            //        Workspace workspace = versionControl.GetWorkspace(workSpaces.SelectedItem.ToString(), System.Environment.UserName);
            //        activeWorkspace = workspace;
            //    }
            //    finally { }
            //    break;
            //}
            #endregion

            // this gets the versioncontrolserver from the activeworkspace and then finds the selected workspace based on the name
            // from the workspace dropdown
            activeWorkspace = activeWorkspace.VersionControlServer.GetWorkspace(workSpaces.SelectedItem.ToString(), System.Environment.UserName);
            // once the workspace is changed, refresh the pending changes list for that workspace
            refreshPendingChangesList();
        }
        /// <summary>
        /// called when an item is taken off the ignore list. returns the pendingchange checkbox back to the last state
        /// the user selected for it
        /// </summary>
        /// <param name="name"></param>
        private void restorePreviousState(string name)
        {
            // DEBUG
            //MessageBox.Show("You removed " + name);
            // if its a wildcard then match with contains.
            if (name.Contains("*"))
            {
                // strips out the *
                name = name.Replace("*", "");
                // DEBUG
                //MessageBox.Show("matching wildcard :"+name);
                
                foreach (changeItem item in changesCollection)
                {
                    if (item.fileName.ToLower().Contains(name.ToLower()))
                    {
                        item.selected = item.previousState;
                    }
                }
            }
            // its not a wildcard so just match the name
            else
            {
                // DEBUG
                //MessageBox.Show("matching just the name");
                foreach (changeItem item in changesCollection)
                {
                    if (item.fileName.ToLower().Equals(name.ToLower()))
                    {
                        item.selected = item.previousState;
                    }
                }
            }
        }
        /// <summary>
        /// if user pushed 'i' on the pending change list, the selected item is moved to the ignore list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void pendingChangesList_KeyDown(object sender, KeyEventArgs e)
                {
                    if (e.Key == Key.I)
                    {
                        ignore_Click(this, e);
                    }
                }
        /// <summary>
        /// toggles the pending changes list comment box
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
        /// <summary>
        /// load a ignorelist
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void loadButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            dlg.FileName = ""; // Default file name
            dlg.DefaultExt = ".txt"; // Default file extension
            dlg.Filter = "Text documents (.txt)|*.txt"; // Filter files by extension
            dlg.Title = "Load Ignore List";
            // Show open file dialog box
            Nullable<bool> loadResult;
            bool itemsOnList = ignoreList.Items.Count > 0;

            if (itemsOnList)
            {
                var saveResult = MessageBox.Show("Do you want to save first?", "Unsaved List", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (saveResult == MessageBoxResult.Cancel)
                {
                    ;//do nothing because they pushed cancel
                }
                else
                {
                    // if they pushed yes, save it
                    if (saveResult == MessageBoxResult.Yes)
                    {
                        saveButton_Click(this, e);
                    }

                    //load dialog stuff
                    loadResult = dlg.ShowDialog();
                    if (loadResult == true)
                    {
                        var appendResult = MessageBox.Show("Do you want to append to the current ignore list?", "Append to list", MessageBoxButton.YesNo, MessageBoxImage.Question);
                        if (appendResult == MessageBoxResult.No)
                        {
                            foreach (string item in ignoreList.Items)
                            {
                                restorePreviousState(item);
                            }
                            ignoreList.Items.Clear();
                        }
                        string[] ignoreListItems;
                        string filename = dlg.FileName;
                        ignoreListItems = System.IO.File.ReadAllLines(filename);

                        foreach (string s in ignoreListItems)
                        {
                            addToIgnoreList(s);
                        }
                        checkIgnoreList();
                    }
                }
            }
            else
            {
                loadResult = dlg.ShowDialog();
                if (loadResult == true)
                {
                    string[] ignoreListItems;
                    string filename = dlg.FileName;
                    ignoreListItems = System.IO.File.ReadAllLines(filename);

                    foreach (string s in ignoreListItems)
                    {
                        addToIgnoreList(s);
                    }
                    checkIgnoreList();
                }
            }
        }
        /// <summary>
        /// save the ignore list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
        /// <summary>
        /// remove item from ignore list, calls restore previous state on the removed item
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            string item = ignoreList.Items[ignoreList.SelectedIndex].ToString();
            restorePreviousState(item);
            if (ignoreList.SelectedIndex != -1)
            {
                ignoreList.Items.RemoveAt(ignoreList.SelectedIndex);
            }
        }
        /// <summary>
        /// clears the entire ignore list, calls restore previous state on all items
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            if (ignoreList.Items.Count > 0)
            {
                var result = MessageBox.Show("Are you sure you want to clear the ignore list?", "Clear List", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    foreach (string item in ignoreList.Items)
                    {
                        restorePreviousState(item);
                    }
                    ignoreList.Items.Clear();
                }
            }
            
        }
        /// <summary>
        /// the method that is called whenever an item is added to the ignorelist
        /// </summary>
        /// <param name="fileName"></param>
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
        /// <summary>
        /// Create New List
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void newButton_Click(object sender, RoutedEventArgs e)
        {
            if (ignoreList.Items.Count > 0)
            {
                var result = MessageBox.Show("Do you want to save first?", "Unsaved List", MessageBoxButton.YesNoCancel, MessageBoxImage.Question);
                if (result == MessageBoxResult.Cancel)
                {
                    ;//do nothing because they pushed cancel
                }
                else
                {
                    // if they pushed yes, save it
                    if (result == MessageBoxResult.Yes)
                    {
                        saveButton_Click(this, e);
                    }
                    // whether they pushed yes or no: restore previous states and clear the list
                    foreach (string item in ignoreList.Items)
                    {
                        restorePreviousState(item);
                    }
                    ignoreList.Items.Clear();
                }
            }
        }
        /// <summary>
        /// allows user to push enter to add item to ignore list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ignoreTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                ignoreListAddButton_Click(this, e);
            }
        }
        /// <summary>
        /// toggles the ignore list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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
        /// <summary>
        /// pressing r when an item is selected in the ignore list will remove it from the ignore list
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ignoreList_KeyDown(object sender, KeyEventArgs e)
                {
                    if (e.Key == Key.R)
                    {
                        RemoveButton_Click(this, e);
                    }
                }
    #endregion

    #region shelve/unshelve window
        private void Shelve()
        {
            //need to add exception if the name already exists
            Shelveset shelveset = new Shelveset(activeWorkspace.VersionControlServer, shelvewindow.shelvesetName.Text, activeWorkspace.OwnerName);
            shelveset.Comment = shelvewindow.comment.Text;
            PendingChange[] toShelve = getSelectedChanges(shelveCollection);
            activeWorkspace.Shelve(shelveset, toShelve, ShelvingOptions.None);
            // have to "UNDO" on the pending changes that were shelved in order to unshelve the set
            activeWorkspace.Undo(toShelve);
            removeFromCollection.Clear();
            foreach (PendingChange change in toShelve)
            {
                removeFromCollection.Add(change.FileName);
            }
            // removes all the pendingchanges tht are in removefromcollection
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
        private void Unshelve()
        {
            // TODO 
            // if you try to unshelve something that you dont have as a pending change already this is fine... but if you try to unshelve
            // a file that you have checked out already (that is, its already in your pending changes list) this will crash out
            // what im going to do to fix it is undo all pending changes 
            // this might not be the best way to do this but i imagine it will work for now. 
            activeWorkspace.Undo(unshelvewindow.changes);

            // TODO
            // im going to tackle this when the problem comes up... i think you have to unshelve the shelveset from the workspace that
            // shelved it or do you unshelve it in your activeworkspace?
            MessageBox.Show("About to unshelve:\n" + unshelvewindow.selectedSet.Name + "," + unshelvewindow.selectedSet.OwnerName);
            try
            {
                activeWorkspace.Unshelve(unshelvewindow.selectedSet.Name, unshelvewindow.selectedSet.OwnerName);
                // put this here becuase i want to make sure the pending changes list is updated... maybe there is a better way?
                loadPendingChangesList();
            }
            catch (Microsoft.TeamFoundation.VersionControl.Client.UnshelveException err)
            {
                MessageBox.Show("Unshelve Exception was thrown:\n" + err.Message);
            }
        }
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
        
        #region properties
        public string fileName { get; set; }
        public string changeType { get; set; }
        public string folder { get; set; }
        public PendingChange change { get; set; }
        public event PropertyChangedEventHandler PropertyChanged;
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
                    //this needs to always mirror the selected state
                    previousState = value;
                    if (PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs("selected"));
                    }

                }
            }
        }
        public bool previousState { get; set; }
        #endregion

        #region private vars
        private bool _selected;
        #endregion
        
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