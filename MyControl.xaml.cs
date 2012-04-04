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



namespace AugustaStateUniversity.SeniorCapstoneIgnoreList
{
    /// <summary>
    /// Interaction logic for MyControl.xaml
    /// </summary>
    /// 

    public partial class MyControl : UserControl
    {
        
        public MyControl()
        {
            
            InitializeComponent();
            this.DataContext = this;
            mc = this;
        }
        #region Properties
        // TODO : does the mycontrol class need a copy of itself as a property?
        public static MyControl mc;
        private List<RegisteredProjectCollection> projects;
        public ObservableCollection<changeItem> changesCollection { get { return _changesCollection; } }
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
            // filter down to only those collections that are currently on-line
            var onlineCollections =
                from collection in this.projects
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

                    // TODO : maybe hook up with a collection load rather than load? that way once you connect to tfs
                    // this will load up. rather than possibly missing the on load stuff
                    Workspace[] workSpaces = versionControl.QueryWorkspaces(null, null, System.Environment.MachineName);

                    // this clear is to clear the observableCollection
                    this.changesCollection.Clear();

                    foreach (Workspace workspace in workSpaces)
                    {
                        PendingChange[] pendingChanges = workspace.GetPendingChanges();
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
                }
                finally { }
                break;
            }
        }
        private void ignore_Click(object sender, RoutedEventArgs e)
        {
            // not sure if this guy is going to stay around
            // yea i see what your saying. The pending changes list should show everyfile that is checked out.....
            // then they just put in what they want to ignore via the other little window. 
            // I do like the idea of having a button that will add that item to the ignore list though in case they already have it selected...
            //object item = this.pendingChangesList.SelectedItem;
            //this.ignoreList.Items.Add(item);
            //this.pendingChangesList.Items.Remove(item);

            if (pendingChangesList.SelectedItem != null)
            {
                changeItem item = (changeItem)pendingChangesList.SelectedItem;
                //ignoreList.Items.Add(item.fileName);
                addToIgnoreList(item.fileName);
            }


        }
        private void checkin_Click(object sender, RoutedEventArgs e)
        {
            // TODO is there someway to abstract this part where you drill down to online collections and then go through each registered collection?
            // im not sure how all the collections and stuff work? can you have multiple collections in one workspace? 
            // i think a workspace is just the specific computer/username that you are using
            var onlineCollections =
                from collection in this.projects
                where collection.Offline == false
                select collection;
            if (onlineCollections.Count() < 1)
            {
                Console.Error.WriteLine("Error: There are no on-line registered project collections");
                Environment.Exit(1);
            }
            List<PendingChange> myChanges = new List<PendingChange>();
            foreach (var registeredProjectCollection in onlineCollections)
            {
                var projectCollection =
                    TfsTeamProjectCollectionFactory.GetTeamProjectCollection(registeredProjectCollection);
                try
                {
                    var versionControl = (VersionControlServer)projectCollection.GetService(typeof(VersionControlServer));

                    // the following code gets the workspace based on the workspace dropwdown

                    Workspace workSpace = versionControl.GetWorkspace(workSpaces.SelectedItem.ToString(), System.Environment.UserName);
                    PendingChange[] pendingChanges = workSpace.GetPendingChanges();
                    foreach (PendingChange pendingChange in pendingChanges)
                    {
                        if (!this.ignoreList.Items.Contains(pendingChange.FileName))
                        {
                            myChanges.Add(pendingChange);
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
                    workSpace.GetPendingChanges();
                    workSpace.CheckIn(arrayChanges, "");

                }
                finally { }
                break;
            }



        }

        #endregion

        #region ignore list section
        private void ignoreListAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (ignoreTextBox.Text != null && ignoreTextBox.Text != "")
            {
                //ignoreList.Items.Add(ignoreTextBox.Text);
                addToIgnoreList(ignoreTextBox.Text);
                ignoreTextBox.Text = "";
            }
            else
            {
                MessageBox.Show("You must enter something to ignore.");
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
                    //ignoreList.Items.Add(s);
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
            ignoreList.Items.Clear();
        }
        public void addToIgnoreList(string fileName)
        {
            if (!ignoreList.Items.Contains(fileName))
            {
                ignoreList.Items.Add(fileName);
            }
        }

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
            folder = "dunno";
        }
        public string fileName { get; set; }
        public string changeType { get; set; }
        public string folder { get; set; }
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
#endregion