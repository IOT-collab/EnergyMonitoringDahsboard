using System.Collections.Generic;
using IEC61850.Common;

namespace example2.Explorer
{
    public class RelayModel
    {
        public List<LogicalDeviceModel> LogicalDevices { get; }
            = new List<LogicalDeviceModel>();
    }

    public class LogicalDeviceModel
    {
        public string Name { get; set; }

        public List<LogicalNodeModel> LogicalNodes { get; }
            = new List<LogicalNodeModel>();
    }

    public class LogicalNodeModel
    {
        public string Name { get; set; }

        public List<DataObjectModel> DataObjects { get; }
            = new List<DataObjectModel>();
    }

    public class DataObjectModel
    {
        public string Name { get; set; }

        public List<DataAttributeModel> Attributes { get; }
            = new List<DataAttributeModel>();
    }

    public class DataAttributeModel
    {
        public string Name { get; set; }

        public FunctionalConstraint FC { get; set; }

        public MmsType Type { get; set; }

        public int Size { get; set; }

        public List<DataAttributeModel> Children { get; }
            = new List<DataAttributeModel>();
    }
}