using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Xml.Linq;
using CoreserviceClientReference.HelperModels;
using System.Xml;
using Tridion.ContentManager.CoreService.Client;

namespace Tridion.Extensions.CoreserviceReference.Helpers
{
    public class CoreServiceHelper
    {
        private static SessionAwareCoreServiceClient cs_client;


        /// <summary>
        /// Create Keywirds in a given category
        /// </summary>
        /// <param name="model">CategoryModel</param>
        /// <param name="CategoryId">tcm:xx-yy-zz</param>
        /// <returns></returns>
        public static string CreateKeywordsInCategory(CategoryModel model, string CategoryId)
        {
            var result = "true";
            cs_client = CoreServiceProvider.CreateCoreService();

            try
            {
                // open the category that is already created in Tridion
                CategoryData category = (CategoryData)cs_client.Read(CategoryId, null);

                var xmlCategoryKeywords = cs_client.GetListXml(CategoryId, new KeywordsFilterData());
                var keywordAny = xmlCategoryKeywords.Elements()
                                      .Where(element => element.Attribute("Key").Value == model.Key)
                                          .Select(element => element.Attribute("ID").Value)
                                              .Select(id => (KeywordData)cs_client.Read(id, null)).FirstOrDefault();

                if (keywordAny == null)
                {
                    // create a new keyword
                    KeywordData keyword = (KeywordData)cs_client.GetDefaultData(Tridion.ContentManager.CoreService.Client.ItemType.Keyword, category.Id, new ReadOptions());
                    // set the id to 0 to notify Tridion that it is new
                    keyword.Id = "tcm:0-0-0";
                    keyword.Title = model.Title;
                    keyword.Key = model.Key;
                    keyword.Description = model.Description;
                    keyword.IsAbstract = false;

                    // create the keyword
                    cs_client.Create(keyword, null);
                    cs_client.Close();
                }
            }
            catch (Exception ex)
            {
                result = "Error: " + ex.Message;
            }
            finally
            {
                cs_client.Close();
            }

            return result;
        }

        /// <summary>
        /// Gets list of CT's along with the associated schema & view
        /// </summary>
        /// <param>none</param>
        /// <returns></returns>
        public static string GetAllItemsInPublication(string pubID)
        {
            RepositoryItemsFilterData filter = SetPageFilterCriterias();
            StringBuilder sb = new StringBuilder();
            cs_client = CoreServiceProvider.CreateCoreService();
            try
            {
                IdentifiableObjectData[] pages = cs_client.GetList(pubID, filter);

                foreach (IdentifiableObjectData iod in pages)
                {
                    PageData pageData = cs_client.Read(iod.Id, new ReadOptions()) as PageData;

                    sb.AppendLine("Page: " + pageData.LocationInfo.Path);
                    sb.AppendLine("PT: " + pageData.PageTemplate.Title);
                    sb.AppendLine("PM: " + pageData.MetadataSchema.Title);

                    foreach (ComponentPresentationData cpd in pageData.ComponentPresentations)
                    {
                        sb.AppendLine("");
                        sb.AppendLine("CP: " + cpd.Component.Title);

                        ComponentData cp = (ComponentData)cs_client.Read(cpd.Component.IdRef, new ReadOptions());
                        sb.AppendLine("CS: " + cp.Schema.Title);

                        sb.AppendLine("CT: " + cpd.ComponentTemplate.Title);
                        ComponentTemplateData ct = (ComponentTemplateData)cs_client.Read(cpd.ComponentTemplate.IdRef, new ReadOptions());
                        sb.AppendLine("CM: " + ct.MetadataSchema.Title);

                        // load the schema
                        var schemaFields = cs_client.ReadSchemaFields(cp.Schema.IdRef, true, new ReadOptions());

                        // build a  Fields object from it
                        var fields = Fields.ForContentOf(schemaFields, cp);

                        // let's first quickly list all values of all fields
                        foreach (var field in fields)
                        {
                            if (field.GetType() == typeof(EmbeddedSchemaFieldDefinitionData))
                            {

                            }
                            if (field.GetType() == typeof(ComponentLinkFieldDefinitionData))
                            {

                            }
                            if (field.GetType() == typeof(EmbeddedSchemaFieldDefinitionData))
                            {

                            }
                        }
                    }

                    //blank line for readability
                    sb.AppendLine("");
                    sb.AppendLine("");
                }
            }
            catch (Exception ex)
            {
                // throw ex;
            }
            finally
            {
                cs_client.Close();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Gets list of CT's along with the associated schema & view
        /// </summary>
        /// <param>none</param>
        /// <returns></returns>
        public static string GetTemplatesInPublication(string pubID)
        {
            //note: this runs for about 1 minute
            StringBuilder sb = new StringBuilder();
            string meta = string.Empty, ct = string.Empty, schema = string.Empty;
            byte[] data = null;
            MemoryStream stm = null; ;
            XDocument doc = null;

            try
            {
                cs_client = CoreServiceProvider.CreateCoreService();

                // get the Id of the publication to import into
                RepositoryItemsFilterData templateFilter = SetTemplateFilterCriterias();
                XElement templates = cs_client.GetListXml(pubID, templateFilter); ;

                foreach (XElement template in templates.Descendants())
                {
                    ComponentTemplateData t = (ComponentTemplateData)cs_client.Read(CheckAttributeValueOrEmpty(template, "ID"), null);

                    if (t.Metadata != "")
                    {
                        ct = t.Title;
                        data = Encoding.ASCII.GetBytes(t.Metadata);
                        stm = new MemoryStream(data, 0, data.Length);
                        doc = XDocument.Load(stm);
                        meta = doc.Root.Value;

                        if (t.RelatedSchemas.Count() > 0)
                        {
                            schema = t.RelatedSchemas[0].Title;
                        }
                        else
                        {
                            schema = "No Schema Found";
                        }

                        sb.AppendLine(ct + "|" + schema + "|" + meta);

                    }
                }
            }
            catch (Exception ex)
            {
                // throw ex;
            }
            finally
            {
                cs_client.Close();
            }

            return sb.ToString();
        }

        /// <summary>
        /// Get publishing targets for any given publication
        /// </summary>
        /// <param name="publicationID"></param>
        /// <returns></returns>
        public static Dictionary<string, List<string>> GetPublishingTargets(List<string> publicationIDs)
        {
            cs_client = CoreServiceProvider.CreateCoreService();
            Dictionary<string, List<string>> resultTargets = new Dictionary<string, List<string>>();
            var pubTargets = cs_client.GetSystemWideList(new PublicationTargetsFilterData());
            foreach (var publicationID in publicationIDs)
            {
                foreach (PublicationTargetData pubTargetdata in pubTargets)
                {
                    List<string> targetIds = new List<string>();
                    PublicationTargetData target = (PublicationTargetData)cs_client.Read(pubTargetdata.Id, new ReadOptions());
                    LinkToPublicationData[] pubDataItems = target.Publications;
                    foreach (LinkToPublicationData publicationData in pubDataItems)
                    {
                        if (publicationData.IdRef == publicationID)
                        {
                            if (resultTargets.ContainsKey(publicationData.Title))
                            {
                                resultTargets[publicationData.Title].Add(pubTargetdata.Id);
                            }

                            else
                            {
                                targetIds.Add(pubTargetdata.Id);
                                resultTargets.Add(publicationData.Title, targetIds);
                            }
                        }
                    }

                }
            }

            return resultTargets;
        }

        /// <summary>
        /// Create bundle from given schema
        /// </summary>
        /// <param name="schemaID"></param>
        /// <param name="folderId"></param>
        /// <returns></returns>
        public static VirtualFolderData CreateBundle(string schemaID, string folderId)
        {
            cs_client = CoreServiceProvider.CreateCoreService();
            SchemaData bundleSchema = (SchemaData)cs_client.Read(schemaID, new ReadOptions());

            SchemaData virtualFolderTypeSchema =
                cs_client.GetVirtualFolderTypeSchema(@"http://www.sdltridion.com/ContentManager/Bundle");

            VirtualFolderData bundle = new VirtualFolderData()
            {
                Id = "tcm:0-0-0",
                Title = "Test Bundle Title",
                Description = "Test Bundle Description",
                MetadataSchema = new LinkToSchemaData()
                {
                    IdRef = bundleSchema.Id
                },
                TypeSchema = new LinkToSchemaData()
                {
                    IdRef = virtualFolderTypeSchema.Id
                },
                LocationInfo = new LocationInfo()
                {
                    OrganizationalItem = new LinkToOrganizationalItemData()
                    {
                        IdRef = folderId
                    }
                }
            };
            bundle = (VirtualFolderData)cs_client.Create(bundle, new ReadOptions());
            return bundle;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="folderID"></param>
        /// <param name="recursive"></param>
        /// <param name="srcSchemaID"></param>
        /// <param name="destSchemaID"></param>
        public static string UpdateSchemaForComponent(string folderID, bool recursive, string srcSchemaID, string destSchemaID, bool isMultiMediaComp)
        {
            cs_client = CoreServiceProvider.CreateCoreService();
            StringBuilder sb = new StringBuilder();
            FolderData folder = cs_client.Read(folderID, null) as FolderData;
            SchemaData schema = cs_client.Read(destSchemaID, null) as SchemaData;
            XNamespace ns = schema.NamespaceUri;
            XElement items = cs_client.GetListXml(folder.Id, SetComponenetFilterCriterias(isMultiMediaComp));
            List<ComponentData> failedItems = new List<ComponentData>();

            foreach (XElement item in items.Elements())
            {
                ComponentData component = cs_client.Read(item.Attribute("ID").Value, null) as ComponentData;

                if (!component.Schema.IdRef.Equals(srcSchemaID))
                {
                    // If the component is not of the schmea that we want to change from, do nothing...
                    return "";
                }

                if (component.Schema.IdRef.Equals(schema.Id))
                {
                    // If the component already has this schema, don't do anything.
                    return "";
                }

                component = cs_client.TryCheckOut(component.Id, new ReadOptions()) as ComponentData;


                if (component.IsEditable.Value)
                {
                    component.Schema.IdRef = destSchemaID;
                    component.Metadata = new XElement(ns + "Metadata").ToString();
                    cs_client.Save(component, null);
                    cs_client.CheckIn(component.Id, null);
                }
                else
                {
                    sb.AppendLine("Schema Can not be updated for: " + component.Id);
                    sb.AppendLine("");
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Sets up the template filter
        /// </summary>
        /// <param>none</param>
        /// <returns></returns>
        private static RepositoryItemsFilterData SetPageFilterCriterias()
        {
            RepositoryItemsFilterData templateFilter = new RepositoryItemsFilterData();
            List<ItemType> types = new List<ItemType>();
            types.Add(ItemType.Page);
            templateFilter.Recursive = true;
            templateFilter.BaseColumns = ListBaseColumns.Extended;
            templateFilter.ItemTypes = types.ToArray();

            return templateFilter;
        }

        /// <summary>
        /// Helper method for XElement attributes
        /// </summary>
        /// <param name="element"></param>
        /// <param name="attibutename"></param>
        /// <returns></returns>
        private static string CheckAttributeValueOrEmpty(XElement element, string attributeName)
        {
            XAttribute xAttribute = element.Attribute(attributeName);
            if (xAttribute != null)
            {
                return xAttribute.Value;
            }
            return "";
        }

        /// <summary>
        /// Sets up the template filter
        /// </summary>
        /// <param>none</param>
        /// <returns></returns>
        private static RepositoryItemsFilterData SetTemplateFilterCriterias()
        {
            RepositoryItemsFilterData templateFilter = new RepositoryItemsFilterData();
            List<ItemType> types = new List<ItemType>();
            types.Add(ItemType.Page);
            templateFilter.Recursive = true;
            templateFilter.BaseColumns = ListBaseColumns.Extended;
            templateFilter.ItemTypes = types.ToArray();
            return templateFilter;
        }

        /// <summary>
        /// Sets up the component filter
        /// </summary>
        /// <param>none</param>
        /// <returns></returns>
        private static OrganizationalItemItemsFilterData SetComponenetFilterCriterias(bool ismultimediaComp)
        {
            OrganizationalItemItemsFilterData filter = new OrganizationalItemItemsFilterData();
            filter.ItemTypes = new ItemType[] { ItemType.Component };

            //if multimedia comoponent
            if (ismultimediaComp)
                filter.ComponentTypes = new ComponentType[] { ComponentType.Multimedia };
            filter.Recursive = true;

            return filter;
        }
    }

}

