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

        public ProjectDataSet UpdateRelations(ProjectDataSet projectDataSet, IDictionary<int, IEnumerable<int>> jiraTaskRelations, out Guid projectId)
        {
            var projectDataSetToAdd = new ProjectDataSet();

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
                        // ++relatedTask.TASK_OUTLINE_LEVEL;
                        // relatedTask.AddPosition = (int)Task.AddPositionType.Middle;
                        // relatedTask.AddAfterTaskUID = mainTask.TASK_UID;

                        // TODO Need to check existing dependencies

                        var dependency = projectDataSetToAdd.Dependency.NewDependencyRow();
                        dependency.LINK_UID = Guid.NewGuid();
                        dependency.PROJ_UID = projectId;
                        dependency.LINK_PRED_UID = relatedTask.TASK_UID;
                        dependency.LINK_SUCC_UID = mainTask.TASK_UID;
                        dependency.LINK_TYPE = 2; // StartFinish, http://msdn.microsoft.com/en-us/library/websvcproject.projectdataset.dependencyrow.link_type(v=office.12).aspx

                        projectDataSetToAdd.Dependency.AddDependencyRow(dependency);
                    }
                }
            }

            return projectDataSetToAdd;
        }
    }
}