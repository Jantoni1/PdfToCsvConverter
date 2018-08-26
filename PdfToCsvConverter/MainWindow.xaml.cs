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
using System.Collections;

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
            parseDictionary();

            showScriptWindow = false;
        }

        bool showScriptWindow;

        private ObservableCollection<String> pdfList;

        private bool converting = false;

        private XmlDocument doc;

        private XmlElement root;

        private string[] propertyNames = { "script_path", "output_path" };

        private string[] alerts = { "Proszę wskazać gdzie znajduje się \n skrypt do konwersji plików.",
                                    "Proszę wybrać folder docelowy \ndla konwertowanych plików."        };

        private void parseDictionary() {
            if (!System.IO.File.Exists("config.xml")) {
                MessageBoxResult result = MessageBox.Show("Nie odnaleziono pliku konfiguracyjnego config.xml."
                    + "\nProgram zostanie zamknięty.",
                                         "Błąd krytyczny",
                                         MessageBoxButton.OK,
                                         MessageBoxImage.Error);
                Application.Current.Shutdown();
            }
            doc = new XmlDocument();
            doc.Load("config.xml");
            validateConfig();
            outputDirectoryPathLabel.Content = root.SelectSingleNode("output_path").InnerText;
        }

        private void validateConfig() {
            root = doc.DocumentElement;

            foreach (String property in propertyNames) {
                if (root.SelectSingleNode(property).InnerText == null) {
                    root.SetAttribute(property, "");
                }
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


        private bool arePathsCorrect() {
            bool areAllPathsCorrect = true;
            for (int i = 0; i < propertyNames.Length; ++i) {
                string property = propertyNames[i];
                if (!System.IO.File.Exists(root.SelectSingleNode(property).InnerText)
                    && !System.IO.Directory.Exists(root.SelectSingleNode(property).InnerText))
                {
                    MessageBox.Show(alerts[i],
                                    "Błąd",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                    areAllPathsCorrect = false;
                }
            }
            return areAllPathsCorrect;
        }

        private void convertButton_Click(object sender, RoutedEventArgs e)
        {
            if (arePathsCorrect() && pdfList.Count > 0) {
                lockUI();
                string outputPath = (string)outputDirectoryPathLabel.Content;
                new Thread(() =>
                {
                    Thread.CurrentThread.IsBackground = true;
                    convertPDFtoCSV(outputPath);
                }).Start();
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
                    startInfo.WindowStyle =  showScriptWindow ? System.Diagnostics.ProcessWindowStyle.Normal : System.Diagnostics.ProcessWindowStyle.Hidden;
                    startInfo.FileName = "cmd.exe";
                    startInfo.Arguments = "/" + (showScriptWindow ? "k" : "c")
                        + " python \"" + root.SelectSingleNode("script_path").InnerText
                        + "\" \"" + filePath + "\" " + root.SelectSingleNode("output_path").InnerText + " /b";
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

        private void ListBox_Drop(object sender, DragEventArgs e)
        {

            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
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
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            dlg.DefaultExt = ".pdf";
            dlg.Filter = "PDF Files (*.pdf)|*.pdf";
            dlg.Multiselect = true;

            Nullable<bool> result = dlg.ShowDialog();


            if (result == true)
            {
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
                    string directoryPath = dialog.SelectedPath.Replace('\\', '/');
                    outputDirectoryPathLabel.Content = directoryPath;
                    root.SelectSingleNode("output_path").InnerText = directoryPath;
                }
            }
        }

        private void advancedPropertiesButton_Click(object sender, RoutedEventArgs e) {
            var advancedSettingsWindow = new AdvancedSettingsWindow();
            advancedSettingsWindow.checkBox.IsChecked = showScriptWindow;
            advancedSettingsWindow.scriptPathLabel.Content = root.SelectSingleNode("script_path").InnerText;

            advancedSettingsWindow.ShowDialog();
            if (advancedSettingsWindow.DialogResult == true) {
                showScriptWindow = advancedSettingsWindow.checkBox.IsChecked.GetValueOrDefault();
                root.SelectSingleNode("script_path").InnerText = (string)advancedSettingsWindow.scriptPathLabel.Content;
            }
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            doc.Save("config.xml");
            base.OnClosing(e);
        }

    }

}
