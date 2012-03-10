using System;
using System.Collections.Generic;
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
       public static MyControl mc;

        public MyControl()
        {
            InitializeComponent();
            mc = this;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1300:SpecifyMessageBoxOptions")]

        private void ignoreListAddButton_Click(object sender, RoutedEventArgs e)
        {
            if (ignoreListTextBox.Text != "")
            {
                string file;
                ignoreList.Items.Add(ignoreListTextBox.Text);
                file = ignoreListTextBox.Text;
                ignoreListTextBox.Clear();
                MessageBox.Show("\"" + file + "\"" + " was added to the ignore list", "File Added");
            }
            else
            {
                MessageBox.Show("You must enter something in the textbox.", "Nothing Added");
            }

        }
        public static void RegisterEventHandlers(VersionControlServer versionControl)
        {            
            
            versionControl.BeforeCheckinPendingChange += OnBeforeCheckinPendingChange;            
            versionControl.NewPendingChange += OnNewPendingChange;
        }

        public static void OnBeforeCheckinPendingChange(Object sender, ProcessingChangeEventArgs e)
        {
            //mc.beforeCheckIn(e);
            if (mc.ignoreList.Items.Contains(e.PendingChange.FileName))
            {
                MessageBox.Show("Do you want to Check in " + e.PendingChange.FileName);
            }
            else
            {
                MessageBox.Show("The file is not in the ignore list. " + e.PendingChange.FileName);
            }
            //MessageBox.Show("Checking in " + e.PendingChange.FileName);      
        }
        public void beforeCheckIn(ProcessingChangeEventArgs e)
        {
            MessageBox.Show(ignoreList.Items.Count.ToString());
            if (ignoreList.Items.Contains(e.PendingChange.FileName))
            {
                MessageBox.Show("Do you want to Check in " + e.PendingChange.FileName);
            }
        }

        /// <summary>
        /// Process new pending changes by writing details about them to the console.
        /// </summary>
        /// <param name="sender">Source of the event</param>
        /// <param name="e">Specifics of the pending changes</param>
        public static void OnNewPendingChange(Object sender, PendingChangeEventArgs e)
        {
            MessageBox.Show("Pending {0} on {1}",
                              PendingChange.GetLocalizedStringForChangeType(e.PendingChange.ChangeType));
        }

        public void MyToolWindow_Loaded(object sender, RoutedEventArgs e)
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

                    RegisterEventHandlers(versionControl);  

                }
                finally
                {
                    

                    
                }

                break;
            }
        }
    }
}