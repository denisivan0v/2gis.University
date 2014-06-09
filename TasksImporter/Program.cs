using System;
using System.Collections.Generic;
using System.Linq;

using DoubleGis.University.DTO;
using DoubleGis.University.ProjectService;

namespace DoubleGis.University
{
    public static class Program
    {
        private const string ProjectName = "CorpUniversity";
        private const string JiraProjectId = "JiraProjectId";
        private const string JiraProjectName = "JiraProjectName";

        public static void Main()
        {
            var parser = new JiraQueryResultParser("AdsModel.xml");
            var jiraTaskDtos = parser.Parse();

            var psiServiceFactory = new PsiServiceClientFactory(new Uri("http://uk-erm-ps/pwa"));
            var projectClient = psiServiceFactory.CreateProjectClient();

            var projectDataSet = PsiUtility.GetProject(projectClient, ProjectName);
            if (projectDataSet == null)
            {
                Console.WriteLine("Project {0} cannot be found", ProjectName);
                return;
            }

            var project = projectDataSet.Project.SingleOrDefault();
            if (project == null)
            {
                Console.WriteLine("Project {0} cannot be found", ProjectName);
                return;
            }

            var customFieldsClient = psiServiceFactory.CreateCustomFieldsClient();
            var customFieldDataSet = customFieldsClient.ReadCustomFields(string.Empty, false);
            var jiraProjectIdCustomField = customFieldDataSet.CustomFields.SingleOrDefault(x => x.MD_PROP_NAME == JiraProjectId);
            var jiraProjectNameCustomField = customFieldDataSet.CustomFields.SingleOrDefault(x => x.MD_PROP_NAME == JiraProjectName);
            if (jiraProjectIdCustomField == null || jiraProjectNameCustomField == null)
            {
                Console.WriteLine("Custom fields {0} and/or {1} cannot be found", JiraProjectId, JiraProjectName);
                return;
            }

            var projectDataSetToAdd = new ProjectDataSet();
            var projectDataSetToUpdate = new ProjectDataSet();
            var projectTasks = projectDataSet.Task.ToDictionary(x => x.TASK_ID, x => x);
            foreach (var taskDto in jiraTaskDtos)
            {
                ProjectDataSet.TaskRow taskRow;
                if (!projectTasks.TryGetValue(taskDto.Id, out taskRow))
                {
                    AddNewTask(project.PROJ_UID,
                               jiraProjectIdCustomField.MD_PROP_UID,
                               jiraProjectNameCustomField.MD_PROP_UID,
                               projectDataSetToAdd.Task,
                               projectDataSetToAdd.TaskCustomFields,
                               taskDto);
                }
                else
                {
                    UpdateTask(project.PROJ_UID,
                               taskRow.TASK_UID,
                               jiraProjectIdCustomField.MD_PROP_UID,
                               jiraProjectNameCustomField.MD_PROP_UID,
                               projectDataSetToUpdate.Task,
                               projectDataSetToUpdate.TaskCustomFields,
                               taskDto);
                }
            }

            AddToProject(projectClient, project.PROJ_UID, projectDataSetToAdd);
            UpdateProject(projectClient, project.PROJ_UID, projectDataSetToUpdate);
        }

        private static void AddNewTask(Guid projectId,
                                       Guid jiraProjectIdCustomFieldUid,
                                       Guid jiraProjectNameCustomFieldUid,
                                       ProjectDataSet.TaskDataTable taskTable,
                                       ProjectDataSet.TaskCustomFieldsDataTable taskCustomFieldsTable,
                                       JiraTaskDto taskDto)
        {
            var task = taskTable.NewTaskRow();
            task.TASK_UID = Guid.NewGuid();

            var jiraProjectIdCustomFieldRow = taskCustomFieldsTable.NewTaskCustomFieldsRow();
            jiraProjectIdCustomFieldRow.CUSTOM_FIELD_UID = Guid.NewGuid();

            var jiraProjectNameCustomFieldRow = taskCustomFieldsTable.NewTaskCustomFieldsRow();
            jiraProjectNameCustomFieldRow.CUSTOM_FIELD_UID = Guid.NewGuid();

            MapTaskFields(projectId,
                          jiraProjectIdCustomFieldUid,
                          jiraProjectNameCustomFieldUid,
                          taskDto,
                          task,
                          jiraProjectIdCustomFieldRow,
                          jiraProjectNameCustomFieldRow);

            taskTable.AddTaskRow(task);
            taskCustomFieldsTable.AddTaskCustomFieldsRow(jiraProjectIdCustomFieldRow);
            taskCustomFieldsTable.AddTaskCustomFieldsRow(jiraProjectNameCustomFieldRow);
        }

        private static void UpdateTask(Guid projectId,
                                       Guid taskId,
                                       Guid jiraProjectIdCustomFieldUid,
                                       Guid jiraProjectNameCustomFieldUid,
                                       IEnumerable<ProjectDataSet.TaskRow> taskTable,
                                       IEnumerable<ProjectDataSet.TaskCustomFieldsRow> taskCustomFields,
                                       JiraTaskDto taskDto)
        {
            var task = taskTable.Single(x => x.TASK_UID == taskId);
            
            var taskCustomFieldsRows = taskCustomFields as ProjectDataSet.TaskCustomFieldsRow[] ?? taskCustomFields.ToArray();
            var jiraProjectIdCustomFieldRow = taskCustomFieldsRows.Single(x => x.TASK_UID == taskId && x.MD_PROP_UID == jiraProjectIdCustomFieldUid);
            var jiraProjectNameCustomFieldRow = taskCustomFieldsRows.Single(x => x.TASK_UID == taskId && x.MD_PROP_UID == jiraProjectNameCustomFieldUid);

            MapTaskFields(projectId,
                          jiraProjectIdCustomFieldUid,
                          jiraProjectNameCustomFieldUid,
                          taskDto,
                          task,
                          jiraProjectIdCustomFieldRow,
                          jiraProjectNameCustomFieldRow);
        }

        private static void MapTaskFields(Guid projectId,
                                          Guid jiraProjectIdCustomFieldUid,
                                          Guid jiraProjectNameCustomFieldUid,
                                          JiraTaskDto taskDto,
                                          ProjectDataSet.TaskRow task,
                                          ProjectDataSet.TaskCustomFieldsRow jiraProjectIdCustomFieldRow,
                                          ProjectDataSet.TaskCustomFieldsRow jiraProjectNameCustomFieldRow)
        {
            task.PROJ_UID = projectId;
            task.TASK_ID = taskDto.Id;
            task.TASK_NAME = taskDto.Name;
            task.TASK_START_DATE = taskDto.Created;
            task.TASK_FINISH_DATE = taskDto.DueDate;
            task.TASK_PRIORITY = taskDto.Priority;

            jiraProjectIdCustomFieldRow.PROJ_UID = projectId;
            jiraProjectIdCustomFieldRow.TASK_UID = task.TASK_UID;
            jiraProjectIdCustomFieldRow.MD_PROP_UID = jiraProjectIdCustomFieldUid;
            jiraProjectIdCustomFieldRow.TEXT_VALUE = taskDto.ProjectId.ToString();

            jiraProjectNameCustomFieldRow.PROJ_UID = projectId;
            jiraProjectNameCustomFieldRow.TASK_UID = task.TASK_UID;
            jiraProjectNameCustomFieldRow.MD_PROP_UID = jiraProjectNameCustomFieldUid;
            jiraProjectNameCustomFieldRow.TEXT_VALUE = taskDto.ProjectName;
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
                projectClient.QueuePublish(jodId, sessionId, true, string.Empty);
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
