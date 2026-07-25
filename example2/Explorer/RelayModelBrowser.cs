using System;
using System.Collections.Generic;
using IEC61850.Client;
using IEC61850.Common;

namespace example2.Explorer
{
    public class RelayModelBrowser
    {
        private readonly IedConnection _con;

        public RelayModelBrowser(IedConnection con)
        {
            _con = con;
        }

        public RelayModel BuildModel()
        {
            RelayModel model = new RelayModel();

            List<string> logicalDevices = _con.GetServerDirectory(false);

            foreach (string ldName in logicalDevices)
            {
                LogicalDeviceModel ld = new LogicalDeviceModel();

                ld.Name = ldName;

                model.LogicalDevices.Add(ld);

                BuildLogicalDevice(ld);
            }

            return model;
        }

        private void BuildLogicalDevice(LogicalDeviceModel ld)
        {
            List<string> logicalNodes =
                _con.GetLogicalDeviceDirectory(ld.Name);

            foreach (string lnName in logicalNodes)
            {
                LogicalNodeModel ln = new LogicalNodeModel();

                ln.Name = lnName;

                ld.LogicalNodes.Add(ln);

                BuildLogicalNode(ld, ln);
            }
        }

        private void BuildLogicalNode(      LogicalDeviceModel ld,           LogicalNodeModel ln)
        {
            string lnReference = $"{ld.Name}/{ln.Name}";

            List<string> dataObjects =
                _con.GetLogicalNodeDirectory(
                    lnReference,
                    ACSIClass.ACSI_CLASS_DATA_OBJECT);

            foreach (string doName in dataObjects)
            {
                DataObjectModel dataObject =
                    new DataObjectModel();

                dataObject.Name = doName;

                ln.DataObjects.Add(dataObject);

                BuildDataObject(
                    lnReference,
                    dataObject);
            }
        }

        private void BuildDataObject(            string lnReference,            DataObjectModel dataObject)
        {
            List<string> dataDirectory =
                _con.GetDataDirectoryFC(
                    lnReference + "." + dataObject.Name);

            foreach (string entry in dataDirectory)
            {
                string attributeName =
                    ObjectReference.getElementName(entry);

                FunctionalConstraint fc =
                    ObjectReference.getFC(entry);

                string reference =
                    lnReference + "." +
                    dataObject.Name + "." +
                    attributeName;

                MmsVariableSpecification spec =
                    _con.GetVariableSpecification(
                        reference,
                        fc);

                DataAttributeModel da =
                    CreateAttribute(spec,
                                    attributeName,
                                    fc);

                dataObject.Attributes.Add(da);
            }
        }

        private DataAttributeModel CreateAttribute(   MmsVariableSpecification spec,   string name,  FunctionalConstraint fc)
        {
            DataAttributeModel attribute =
                new DataAttributeModel();

            attribute.Name = name;
            attribute.FC = fc;
            attribute.Type = spec.GetType();
            attribute.Size = spec.Size();

            if (spec.GetType() == MmsType.MMS_STRUCTURE)
            {
                foreach (MmsVariableSpecification child in spec)
                {
                    attribute.Children.Add(
                        CreateAttribute(
                            child,
                            child.GetName(),
                            fc));
                }
            }

            if (spec.GetType() == MmsType.MMS_ARRAY)
            {
                MmsVariableSpecification child =
                    spec.getArrayElementType();

                attribute.Children.Add(
                    CreateAttribute(
                        child,
                        "ArrayElement",
                        fc));
            }

            return attribute;
        }
    }
}