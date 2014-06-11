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

        private readonly XElement _xml;

        public JiraQueryResultParser(string filePathName)
        {
            _xml = XElement.Load(filePathName);
        }

        public IEnumerable<JiraTaskDto> ReadTasks()
        {
            // ReSharper disable PossibleNullReferenceException
            return (from item in _xml.Descendants("item")
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
                            DueDate = !item.Element("due").IsEmpty && !string.IsNullOrEmpty(item.Element("due").Value)
                                          ? DateTime.ParseExact(item.Element("due").Value, DateFormat, RuCulture)
                                          : DateTime.Now,
                            TaskLinks = from link in item.Descendants("issuelink")
                                        select (int)link.Element("issuekey").Attribute("id")
                        })
                .ToArray();

            // ReSharper enable PossibleNullReferenceException
        }

        public IDictionary<int, IEnumerable<int>> ReadTaskRelations()
        {
            return (from item in _xml.Descendants("item")
                    let outwardLinks = item.Descendants("outwardlinks")
                    where outwardLinks.Any()
                    select new
                        {
                            Id = (int)item.Element("key").Attribute("id"),
                            OutwardLinks = from outwardLink in outwardLinks
                                           from issueKey in outwardLink.Descendants("issuekey")
                                           select (int)issueKey.Attribute("id")
                        })
                .ToDictionary(x => x.Id, x => x.OutwardLinks);
        }
    }
}