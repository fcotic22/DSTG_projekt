using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace DSTG_projekt
{
    public class JavaScriptLoopAnalyzer
    {
        public List<LoopInfo> forLoops = new List<LoopInfo>();
        public List<LoopInfo> whileLoops = new List<LoopInfo>();
        public List<LoopInfo> foreachLoops = new List<LoopInfo>();
        public List<LoopInfo> doWhileLoops = new List<LoopInfo>();
        public List<LoopInfo> forInLoops = new List<LoopInfo>();
        public List<LoopInfo> forOfLoops = new List<LoopInfo>();

        public HelperFunctions helperFunctions;

        public List<LoopInfo> ExtractJavaScriptLoops(string code)
        {
            helperFunctions = new HelperFunctions();
            var loops = new List<LoopInfo>();
            var lines = code.Split('\n');

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                if (Regex.IsMatch(line, @"for\s*\([^)]*\)\s*\{") && !line.Contains("for in") && !line.Contains("for of"))
                {
                    var loop = helperFunctions.ExtractBracketedLoop(lines, i, "for");
                    if (loop != null) loops.Add(loop);
                }
                else if (Regex.IsMatch(line, @"for\s+in\s+"))
                {
                    var loop = helperFunctions.ExtractBracketedLoop(lines, i, "for in");
                    if (loop != null) loops.Add(loop);
                }
                else if (Regex.IsMatch(line, @"for\s+of\s+"))
                {
                    var loop = helperFunctions.ExtractBracketedLoop(lines, i, "for of");
                    if (loop != null) loops.Add(loop);
                }
                else if (Regex.IsMatch(line, @"while\s*\([^)]*\)\s*\{"))
                {
                    var loop = helperFunctions.ExtractBracketedLoop(lines, i, "while");
                    if (loop != null) loops.Add(loop);
                }
                else if (Regex.IsMatch(line, @"do\s*\{"))
                {
                    var loop = helperFunctions.ExtractDoWhileLoop(lines, i);
                    if (loop != null) loops.Add(loop);
                }
            }

            return loops;
        }

        public void CategorizeJavaScriptLoops(List<LoopInfo> loops)
        {
            foreach (var loop in loops)
            {
                if (loop.Type == "for")
                    forLoops.Add(loop);
                else if (loop.Type == "while")
                    whileLoops.Add(loop);
                else if (loop.Type == "for in")
                    forInLoops.Add(loop);
                else if (loop.Type == "for of")
                    forOfLoops.Add(loop);
                else if (loop.Type == "do-while")
                    doWhileLoops.Add(loop);
            }
        }

        public void AnalyzeAllJavaScriptLoops(string language)
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
                var variables = AnalyzeJavaScriptLoopVariables(loop);
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

        public void AnalyzeAllJavaScriptVariables(string code)
        {
            if (helperFunctions == null)
                helperFunctions = new HelperFunctions();

            string cleanCode = RemoveStringsAndCommentsJavaScript(code);
            var allVariables = new HashSet<string>();
            var lines = cleanCode.Split('\n');

            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                string trimmedLine = line.Trim();

                var assignmentMatch = Regex.Match(trimmedLine, @"(?:let|const|var)\s+([a-zA-Z_$][a-zA-Z0-9_$]*)\s*=");
                if (assignmentMatch.Success)
                {
                    string varName = assignmentMatch.Groups[1].Value;
                    if (!IsJavaScriptKeyword(varName) && !IsJavaScriptBuiltinFunction(varName))
                    {
                        allVariables.Add(varName);
                    }
                }
                else
                {
                    var simpleAssignment = Regex.Match(trimmedLine, @"^([a-zA-Z_$][a-zA-Z0-9_$]*)\s*=");
                    if (simpleAssignment.Success)
                    {
                        string varName = simpleAssignment.Groups[1].Value;
                        if (!IsJavaScriptKeyword(varName) && !IsJavaScriptBuiltinFunction(varName))
                        {
                            allVariables.Add(varName);
                        }
                    }
                }

                var allVarMatches = Regex.Matches(trimmedLine, @"\b([a-zA-Z_$][a-zA-Z0-9_$]*)\b");
                foreach (Match match in allVarMatches)
                {
                    string varName = match.Groups[1].Value;

                    if (IsJavaScriptKeyword(varName) || IsJavaScriptBuiltinFunction(varName) || IsJavaScriptType(varName))
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
                        allVariables.Add(varName);
                        continue;
                    }

                    if (!skipVariable)
                    {
                        if (charAfter == '(')
                        {
                            skipVariable = true;
                        }
                    }

                    if (!skipVariable)
                    {
                        allVariables.Add(varName);
                    }
                }
            }

            foreach (var v in allVariables)
                helperFunctions.allVariables.Add(v);
        }


        public List<string> AnalyzeJavaScriptLoopVariables(LoopInfo loop)
        {
            var variables = new HashSet<string>();

            string cleanHeader = RemoveStringsAndCommentsJavaScript(loop.LoopHeader);
            string cleanBody = RemoveStringsAndCommentsJavaScript(loop.LoopBody);

            string bodyWithoutNested = RemoveNestedLoopsFromJavaScriptBody(cleanBody);
            cleanBody = bodyWithoutNested;

            if (loop.Type == "for")
            {
                var forHeaderMatch = Regex.Match(cleanHeader, @"for\s*\(([^)]+)\)");
                if (forHeaderMatch.Success)
                {
                    var headerVars = Regex.Matches(forHeaderMatch.Groups[1].Value, @"\b([a-zA-Z_$][a-zA-Z0-9_$]*)\b");
                    foreach (Match match in headerVars)
                    {
                        string varName = match.Groups[1].Value;
                        int absIndex = forHeaderMatch.Groups[1].Index + match.Index;
                        char charBefore = '\0';
                        char charAfter = '\0';
                        GetPrevNextNonWhitespace(cleanHeader, absIndex, varName.Length, out charBefore, out charAfter);
                        if (charBefore == '.') continue;
                        if (charAfter == '.') { variables.Add(varName); continue; }
                        if (!IsJavaScriptKeyword(varName) && !IsJavaScriptBuiltinFunction(varName) && !char.IsDigit(varName[0]))
                        {
                            variables.Add(varName);
                        }
                    }
                }
            }
            else if (loop.Type == "for in" || loop.Type == "for of")
            {
                var forInMatch = Regex.Match(cleanHeader, @"for\s+(\w+)\s+(?:in|of)");
                if (forInMatch.Success)
                {
                    variables.Add(forInMatch.Groups[1].Value);
                }
                var forInMatch2 = Regex.Match(cleanHeader, @"(?:in|of)\s+(\w+)");
                if (forInMatch2.Success)
                {
                    string varName = forInMatch2.Groups[1].Value;
                    int absIndex = forInMatch2.Index + forInMatch2.Groups[1].Index;
                    char charBefore = '\0';
                    char charAfter = '\0';
                    GetPrevNextNonWhitespace(cleanHeader, absIndex, varName.Length, out charBefore, out charAfter);
                    if (charBefore == '.') { }
                    else if (charAfter == '.') { variables.Add(varName); }
                    else if (!IsJavaScriptKeyword(varName) && !IsJavaScriptBuiltinFunction(varName))
                    {
                        variables.Add(varName);
                    }
                }
            }
            else if (loop.Type == "while")
            {
                var conditionMatch = Regex.Match(cleanHeader, @"while\s*\(([^)]+)\)");
                if (conditionMatch.Success)
                {
                    var conditionVars = Regex.Matches(conditionMatch.Groups[1].Value, @"\b([a-zA-Z_$][a-zA-Z0-9_$]*)\b");
                    foreach (Match match in conditionVars)
                    {
                        string varName = match.Groups[1].Value;
                        int absIndex = conditionMatch.Groups[1].Index + match.Index;
                        char charBefore = '\0';
                        char charAfter = '\0';
                        GetPrevNextNonWhitespace(cleanHeader, absIndex, varName.Length, out charBefore, out charAfter);
                        if (charBefore == '.') continue;
                        if (charAfter == '.') { variables.Add(varName); continue; }
                        if (!IsJavaScriptKeyword(varName) && !IsJavaScriptBuiltinFunction(varName) && !char.IsDigit(varName[0]))
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
                    var argVars = Regex.Matches(content, @"\b([a-zA-Z_$][a-zA-Z0-9_$]*)\b");
                    foreach (Match m in argVars)
                    {
                        string name = m.Groups[1].Value;
                        int absIndex = pm.Groups[1].Index + m.Index;
                        char charBefore = '\0';
                        char charAfter = '\0';
                        GetPrevNextNonWhitespace(cleanHeader, absIndex, name.Length, out charBefore, out charAfter);
                        if (charBefore == '.') continue;
                        if (charAfter == '.') { variables.Add(name); continue; }
                        if (!IsJavaScriptKeyword(name) && !IsJavaScriptBuiltinFunction(name) && !IsJavaScriptType(name) && !char.IsDigit(name[0]))
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

                var assignmentMatch = Regex.Match(trimmedLine, @"(?:let|const|var)\s+([a-zA-Z_$][a-zA-Z0-9_$]*)\s*=");
                if (assignmentMatch.Success)
                {
                    string varName = assignmentMatch.Groups[1].Value;
                    if (!IsJavaScriptKeyword(varName) && !IsJavaScriptBuiltinFunction(varName))
                    {
                        variables.Add(varName);
                    }
                }
                else
                {
                    var simpleAssignment = Regex.Match(trimmedLine, @"^([a-zA-Z_$][a-zA-Z0-9_$]*)\s*=");
                    if (simpleAssignment.Success)
                    {
                        string varName = simpleAssignment.Groups[1].Value;
                        if (!IsJavaScriptKeyword(varName) && !IsJavaScriptBuiltinFunction(varName))
                        {
                            variables.Add(varName);
                        }
                    }
                }

                var funcCalls = Regex.Matches(trimmedLine, @"[a-zA-Z_$][a-zA-Z0-9_$]*\s*\(([^)]*)\)");
                foreach (Match f in funcCalls)
                {
                    var args = f.Groups[1].Value;
                    var argMatches = Regex.Matches(args, @"\b([a-zA-Z_$][a-zA-Z0-9_$]*)\b");
                    foreach (Match am in argMatches)
                    {
                        string argName = am.Groups[1].Value;
                        int absIndex = f.Groups[1].Index + am.Index;
                        char charBefore = '\0';
                        char charAfter = '\0';
                        GetPrevNextNonWhitespace(trimmedLine, absIndex, argName.Length, out charBefore, out charAfter);
                        if (charBefore == '.') continue;
                        if (charAfter == '.') { variables.Add(argName); continue; }
                        if (!IsJavaScriptKeyword(argName) && !IsJavaScriptBuiltinFunction(argName) && !IsJavaScriptType(argName) && !char.IsDigit(argName[0]))
                        {
                            variables.Add(argName);
                        }
                    }
                }

                var allVarMatches = Regex.Matches(trimmedLine, @"\b([a-zA-Z_$][a-zA-Z0-9_$]*)\b");
                foreach (Match match in allVarMatches)
                {
                    string varName = match.Groups[1].Value;

                    if (IsJavaScriptKeyword(varName) || IsJavaScriptBuiltinFunction(varName) || IsJavaScriptType(varName))
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
                        if (charAfter == '(')
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

        private bool IsJavaScriptKeyword(string word)
        {
            string[] keywords = { "if", "else", "for", "while", "do", "function", "var", "let", "const", "return", "break", "continue", "in", "of", "true", "false", "null", "undefined", "async", "await", "class", "extends", "super", "this", "new", "typeof", "instanceof", "try", "catch", "finally", "throw", "switch", "case", "default" };
            return keywords.Contains(word);
        }
        private bool IsJavaScriptType(string word)
        {
            if (char.IsUpper(word[0]) && word.Length >1)
            {
                string[] commonTypes = { "Match", "Regex", "Array", "Object", "Number", "String", "Boolean", "Date", "Math", "JSON", "LoopInfo", "LoopVariableUsage", "LoopBody", "LoopHeader", "LoopId", "Variables", "Type", "StartLine", "EndLine", "FullText", "Count", "Length", "MessageBox", "MessageBoxButton", "MessageBoxImage", "RoutedEventArgs", "RoutedEvent", "Window", "Control", "TextBox", "ComboBox", "Button", "Label" };
                if (commonTypes.Contains(word))
                    return true;
            }
            return false;
        }

        private bool IsJavaScriptBuiltinFunction(string word)
        {
            string[] builtins = { "console", "log", "push", "pop", "length", "toString", "parseInt", "parseFloat", "Number", "String", "Boolean", "Array", "Object", "Math", "JSON", "Date", "setTimeout", "setInterval", "clearTimeout", "clearInterval", "Promise", "fetch", "then", "catch" };
            return builtins.Contains(word);
        }

        private string RemoveStringsAndCommentsJavaScript(string code)
        {
            string result = code;
            result = Regex.Replace(result, @"//.*", "");
            result = Regex.Replace(result, @"/\*.*?\*/", "", RegexOptions.Singleline);
            result = Regex.Replace(result, @"`[^`]*`", "");
            result = Regex.Replace(result, @"""(?:[^""""]|"""")*""", "");
            result = Regex.Replace(result, @"'(?:[^'']|'')*'", "");

            return result;
        }
        private string RemoveNestedLoopsFromJavaScriptBody(string body)
        {
            if (string.IsNullOrWhiteSpace(body))
                return body;

            var lines = body.Split('\n');
            var result = new List<string>();

            for (int i =0; i < lines.Length; i++)
            {
                string line = lines[i];
                string trimmed = line.Trim();

                if (Regex.IsMatch(trimmed, @"^(for|while|do)\s*"))
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
