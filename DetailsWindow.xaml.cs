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
        public DetailsWindow()
        {
            InitializeComponent();
        }
        public DetailsWindow(ObservableCollection<Shelveset> shelveSetCollection)
        {
            InitializeComponent();
            this.DataContext = this;
            shelveCollection = shelveSetCollection;
        }

        private ObservableCollection<Shelveset> _shelveCollection;
        public ObservableCollection<Shelveset> shelveCollection { get { return _shelveCollection; } set { _shelveCollection = value; } }

    }
}
