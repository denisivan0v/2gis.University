using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

using DoubleGis.University.DTO;

namespace DoubleGis.University
{
    public class JiraQueryResultParser
    {
        private const string DateFormat = "ddd, dd MMM yyyy HH:mm:ss +0700";
        private static readonly CultureInfo RuCulture = CultureInfo.CreateSpecificCulture("ru-RU");

        private readonly string _filePathName;

        public JiraQueryResultParser(string filePathName)
        {
            _filePathName = filePathName;
        }

        public IEnumerable<JiraTaskDto> Parse()
        {
            var xml = XElement.Load(_filePathName);
            
            // ReSharper disable PossibleNullReferenceException
            return (from item in xml.Descendants("item")
                    select new JiraTaskDto
                        {
                            Id = (int)item.Element("key").Attribute("id"),
                            ProjectId = (int)item.Element("project").Attribute("id"),
                            ProjectName = (string)item.Element("project"),
                            Name = (string)item.Element("title"),
                            Type = (string)item.Element("type"),
                            Priority = (int)item.Element("priority").Attribute("id"),
                            Status = (string)item.Element("status"),
                            Resolution = (string)item.Element("resolution"),
                            Assignee = (string)item.Element("assignee"),
                            Created = (DateTime)item.Element("created"),
                            DueDate = !item.Element("due").IsEmpty ? DateTime.ParseExact(item.Element("due").Value, DateFormat, RuCulture) : DateTime.Now,
                            TaskLinks = from link in item.Descendants("issuelink")
                                        select (int)link.Element("issuekey").Attribute("id")
                        })
                .ToArray();

            // ReSharper enable PossibleNullReferenceException
        }
    }
}