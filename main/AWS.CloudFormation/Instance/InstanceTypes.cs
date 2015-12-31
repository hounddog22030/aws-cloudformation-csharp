﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace AWS.CloudFormation.Instance
{
    internal class InstanceTypesConverter : StringEnumConverter
    {
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            InstanceTypes valueAsInstanceTypes = (InstanceTypes)value;
            switch (valueAsInstanceTypes)
            {
                case InstanceTypes.T2Nano:
                    writer.WriteValue(Instance.T2Nano);
                    break;
                case InstanceTypes.T2Micro:
                    writer.WriteValue(Instance.T2Micro);
                    break;
                case InstanceTypes.T2Small:
                    writer.WriteValue(Instance.T2Small);
                    break;
                case InstanceTypes.M4Xlarge:
                    writer.WriteValue(Instance.T2Small);
                    break;

                default:
                    throw new InvalidEnumArgumentException();
            }
        }
    }

    [JsonConverter(typeof(InstanceTypesConverter))]
    public enum InstanceTypes
    {
        None,
        T2Nano,
        T2Micro,
        T2Small,
        M4Xlarge
    }
}
