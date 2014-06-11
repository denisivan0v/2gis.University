using System;
using System.Collections.Generic;
using System.Data;
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
            projectDataSet = GetProject(ProjectName);
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
            using (var customFieldsClient = _psiServiceClientFactory.CreateCustomFieldsClient())
            {
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
        }

        public void MakeChangesInProjectServer(Guid projectId,
                                               ProjectDataSet projectDataSetToAdd,
                                               ProjectDataSet projectDataSetToUpdate)
        {
            if (projectDataSetToAdd.Task.Any())
            {
                AddToProject(projectId, projectDataSetToAdd);
            }

            var taskChanges = projectDataSetToUpdate.Task.GetChanges(DataRowState.Modified);
            var taskCustomFieldsChanges = projectDataSetToUpdate.TaskCustomFields.GetChanges(DataRowState.Modified);
            if ((taskChanges != null && taskChanges.Rows.Count != 0) ||
                (taskCustomFieldsChanges != null && taskCustomFieldsChanges.Rows.Count != 0))
            {
                UpdateProject(projectId, projectDataSetToUpdate);
            }
        }

        private ProjectDataSet GetProject(string projectName)
        {
            using (var projectClient = _psiServiceClientFactory.CreateProjectClient())
            {
                var projectList = projectClient.ReadProjectList();
                var projectId = projectList.Project
                                           .Where(x => x.PROJ_NAME == projectName)
                                           .Select(x => x.PROJ_UID)
                                           .SingleOrDefault();
                return projectId != Guid.Empty ? projectClient.ReadProject(projectId, DataStoreEnum.PublishedStore) : null;
            }
        }

        private void AddToProject(Guid projectId, ProjectDataSet projectDataSet)
        {
            AddToOrUpdateProject(projectId,
                                 (jodId, sessionId, client) => client.QueueAddToProject(jodId, sessionId, projectDataSet, false));
        }

        private void UpdateProject(Guid projectId, ProjectDataSet projectDataSet)
        {
            AddToOrUpdateProject(projectId,
                                 (jodId, sessionId, client) => client.QueueUpdateProject(jodId, sessionId, projectDataSet, false));
        }

        private void AddToOrUpdateProject(Guid projectId, Action<Guid, Guid, Project> action)
        {
            var sessionId = Guid.NewGuid();
            var jodId = Guid.NewGuid();

            using (var projectClient = _psiServiceClientFactory.CreateProjectClient())
            {
                try
                {
                    projectClient.CheckOutProject(projectId, sessionId, string.Empty);
                    action(jodId, sessionId, projectClient);
                    projectClient.QueuePublish(jodId, projectId, true, string.Empty);
                }
                catch (Exception ex)
                {
                    _errorsContainer.AppendFormat("Error occured while updating data using PSI. Details: {0}", ex);
                    throw;
                }
                finally
                {
                    projectClient.QueueCheckInProject(jodId, projectId, true, sessionId, "TasksImporter updates");
                }
            }
        }
    }
}