using System;
using System.Collections.Generic;
using System.Linq;

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

        public ProjectDataSet UpdateRelations(ProjectDataSet projectDataSetToUpdate, IDictionary<int, IEnumerable<int>> jiraTaskRelations, out Guid projectId)
        {
            var projectDataSetToAdd = new ProjectDataSet();

            var tasksByJiraKeys = projectDataSetToUpdate.GetExistingTasksByJiraKeys(_jiraTaskIdCustomField, out projectId);
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
                        var dependency = projectDataSetToUpdate.Dependency.SingleOrDefault(x => x.LINK_PRED_UID == relatedTask.TASK_UID &&
                                                                                                x.LINK_SUCC_UID == mainTask.TASK_UID);
                        if (dependency == null)
                        {
                            dependency = projectDataSetToAdd.Dependency.NewDependencyRow();
                            dependency.LINK_UID = Guid.NewGuid();
                            dependency.PROJ_UID = projectId;
                            dependency.LINK_PRED_UID = relatedTask.TASK_UID;
                            dependency.LINK_SUCC_UID = mainTask.TASK_UID;
                            dependency.LINK_TYPE = 2; // StartFinish, http://msdn.microsoft.com/en-us/library/websvcproject.projectdataset.dependencyrow.link_type(v=office.12).aspx

                            projectDataSetToAdd.Dependency.AddDependencyRow(dependency);
                        }
                    }
                }
            }

            return projectDataSetToAdd;
        }
    }
}