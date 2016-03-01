using System;
using System.Configuration;
using System.Net;
using System.Security.Principal;
using System.ServiceModel;
using Tridion.ContentManager.CoreService.Client;

namespace Tridion.Extensions.CoreserviceReference
{
    public class CoreServiceClent : IDisposable
    {
        private readonly SessionAwareCoreServiceClient _client;

        public CoreServiceClent()
        {
            _client = new SessionAwareCoreServiceClient("wsHttp");
            _client.Impersonate(WindowsIdentity.GetCurrent().Name);
        }

        public CoreServiceClent(string bindingName, string domainName,string userName, string passWord)
        {
            //priority, if binding name existins in config file
            if (!string.IsNullOrEmpty(bindingName))
            {
                _client = new SessionAwareCoreServiceClient(bindingName);
            }
            else
            {
                //default binding set to wsHttp
                _client = new SessionAwareCoreServiceClient("wsHttp");
            }

            //priority, if custom domain, username, password exists in cofig file
            if (!string.IsNullOrEmpty(domainName) && !string.IsNullOrEmpty(userName) && !string.IsNullOrEmpty(passWord))
            {
                string usernameFull = domainName + "\\" + userName;
                var credentials = new NetworkCredential(usernameFull, passWord);
                _client.ChannelFactory.Credentials.Windows.ClientCredential = credentials;
            }
            else
            {
                //default set to current windows login
                _client.Impersonate(WindowsIdentity.GetCurrent().Name);
            }
        }

        public SessionAwareCoreServiceClient GetClient
        {
            get
            {
                return _client;
            }
        }

        public void Dispose()
        {
            if (_client.State == CommunicationState.Faulted)
            {
                _client.Abort();
            }
            else
            {
                _client.Close();
            }
        }

        public UserData GetCurrentUser()
        {
            return _client.GetCurrentUser();
        }

    }
}
