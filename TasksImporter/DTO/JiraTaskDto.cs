using System;
using System.Collections.Generic;

namespace DoubleGis.University.DTO
{
    public class JiraTaskDto
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public int Priority { get; set; }
        public string Status { get; set; }
        public string Resolution { get; set; }
        public string Assignee { get; set; }
        public DateTime Created { get; set; }
        public DateTime DueDate { get; set; }
        public IEnumerable<int> TaskLinks { get; set; }
    }
}