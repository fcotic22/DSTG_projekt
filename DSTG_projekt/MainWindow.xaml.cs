using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Text.RegularExpressions;
using System.Linq;
using System.Collections.Generic;

namespace DSTG_projekt
{
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

        public List<LoopInfo> forLoops = new List<LoopInfo>();
        public List<LoopInfo> whileLoops = new List<LoopInfo>();
        public List<LoopInfo> foreachLoops = new List<LoopInfo>();
        public List<LoopInfo> doWhileLoops = new List<LoopInfo>();
        public List<LoopInfo> forInLoops = new List<LoopInfo>();
        public List<LoopInfo> forOfLoops = new List<LoopInfo>();

        public List<LoopVariableUsage> loopVariableResults = new List<LoopVariableUsage>();

        private void btnAnalyze_Click(object sender, RoutedEventArgs e)
        {
            FillDictionaryAndLoopTypes();

            var chosenLang = cmbLanguage.SelectedValue.ToString();

            if (txtPath.Text == string.Empty || cmbLanguage.SelectedIndex == -1)
            {
                MessageBox.Show("Učitaj datoteku i odaberi jezik!");
                return;

            }
            else if (langExtPairs[cmbLanguage.SelectedValue?.ToString() ?? ""] != Path.GetExtension(Filepath ?? ""))
            {
                MessageBox.Show($"Odabrani jezik i tip datoteke se ne poklapaju! {langExtPairs[chosenLang ?? ""]} - {Path.GetExtension(Filepath ?? "")}");
                return;
            }
            else
            {
                forLoops.Clear();
                whileLoops.Clear();
                foreachLoops.Clear();
                doWhileLoops.Clear();
                forInLoops.Clear();
                forOfLoops.Clear();
                loopVariableResults.Clear();
                 
                if (Filepath == null)
                {
                    MessageBox.Show("Datoteka nije odabrana!");
                    return;
                }
                string code = File.ReadAllText(Filepath);

                if (chosenLang == "Python")
                {
                    var allLoops = ExtractPythonLoops(code);
                    CategorizePythonLoops(allLoops);
                    AnalyzeAllLoops(chosenLang);
                    CreateGraphRepresentation();
                }
                else if (chosenLang == "C#")
                {
                }
                else
                {
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

        public class LoopInfo
        {
            public string Type { get; set; } = string.Empty; 
            public string FullText { get; set; } = string.Empty; 
            public int StartLine { get; set; }
            public int EndLine { get; set; }
            public string LoopHeader { get; set; } = string.Empty; 
            public string LoopBody { get; set; } = string.Empty; 
        }

        public class LoopVariableUsage
        {
            public string LoopId { get; set; } = string.Empty; 
            public string LoopType { get; set; } = string.Empty; 
            public List<string> Variables { get; set; } = new List<string>(); 
        }
        private List<LoopInfo> ExtractPythonLoops(string code)
        {
            var loops = new List<LoopInfo>();
            var lines = code.Split('\n');
            var loopStack = new Stack<(int startLine, string type, int indent)>();

            for (int i = 0; i < lines.Length; i++)
            {
                string originalLine = lines[i];
                string trimmedLine = originalLine.Trim();
                int indent = originalLine.TakeWhile(c => c == ' ' || c == '\t').Count();

                if (string.IsNullOrWhiteSpace(trimmedLine) || trimmedLine.StartsWith("#")) continue;
                
                if (Regex.IsMatch(trimmedLine, @"^for\s+"))
                {
                    loopStack.Push((i, "for", indent));
                }
                else if (Regex.IsMatch(trimmedLine, @"^while\s+"))
                {
                    loopStack.Push((i, "while", indent));
                }
                else if (loopStack.Count > 0)
                {
                    while (loopStack.Count > 0)
                    {
                        var (startLine, type, startIndent) = loopStack.Peek();

                        if (indent <= startIndent)
                        {
                            var loop = loopStack.Pop();
                            var loopBody = string.Join("\n", lines.Skip(loop.startLine + 1).Take(i - loop.startLine - 1));
                            loops.Add(new LoopInfo
                            {
                                Type = loop.type,
                                StartLine = loop.startLine + 1,
                                EndLine = i + 1,
                                LoopHeader = lines[loop.startLine].Trim(),
                                LoopBody = loopBody,
                                FullText = string.Join("\n", lines.Skip(loop.startLine).Take(i - loop.startLine + 1))
                            });
                        }
                        else
                        {
                            break; 
                        }
                    }
                }
            }
            while (loopStack.Count > 0)
            {
                var loop = loopStack.Pop();
                var loopBody = string.Join("\n", lines.Skip(loop.startLine + 1));
                loops.Add(new LoopInfo
                {
                    Type = loop.type,
                    StartLine = loop.startLine + 1,
                    EndLine = lines.Length,
                    LoopHeader = lines[loop.startLine].Trim(),
                    LoopBody = loopBody,
                    FullText = string.Join("\n", lines.Skip(loop.startLine))
                });
            }

            return loops;
        }

        private void CategorizePythonLoops(List<LoopInfo> loops)
        {
            foreach (var loop in loops)
            {
                if (loop.Type == "for")
                    forLoops.Add(loop);
                else if (loop.Type == "while")
                    whileLoops.Add(loop);
            }
        }

       private List<string> AnalyzeLoopVariables(LoopInfo loop, string language)
        {
            var variables = new HashSet<string>();
            string codeToAnalyze = loop.LoopBody + " " + loop.LoopHeader;

            if (language == "Python")
            {
                return AnalyzePythonLoopVariables(loop);
            }
            else if (language == "C#")
            {
                return null;//AnalyzeCSharpLoopVariables(loop);
            }
            else 
            {
                return null;//AnalyzeJavaScriptLoopVariables(loop);
            }
        }
        
        private string RemoveStringsAndCommentsPython(string code)
        {
            string result = code;

            result = Regex.Replace(result, @"#.*", "");

            result = Regex.Replace(result, @"f""[^""]*""", "");
            result = Regex.Replace(result, @"f'[^']*'", "");
            result = Regex.Replace(result, @"""[^""]*""", "");
            result = Regex.Replace(result, @"'[^']*'", "");

            return result;
        }
        private string RemoveNestedLoopsFromBody(string body, int parentIndent)
        {
            if (string.IsNullOrWhiteSpace(body))
                return body;

            var lines = body.Split('\n');
            var result = new List<string>();

            int bodyIndent = -1;
            foreach (var line in lines)

            {
                string trimmed = line.Trim();
                if (!string.IsNullOrWhiteSpace(trimmed) && !trimmed.StartsWith("#"))
                {
                    int indent = line.TakeWhile(c => c == ' ' || c == '\t').Count();
                    if (indent > parentIndent)
                    {
                        bodyIndent = indent;
                        break;
                    }
                }
            }

            if (bodyIndent == -1)
                return "";

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                int indent = line.TakeWhile(c => c == ' ' || c == '\t').Count();
                string trimmed = line.Trim();

                if (string.IsNullOrWhiteSpace(trimmed) || trimmed.StartsWith("#"))
                {
                    continue;
                }

                if (indent == bodyIndent && (Regex.IsMatch(trimmed, @"^for\s+") || Regex.IsMatch(trimmed, @"^while\s+")))
                {
                    i++; 

                    while (i < lines.Length)
                    {
                        int currentIndent = lines[i].TakeWhile(c => c == ' ' || c == '\t').Count();
                        string currentTrimmed = lines[i].Trim();

                        if (currentIndent <= bodyIndent && !string.IsNullOrWhiteSpace(currentTrimmed))
                        {
                            i--; 
                            break;
                        }
                        i++;
                    }
                    continue;
                }

                if (indent >= bodyIndent)
                {
                    result.Add(line);
                }
            }

            return string.Join("\n", result);
        }

        private List<string> AnalyzePythonLoopVariables(LoopInfo loop)
        {
            var variables = new HashSet<string>();

            int headerIndent = loop.LoopHeader.TakeWhile(c => c == ' ' || c == '\t').Count();
            string bodyWithoutNested = RemoveNestedLoopsFromBody(loop.LoopBody, headerIndent);

            string cleanHeader = RemoveStringsAndCommentsPython(loop.LoopHeader);
            string cleanBody = RemoveStringsAndCommentsPython(bodyWithoutNested);

            if (loop.Type == "for")
            {
                var headerMatch = Regex.Match(cleanHeader, @"for\s+(\w+)\s+in");
                if (headerMatch.Success)
                {
                    variables.Add(headerMatch.Groups[1].Value);
                }
            }
            else if (loop.Type == "while")
            {
                var conditionMatch = Regex.Match(cleanHeader, @"while\s+(.+)");
                if (conditionMatch.Success)
                {
                    var conditionVars = Regex.Matches(conditionMatch.Groups[1].Value, @"\b([a-zA-Z_][a-zA-Z0-9_]*)\b");
                    foreach (Match match in conditionVars)
                    {
                        string varName = match.Groups[1].Value;
                        if (!IsPythonKeyword(varName) && !IsPythonBuiltinFunction(varName) && !char.IsDigit(varName[0]))
                        {
                            variables.Add(varName);
                        }
                    }
                }
            }
            var lines = cleanBody.Split('\n');
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                string trimmedLine = line.Trim();

                var assignmentMatch = Regex.Match(trimmedLine, @"^([a-zA-Z_][a-zA-Z0-9_]*)\s*=");
                if (assignmentMatch.Success)
                {
                    string varName = assignmentMatch.Groups[1].Value;
                    if (!IsPythonKeyword(varName) && !IsPythonBuiltinFunction(varName))
                    {
                        variables.Add(varName);
                    }
                }

                var allVarMatches = Regex.Matches(trimmedLine, @"\b([a-zA-Z_][a-zA-Z0-9_]*)\b");
                foreach (Match match in allVarMatches)
                {
                    string varName = match.Groups[1].Value;

                    if (IsPythonKeyword(varName) || IsPythonBuiltinFunction(varName) || IsPythonType(varName))
                        continue;

                    int matchIndex = match.Index;
                    bool isFunctionCall = false;
                    if (matchIndex + match.Length < trimmedLine.Length)
                    {
                        string afterMatch = trimmedLine.Substring(matchIndex + match.Length).TrimStart();
                        if (afterMatch.StartsWith("("))
                        {
                            isFunctionCall = true;
                        }
                    }

                    if (!isFunctionCall)
                    {
                        variables.Add(varName);
                    }
                }
            }

            return variables.OrderBy(v => v).ToList();
        }

        private bool IsPythonBuiltinFunction(string word)
        {
            string[] builtins = { "print", "range", "len", "str", "int", "float", "bool", "list", "dict", "set", "tuple", "max", "min", "sum", "abs", "round" };
            return builtins.Contains(word);
        }

      

        private bool IsPythonKeyword(string word)
        {
            string[] keywords = { "if", "else", "elif", "for", "while", "def", "class", "import", "from", "return", "print", "True", "False", "None", "and", "or", "not", "in", "is", "pass", "break", "continue" };
            return keywords.Contains(word);
        }

        private bool IsPythonType(string word)
        {
            if (char.IsUpper(word[0]) && word.Length > 1)
            {
                string[] commonTypes = { "Match", "Regex", "List", "Dict", "Set", "Tuple", "LoopInfo", "LoopVariableUsage" };
                if (commonTypes.Contains(word))
                    return true;
            }
            return false;
        }

        
        private void AnalyzeAllLoops(string language)
        {
            var allLoops = new List<LoopInfo>();
            allLoops.AddRange(forLoops);
            allLoops.AddRange(whileLoops);
            allLoops.AddRange(foreachLoops);
            allLoops.AddRange(doWhileLoops);
            allLoops.AddRange(forInLoops);
            allLoops.AddRange(forOfLoops);
            allLoops = allLoops.OrderBy(l => l.StartLine).ToList();

            int loopCounter = 1;
            foreach (var loop in allLoops)
            {
                var variables = AnalyzeLoopVariables(loop, language);
                loopVariableResults.Add(new LoopVariableUsage
                {
                    LoopId = $"petlja{loopCounter}",
                    LoopType = loop.Type, 
                    Variables = variables
                });
                loopCounter++;
            }
        }
        private void CreateGraphRepresentation()
        {
            string graphText = "Graf korištenja varijabli u petljama:\n\n";

            if (loopVariableResults.Count == 0)
            {
                graphText += "Nisu pronađene petlje u kodu.";
            }
            else
            {
                foreach (var result in loopVariableResults)
                {
                    string loopTypeDisplay = result.LoopType;
                    if (loopTypeDisplay == "for in")
                        loopTypeDisplay = "for-in";
                    else if (loopTypeDisplay == "for of")
                        loopTypeDisplay = "for-of";
                    else if (loopTypeDisplay == "do-while")
                        loopTypeDisplay = "do-while";

                    if (result.Variables.Count > 0)
                    {
                        string variablesStr = string.Join(" ", result.Variables);
                        graphText += $"{result.LoopId} ({loopTypeDisplay}) ({variablesStr})\n";
                    }
                    else
                    {
                        graphText += $"{result.LoopId} ({loopTypeDisplay}) (nema varijabli)\n";
                    }
                }
            }
            MessageBox.Show(graphText, "Rezultati analize petlji", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}