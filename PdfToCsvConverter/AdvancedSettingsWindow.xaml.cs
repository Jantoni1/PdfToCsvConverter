using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace PdfToCsvConverter
{
    /// <summary>
    /// Interaction logic for AdvancedSettingsWindow.xaml
    /// </summary>
    public partial class AdvancedSettingsWindow : Window
    {
        public AdvancedSettingsWindow()
        {
            InitializeComponent();
        }


        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = true;
        }

        private void scriptPathBrowseButton_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            dlg.DefaultExt = ".py";
            dlg.Filter = "Python files (*.py)|*.py";

            Nullable<bool> result = dlg.ShowDialog();

            if (result == true)
            {
                scriptPathLabel.Content = dlg.FileName;
            }
        }


    }
}
