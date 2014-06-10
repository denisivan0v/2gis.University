using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using DoubleGis.University.CustomFieldsService;
using DoubleGis.University.DTO;
using DoubleGis.University.ProjectService;

namespace DoubleGis.University
{
    public class TaskImportManager
    {
        private const string ProjectName = "CorpUniversity";
        private const string JiraProjectId = "JiraProjectId";
        private const string JiraProjectName = "JiraProjectName";
        private const string JiraTaskId = "JiraTaskId";

        private readonly PsiServiceClientFactory _psiServiceClientFactory;
        private readonly string _issuesFileName;

        private readonly StringBuilder _errorsContainer = new StringBuilder();

        public TaskImportManager(Uri pwaUri, string issuesFileName)
        {
            _psiServiceClientFactory = new PsiServiceClientFactory(pwaUri);
            _issuesFileName = issuesFileName;
        }

        public string GetAllErrors()
        {
            return _errorsContainer.ToString();
        }

        public bool TryParseSource(out IEnumerable<JiraTaskDto> taskDtos, out IDictionary<int, IEnumerable<int>> taskRelations)
        {
            try
            {
                var parser = new JiraQueryResultParser(_issuesFileName);
                taskDtos = parser.ReadTasks();
                taskRelations = parser.ReadTaskRelations();

                return true;
            }
            catch (Exception ex)
            {
                _errorsContainer.AppendFormat("Error occured while parsing source. Details: {0}", ex);
                taskDtos = null;
                taskRelations = null;
                return false;
            }
        }

        public bool TryGetProjectDataSet(out ProjectDataSet projectDataSet)
        {
            var projectClient = _psiServiceClientFactory.CreateProjectClient();

            projectDataSet = PsiUtility.GetProject(projectClient, ProjectName);
            if (projectDataSet == null)
            {
                _errorsContainer.AppendFormat("Project {0} cannot be found", ProjectName);
                return false;
            }

            var project = projectDataSet.Project.SingleOrDefault();
            if (project == null)
            {
                _errorsContainer.AppendFormat("Project {0} cannot be found within project data set", ProjectName);
                return false;
            }

            return true;
        }

        public bool TryReadCustomFields(out CustomFieldDataSet.CustomFieldsRow jiraProjectIdCustomField,
                                        out CustomFieldDataSet.CustomFieldsRow jiraProjectNameCustomField,
                                        out CustomFieldDataSet.CustomFieldsRow jiraTaskIdCustomField)
        {
            var customFieldsClient = _psiServiceClientFactory.CreateCustomFieldsClient();
            var customFieldDataSet = customFieldsClient.ReadCustomFields(string.Empty, false);
            jiraProjectIdCustomField = customFieldDataSet.CustomFields.SingleOrDefault(x => x.MD_PROP_NAME == JiraProjectId);
            jiraProjectNameCustomField = customFieldDataSet.CustomFields.SingleOrDefault(x => x.MD_PROP_NAME == JiraProjectName);
            jiraTaskIdCustomField = customFieldDataSet.CustomFields.SingleOrDefault(x => x.MD_PROP_NAME == JiraTaskId);

            if (jiraProjectIdCustomField != null && jiraProjectNameCustomField != null && jiraTaskIdCustomField != null)
            {
                return true;
            }

            _errorsContainer.AppendFormat("Custom fields {0} and/or {1} and/or {2} cannot be found", JiraProjectId, JiraProjectName, jiraTaskIdCustomField);
            return false;
        }

        public IDictionary<int, ProjectDataSet.TaskRow> GetExistingTasksByJiraKeys(CustomFieldDataSet.CustomFieldsRow jiraTaskIdCustomField, out Guid projectId)
        {
            ProjectDataSet projectDataSet;
            if (!TryGetProjectDataSet(out projectDataSet))
            {
                projectId = Guid.Empty;
                return null;
            }

            projectId = projectDataSet.Project.Single().PROJ_UID;

            return (from task in projectDataSet.Task
                    join customField in projectDataSet.TaskCustomFields.Where(x => x.MD_PROP_UID == jiraTaskIdCustomField.MD_PROP_UID)
                        on task.TASK_UID equals customField.TASK_UID
                    select new
                        {
                            JiraTaskId = customField != null ? int.Parse(customField.TEXT_VALUE) : 0,
                            Task = task
                        })
                .ToDictionary(x => x.JiraTaskId, x => x.Task);
        }
    }
}