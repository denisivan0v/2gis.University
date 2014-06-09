using System;
using System.Linq;

using DoubleGis.University.ProjectService;

namespace DoubleGis.University
{
    public static class PsiUtility
    {
        public static ProjectDataSet GetProject(Project projectClient, string projectName)
        {
            var projectList = projectClient.ReadProjectList();
            var projectId = projectList.Project
                                       .Where(x => x.PROJ_NAME == projectName)
                                       .Select(x => x.PROJ_UID)
                                       .SingleOrDefault();
            return projectId != Guid.Empty ? projectClient.ReadProject(projectId, DataStoreEnum.PublishedStore) : null;
        }
    }
}