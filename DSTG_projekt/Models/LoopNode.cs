using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DSTG_projekt.Models
{
    public class LoopNode
    {
        public int Id { get; set; }
        public string LoopType { get; set; }
        public HashSet<string> Variables { get; set; } = new();
    }
}
