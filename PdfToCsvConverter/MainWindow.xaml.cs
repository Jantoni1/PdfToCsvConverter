using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;

namespace PdfToCsvConverter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.DataContext = this;

            InitializeComponent();

            pdfList = new ObservableCollection<String>();

            pdfListBox.ItemsSource = pdfList;
            pdfListBox.SelectionMode = SelectionMode.Extended;
            pdfListBox.SelectionChanged += PdfListBox_SelectionChanged;
            removeButton.Click += removeButton_Click;
            addButton.Click += addButton_Click;
            selectAllButton.Click += selectAllButton_Click;
            browseDirectoryButton.Click += browseDirectoryButton_Click;
            convertButton.Click += convertButton_Click;
            parseDictionary();

        }

        private Dictionary<string, string> properties;

        private void parseDictionary() {
            if (!System.IO.File.Exists("config.xml")) {
                MessageBoxResult result = MessageBox.Show("Nie odnaleziono pliku konfiguracyjnego config.xml."
                    + "\nProgram zostanie zamknięty.",
                                         "Błąd krytyczny",
                                         MessageBoxButton.OK,
                                         MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
            properties = new Dictionary<string, string>();
            XmlDocument doc = new XmlDocument();
            doc.Load("config.xml");
            foreach (XmlNode node in doc.DocumentElement.ChildNodes)
            {
                properties.Add(node.Name, node.InnerText);
            }

            if (!properties.ContainsKey("script_path") || properties["script_path"] == null
                || properties["script_path"] == "" || !System.IO.File.Exists(properties["script_path"])) {
                MessageBoxResult result = MessageBox.Show("Nie odnaleziono poprawnej śeicżki do skryptu \nkonwertującego"
                    + "(script_path) w pliku config.xml. \nProgram zostanie zamknięty.",
                                         "Błąd krytyczny",
                                         MessageBoxButton.OK,
                                         MessageBoxImage.Error);
                    Application.Current.Shutdown();
            }

        }

        private void PdfListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (pdfListBox.SelectedItems.Count == pdfList.Count)
            {
                selectAllButton.Header = "_Odznacz wszystko";
            }
            else {
                selectAllButton.Header = "_Zaznacz wszystko";
            }
        
        }

        private void convertButton_Click(object sender, RoutedEventArgs e)
        {
            if ((string)outputDirectoryPathLabel.Content != "Nie wybrano")
            {
                if (pdfList.Count > 0) {
                    lockUI();
                    string outputPath = (string)outputDirectoryPathLabel.Content;
                    new Thread(() =>
                    {
                        Thread.CurrentThread.IsBackground = true;
                        convertPDFtoCSV(outputPath);
                    }).Start();
                }

            }
            else {
                MessageBox.Show("Proszę wybrać folder docelowy \ndla konwertowanych plików.",
                                    "Błąd",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
            }
        }

        private void lockUI() {
            browseDirectoryButton.IsEnabled = false;
            addButton.IsEnabled = false;
            removeButton.IsEnabled = false;
            selectAllButton.IsEnabled = false;
            pdfListBox.IsEnabled = false;
            convertButton.Content = "Zatrzymaj";
            convertButton.Click -= convertButton_Click;
            convertButton.Click += cancelButton_Click;
        }

        private void unlockUI() {
            browseDirectoryButton.IsEnabled = true;
            addButton.IsEnabled = true;
            removeButton.IsEnabled = true;
            selectAllButton.IsEnabled = true;
            pdfListBox.IsEnabled = true;
            convertButton.Content = "Konwertuj";
            convertButton.Click += convertButton_Click;
            convertButton.Click -= cancelButton_Click;
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e) {
            converting = false;
        }

        private void convertPDFtoCSV(string outputPath) {
            if (converting == false)
            {
                converting = true;
                while (pdfList.Count > 0 && converting == true)
                {
                    string filePath = (string)pdfList[0];
                    Dispatcher.BeginInvoke(new Action(delegate ()
                    {
                        pdfList.Remove(filePath);
                    }));

                    System.Diagnostics.Process process = new System.Diagnostics.Process();
                    System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                    startInfo.WindowStyle = System.Diagnostics.ProcessWindowStyle.Hidden;
                    startInfo.FileName = "cmd.exe";
                    startInfo.Arguments = "/c python \"" + properties["script_path"] + "\" \"" + filePath + "\" " + outputPath + " /b";
                    process.StartInfo = startInfo;
                    process.Start();
                    process.WaitForExit();
                }
                Dispatcher.BeginInvoke(new Action(delegate ()
                {
                    converting = false;
                    unlockUI();
                    if (pdfList.Count == 0)
                    {
                        selectAllButton.Header = "_Zaznacz wszystko";
                    }
                }));
            }
        }

        private ObservableCollection<String> pdfList;
        private bool converting = false;

        private void ListBox_Drop(object sender, DragEventArgs e)
        {

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                //// Note that you can have more than one file.
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

                foreach(string fileName in files) {
                    if (!pdfList.Contains(fileName))
                    {
                        pdfList.Add(fileName);
                    }
                    else
                    {
                        selectAllButton.Header = "_Zaznacz wszystko";
                        pdfListBox.SelectedItems.Add(fileName);
                    }
                }
            }
        }

        private void removeButton_Click(object sender, RoutedEventArgs e)
        {
            while (pdfListBox.SelectedItems.Count > 0) {
                pdfList.Remove((string)pdfListBox.SelectedItems[0]);
            }
            selectAllButton.Header = "_Zaznacz wszystko";

        }

        private void addButton_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();



            //// Set filter for file extension and default file extension 
            dlg.DefaultExt = ".pdf";
            dlg.Filter = "PDF Files (*.pdf)|*.pdf";
            dlg.Multiselect = true;

            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();


            //// Get the selected file names and display in a TextBox 
            if (result == true)
            {
                // Open document 
                foreach (String fileName in dlg.FileNames)
                {
                    if (!pdfList.Contains(fileName))
                    {
                        pdfList.Add(fileName);
                    }
                    else {
                        selectAllButton.Header = "_Zaznacz wszystko";
                        pdfListBox.SelectedItems.Add(fileName);
                    }
                }
            }
        }

        private void selectAllButton_Click(object sender, RoutedEventArgs e)
        {
            if ((string)selectAllButton.Header == "_Zaznacz wszystko")
            {
                pdfListBox.SelectAll();
            }
            else {
                pdfListBox.SelectedItems.Clear();
            }
        }

        private void browseDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK) {
                    outputDirectoryPathLabel.Content = dialog.SelectedPath;
                }
            }
        }
    }

}
