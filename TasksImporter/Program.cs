using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

using DoubleGis.University.CustomFieldsService;
using DoubleGis.University.DTO;
using DoubleGis.University.ProjectService;

using Microsoft.Office.Project.Server.Library;

using Project = DoubleGis.University.ProjectService.Project;

namespace DoubleGis.University
{
    public static class Program
    {
        private const string IssuesFileName = "AdsModel.xml";
       
        // private static readonly Uri PwaUri = new Uri("http://uk-erm-ps/pwa");
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

            Guid projectId;
            var projectTasks = taskImportManager.GetExistingTasksByJiraKeys(jiraTaskIdCustomField, out projectId);
            if (projectTasks == null)
            {
                Console.WriteLine(taskImportManager.GetAllErrors());
                return;
            }

            //ProjectDataSet projectDataSet;
            //if (!taskImportManager.TryGetProjectDataSet(out projectDataSet))
            //{
            //    Console.WriteLine(taskImportManager.GetAllErrors());
            //    return;
            //}

            var projectDataSetToAdd = new ProjectDataSet();
            foreach (var taskDto in jiraTaskDtos)
            {
                ProjectDataSet.TaskRow taskRow;
                if (!projectTasks.TryGetValue(taskDto.Id, out taskRow))
                {
                    AddNewTask(projectId,
                               jiraProjectIdCustomField.MD_PROP_UID,
                               jiraProjectNameCustomField.MD_PROP_UID,
                               jiraTaskIdCustomField.MD_PROP_UID,
                               projectDataSetToAdd.Task,
                               projectDataSetToAdd.TaskCustomFields,
                               taskDto);
                }
                else
                {
                    UpdateTask(projectId,
                               taskRow.TASK_UID,
                               jiraProjectIdCustomField.MD_PROP_UID,
                               jiraProjectNameCustomField.MD_PROP_UID,
                               jiraTaskIdCustomField.MD_PROP_UID,
                               projectDataSet.Task,
                               projectDataSet.TaskCustomFields,
                               taskDto);
                }
            }

            if (projectDataSetToAdd.Task.Any())
            {
                AddToProject(projectClient, projectId, projectDataSetToAdd);
            }

            var taskChanges = projectDataSet.Task.GetChanges(DataRowState.Modified | DataRowState.Added);
            var taskCustomFieldsChanges = projectDataSet.TaskCustomFields.GetChanges(DataRowState.Modified | DataRowState.Added);
            if ((taskChanges != null && taskChanges.Rows.Count != 0) ||
                (taskCustomFieldsChanges != null && taskCustomFieldsChanges.Rows.Count != 0))
            {
                UpdateProject(projectClient, projectId, projectDataSet);
            }
        }

        private static void AddNewTask(Guid projectId,
                                       Guid jiraProjectIdCustomFieldUid,
                                       Guid jiraProjectNameCustomFieldUid,
                                       Guid jiraTaskIdCustomFieldUid,
                                       ProjectDataSet.TaskDataTable taskTable,
                                       ProjectDataSet.TaskCustomFieldsDataTable taskCustomFieldsTable,
                                       JiraTaskDto taskDto)
        {
            var task = taskTable.NewTaskRow();
            task.TASK_UID = Guid.NewGuid();

            var jiraProjectIdCustomFieldRow = taskCustomFieldsTable.NewTaskCustomFieldsRow();
            jiraProjectIdCustomFieldRow.CUSTOM_FIELD_UID = Guid.NewGuid();
            jiraProjectIdCustomFieldRow.PROJ_UID = projectId;
            jiraProjectIdCustomFieldRow.TASK_UID = task.TASK_UID;
            jiraProjectIdCustomFieldRow.MD_PROP_UID = jiraProjectIdCustomFieldUid;

            var jiraProjectNameCustomFieldRow = taskCustomFieldsTable.NewTaskCustomFieldsRow();
            jiraProjectNameCustomFieldRow.CUSTOM_FIELD_UID = Guid.NewGuid();
            jiraProjectNameCustomFieldRow.PROJ_UID = projectId;
            jiraProjectNameCustomFieldRow.TASK_UID = task.TASK_UID;
            jiraProjectNameCustomFieldRow.MD_PROP_UID = jiraProjectNameCustomFieldUid;

            var jiraTaskIdCustomFieldRow = taskCustomFieldsTable.NewTaskCustomFieldsRow();
            jiraTaskIdCustomFieldRow.CUSTOM_FIELD_UID = Guid.NewGuid();
            jiraTaskIdCustomFieldRow.PROJ_UID = projectId;
            jiraTaskIdCustomFieldRow.TASK_UID = task.TASK_UID;
            jiraTaskIdCustomFieldRow.MD_PROP_UID = jiraTaskIdCustomFieldUid;

            MapTaskFields(projectId,
                          taskDto,
                          task,
                          jiraProjectIdCustomFieldRow,
                          jiraProjectNameCustomFieldRow,
                          jiraTaskIdCustomFieldRow);

            taskTable.AddTaskRow(task);
            taskCustomFieldsTable.AddTaskCustomFieldsRow(jiraProjectIdCustomFieldRow);
            taskCustomFieldsTable.AddTaskCustomFieldsRow(jiraProjectNameCustomFieldRow);
            taskCustomFieldsTable.AddTaskCustomFieldsRow(jiraTaskIdCustomFieldRow);
        }

        private static void UpdateTask(Guid projectId,
                                       Guid taskId,
                                       Guid jiraProjectIdCustomFieldUid,
                                       Guid jiraProjectNameCustomFieldUid,
                                       Guid jiraTaskIdCustomFieldUid,
                                       IEnumerable<ProjectDataSet.TaskRow> taskTable,
                                       IEnumerable<ProjectDataSet.TaskCustomFieldsRow> taskCustomFields,
                                       JiraTaskDto taskDto)
        {
            var task = taskTable.Single(x => x.TASK_UID == taskId);

            // ReSharper disable PossibleMultipleEnumeration
            var jiraProjectIdCustomFieldRow = taskCustomFields.Single(x => x.TASK_UID == taskId && x.MD_PROP_UID == jiraProjectIdCustomFieldUid);
            var jiraProjectNameCustomFieldRow = taskCustomFields.Single(x => x.TASK_UID == taskId && x.MD_PROP_UID == jiraProjectNameCustomFieldUid);
            var jiraTaskIdCustomFieldRow = taskCustomFields.Single(x => x.TASK_UID == taskId && x.MD_PROP_UID == jiraTaskIdCustomFieldUid);
            // ReSharper restore PossibleMultipleEnumeration

            MapTaskFields(projectId,
                          taskDto,
                          task,
                          jiraProjectIdCustomFieldRow,
                          jiraProjectNameCustomFieldRow,
                          jiraTaskIdCustomFieldRow);
        }

        private static void MapTaskFields(Guid projectId,
                                          JiraTaskDto taskDto,
                                          ProjectDataSet.TaskRow task,
                                          ProjectDataSet.TaskCustomFieldsRow jiraProjectIdCustomFieldRow,
                                          ProjectDataSet.TaskCustomFieldsRow jiraProjectNameCustomFieldRow,
                                          ProjectDataSet.TaskCustomFieldsRow jiraTaskIdCustomFieldRow)
        {
            task.PROJ_UID = projectId;
            task.TASK_NAME = taskDto.Name;
            task.TASK_DUR_FMT = (int)Task.DurationFormat.Day;
            task.TASK_START_DATE = taskDto.Created;
            task.TASK_FINISH_DATE = taskDto.DueDate;
            task.TASK_PRIORITY = taskDto.Priority;
            jiraProjectIdCustomFieldRow.TEXT_VALUE = taskDto.ProjectId.ToString();
            jiraProjectNameCustomFieldRow.TEXT_VALUE = taskDto.ProjectName;
            jiraTaskIdCustomFieldRow.TEXT_VALUE = taskDto.Id.ToString();
        }

        private static void AddToProject(Project projectClient, Guid projectId, ProjectDataSet projectDataSet)
        {
            AddToOrUpdateProject(projectClient,
                                 projectId,
                                 (jodId, sessionId, client) => client.QueueAddToProject(jodId, sessionId, projectDataSet, false));
        }

        private static void UpdateProject(Project projectClient, Guid projectId, ProjectDataSet projectDataSet)
        {
            AddToOrUpdateProject(projectClient,
                                 projectId,
                                 (jodId, sessionId, client) => client.QueueUpdateProject(jodId, sessionId, projectDataSet, false));
        }

        private static void AddToOrUpdateProject(Project projectClient, Guid projectId, Action<Guid, Guid, Project> action)
        {
            var sessionId = Guid.NewGuid();
            var jodId = Guid.NewGuid();

            try
            {
                projectClient.CheckOutProject(projectId, sessionId, string.Empty);
                action(jodId, sessionId, projectClient);
                projectClient.QueuePublish(jodId, projectId, true, string.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                projectClient.QueueCheckInProject(jodId, projectId, true, sessionId, "TasksImporter updates");
            }
        }
    }
}
