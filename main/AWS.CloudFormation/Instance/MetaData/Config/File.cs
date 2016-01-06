﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AWS.CloudFormation.Common;
using Newtonsoft.Json;

namespace AWS.CloudFormation.Instance.Metadata.Config
{
    public class ConfigFile : CloudFormationDictionary
    {
        public ConfigFile(Instance resource) : base(resource)
        {
            Content = this.Add("content", new CloudFormationDictionary(resource));
        }

        public CloudFormationDictionary Content { get; }

        public string Source
        {
            get
            {
                if (this.ContainsKey("source"))
                {
                    return this["source"] as string;
                }
                return null;
            }
            set
            {
                if (this.ContainsKey("source"))
                {
                    this["source"] = value;
                }
                else
                {
                    this.Add("source", value);
                }
            }
        }

        public string Authentication
        {
            get
            {
                if (this.ContainsKey("authentication"))
                {
                    return this["authentication"] as string;
                }
                return null;
            }
            set
            {
                if (this.ContainsKey("authentication"))
                {
                    this["authentication"] = value;
                }
                else
                {
                    this.Add("authentication", value);
                }
            }
        }
    }
}
