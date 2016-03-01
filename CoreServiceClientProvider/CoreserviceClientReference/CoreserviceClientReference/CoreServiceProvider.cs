using System;
using System.Security.Principal;
using System.ServiceModel;
using System.Configuration;
using System.Net;
using Tridion.ContentManager.CoreService.Client;

namespace Tridion.Extensions.CoreserviceReference
{
    public class CoreServiceProvider
    {
        public static SessionAwareCoreServiceClient CreateCoreService()
        {
            var bindingName = System.Configuration.ConfigurationManager.AppSettings["BindingName"];
            //var bindingName = (!string.IsNullOrEmpty(bindingNameConfig)) ? bindingNameConfig : "wsHttp";
            
            //If custom username and password provided, will have priority
            string customDN = ConfigurationManager.AppSettings["DomainName"] ?? string.Empty;
            string customUN = ConfigurationManager.AppSettings["UserName"] ?? string.Empty;
            string customPWD = ConfigurationManager.AppSettings["PassWord"] ?? string.Empty;
           
            if (!string.IsNullOrEmpty(bindingName) && !string.IsNullOrEmpty(customUN) && !string.IsNullOrEmpty(customPWD))
            {
                return new CoreServiceClent(bindingName,customDN,customUN,customPWD).GetClient;
            }

            return new CoreServiceClent().GetClient;           
        }


        //some client provided methods, you can extend available other methods you want

        public string GetApiVersionNumber()
        {
            using (var client = CreateCoreService())
            {
                return client.GetApiVersion();
            }
        }

        public IdentifiableObjectData[] GetSearchResults(SearchQueryData filter)
        {
            using (var client = CreateCoreService())
            {
                return client.GetSearchResults(filter);
            }
        }

        public IdentifiableObjectData Read(string id)
        {
            using (var client = CreateCoreService())
            {
                try
                {
                    return client.Read(id, new ReadOptions());
                }
                catch (Exception ex)
                {
                    return null;
                    throw ex;
                }
            }
        }

        public static void SaveApplicationData(string subjectId, ApplicationData[] applicationData)
        {
            using (var client = CreateCoreService())
            {
                try
                {
                    client.SaveApplicationData(subjectId, applicationData);
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
        }

        public static ApplicationData ReadApplicationData(string subjectId, string applicationId)
        {
            using (var client = CreateCoreService())
            {
                try
                {
                    return client.ReadApplicationData(subjectId, applicationId);
                }
                catch (Exception ex)
                {
                    return null;
                    throw ex;
                }
            }
        }

        public static T Get<T>(string id, ReadOptions readOptions = null) where T : class
        {
            object obj = null;
            using (var client = CreateCoreService())
            {
                try
                {
                    if (readOptions == null)
                    {
                        readOptions = new ReadOptions { LoadFlags = LoadFlags.Expanded };
                    }
                    obj = client.Read(id, readOptions);
                }
                catch (Exception ex)
                {
                }
            };
            return obj as T;
        }

        public static T TryGet<T>(string id, ReadOptions readOptions = null) where T : class
        {
            try
            {
                return string.IsNullOrWhiteSpace(id) ? null : Get<T>(id, readOptions);
            }
            catch (Exception exception)
            {
                if (exception.Message.Contains("Invalid URI:")) return null;
                throw;
            }
        }
    }
}
