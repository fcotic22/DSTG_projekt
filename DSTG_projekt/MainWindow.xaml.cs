<<<<<<< HEAD
﻿using System.Diagnostics;
using System.IO;
=======
﻿    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
>>>>>>> d_python
    using System.Windows;
    using System.Windows.Controls;
    using DSTG_projekt.Services;

namespace DSTG_projekt
    {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public class LoopNode
    {
        public int Id { get; set; }
        public string LoopType { get; set; } = "";
        public HashSet<string> Variables { get; set; } = new();
    }


    public class LoopVariableGraph
    {
        public Dictionary<int, LoopNode> Loops { get; set; } = new();
    }

    public partial class MainWindow : Window
        {
            public MainWindow()
            {
                InitializeComponent();
            }

            private string? Filepath { get; set; }
        private CSharpLoopAnalyzer cSharp { get; set; }

        public Dictionary<string, string> langExtPairs = new Dictionary<string, string>();

        public List<string> pythonLoops = new List<string>();
        public List<string> cSharpLoops = new List<string>();
        public List<string> javaScriptLoops = new List<string>();

        private void btnAnalyze_Click(object sender, RoutedEventArgs e)
        {
            FillDictionaryAndLoopTypes();

            string code = File.ReadAllText(Filepath);

            if (txtPath.Text == string.Empty || cmbLanguage.SelectedIndex == -1)
            {
                MessageBox.Show("Učitaj datoteku i odaberi jezik!");
                return;
            
<<<<<<< HEAD
=======
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
                        StringBuilder sb = PythonService.AnalyzePythonCode(Filepath!);

                        MessageBox.Show(sb.ToString(), "Python graf petlja–varijabla");
                    }
                    else if (chosenLang == "C#")
                    {

                    }
                    else
                    {
                        //JavaScript


                    }
                }
               
>>>>>>> d_python
            }
            else if(langExtPairs[cmbLanguage.SelectedValue.ToString()] != Path.GetExtension(Filepath))
            {
                var chosenLang = cmbLanguage.SelectedValue.ToString();
                MessageBox.Show($"Odabrani jezik i tip datoteke se ne poklapaju! {langExtPairs[chosenLang]} - {Path.GetExtension(Filepath)}");
                return;
            }
            else
            {
                var chosenLang = cmbLanguage.SelectedValue.ToString();
            if (chosenLang == "Python")
            {
                    
            }
            else if (chosenLang == "C#")
            {
                cSharp = new CSharpLoopAnalyzer();
                var loops = cSharp.ExtractCSharpLoops(code);
                cSharp.CategorizeCSharpLoops(loops);
                cSharp.AnalyzeAllCSharpLoops(chosenLang);
                cSharp.helperFunctions.CreateMessageBoxLoopsInfo();
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

            private void FillDictionaryAndLoopTypes()
            {
                langExtPairs.Clear();
                langExtPairs.Add("Python", ".py");
                langExtPairs.Add("C#", ".cs");
                langExtPairs.Add("JavaScript", ".js");

                pythonLoops.AddRange(new string[] { "for", "while" });
                cSharpLoops.AddRange(new string[] { "for(", "foreach(", "while(", "do" });
                javaScriptLoops.AddRange(new string[] { "for(", "for in", "for of", "while(", "do" });
        }
    }
    }