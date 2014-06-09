using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using TasksImporter.DTO;

namespace DoubleGis.University
{
    public class JiraQueryResultParser
    {
        const string DateFormat = "ddd, dd MMM yyyy HH:mm:ss +0700";
        private static readonly CultureInfo RuCulture = CultureInfo.CreateSpecificCulture("ru-RU");

        private readonly string _filePathName;

        public JiraQueryResultParser(string filePathName)
        {
            _filePathName = filePathName;
        }

        public IEnumerable<JiraTaskDto> Parse()
        {
            var xElement = XElement.Load(_filePathName);
            return (from item in xElement.Descendants("item")
                select new JiraTaskDto
                {
                    Id = int.Parse(item.Element("key").Attribute("id").Value),
                    ProjectId = int.Parse(item.Element("project").Attribute("id").Value),
                    ProjectName = item.Element("project").Value,
                    Name = item.Element("title").Value,
                    Type = item.Element("type").Value,
                    Priority = item.Element("priority").Value,
                    Status = item.Element("status").Value,
                    Resolution = item.Element("resolution").Value,
                    Assignee = item.Element("assignee").Value,
                    Created = DateTime.ParseExact(item.Element("created").Value, DateFormat, RuCulture),
                    DueDate = DateTime.ParseExact(item.Element("due").Value, DateFormat, RuCulture),
                    TaskLinks = from link in item.Descendants("issuelink")
                        select int.Parse(link.Element("issuekey").Attribute("id").Value)
                })
                .ToArray();
        }
    }
}