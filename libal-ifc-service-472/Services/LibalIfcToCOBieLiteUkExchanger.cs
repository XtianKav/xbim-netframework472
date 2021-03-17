using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xbim.CobieLiteUk;
using Xbim.CobieLiteUk.FilterHelper;
using Xbim.Common;
using Xbim.Ifc4.Interfaces;
using XbimExchanger.IfcHelpers;
using XbimExchanger.IfcToCOBieLiteUK;

namespace libal.Services
{
    class LibalIfcToCOBieLiteUkExchanger : IfcToCOBieLiteUkExchanger
    {
        internal CoBieLiteUkHelper Helper;

        public LibalIfcToCOBieLiteUkExchanger(IModel source, List<Facility> target, ReportProgressDelegate reportProgress = null, OutPutFilters filter = null, string configFile = null, EntityIdentifierMode extId = EntityIdentifierMode.GloballyUniqueIds, SystemExtractionMode sysMode = SystemExtractionMode.System | SystemExtractionMode.Types)
           : base(source, target, reportProgress, filter, configFile, extId, sysMode)
        {
            ReportProgress.Progress = reportProgress;
            Helper = new CoBieLiteUkHelper(source, ReportProgress, filter, configFile, extId, sysMode);
        }
        public Xbim.CobieLiteUk.System mapSystem(IIfcSystem ifcSystem, Xbim.CobieLiteUk.System target)
        {
            target.ExternalEntity = Helper.ExternalEntityName(ifcSystem);
            target.ExternalId = Helper.ExternalEntityIdentity(ifcSystem);
            target.AltExternalId = ifcSystem.GlobalId;
            target.ExternalSystem = Helper.GetCreatingApplication(ifcSystem);
            target.Name = ifcSystem.Name;
            target.Description = ifcSystem.Description;
            target.Categories = Helper.GetCategories(ifcSystem);
            target.Attributes = Helper.GetAttributes(ifcSystem);

            IEnumerable<IIfcDocumentSelect> documentEnumerable = Helper.GetDocuments(ifcSystem);

            List<Document> documents = new List<Document>();
            List<string> UsedNames = new List<string>();
            foreach (var ifcDocumentSelect in documentEnumerable)
            {
                if (ifcDocumentSelect is IIfcDocumentReference)
                {
                    var ifcDocumentReference = ifcDocumentSelect as IIfcDocumentReference;
                    if (ifcDocumentReference != null)
                    {
                        Document document = ConvertToDocument(ifcDocumentReference, ifcDocumentReference.ReferencedDocument, UsedNames);
                        documents.Add(document);
                    }
                }
            }

            target.Documents = documents;

            return target;
        }

        private Document ConvertToDocument(IIfcDocumentReference ifcDocumentReference, IIfcDocumentInformation ifcDocumentInformation, List<string> UsedNames)
        {

            string name = GetName(ifcDocumentInformation) ?? GetName(ifcDocumentReference);
            //fail to get from IfcDocumentReference, so try assign a default
            if (string.IsNullOrEmpty(name))
            {
                name = "Document";
            }
            //check for duplicates, if found add a (#) => "DocName(1)", if none return name unchanged
            name = Helper.GetNextName(name, UsedNames);

            var document = new Document();
            document.Name = name;

            document.Categories = (ifcDocumentInformation != null) && (!string.IsNullOrEmpty(ifcDocumentInformation.Purpose)) ? new List<Category>(new[] { new Category { Code = ifcDocumentInformation.Purpose } }) : null;

            document.ApprovalBy = (ifcDocumentInformation != null) && (!string.IsNullOrEmpty(ifcDocumentInformation.IntendedUse)) ? ifcDocumentInformation.IntendedUse : null; //once fixed

            document.Stage = (ifcDocumentInformation != null) && (!string.IsNullOrEmpty(ifcDocumentInformation.Scope)) ? ifcDocumentInformation.Scope : null;

            document.Directory = GetFileDirectory(ifcDocumentReference);
            document.File = GetFileName(ifcDocumentReference);

            document.ExternalSystem = null;
            document.ExternalEntity = ifcDocumentReference.GetType().Name;
            document.ExternalId = null;

            document.Description = (ifcDocumentInformation != null) && (!string.IsNullOrEmpty(ifcDocumentInformation.Description)) ? ifcDocumentInformation.Description.ToString() : null;
            document.Reference = ifcDocumentInformation.Identification;

            UsedNames.Add(document.Name);
            return document;
        }

        /// <summary>
        /// Get the file directory/location
        /// </summary>
        /// <param name="ifcDocumentReference">Document Reference Object</param>
        /// <returns>string</returns>
        private string GetFileDirectory(IIfcDocumentReference ifcDocumentReference)
        {
            if (ifcDocumentReference == null)
                return null;

            if (!string.IsNullOrEmpty(ifcDocumentReference.Location))
            {
                return ifcDocumentReference.Location;
            }
            return null;
        }

        private string GetFileName(IIfcDocumentReference ifcDocumentReference)
        {
            if (ifcDocumentReference == null)
                return null;
            if (!string.IsNullOrEmpty(ifcDocumentReference.Name))
            {
                return ifcDocumentReference.Name;
            }

            if (!string.IsNullOrEmpty(ifcDocumentReference.Location))
            {
                try
                {
                    return Path.GetFileName(ifcDocumentReference.Location);
                }
                catch (Exception) //if exception just return the stored string
                {
                    return ifcDocumentReference.Location;
                }
            }
            return null;
        }

        /// <summary>
        /// Get Name from IfcDocumentInformation
        /// </summary>
        /// <param name="ifcDocumentInformation">Document Information Object</param>
        /// <returns>string or null</returns>
        private string GetName(IIfcDocumentInformation ifcDocumentInformation)
        {
            if (ifcDocumentInformation == null)
                return null;

            if (!string.IsNullOrEmpty(ifcDocumentInformation.Name))
            {
                return ifcDocumentInformation.Name;
            }

            return null;
        }
        private string GetName(IIfcDocumentReference ifcDocumentReference)
        {
            if (ifcDocumentReference == null)
                return null;

            if (!string.IsNullOrEmpty(ifcDocumentReference.Name))
            {
                return ifcDocumentReference.Name;
            }
            if (!string.IsNullOrEmpty(ifcDocumentReference.Location))
            {
                return Path.GetFileNameWithoutExtension(ifcDocumentReference.Location);
            }
            //we ignore  ItemReference, "which refers to a system interpretable position within the document" from http://www.buildingsmart-tech.org/ifc/IFC2x3/TC1/html/ifcexternalreferenceresource/lexical/ifcdocumentreference.htm


            return null;
        }
    }
}
