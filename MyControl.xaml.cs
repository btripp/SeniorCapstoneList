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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.TeamFoundation.Client;
using Microsoft.TeamFoundation.VersionControl.Client;
using System.Text.RegularExpressions;

namespace AugustaStateUniversity.SeniorCapstoneIgnoreList
{
    public partial class MyControl : UserControl
    {
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
        private List<RegisteredProjectCollection> projects;
        public ObservableCollection<changeItem> changesCollection { get { return _changesCollection; } }
        public Workspace activeWorkspace { get; set; }
        public ShelveWindow shelvewindow { get; set; }
        public UnshelveWindow unshelvewindow { get; set; }
        #endregion
        #region Private Vars
        // do we need this?
        ObservableCollection<changeItem> _changesCollection = new ObservableCollection<changeItem>();
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
        public void loadPendingChangesList()
        {
            // because i pass the workspace... this will only have pending changes for the current workspace
            // this clear is to clear the observableCollection
            this.changesCollection.Clear();
            PendingChange[] pendingChanges = activeWorkspace.GetPendingChanges();
            foreach (PendingChange pendingChange in pendingChanges)
            {
                // i have this next thing there to make sure that you dont add the same file twice if we keep this
                // onload
                // TODO we are going to have to add something to the list in a different way so that we can
                // dynamically add the checkboxes as well. in order to get it to look like the VS window
                if (!mc.pendingChangesList.Items.Contains(pendingChange.FileName))
                {
                    this.changesCollection.Add(new changeItem(pendingChange));
                }
            }
            string message = "";
            foreach (changeItem item in changesCollection)
            {
                message += (item.fileName) + "\n";
            }
            // DEBUG - this shows what the collection contains that the list is bound to
            //MessageBox.Show(message);
        }
        //I would say that we keep this guy. I think its a nice feature to have.
        
        //added regex stuff
        //added stuff to make sure a change isnt added twice... not sure if it ever can.
        //TODO Need to add logic to make sure we dont try to check in nothing
        private void checkin_Click(object sender, RoutedEventArgs e)
        {
            bool found;
            string[] ignoreListArray = new string[ignoreList.Items.Count];
            ignoreList.Items.CopyTo(ignoreListArray,0);
            var filters = from f in ignoreListArray
                          where f.Contains("*")
                          select f;
            
            List<PendingChange> myChanges = new List<PendingChange>();
            PendingChange[] pendingChanges = activeWorkspace.GetPendingChanges();

            //build changes to be checked in.
            foreach (PendingChange pendingChange in pendingChanges)
            {
                found = false;
                if (filters.Count() > 0)
                {
                    foreach (var filter in filters)
                    {
                        Wildcard wildcard = new Wildcard(filter, RegexOptions.IgnoreCase);
                        
                        if (wildcard.IsMatch(pendingChange.FileName))
                        {
                            found = true;
                            break;
                        }
                    }
                    if (found == false)
                    {
                        if (ignoreListArray.Contains(pendingChange.FileName,StringComparer.OrdinalIgnoreCase) == false)
                        {
                            //add if not already added
                            if (myChanges.Contains(pendingChange) == false)
                            {
                                myChanges.Add(pendingChange);
                            }
                        }
                    }
                }
                else
                {
                    if (ignoreListArray.Contains(pendingChange.FileName, StringComparer.OrdinalIgnoreCase) == false)
                    {
                        //add if not already added
                        if (myChanges.Contains(pendingChange) == false)
                        {
                            myChanges.Add(pendingChange);
                        }
                    }
                }
            }

            PendingChange[] arrayChanges = myChanges.ToArray();
            // DEBUG
            string message = "These are the files that are to be checked in: \n";
            foreach (PendingChange change in arrayChanges)
            {
                message += change.FileName + "\n";
            }
            MessageBox.Show(message);
            activeWorkspace.GetPendingChanges();
            if (arrayChanges.Count() > 0)
            {
                activeWorkspace.CheckIn(arrayChanges, commentBox.Text);
                MessageBox.Show(arrayChanges.Count() + " File(s) checked in.", "Files Checked in...", MessageBoxButton.OK, MessageBoxImage.Information);
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
            shelvewindow.ShowDialog();
            // any code after show dialog will not execute until the dialog has been closed, we can still you the things that belong
            // to that window
            //shelvewindow.shelvesetName.Text
        }

        private void unshelve_Click(object sender, RoutedEventArgs e)
        {
            unshelvewindow.ShowDialog();
            // this one will be easy once i fully understand the shelve method
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
        #endregion
        #region ignore list section
        private void ignoreListAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (ignoreTextBox.Text != null && ignoreTextBox.Text != "")
            {
                addToIgnoreList(ignoreTextBox.Text);
                ignoreTextBox.Text = "";
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
    }
    
        

    public class changeItem
    {
        public changeItem()
        {
            fileName = "test name";
            changeType = "test type";
            folder = "test folder";
        }
        public changeItem(PendingChange change)
        {
            fileName = change.FileName;
            changeType = change.ChangeType.ToString();
            folder = change.LocalOrServerFolder;
        }
        // TODO 
        // maybe add another property for a checkbox?
        // if that happens i can have a template for the grid that has a checkbox that is checked based on the checkbox property
        // if there is a property like that, when checking in i can just run through the observable collection to make a newpendingchanges list that
        // moves everything over that has a checked checkbox
        // similarly, when we add something to the ignore list (if it doesnt exist already) we can find the element in the observable collection that 
        // shares the same name and change that changeItem checkbox property to false.
        public string fileName { get; set; }
        public string changeType { get; set; }
        public string folder { get; set; }
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
#endregion