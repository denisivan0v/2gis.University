using System;
using System.Linq;
using DoubleGis.University.ProjectService;
using TasksImporter;
using TasksImporter.DTO;

namespace DoubleGis.University
{
    public static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        public static void Main()
        {
            var parser = new JiraQueryResultParser("AdsModel.xml");
            var jiraTaskDtos = parser.Parse();

            var psiServiceFactory = new PsiServiceClientFactory(new Uri("http://uk-erm-ps/pwa"));
            var projectClient = psiServiceFactory.CreateProjectClient();
            
            var project = GetProject(projectClient);

            var projectTasks = project.Task.ToDictionary(x => x.TASK_ID, x => x);
            foreach (var taskDto in jiraTaskDtos)
            {
                ProjectDataSet.TaskRow taskRow;
                if (!projectTasks.TryGetValue(taskDto.Id, out taskRow))
                {
                    AddNewTask(project.Task, taskDto);
                }
            }

            foreach (var taskRow in project.Task.AsEnumerable())
            {
                
            }
        }

        private static void AddNewTask(ProjectDataSet.TaskDataTable taskTable, JiraTaskDto taskDto)
        {
            var task = taskTable.NewTaskRow();
            task.TASK_UID = Guid.NewGuid();
        }

        private static ProjectDataSet GetProject(Project projectClient)
        {
            var projectList = projectClient.ReadProjectList();
            var projectId = projectList.Project.AsEnumerable()
                .Where(x => x.PROJ_NAME == "CorpUniversity")
                .Select(x => x.PROJ_UID)
                .SingleOrDefault();
            return projectId != Guid.Empty ? projectClient.ReadProject(projectId, DataStoreEnum.WorkingStore) : null;
        }
    }
}
