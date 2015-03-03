using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;

namespace MSbuildParser
{
  class Program
  {
    static void Main()
    {
      var listOfRegAsmEvents = GetRegAsmEvents();
      using (var writer = new XmlTextWriter("c:\\temp\\msbuild.xml", Encoding.Unicode))
      {
        writer.Formatting = Formatting.Indented;
        writer.WriteStartDocument();
        writer.WriteStartElement("Root");
        foreach (var asmEvent in listOfRegAsmEvents)
        {
          writer.WriteStartElement("ResAsm");
          writer.WriteString(asmEvent);
          writer.WriteEndElement();
        }
        writer.WriteEndDocument();
      }

      return;
      var buildEventsList = GetBuildEvents();
      var root = new TreeNode<string>(buildEventsList[0].WhoBuilding);
      TreeNode<string> previous = null;
      foreach (var buildEvent in buildEventsList)
      {
        var @event = buildEvent;
        var found = root.FindTreeNode(node => node.Data != null && node.Data.Contains(@event.WhoBuilding) && !node.IsClosed);
        var current = found != null ? found.AddChild(buildEvent.WhatBuildeing, buildEvent.Raison) : root.AddChild(buildEvent.WhatBuildeing, buildEvent.Raison);

        if (current.Parent != root) continue;
        if (previous != null)
          previous.Close();
        previous = current;
      }

      using (var writer = new XmlTextWriter("c:\\temp\\msbuild.xml", Encoding.Unicode))
      {
        writer.Formatting = Formatting.Indented;
        writer.WriteStartDocument();
        writer.WriteStartElement("Root");
        writer.WriteAttributeString("Name", root.ToString());
        MakeReport(root.Children, writer);
        writer.WriteEndElement();
        writer.WriteEndDocument();
      }

      //Process objProcess = Process.Start("IEXPLORE.EXE", "c:\\temp\\msbuild.xml");
    }

    private static void MakeReport(IEnumerable<TreeNode<string>> root, XmlWriter writer)
    {
      foreach (var node in root)
      {
        writer.WriteStartElement("Project");
        writer.WriteAttributeString("Name", node.ToString());
        writer.WriteAttributeString("Reason", node.Note);
        var buildNumber = node.BuildNumber;
        if (buildNumber > 1)
          writer.WriteAttributeString("BuildNumber", buildNumber.ToString()); 
        if (node.Children.Count > 0)
          MakeReport(node.Children, writer);
        writer.WriteEndElement();
      }
    }

    private static List<BuildEvent> GetBuildEvents()
    {
      var regexToSplit = new Regex("\" \\(\\d*(\\:\\d*)?\\) is building \"".ToLower());
      var regexToSplit2 = new Regex("\\([a-zA-Z]* target\\(?s?\\)?\\)");
      var listOfProjects = GetProjects();

      var buildEventsList = new List<BuildEvent>();
      foreach (var project in listOfProjects)
      {
        var strToSplit = regexToSplit.Matches(project)[0].Captures[0].Value;
        var index = project.IndexOf(strToSplit, StringComparison.Ordinal);
        var part1 = project.Substring(0, index).Replace("project \"".ToLower(), "");
        var part2 = project.Substring(index + strToSplit.Length, project.Length - index - strToSplit.Length);
        var raisonCode = regexToSplit2.Matches(part2)[0].Captures[0].Value;
        index = part2.IndexOf("proj\" (", StringComparison.Ordinal);
        part2 = part2.Substring(0, index + 4);


        buildEventsList.Add(new BuildEvent { WhoBuilding = part1, WhatBuildeing = part2, Raison = raisonCode });
      }
      return buildEventsList;
    }

    private static IEnumerable<string> GetProjects()
    {
      var msbuildLog = new StreamReader("c:\\temp\\msbuild.log");

      const string regexExpression = "(Project \"C\\:\\\\dev\\\\MetraNetDev.*proj\\\" \\(\\d*(:\\d*)?\\) is building \"C\\:\\\\dev\\\\MetraNetDev.*proj\\\" \\(\\d*(:\\d*)?\\) on node \\d* \\(.* target.*\\)\\.\r\nInitial Properties\\:)";

      var r = new Regex(regexExpression.ToLower());
      var logText = msbuildLog.ReadToEnd();
      msbuildLog.Close();

      return (from Match match in r.Matches(logText.ToLower()) select match.Captures[0].Value).ToList();
    }

    private static IEnumerable<string> GetRegAsmEvents()
    {
      var msbuildLog = new StreamReader("c:\\temp\\msbuild.log");

      const string regexExpression = "(  regasm.exe /nologo /silent /codebase C\\:\\\\dev\\\\MetraNetDev\\\\output\\\\Debug\\\\bin\\\\.*.dll /tlb\\:C\\:\\\\dev\\\\MetraNetDev\\\\output\\\\Debug\\\\Include\\\\.*.tlb)";

      var r = new Regex(regexExpression.ToLower());
      var logText = msbuildLog.ReadToEnd();
      msbuildLog.Close();

      return (from Match match in r.Matches(logText.ToLower()) select match.Captures[0].Value).ToList();
    }

    struct BuildEvent
    {
      public string WhoBuilding;
      public string WhatBuildeing;
      public string Raison;
    }
  }
}