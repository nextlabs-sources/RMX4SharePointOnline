using System;
using System.Configuration;

namespace RmxForSPOWeb.Common.ConfigFileUtility
{
    [ConfigurationCollection(typeof(KeyValueElement))]
    public class KeyValueElementCollection : ConfigurationElementCollection
    {
        public KeyValueElementCollection() : base(StringComparer.OrdinalIgnoreCase) { }
       
        new public KeyValueElement this[string name]
        {
            get { return (KeyValueElement)base.BaseGet(name); }
            set
            {
                if (base.Properties.Contains(name)) base[name] = value;
                else base.BaseAdd(value);
            }
        }
       
        protected override ConfigurationElement CreateNewElement()
        {
            return new KeyValueElement();
        }
       
        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((KeyValueElement)element).Key;
        }
    }
}