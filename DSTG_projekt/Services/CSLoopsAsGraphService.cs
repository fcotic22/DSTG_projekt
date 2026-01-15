using DSTG_projekt.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows;

namespace DSTG_projekt.Services
{
    public static class CSLoopsAsGraphService
    {

        public static StringBuilder GetGraph(string path) {
            var graph = AnalyzeCSharpLoopsAsGraph(path);

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


        public static LoopVariableGraph AnalyzeCSharpLoopsAsGraph(string path)
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


        public static bool IsCSharpLoop(string line, out string loopType)
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


        public static HashSet<string> AnalyzeLoopVariableUsage(List<string> loopBody)
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


        public static IEnumerable<string> Tokenize(string line)
        {
            char[] sep = { ' ', '(', ')', '{', '}', ';', '=', '+', '-', '*', '/', '<', '>' };
            return line.Split(sep, StringSplitOptions.RemoveEmptyEntries);
        }


        public static bool IsVariable(string token)
        {
            string[] keywords = {
                "for","foreach","while","do","if","else",
                "int","double","float","var","string","bool",
                "return","new"
            };

            return char.IsLetter(token[0]) && !keywords.Contains(token);
        }


        public static List<string> ExtractLoopBody(string[] lines, ref int index)
        {
            var body = new List<string>();

            int braceCount = 0;
            bool insideLoop = false;

            for (; index < lines.Length; index++)
            {
                string line = lines[index];

                // Pocetak bloka
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
    }
}
