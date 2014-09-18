#region License

// /*
// Transformalize - Replicate, Transform, and Denormalize Your Data...
// Copyright (C) 2013 Dale Newman
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
// */

#endregion

using System.Configuration;

namespace Transformalize.Configuration {
    public class TemplateConfigurationElement : ConfigurationElement {
        private const string CONTENT_TYPE = "content-type";
        private const string FILE = "file";
        private const string PARAMETERS = "parameters";
        private const string ACTIONS = "actions";
        private const string NAME = "name";
        private const string CACHE = "cache";
        private const string ENABLED = "enabled";
        private const string CONDITIONAL = "conditional";
        private const string ENGINE = "engine";

        [ConfigurationProperty(CONDITIONAL, IsRequired = false, DefaultValue = false)]
        public bool Conditional {
            get { return (bool)this[CONDITIONAL]; }
            set { this[CONDITIONAL] = value; }
        }

        [ConfigurationProperty(NAME, IsRequired = true)]
        public string Name {
            get { return this[NAME] as string; }
            set { this[NAME] = value; }
        }

        [ConfigurationProperty(ENGINE, IsRequired = false, DefaultValue = "razor")]
        public string Engine {
            get { return this[ENGINE] as string; }
            set { this[ENGINE] = value; }
        }

        [ConfigurationProperty(FILE, IsRequired = true)]
        public string File {
            get { return this[FILE] as string; }
            set { this[FILE] = value; }
        }

        [ConfigurationProperty(CACHE, IsRequired = false, DefaultValue = false)]
        public bool Cache {
            get { return (bool)this[CACHE]; }
            set { this[CACHE] = value; }
        }

        [ConfigurationProperty(ENABLED, IsRequired = false, DefaultValue = true)]
        public bool Enabled {
            get { return (bool)this[ENABLED]; }
            set { this[ENABLED] = value; }
        }

        [ConfigurationProperty(CONTENT_TYPE, IsRequired = false, DefaultValue = "raw")]
        public string ContentType {
            get { return this[CONTENT_TYPE] as string; }
            set { this[CONTENT_TYPE] = value; }
        }

        [ConfigurationProperty(PARAMETERS)]
        public ParameterElementCollection Parameters {
            get { return this[PARAMETERS] as ParameterElementCollection; }
        }

        [ConfigurationProperty(ACTIONS)]
        public ActionElementCollection Actions {
            get { return this[ACTIONS] as ActionElementCollection; }
        }

        public override bool IsReadOnly() {
            return false;
        }
    }
}