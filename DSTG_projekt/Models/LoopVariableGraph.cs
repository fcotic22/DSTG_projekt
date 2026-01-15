using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSTG_projekt.Models
{
    public class LoopVariableGraph
    {
        public Dictionary<int, LoopNode> Loops { get; set; } = new();
    }
}
