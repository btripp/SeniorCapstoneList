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
        // TODO : does the mycontrol class need a copy of itself as a property?

        public static MyControl mc;
        private List<RegisteredProjectCollection> projects;
        public MyControl()
        {
            
            InitializeComponent();
            this.DataContext = this;
            mc = this;
        }
        public ObservableCollection<int> test { get; set; }
        ObservableCollection<changeItem> _changesCollection = new ObservableCollection<changeItem>();
        public ObservableCollection<changeItem> changesCollection { get { return _changesCollection; } }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions")]

        private void ignoreListAddButton_Click(object sender, RoutedEventArgs e)
        {
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

        }
        public static void RegisterEventHandlers(VersionControlServer versionControl)
        {
            //MessageBox.Show("registering event handlers");
            versionControl.CommitCheckin += (versionControl_CommitCheckin);
            versionControl.BeforeCheckinPendingChange += beforeCheckIn;
            versionControl.NewPendingChange += OnNewPendingChange;
            versionControl.OperationFinished += new OperationEventHandler(afterUpdate);
            versionControl.Getting += versionControl_GetCompleted;
        }

        public static void afterUpdate(Object sender, OperationEventArgs e)
        {
            mc.loadPendingChangesList();
        }
        public static void versionControl_GetCompleted(Object sender, GettingEventArgs e)
        {
            MessageBox.Show("getting");
        }
        public static void versionControl_CommitCheckin (Object sender,CommitCheckinEventArgs e)
        {
            //MessageBox.Show("thiss is the commit checkin stuff");
            IEnumerable<PendingChange> test = e.Workspace.GetPendingChangesEnumerable();
            foreach (PendingChange change in test)
            {
                //MessageBox.Show("changeType = " + change.ChangeType + "\n" +
                  //              "changeTypeName = " + change.ChangeTypeName + "\n" +
                    //            "filename = " + change.FileName);
            }
        }
        public static void OnBeforeCheckinPendingChange(Object sender, ProcessingChangeEventArgs e)
        {
            
            //mc.beforeCheckIn(e);
            // TODO : do we need the mc here?
            MessageBox.Show(e.PendingChange.ChangeType.ToString());
            MessageBox.Show(e.PendingChange.ChangeTypeName.ToString());
            
            if (mc.ignoreList.Items.Contains(e.PendingChange.FileName))
            {
                MessageBox.Show("**before checkinpending change**\nDo you want to Check in " + e.PendingChange.FileName);
            }
            else
            {
                MessageBox.Show("The file is not in the ignore list. " + e.PendingChange.FileName);
            }
            //MessageBox.Show("Checking in " + e.PendingChange.FileName);      
        }
        public static void beforeCheckIn(Object sender, ProcessingChangeEventArgs e)
        {
            PendingChange[] changes = e.Workspace.GetPendingChanges();
            List<string> itemsList = new List<string>();
            string[] items;
            foreach (PendingChange change in changes)
            {
                if (!change.FileName.Equals("Program.cs"))
                {
                    itemsList.Add(change.FileName);
                }
            }
            items = itemsList.ToArray();
            //MessageBox.Show("the new list is");
            foreach (string item in items)
            {
                //MessageBox.Show(item);
            }
            changes = e.Workspace.GetPendingChanges(items, RecursionType.Full);
            foreach (PendingChange change in changes)
            {
                //MessageBox.Show(change.FileName);
            }
            //List<PendingChange> newChanges = new List<PendingChange>();
            //foreach (PendingChange change in changes)
            //{
            //    if (!change.FileName.Equals("Program.cs"))
            //    {
            //        newChanges.Add(change);
            //    }
            //    //MessageBox.Show(PendingChange.GetLocalizedStringForChangeType(change.ChangeType));
            //    //MessageBox.Show("type = " + change.ChangeType + ",\ntype name = " + change.ChangeTypeName + ",\nfilename=" + change.FileName);
            //    //MessageBox.Show(change.DeletionId + "\n" + change.Encoding + "\n" + change.EncodingName + "\n" + change.IsAdd + "\n" +
            //      //              change.IsEdit + "\n" + change.IsMerge + "\n" + change.PendingChangeId);
            //}
            //if (e.PendingChange.FileName.Equals("Program.cs"))
            //{
            //    e.Workspace.EvaluateCheckin(CheckinEvaluationOptions.All, , changes, "comment", c
            //}
            //MessageBox.Show(e.Workspace.Name + ","+ e.Workspace.OwnerName + "," + e.Workspace.Computer);
            //MessageBox.Show("this is the beforeCheckIn");
            //MessageBox.Show(e.PendingChange.FileName);
            //PendingChange[] changes = e.PendingChange.g

            if (mc.ignoreList.Items.Contains(e.PendingChange.FileName))
            {
                //MessageBox.Show("**before checkin**\nDo you want to Check in " + e.PendingChange.FileName);
                
            }
        }

        /// <summary>
        /// Process new pending changes by writing details about them to the console.
        /// </summary>
        /// <param name="sender">Source of the event</param>
        /// <param name="e">Specifics of the pending changes</param>
        public static void OnNewPendingChange(Object sender, PendingChangeEventArgs e)
        {
            //MessageBox.Show("Pending {0} on {1}",
            //                  PendingChange.GetLocalizedStringForChangeType(e.PendingChange.ChangeType));
            //MessageBox.Show("on new pending change");
            //MessageBox.Show("*****  Pending " + PendingChange.GetLocalizedStringForChangeType(e.PendingChange.ChangeType) +
                              //" on " + e.PendingChange.LocalItem);
        }
        public void loadPendingChangesList()
        {
            //MessageBox.Show("it got into the load");
            //MessageBox.Show("k its loaded now");
            List<RegisteredProjectCollection> projectCollections;

            // get all registered project collections (previously connected to from Team Explorer)
            projectCollections = new List<RegisteredProjectCollection>(
                (RegisteredTfsConnections.GetProjectCollections()));
            this.projects = projectCollections;

            // filter down to only those collections that are currently on-line
            var onlineCollections =
                from collection in projectCollections
                where collection.Offline == false
                select collection;
            //MessageBox.Show(onlineCollections.Count().ToString());
            // fail if there are no registered collections that are currently on-line
            if (onlineCollections.Count() < 1)
            {
                Console.Error.WriteLine("Error: There are no on-line registered project collections");
                Environment.Exit(1);
            }
            // find a project collection with at least one team project
            foreach (var registeredProjectCollection in onlineCollections)
            {
                var projectCollection =
                    TfsTeamProjectCollectionFactory.GetTeamProjectCollection(registeredProjectCollection);
                try
                {
                    //MessageBox.Show("finding workspaces");
                    var versionControl = (VersionControlServer)projectCollection.GetService(typeof(VersionControlServer));
                    var teamProjects = new List<TeamProject>(versionControl.GetAllTeamProjects(false));
                    //if there are no team projects in this collection, skip it
                    if (teamProjects.Count < 1) continue;
                    //MessageBox.Show(versionControl.ToString());
                    //MessageBox.Show("the next line registers events");

                    //Workspace test = versionControl.QueryWorkspaces(
                    //MessageBox.Show(teamProjects[0].Name);
                    //PendingChange[] pendingChanges = test.GetPendingChanges();
                    //foreach (PendingChange change in pendingChanges)
                    //{
                    //    MessageBox.Show(change.FileName);
                    //}
                    RegisterEventHandlers(versionControl);

                    // TODO : maybe hook up with a collection load rather than load? that way once you connect to tfs
                    // this will load up. rather than possibly missing the on load stuff
                    Workspace[] workSpaces = versionControl.QueryWorkspaces(null, null, System.Environment.MachineName);
                    //MessageBox.Show("machine name: " + System.Environment.MachineName);
                    this.changesCollection.Clear();
                    foreach (Workspace workspace in workSpaces)
                    {
                        PendingChange[] pendingChanges = workspace.GetPendingChanges();
                        //mc.pendingChangesList.Items.Add(pendingChanges);
                        foreach (PendingChange pendingChange in pendingChanges)
                        {
                            //MessageBox.Show("adding an item");
                            // i have this next thing there to make sure that you dont add the same file twice if we keep this
                            // onload
                            // TODO we are going to have to add something to the list in a different way so that we can
                            // dynamically add the checkboxes as well. in order to get it to look like the VS window
                            if (!mc.pendingChangesList.Items.Contains(pendingChange.FileName))
                            {
                                //mc.pendingChangesList.Items.Add(pendingChange.FileName);
                                this.changesCollection.Add(new changeItem(pendingChange));
                                //mc.pendingChangesList.Items.Add(pendingChange);
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


                finally
                {



                }

                break;
            }
        }
        public void MyToolWindow_Loaded(object sender, RoutedEventArgs e)
        {
            loadPendingChangesList();
           
        }

        private void MyToolWindow_Init(object sender, EventArgs e)
        {
            List<RegisteredProjectCollection> projectCollections;

            // get all registered project collections (previously connected to from Team Explorer)
            projectCollections = new List<RegisteredProjectCollection>(
                (RegisteredTfsConnections.GetProjectCollections()));


            // filter down to only those collections that are currently on-line
            var onlineCollections =
                from collection in projectCollections
                where collection.Offline == false
                select collection;

            // fail if there are no registered collections that are currently on-line
            if (onlineCollections.Count() < 1)
            {
                Console.Error.WriteLine("Error: There are no on-line registered project collections");
                Environment.Exit(1);
            }

            // find a project collection with at least one team project
            foreach (var registeredProjectCollection in onlineCollections)
            {
                var projectCollection =
                    TfsTeamProjectCollectionFactory.GetTeamProjectCollection(registeredProjectCollection);


                try
                {
                    var versionControl = (VersionControlServer)projectCollection.GetService(typeof(VersionControlServer));

                    var teamProjects = new List<TeamProject>(versionControl.GetAllTeamProjects(false));

                    //if there are no team projects in this collection, skip it
                    if (teamProjects.Count < 1) continue;
                    MessageBox.Show(versionControl.ToString());
                    RegisterEventHandlers(versionControl);

                }
                catch(Exception err)
                {
                    MessageBox.Show("it caught something in the init\n"+err);
                }
                finally
                {



                }

                break;
            }
        }

        private void button3_Click(object sender, RoutedEventArgs e)
        {
            object item = this.pendingChangesList.SelectedItem;
            this.ignoreList.Items.Add(item);
            this.pendingChangesList.Items.Remove(item);
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            // ================================================================
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
                    Workspace[] workSpaces = versionControl.QueryWorkspaces(null, null, null);
                    foreach (Workspace workspace in workSpaces)
                    {
                        PendingChange[] pendingChanges = workspace.GetPendingChanges();

                        foreach (PendingChange pendingChange in pendingChanges)
                        {
                            if (!this.ignoreList.Items.Contains(pendingChange.FileName))
                            {
                                myChanges.Add(pendingChange);
                            }
                        }
                    }
                    PendingChange[] arrayChanges = myChanges.ToArray();
                    string message = "These are the files that are to be checked in: \n";
                    foreach (PendingChange change in arrayChanges)
                    {
                        message += change.FileName + "\n";
                    }
                    // TODO when i check this in here it says that these things are not part of my workspace... is it because when we 
                    // run the experimental instance of VS that it isnt in the same place as my files? 
                    // TODO replace all the workspace queries with a query using the machine name and username or let the user pick
                    // through the workspaces if there is more than one using the dropdown but make the default one the machine/username WS
                    MessageBox.Show(message);
                    //MessageBox.Show(System.Environment.MachineName+","+ System.Environment.UserName);
                    Workspace activeWorkspace = versionControl.GetWorkspace(System.Environment.MachineName, System.Environment.UserName);
                    activeWorkspace.GetPendingChanges();
                    activeWorkspace.CheckIn(arrayChanges, "");

                }
                finally { }
                break;
            }
            // ================================================================
            
            

        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            if (ignoreTextBox.Text != null && ignoreTextBox.Text != "")
            {
                ignoreList.Items.Add(ignoreTextBox.Text);
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
                    ignoreList.Items.Add(s);
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