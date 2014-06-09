using System;
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
            
            var projectTasks = projectDataSet.Task.ToDictionary(x => x.TASK_ID, x => x);
            foreach (var taskDto in jiraTaskDtos)
            {
                ProjectDataSet.TaskRow taskRow;
                if (!projectTasks.TryGetValue(taskDto.Id, out taskRow))
                {
                    AddNewTask(project.PROJ_UID,
                               jiraProjectIdCustomField.MD_PROP_UID,
                               jiraProjectNameCustomField.MD_PROP_UID,
                               projectDataSet.Task,
                               projectDataSet.TaskCustomFields,
                               taskDto);
                }
            }

            // UpdateProject(projectClient, project, projectDataSet);
        }

        private static void AddNewTask(Guid projectId,
                                       Guid jiraProjectIdCustomFieldUid,
                                       Guid jiraProjectNameCustomFieldUid,
                                       ProjectDataSet.TaskDataTable taskTable,
                                       ProjectDataSet.TaskCustomFieldsDataTable taskCustomFieldsTable,
                                       JiraTaskDto taskDto)
        {
            var task = taskTable.NewTaskRow();

            task.PROJ_UID = projectId;
            task.TASK_UID = Guid.NewGuid();
            task.TASK_ID = taskDto.Id;
            task.TASK_NAME = taskDto.Name;
            task.TASK_START_DATE = taskDto.Created;
            task.TASK_FINISH_DATE = taskDto.DueDate;
            task.TASK_PRIORITY = taskDto.Priority;

            taskTable.AddTaskRow(task);

            var jiraProjectIdCustomFieldRow = taskCustomFieldsTable.NewTaskCustomFieldsRow();
            jiraProjectIdCustomFieldRow.TASK_UID = task.TASK_UID;
            jiraProjectIdCustomFieldRow.CUSTOM_FIELD_UID = jiraProjectIdCustomFieldUid;
            jiraProjectIdCustomFieldRow.TEXT_VALUE = taskDto.ProjectId.ToString();

            var jiraProjectNameCustomFieldRow = taskCustomFieldsTable.NewTaskCustomFieldsRow();
            jiraProjectNameCustomFieldRow.TASK_UID = task.TASK_UID;
            jiraProjectNameCustomFieldRow.CUSTOM_FIELD_UID = jiraProjectNameCustomFieldUid;
            jiraProjectNameCustomFieldRow.TEXT_VALUE = taskDto.ProjectName;

            taskCustomFieldsTable.AddTaskCustomFieldsRow(jiraProjectIdCustomFieldRow);
        }

        private static void UpdateProject(Project projectClient, ProjectDataSet.ProjectRow project, ProjectDataSet projectDataSet)
        {
            var sessionId = Guid.NewGuid();
            var jodId = Guid.NewGuid();
            projectClient.CheckOutProject(project.PROJ_UID, sessionId, string.Empty);
            projectClient.QueueAddToProject(jodId, sessionId, projectDataSet, false);
        }
    }
}
