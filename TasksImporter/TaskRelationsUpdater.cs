using System;
using System.Collections.Generic;

using DoubleGis.University.CustomFieldsService;
using DoubleGis.University.ProjectService;

namespace DoubleGis.University
{
    public class TaskRelationsUpdater
    {
        private readonly CustomFieldDataSet.CustomFieldsRow _jiraTaskIdCustomField;

        public TaskRelationsUpdater(CustomFieldDataSet.CustomFieldsRow jiraTaskIdCustomField)
        {
            _jiraTaskIdCustomField = jiraTaskIdCustomField;
        }

        public Guid UpdateRelations(ProjectDataSet projectDataSet, IDictionary<int, IEnumerable<int>> jiraTaskRelations)
        {
            Guid projectId;
            var tasksByJiraKeys = projectDataSet.GetExistingTasksByJiraKeys(_jiraTaskIdCustomField, out projectId);
            foreach (var jiraTaskRelation in jiraTaskRelations)
            {
                ProjectDataSet.TaskRow mainTask;
                if (!tasksByJiraKeys.TryGetValue(jiraTaskRelation.Key, out mainTask))
                {
                    continue;
                }

                foreach (var relatedTaskId in jiraTaskRelation.Value)
                {
                    ProjectDataSet.TaskRow relatedTask;

                    if (tasksByJiraKeys.TryGetValue(relatedTaskId, out relatedTask))
                    {
                        relatedTask.AddAfterTaskUID = mainTask.TASK_UID;
                    }
                }
            }

            return projectId;
        }
    }
}