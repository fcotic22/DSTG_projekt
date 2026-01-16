using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DSTG_projekt
{
    public class CSharpLoopAnalyzer
    {
        public List<LoopInfo> forLoops = new List<LoopInfo>();
        public List<LoopInfo> whileLoops = new List<LoopInfo>();
        public List<LoopInfo> foreachLoops = new List<LoopInfo>();
        public List<LoopInfo> doWhileLoops = new List<LoopInfo>();
        public List<LoopInfo> forInLoops = new List<LoopInfo>();
        public List<LoopInfo> forOfLoops = new List<LoopInfo>();

        public HelperFunctions helperFunctions;

        public List<LoopInfo> ExtractCSharpLoops(string code)
        {
            helperFunctions = new HelperFunctions();
            var loops = new List<LoopInfo>();
            var lines = code.Split('\n');

            var forPattern = @"for\s*\([^)]*\)";
            var foreachPattern = @"foreach\s*\([^)]*\)";
            var whilePattern = @"while\s*\([^)]*\)";
            var doPattern = @"do\s*\{";

            for (int i =0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmedLine = line.Trim();

                if (Regex.IsMatch(trimmedLine, forPattern))
                {
                    var loop = helperFunctions.ExtractBracketedLoop(lines, i, "for");
                    if (loop != null) loops.Add(loop);
                }
                else if (Regex.IsMatch(trimmedLine, foreachPattern))
                {
                    var loop = helperFunctions.ExtractBracketedLoop(lines, i, "foreach");
                    if (loop != null) loops.Add(loop);
                }
                else if (Regex.IsMatch(trimmedLine, whilePattern))
                {
                    var loop = helperFunctions.ExtractBracketedLoop(lines, i, "while");
                    if (loop != null) loops.Add(loop);
                }
                else if (Regex.IsMatch(trimmedLine, doPattern))
                {
                    var loop = helperFunctions.ExtractDoWhileLoop(lines, i);
                    if (loop != null) loops.Add(loop);
                }
            }

            return loops;
        }

        public void CategorizeCSharpLoops(List<LoopInfo> loops)
        {
            foreach (var loop in loops)
            {
                if (loop.Type == "for")
                    forLoops.Add(loop);
                else if (loop.Type == "while")
                    whileLoops.Add(loop);
                else if (loop.Type == "foreach")
                    foreachLoops.Add(loop);
                else if (loop.Type == "do-while")
                    doWhileLoops.Add(loop);
            }
        }

        public void AnalyzeAllCSharpLoops(string language)
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
                var variables = AnalyzeCSharpLoopVariables(loop);
                helperFunctions.loopVariableResults.Add(new LoopVariableUsage
                {
                    LoopId = $"petlja{loopCounter}",
                    LoopType = loop.Type, 
                    Variables = variables
                });
                loopCounter++;
            }

            if (helperFunctions != null)
                helperFunctions.CreateMessageBoxLoopsInfo();
        }


        public List<string> AnalyzeCSharpLoopVariables(LoopInfo loop)
        {
            var variables = new HashSet<string>();

            string cleanHeader = RemoveStringsAndCommentsCSharp(loop.LoopHeader);
            string cleanBody = RemoveStringsAndCommentsCSharp(loop.LoopBody);

            string bodyWithoutNested = RemoveNestedLoopsFromCSharpBody(cleanBody);
            cleanBody = bodyWithoutNested;

            if (loop.Type == "for")
            {
                var forHeaderMatch = Regex.Match(cleanHeader, @"for\s*\(([^)]+)\)");
                if (forHeaderMatch.Success)
                {
                    var headerVars = Regex.Matches(forHeaderMatch.Groups[1].Value, @"\b([a-zA-Z_][a-zA-Z0-9_]*)\b");
                    foreach (Match match in headerVars)
                    {
                        string varName = match.Groups[1].Value;
                        int absIndex = forHeaderMatch.Groups[1].Index + match.Index;
                        char charBefore = '\0';
                        char charAfter = '\0';
                        GetPrevNextNonWhitespace(cleanHeader, absIndex, varName.Length, out charBefore, out charAfter);
                        if (charBefore == '.') continue;
                        if (charAfter == '.') { variables.Add(varName); continue; }
                        if (!IsCSharpKeyword(varName) && !IsCSharpBuiltinFunction(varName) && !char.IsDigit(varName[0]))
                        {
                            variables.Add(varName);
                        }
                    }
                }
            }
            else if (loop.Type == "foreach")
            {
                var foreachMatch = Regex.Match(cleanHeader, @"foreach\s*\(\s*(?:[\w<>,\s]+\s+)?([a-zA-Z_][a-zA-Z0-9_]*)\s+in");
                if (foreachMatch.Success)
                {
                    var varName = foreachMatch.Groups[1].Value;
                    if (!IsCSharpKeyword(varName) && !IsCSharpBuiltinFunction(varName))
                        variables.Add(varName);
                }
            }
            else if (loop.Type == "while")
            {
                var conditionMatch = Regex.Match(cleanHeader, @"while\s*\(([^)]+)\)");
                if (conditionMatch.Success)
                {
                    var conditionVars = Regex.Matches(conditionMatch.Groups[1].Value, @"\b([a-zA-Z_][a-zA-Z0-9_]*)\b");
                    foreach (Match match in conditionVars)
                    {
                        string varName = match.Groups[1].Value;
                        int absIndex = conditionMatch.Groups[1].Index + match.Index;
                        char charBefore = '\0';
                        char charAfter = '\0';
                        GetPrevNextNonWhitespace(cleanHeader, absIndex, varName.Length, out charBefore, out charAfter);
                        if (charBefore == '.') continue;
                        if (charAfter == '.') { variables.Add(varName); continue; }
                        if (!IsCSharpKeyword(varName) && !IsCSharpBuiltinFunction(varName) && !char.IsDigit(varName[0]))
                        {
                            variables.Add(varName);
                        }
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(cleanHeader))
            {
                var headerParens = Regex.Matches(cleanHeader, @"\(([^)]*)\)");
                foreach (Match pm in headerParens)
                {
                    var content = pm.Groups[1].Value;
                    var argVars = Regex.Matches(content, @"\b([a-zA-Z_][a-zA-Z0-9_]*)\b");
                    foreach (Match m in argVars)
                    {
                        string name = m.Groups[1].Value;
                        int absIndex = pm.Groups[1].Index + m.Index;
                        char charBefore = '\0';
                        char charAfter = '\0';
                        GetPrevNextNonWhitespace(cleanHeader, absIndex, name.Length, out charBefore, out charAfter);
                        if (charBefore == '.') continue;
                        if (charAfter == '.') { variables.Add(name); continue; }
                        if (!IsCSharpKeyword(name) && !IsCSharpBuiltinFunction(name) && !IsCSharpType(name) && !char.IsDigit(name[0]))
                            variables.Add(name);
                    }
                }
            }

            var lines = cleanBody.Split('\n');
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                string trimmedLine = line.Trim();

                var assignmentMatch = Regex.Match(trimmedLine, @"(?:int|string|bool|var|double|float|long|short|byte|char|decimal|List|Dictionary|Array|IEnumerable)\s+([a-zA-Z_][a-zA-Z0-9_]*)\s*=");
                if (assignmentMatch.Success)
                {
                    string varName = assignmentMatch.Groups[1].Value;
                    if (!IsCSharpKeyword(varName) && !IsCSharpBuiltinFunction(varName))
                    {
                        variables.Add(varName);
                    }
                }
                else
                {
                    var simpleAssignment = Regex.Match(trimmedLine, @"^([a-zA-Z_][a-zA-Z0-9_]*)\s*=");
                    if (simpleAssignment.Success)
                    {
                        string varName = simpleAssignment.Groups[1].Value;
                        if (!IsCSharpKeyword(varName) && !IsCSharpBuiltinFunction(varName))
                        {
                            variables.Add(varName);
                        }
                    }
                }

                var funcCalls = Regex.Matches(trimmedLine, @"[a-zA-Z_][a-zA-Z0-9_]*\s*\(([^)]*)\)");
                foreach (Match f in funcCalls)
                {
                    var args = f.Groups[1].Value;
                    var argMatches = Regex.Matches(args, @"\b([a-zA-Z_][a-zA-Z0-9_]*)\b");
                    foreach (Match am in argMatches)
                    {
                        string argName = am.Groups[1].Value;
                        int absIndex = f.Groups[1].Index + am.Index;
                        char charBefore = '\0';
                        char charAfter = '\0';
                        GetPrevNextNonWhitespace(trimmedLine, absIndex, argName.Length, out charBefore, out charAfter);
                        if (charBefore == '.') continue;
                        if (charAfter == '.') { variables.Add(argName); continue; }
                        if (!IsCSharpKeyword(argName) && !IsCSharpBuiltinFunction(argName) && !IsCSharpType(argName) && !char.IsDigit(argName[0]))
                        {
                            variables.Add(argName);
                        }
                    }
                }

                var allVarMatches = Regex.Matches(trimmedLine, @"\b([a-zA-Z_][a-zA-Z0-9_]*)\b");
                foreach (Match match in allVarMatches)
                {
                    string varName = match.Groups[1].Value;

                    if (IsCSharpKeyword(varName) || IsCSharpBuiltinFunction(varName) || IsCSharpType(varName))
                        continue;

                    int matchIndex = match.Index;
                    bool skipVariable = false;

                    char charBefore = '\0';
                    char charAfter = '\0';
                    GetPrevNextNonWhitespace(trimmedLine, matchIndex, varName.Length, out charBefore, out charAfter);

                    if (charBefore == '.')
                    {
                        skipVariable = true;
                    }
                    else if (charAfter == '.')
                    {
                        variables.Add(varName);
                        continue;
                    }

                    if (!skipVariable)
                    {
                        if (charAfter == '(' || charAfter == '[')
                        {
                            skipVariable = true;
                        }
                    }

                    if (!skipVariable && matchIndex >0)
                    {
                        string beforeMatch = trimmedLine.Substring(0, matchIndex).TrimEnd();
                        if (Regex.IsMatch(beforeMatch, @"\b(int|string|bool|var|double|float|long|short|byte|char|decimal|List|Dictionary|Array|IEnumerable)\s*$"))
                        {
                            skipVariable = true; 
                        }
                    }

                    if (!skipVariable)
                    {
                        variables.Add(varName);
                    }
                }
            }

            if (helperFunctions == null)
                helperFunctions = new HelperFunctions();

            foreach (var v in variables)
                helperFunctions.allVariables.Add(v);

            return variables.OrderBy(v => v).ToList();
        }

        private void GetPrevNextNonWhitespace(string line, int startIndex, int length, out char prev, out char next)
        {
            prev = '\0';
            next = '\0';
            int prevIdx = startIndex -1;
            while (prevIdx >=0 && prevIdx < line.Length && char.IsWhiteSpace(line[prevIdx])) prevIdx--;
            if (prevIdx >=0 && prevIdx < line.Length) prev = line[prevIdx];
            int nextIdx = startIndex + length;
            while (nextIdx < line.Length && char.IsWhiteSpace(line[nextIdx])) nextIdx++;
            if (nextIdx >=0 && nextIdx < line.Length) next = line[nextIdx];
        }

        private bool IsCSharpKeyword(string word)
        {
            string[] keywords = { "if", "else", "for", "while", "foreach", "do", "class", "public", "private", "return", "void", "int", "string", "bool", "var", "let", "const", "break", "continue", "true", "false", "null", "new", "typeof", "goto", "in", "out", "ref", "async", "await", "partial", "sealed", "readonly", "volatile", "operator", "event", "override", "virtual", "namespace", "using", "get", "set", "add", "remove", "value", "where", "params", "extern", "unsafe", "fixed", "implicit", "explicit", "record", "init" };
            return keywords.Contains(word);
        }
        private bool IsCSharpType(string word)
        {
            if (char.IsUpper(word[0]) && word.Length >1)
            {
                string[] commonTypes = { "Match", "Regex", "List", "Dictionary", "Array", "IEnumerable", "HashSet", "Stack", "Queue", "String", "Int32", "Int64", "Double", "Float", "Boolean", "Object", "LoopInfo", "LoopVariableUsage", "LoopBody", "LoopHeader", "LoopId", "Variables", "Type", "StartLine", "EndLine", "FullText", "Count", "Length", "MessageBox", "MessageBoxButton", "MessageBoxImage", "RoutedEventArgs", "RoutedEvent", "Window", "Control", "TextBox", "ComboBox", "Button", "Label" };
                if (commonTypes.Contains(word))
                    return true;
            }
            return false;
        }

        private bool IsCSharpBuiltinFunction(string word)
        {
            string[] builtins = { "Console", "WriteLine", "Write", "ReadLine", "Read", "List", "Dictionary", "Array", "String", "Int32", "Int64", "Double", "Float", "Boolean", "Object", "new", "typeof", "GetType", "ToString", "Parse", "TryParse" };
            return builtins.Contains(word);
        }

        private string RemoveStringsAndCommentsCSharp(string code)
        {
            string result = code;
            result = Regex.Replace(result, @"//.*", "");
            result = Regex.Replace(result, @"/\*.*?\*/", "", RegexOptions.Singleline);
            result = Regex.Replace(result, @"\$""(?:[^""""]|"""")*""", "");
            result = Regex.Replace(result, @"\$'(?:[^'']|'')*'", "");
            result = Regex.Replace(result, @"""(?:[^""""]|"""")*""", "");
            result = Regex.Replace(result, @"'(?:[^'']|'')*'", "");

            return result;
        }
        private string RemoveNestedLoopsFromCSharpBody(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return body;

            var lines = body.Split('\n');
            var result = new List<string>();

            for (int i =0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmed = line.Trim();

                if (Regex.IsMatch(trimmed, @"^(for|foreach|while|do)\s*"))
                {
                    int braceCount =0;
                    bool foundOpeningBrace = false;
                    i++; 

                    while (i < lines.Length)
                    {
                        string currentLine = lines[i];
                        foreach (char c in currentLine)
                        {
                            if (c == '{')
                            {
                                braceCount++;
                                foundOpeningBrace = true;
                            }
                            else if (c == '}')
                            {
                                braceCount--;
                                if (braceCount ==0 && foundOpeningBrace)
                                {
                                    goto nextLine;
                                }
                            }
                        }
                        i++;
                    }
                nextLine:
                    continue;
                }
            result.Add(line);
            }

            return string.Join("\n", result);
        }
    }
}
