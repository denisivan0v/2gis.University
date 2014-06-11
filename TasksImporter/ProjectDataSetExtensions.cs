using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;

using DoubleGis.University.CustomFieldsService;
using DoubleGis.University.DTO;
using DoubleGis.University.ProjectService;

using Microsoft.Office.Project.Server.Library;

namespace DoubleGis.University
{
    public static class ProjectDataSetExtensions
    {
        private static readonly string[] TaskCompletedStatuses = { "Closed", "Resolved" };

        public static IDictionary<int, ProjectDataSet.TaskRow> GetExistingTasksByJiraKeys(this ProjectDataSet projectDataSet,
                                                                                          CustomFieldDataSet.CustomFieldsRow jiraTaskIdCustomField,
                                                                                          out Guid projectId)
        {
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

        public static void AddNewTask(this ProjectDataSet projectDataSet,
                                      Guid projectId,
                                      Guid jiraProjectIdCustomFieldUid,
                                      Guid jiraProjectNameCustomFieldUid,
                                      Guid jiraTaskIdCustomFieldUid,
                                      JiraTaskDto taskDto)
        {
            var task = projectDataSet.Task.NewTaskRow();
            task.TASK_UID = Guid.NewGuid();

            var jiraProjectIdCustomFieldRow = projectDataSet.TaskCustomFields.NewTaskCustomFieldsRow();
            jiraProjectIdCustomFieldRow.CUSTOM_FIELD_UID = Guid.NewGuid();
            jiraProjectIdCustomFieldRow.PROJ_UID = projectId;
            jiraProjectIdCustomFieldRow.TASK_UID = task.TASK_UID;
            jiraProjectIdCustomFieldRow.MD_PROP_UID = jiraProjectIdCustomFieldUid;

            var jiraProjectNameCustomFieldRow = projectDataSet.TaskCustomFields.NewTaskCustomFieldsRow();
            jiraProjectNameCustomFieldRow.CUSTOM_FIELD_UID = Guid.NewGuid();
            jiraProjectNameCustomFieldRow.PROJ_UID = projectId;
            jiraProjectNameCustomFieldRow.TASK_UID = task.TASK_UID;
            jiraProjectNameCustomFieldRow.MD_PROP_UID = jiraProjectNameCustomFieldUid;

            var jiraTaskIdCustomFieldRow = projectDataSet.TaskCustomFields.NewTaskCustomFieldsRow();
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

            projectDataSet.Task.AddTaskRow(task);
            projectDataSet.TaskCustomFields.AddTaskCustomFieldsRow(jiraProjectIdCustomFieldRow);
            projectDataSet.TaskCustomFields.AddTaskCustomFieldsRow(jiraProjectNameCustomFieldRow);
            projectDataSet.TaskCustomFields.AddTaskCustomFieldsRow(jiraTaskIdCustomFieldRow);
        }

        public static void UpdateTask(this ProjectDataSet projectDataSet,
                                      Guid projectId,
                                      Guid taskId,
                                      Guid jiraProjectIdCustomFieldUid,
                                      Guid jiraProjectNameCustomFieldUid,
                                      Guid jiraTaskIdCustomFieldUid,
                                      JiraTaskDto taskDto)
        {
            var task = projectDataSet.Task.Single(x => x.TASK_UID == taskId);

            // ReSharper disable PossibleMultipleEnumeration
            var jiraProjectIdCustomFieldRow = projectDataSet.TaskCustomFields.Single(x => x.TASK_UID == taskId && x.MD_PROP_UID == jiraProjectIdCustomFieldUid);
            var jiraProjectNameCustomFieldRow = projectDataSet.TaskCustomFields.Single(x => x.TASK_UID == taskId && x.MD_PROP_UID == jiraProjectNameCustomFieldUid);
            var jiraTaskIdCustomFieldRow = projectDataSet.TaskCustomFields.Single(x => x.TASK_UID == taskId && x.MD_PROP_UID == jiraTaskIdCustomFieldUid);
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
            task.AddPosition = (int)Task.AddPositionType.Last;
            if (TaskCompletedStatuses.Contains(taskDto.Status))
            {
                task.TASK_PHY_PCT_COMP = 100;
            }

            jiraProjectIdCustomFieldRow.TEXT_VALUE = taskDto.ProjectId.ToString();
            jiraProjectNameCustomFieldRow.TEXT_VALUE = taskDto.ProjectName;
            jiraTaskIdCustomFieldRow.TEXT_VALUE = taskDto.Id.ToString();
        }
    }
}