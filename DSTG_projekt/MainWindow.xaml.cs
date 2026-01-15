    using DSTG_projekt.Models;
    using System.IO;
    using System.Windows;
    using System.Windows.Controls;
    using System.Text;
    using DSTG_projekt.Services;


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

            public List<string> pythonLoops = new List<string>();
            public List<string> cSharpLoops = new List<string>();
            public List<string> javaScriptLoops = new List<string>();

        private void btnAnalyze_Click(object sender, RoutedEventArgs e)
            {
                FillDictionaryAndLoopTypes();

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


                        var graph = CSLoopsAsGraphService.GetGraph(Filepath!);

                        MessageBox.Show(graph.ToString(), "Graf petlja–varijabla");
                    }
                    else
                    {
                        //JavaScript


                    }
                }
               
            }



        private LoopVariableGraph AnalyzeCSharpLoopsAsGraph(string path)
        {
            var lines = File.ReadAllLines(path);
            var graph = new LoopVariableGraph();

            int loopId = 1;

            for (int i = 0; i < lines.Length; i++)
            {
                if (IsCSharpLoop(lines[i], out string loopType))
                {
                    var body = ExtractLoopBody(lines, ref i);
                    var usedVariables = AnalyzeLoopVariableUsage(body);

                    graph.Loops.Add(loopId, new LoopNode
                    {
                        Id = loopId,
                        LoopType = loopType,
                        Variables = usedVariables
                    });

                    loopId++;
                }
            }

            return graph;
        }

        private bool IsCSharpLoop(string line, out string loopType)
        {
            loopType = "";
            line = line.Trim();

            if (line.StartsWith("for"))
                loopType = "for";
            else if (line.StartsWith("foreach"))
                loopType = "foreach";
            else if (line.StartsWith("while"))
                loopType = "while";
            else if (line.StartsWith("do"))
                loopType = "do";

            return loopType != "";
        }

        private HashSet<string> AnalyzeLoopVariableUsage(List<string> loopBody)
        {
            var variables = new HashSet<string>();

            foreach (var line in loopBody)
            {
                foreach (var token in Tokenize(line))
                {
                    if (IsVariable(token))
                        variables.Add(token);
                }
            }

            return variables;
        }

        private IEnumerable<string> Tokenize(string line)
        {
            char[] sep = { ' ', '(', ')', '{', '}', ';', '=', '+', '-', '*', '/', '<', '>' };
            return line.Split(sep, StringSplitOptions.RemoveEmptyEntries);
        }

        private bool IsVariable(string token)
        {
            string[] keywords =
            {
                "for","foreach","while","do","if","else",
                "int","double","float","var","string","bool",
                "return","new"
            };

            return char.IsLetter(token[0]) && !keywords.Contains(token);
        }


        private List<string> ExtractLoopBody(string[] lines, ref int index)
        {
            var body = new List<string>();

            int braceCount = 0;
            bool insideLoop = false;

            for (; index < lines.Length; index++)
            {
                string line = lines[index];

                // Početak bloka
                if (line.Contains("{"))
                {
                    braceCount++;
                    insideLoop = true;
                }

                if (insideLoop)
                    body.Add(line);

                // Kraj bloka
                if (line.Contains("}"))
                {
                    braceCount--;
                    if (braceCount == 0)
                        break;
                }
            }

            return body;
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