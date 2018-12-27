using System;
using System.Collections.Generic;
using System.Text;

namespace HelpersForCore
{
    public static class CodingExtension
    {
        public static GenerateNode Add(this List<GenerateNode> nodes, string name, string text)
        {
            GenerateNode newNode = new GenerateNode(name, text);
            nodes.Add(newNode);
            return newNode;
        }

        public static void Add(this List<GenerateNode> nodes, string name, IEnumerable<string> texts)
        {
            foreach (string text in texts)
            {
                nodes.Add(new GenerateNode(name, text));
            }
        }

        public static string Generate(this GenerateNode node)
        {
            return CodingHelper.Generate(node);
        }
    }
}
