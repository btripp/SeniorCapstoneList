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
using System.Windows.Shapes;

namespace AugustaStateUniversity.SeniorCapstoneIgnoreList
{
    /// <summary>
    /// Interaction logic for ShelveWindow.xaml
    /// </summary>
    public partial class ShelveWindow : Window
    {
        public ShelveWindow()
        {
            InitializeComponent();
        }

        private void cancelShelve_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
