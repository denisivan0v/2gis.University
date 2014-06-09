using System;
using System.Security.Principal;
using System.ServiceModel;
using DoubleGis.University.CustomFieldsService;
using DoubleGis.University.LookupTableService;
using DoubleGis.University.ObjectLinkProviderService;
using DoubleGis.University.ProjectService;
using DoubleGis.University.ResourceService;
using DoubleGis.University.StatusingService;

namespace DoubleGis.University
{
    internal class PsiServiceClientFactory
    {
        private const int MaxSize = 500000000;
        private const string PsiServicesRouter = "/_vti_bin/PSI/ProjectServer.svc";

        private readonly BasicHttpBinding _binding;
        private readonly EndpointAddress _endpointAddress;

        private ResourceClient _resourceClient;
        private ProjectClient _projectClient;
        private CustomFieldsClient _customFieldsClient;
        private LookupTableClient _lookupTableClient;
        private ObjectLinkProviderClient _objectLinkProviderClient;
        private StatusingClient _statusingClient;

        public PsiServiceClientFactory(Uri pwaUri)
        {
            var pwaUrl = pwaUri.Scheme + Uri.SchemeDelimiter + pwaUri.Host + ":" + pwaUri.Port + pwaUri.AbsolutePath;

            _binding = pwaUri.Scheme.Equals(Uri.UriSchemeHttps)
                          ? new BasicHttpBinding(BasicHttpSecurityMode.Transport)
                          : new BasicHttpBinding(BasicHttpSecurityMode.TransportCredentialOnly);

            _binding.Name = "basicHttpConf";
            _binding.SendTimeout = TimeSpan.MaxValue;
            _binding.MaxReceivedMessageSize = MaxSize;
            _binding.ReaderQuotas.MaxNameTableCharCount = MaxSize;
            _binding.MessageEncoding = WSMessageEncoding.Text;
            _binding.Security.Transport.ClientCredentialType = HttpClientCredentialType.Ntlm;

            // The endpoint address is the ProjectServer.svc router for all public PSI calls.
            _endpointAddress = new EndpointAddress(pwaUrl + PsiServicesRouter);
        }

        public ResourceClient CreateResourceClient()
        {
            if (_resourceClient == null)
            {
                _resourceClient = new ResourceClient(_binding, _endpointAddress);
                SetCredenticalProperties<ResourceClient, Resource>(_resourceClient);
            }

            return _resourceClient;
        }

        public ProjectClient CreateProjectClient()
        {
            if (_projectClient == null)
            {
                _projectClient = new ProjectClient(_binding, _endpointAddress);
                SetCredenticalProperties<ProjectClient, Project>(_projectClient);
            }

            return _projectClient;
        }

        public CustomFieldsClient CreateCustomFieldsClient()
        {
            if (_customFieldsClient == null)
            {
                _customFieldsClient = new CustomFieldsClient(_binding, _endpointAddress);
                SetCredenticalProperties<CustomFieldsClient, CustomFields>(_customFieldsClient);
            }

            return _customFieldsClient;
        }

        public LookupTableClient CreateLookupTableClient()
        {
            if (_lookupTableClient == null)
            {
                _lookupTableClient = new LookupTableClient(_binding, _endpointAddress);
                SetCredenticalProperties<LookupTableClient, LookupTable>(_lookupTableClient);
            }

            return _lookupTableClient;
        }

        public ObjectLinkProviderClient CreateObjectLinkProviderClient()
        {
            if (_objectLinkProviderClient == null)
            {
                _objectLinkProviderClient = new ObjectLinkProviderClient(_binding, _endpointAddress);
                SetCredenticalProperties<ObjectLinkProviderClient, ObjectLinkProvider>(_objectLinkProviderClient);
            }

            return _objectLinkProviderClient;
        }

        public StatusingClient CreateStatusingClient()
        {
            if (_statusingClient == null)
            {
                _statusingClient = new StatusingClient(_binding, _endpointAddress);
                SetCredenticalProperties<StatusingClient, Statusing>(_statusingClient);
            }

            return _statusingClient;
        }

        private static void SetCredenticalProperties<TClient, TInterface>(TClient client) 
            where TClient : ClientBase<TInterface> 
            where TInterface : class
        {
            client.ChannelFactory.Credentials.Windows.AllowedImpersonationLevel = TokenImpersonationLevel.Delegation;
        }
    }
}