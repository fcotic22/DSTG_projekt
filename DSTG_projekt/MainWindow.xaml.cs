using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace DSTG_projekt
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private string? Filepath { get; set; }

        public Dictionary<string, string> langExtPairs = new Dictionary<string, string>();

        private void btnAnalyze_Click(object sender, RoutedEventArgs e)
        {
            FillDictionary();

            var chosenLang = cmbLanguage.SelectedValue.ToString();

            if (txtPath.Text == string.Empty || cmbLanguage.SelectedIndex == -1)
            {
                MessageBox.Show("Učitaj datoteku i odaberi jezik!");
                return;
            
            }
            else if(langExtPairs[cmbLanguage.SelectedValue.ToString()] != Path.GetExtension(Filepath))
            {
                MessageBox.Show($"Odabrani jezik i tip datoteke se ne poklapaju! {langExtPairs[chosenLang]} - {Path.GetExtension(Filepath)}");
                return;
            }
            else
            {
                if (chosenLang == "Python")
                {

                }
                else if (chosenLang == "C#")
                {

                }
                else
                {
                    //JavaScript


                }
            }
               
        }

        private void btnSelectDocument_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Filter = "Sve datoteke|*.*";

            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                Filepath = dialog.FileName;

                txtPath.Text += Path.GetFileName(Filepath);
                txtPath.Visibility = Visibility.Visible;
                btnRemoveFile.Visibility = Visibility.Visible;

                btnSelectDocument.IsEnabled = false;
            }
        }

        private void btnRemoveFile_Click(object sender, RoutedEventArgs e)
        {
            txtPath.Text = string.Empty;
            txtPath.Visibility = Visibility.Hidden;
            btnRemoveFile.Visibility = Visibility.Hidden;
            btnSelectDocument.IsEnabled = true;
            Filepath = null;
        }

        private void FillDictionary()
        {
            langExtPairs.Clear();
            langExtPairs.Add("Python", ".py");
            langExtPairs.Add("C#", ".cs");
            langExtPairs.Add("JavaScript", ".js");
        }
    }
}