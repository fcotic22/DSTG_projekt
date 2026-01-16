using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace DSTG_projekt
{
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

    public class HelperFunctions
    {
        public List<LoopVariableUsage> loopVariableResults = new List<LoopVariableUsage>();
        public LoopInfo? ExtractBracketedLoop(string[] lines, int startLine, string type)
        {
            int braceCount = 0;
            int endLine = startLine;
            bool foundOpeningBrace = false;

            for (int i = startLine; i < lines.Length; i++)
            {
                string line = lines[i];
                foreach (char c in line)
                {
                    if (c == '{')
                    {
                        braceCount++;
                        foundOpeningBrace = true;
                    }
                    else if (c == '}')
                    {
                        braceCount--;
                        if (braceCount == 0 && foundOpeningBrace)
                        {
                            endLine = i;
                            var loopBody = string.Join("\n", lines.Skip(startLine + 1).Take(endLine - startLine - 1));
                            return new LoopInfo
                            {
                                Type = type,
                                StartLine = startLine + 1,
                                EndLine = endLine + 1,
                                LoopHeader = lines[startLine].Trim(),
                                LoopBody = loopBody,
                                FullText = string.Join("\n", lines.Skip(startLine).Take(endLine - startLine + 1))
                            };
                        }
                    }
                }
            }

            return null;
        }

        public LoopInfo? ExtractDoWhileLoop(string[] lines, int startLine)
        {
            int braceCount = 0;
            int endLine = startLine;
            bool foundOpeningBrace = false;

            for (int i = startLine; i < lines.Length; i++)
            {
                string line = lines[i];
                foreach (char c in line)
                {
                    if (c == '{')
                    {
                        braceCount++;
                        foundOpeningBrace = true;
                    }
                    else if (c == '}')
                    {
                        braceCount--;
                        if (braceCount == 0 && foundOpeningBrace)
                        {
                            for (int j = i + 1; j < lines.Length; j++)
                            {
                                if (Regex.IsMatch(lines[j], @"while\s*\([^)]*\)\s*;?"))
                                {
                                    endLine = j;
                                    var loopBody = string.Join("\n", lines.Skip(startLine + 1).Take(i - startLine - 1));
                                    return new LoopInfo
                                    {
                                        Type = "do-while",
                                        StartLine = startLine + 1,
                                        EndLine = endLine + 1,
                                        LoopHeader = lines[startLine].Trim() + " " + lines[endLine].Trim(),
                                        LoopBody = loopBody,
                                        FullText = string.Join("\n", lines.Skip(startLine).Take(endLine - startLine + 1))
                                    };
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }

        public void CreateMessageBoxLoopsInfo()
        {
            string text = "Prikaz korištenja varijabli u petljama:\n\n";

            if (loopVariableResults.Count == 0)
            {
                text += "Nisu pronađene petlje u kodu.";
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
                        text += $"{result.LoopId} ({loopTypeDisplay}) ({variablesStr})\n";
                    }
                    else
                    {
                        text += $"{result.LoopId} ({loopTypeDisplay}) (nema varijabli)\n";
                    }
                }
            }
            MessageBox.Show(text, "Rezultati analize petlji", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
}
