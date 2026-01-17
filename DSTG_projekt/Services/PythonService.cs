using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.IO;

namespace DSTG_projekt.Services
{

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


    public static class PythonService
    {


        public static StringBuilder AnalyzePythonCode(String path)
        {
            var graph = AnalyzePythonLoopsAsGraph(path);

            StringBuilder sb = new StringBuilder();

            foreach (var loop in graph.Loops.Values)
            {
                sb.AppendLine($"Petlja {loop.Id} ({loop.LoopType})");

                foreach (var variable in loop.Variables)
                {
                    sb.AppendLine($"   -> {variable}");
                }
            }

            return sb;
        }


        private static LoopVariableGraph AnalyzePythonLoopsAsGraph(string path)
        {
            var lines = File.ReadAllLines(path);
            var graph = new LoopVariableGraph();

            int loopId = 1;

            for (int i = 0; i < lines.Length; i++)
            {
                if (IsPythonLoop(lines[i], out string loopType))
                {
                    var body = ExtractPythonLoopBody(lines, i, out int endIndex);

                    var variables = AnalyzePythonVariableUsage(body, lines[i]);

                    graph.Loops.Add(loopId, new LoopNode
                    {
                        Id = loopId,
                        LoopType = loopType,
                        Variables = variables
                    });

                    i = endIndex;
                    loopId++;
                }
            }

            return graph;
        }

        private static bool IsPythonLoop(string line, out string loopType)
        {
            loopType = "";
            line = line.Trim();

            if (line.StartsWith("for "))
                loopType = "for";
            else if (line.StartsWith("while "))
                loopType = "while";

            return loopType != "";
        }

        private static List<string> ExtractPythonLoopBody(string[] lines, int startIndex, out int endIndex)
        {
            var body = new List<string>();

            int baseIndent = CountIndent(lines[startIndex]);
            endIndex = startIndex;

            for (int i = startIndex + 1; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i]))
                    continue;

                int currentIndent = CountIndent(lines[i]);

                if (currentIndent <= baseIndent)
                    break;

                body.Add(lines[i]);
                endIndex = i;
            }

            return body;
        }


        private static HashSet<string> AnalyzePythonVariableUsage(List<string> loopBody, string forHeaderLine)
        {
            var variables = new HashSet<string>();

            foreach (var v in ExtractPythonForHeaderVariables(forHeaderLine))
                variables.Add(v);

            foreach (var rawLine in loopBody)
            {
                var line = CleanPythonLine(rawLine);


                var attributeNames = Regex.Matches(rawLine, @"\.\s*([a-zA-Z_][a-zA-Z0-9_]*)")
                              .Select(m => m.Groups[1].Value)
                              .ToHashSet();

                foreach (var v in ExtractBaseVariables(rawLine))
                {
                    if (!IsPythonKeyword(v))
                        variables.Add(v);
                }

                foreach (Match m in PythonVariableRegex().Matches(line))
                {
                    var token = m.Value;

                    if (IsPythonKeyword(token))
                        continue;

                    if (Regex.IsMatch(rawLine, $@"\b{token}\s*\("))
                        continue;

                    if (attributeNames.Contains(token))
                        continue;

                    variables.Add(token);
                }
            }

            return variables;
        }

        private static string CleanPythonLine(string line)
        {
            line = Regex.Replace(line, @"\bf(['""])(?:(?=(\\?))\2.)*?\1", "");
            line = Regex.Replace(line, @"(['""])(?:(?=(\\?))\2.)*?\1", "");
            line = Regex.Replace(line, @"\[[^\]]*\]", "");
            line = Regex.Replace(line, @"\.[a-zA-Z_][a-zA-Z0-9_]*", "");

            return line;
        }

        private static int CountIndent(string line)
        {
            int count = 0;
            foreach (char c in line)
            {
                if (c == ' ')
                    count++;
                else
                    break;
            }
            return count;
        }


        private static HashSet<string> ExtractPythonForHeaderVariables(string line)
        {
            var vars = new HashSet<string>();

            var match = Regex.Match(line, @"for\s+(.+?)\s+in\s+(.+?):");
            if (!match.Success)
                return vars;

            var left = match.Groups[1].Value.Trim();
            vars.Add(left);

            var right = match.Groups[2].Value.Trim();
            right = right.Split('[')[0].Trim();

            if (!Regex.IsMatch(right, @"\w+\s*\("))
                vars.Add(right);

            return vars;
        }

        private static IEnumerable<string> ExtractBaseVariables(string line)
        {
            var matches = Regex.Matches(line, @"\b([a-zA-Z_][a-zA-Z0-9_]*)\s*\.");

            foreach (Match m in matches)
                yield return m.Groups[1].Value;
        }

        private static Regex PythonVariableRegex()
        {
            return new Regex(
                @"(?<!\.)\b[a-zA-Z_][a-zA-Z0-9_]*\b",
                RegexOptions.Compiled
            );
        }

        private static bool IsPythonKeyword(string token)
        {
            string[] keywords =
            {
                "for", "while", "if", "elif", "else", "with",
                "try", "except", "finally",
                "in", "and", "or", "not", "is",
                "return", "break", "continue", "pass",
                "int", "float", "str", "bool", "list", "dict", "set", "tuple",
                "Exception", "exception", "as",
                "iloc",
                "None", "True", "False",
                "range", "len", "print", "enumerate", "zip",
                "map", "filter", "sum", "min", "max", "abs",
                "open", "sorted", "any", "all"
            };

            return keywords.Contains(token);
        }
    }
}
