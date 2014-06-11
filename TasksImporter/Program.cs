using System;
using System.Collections.Generic;

using DoubleGis.University.CustomFieldsService;
using DoubleGis.University.DTO;
using DoubleGis.University.ProjectService;

namespace DoubleGis.University
{
    public static class Program
    {
        private const string IssuesFileName = "AdsModel.xml";
       
        private static readonly Uri PwaUri = new Uri("http://erm-project.cloudapp.net/pwa");

        public static void Main()
        {
            var taskImportManager = new TaskImportManager(PwaUri, IssuesFileName);

            IEnumerable<JiraTaskDto> jiraTaskDtos;
            IDictionary<int, IEnumerable<int>> jiraTaskRelations;
            if (!taskImportManager.TryParseSource(out jiraTaskDtos, out jiraTaskRelations))
            {
                Console.WriteLine(taskImportManager.GetAllErrors());
                return;
            }

            CustomFieldDataSet.CustomFieldsRow jiraProjectIdCustomField;
            CustomFieldDataSet.CustomFieldsRow jiraProjectNameCustomField;
            CustomFieldDataSet.CustomFieldsRow jiraTaskIdCustomField;
            if (!taskImportManager.TryReadCustomFields(out jiraProjectIdCustomField, out jiraProjectNameCustomField, out jiraTaskIdCustomField))
            {
                return;
            }

            AddOrUpdateTasks(taskImportManager, jiraTaskDtos, jiraTaskIdCustomField, jiraProjectIdCustomField, jiraProjectNameCustomField);
            while (!TryUpdateTaskRelations(taskImportManager, jiraTaskIdCustomField, jiraTaskRelations))
            {
                Console.WriteLine("Retrying update task relations...");
            }
        }

        private static void AddOrUpdateTasks(TaskImportManager taskImportManager,
                                                IEnumerable<JiraTaskDto> jiraTaskDtos,
                                                CustomFieldDataSet.CustomFieldsRow jiraTaskIdCustomField,
                                                CustomFieldDataSet.CustomFieldsRow jiraProjectIdCustomField,
                                                CustomFieldDataSet.CustomFieldsRow jiraProjectNameCustomField)
        {
            var projectDataSetToAdd = new ProjectDataSet();

            ProjectDataSet projectDataSetToUpdate;
            if (!taskImportManager.TryGetProjectDataSet(out projectDataSetToUpdate))
            {
                Console.WriteLine(taskImportManager.GetAllErrors());
            }

            Guid projectId;
            var projectTasks = projectDataSetToUpdate.GetExistingTasksByJiraKeys(jiraTaskIdCustomField, out projectId);

            foreach (var taskDto in jiraTaskDtos)
            {
                ProjectDataSet.TaskRow taskRow;
                if (!projectTasks.TryGetValue(taskDto.Id, out taskRow))
                {
                    projectDataSetToAdd.AddNewTask(projectId,
                                                   jiraProjectIdCustomField.MD_PROP_UID,
                                                   jiraProjectNameCustomField.MD_PROP_UID,
                                                   jiraTaskIdCustomField.MD_PROP_UID,
                                                   taskDto);
                }
                else
                {
                    projectDataSetToUpdate.UpdateTask(projectId,
                                                      taskRow.TASK_UID,
                                                      jiraProjectIdCustomField.MD_PROP_UID,
                                                      jiraProjectNameCustomField.MD_PROP_UID,
                                                      jiraTaskIdCustomField.MD_PROP_UID,
                                                      taskDto);
                }
            }

            taskImportManager.MakeChangesInProjectServer(projectId, projectDataSetToAdd, projectDataSetToUpdate);
        }

        private static bool TryUpdateTaskRelations(TaskImportManager taskImportManager,
                                                   CustomFieldDataSet.CustomFieldsRow jiraTaskIdCustomField,
                                                   IDictionary<int, IEnumerable<int>> jiraTaskRelations)
        {
            var taskRelationsUpdater = new TaskRelationsUpdater(jiraTaskIdCustomField);
            try
            {
                ProjectDataSet projectDataSet;
                if (!taskImportManager.TryGetProjectDataSet(out projectDataSet))
                {
                    Console.WriteLine(taskImportManager.GetAllErrors());
                    return false;
                }

                Guid projectId;
                var projectDataSetToAdd = taskRelationsUpdater.UpdateRelations(projectDataSet, jiraTaskRelations, out projectId);
                taskImportManager.MakeChangesInProjectServer(projectId, projectDataSetToAdd, projectDataSet);
                return true;
            }
            catch (Exception)
            {
                Console.WriteLine(taskImportManager.GetAllErrors());
                return false;
            }
        }
    }
}
