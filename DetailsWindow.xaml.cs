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
        public DetailsWindow(Shelveset selectedShelveSet)
        {
            InitializeComponent();
            this.DataContext = this;
            shelveSet = selectedShelveSet;


            shelvesetName.Text = shelveSet.Name;
            owner.Text = shelveSet.OwnerName;
            date.Text = shelveSet.CreationDate.ToString();
            comment.Text = shelveSet.Comment;

        }

        private Shelveset _shelveSet;
        public Shelveset shelveSet { get { return _shelveSet; } set { _shelveSet = value; } }




    }
}
