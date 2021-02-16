using System;
using System.Collections.Generic;


namespace HelloZen
{
    public class Pod
    {
        public string nameSpace;
        public Dictionary<string, string> labels;
        public Pod(string ns, Dictionary<string, string> lbs)
        {
            nameSpace = ns;
            labels = lbs;
        }
        public Pod(string ns="default") : this(ns, new Dictionary<string, string>()) { }
        public bool addLabel(string key, string val)
        {
            return labels.TryAdd(key, val);
        }
        public bool removeLabel(string key)
        {
            return labels.Remove(key);
        }
        public void updateLabel(string key, string newVal)
        {
            labels[key] = newVal;
        }
    };
    // one real policy could be split into several Policy objects
    public class Policy
    {
        public bool ingress;
        public string ns;
        public Dictionary<string, string>? selectLabels;
        // null means no condition applies to this field
        // empty dictionary means no selection allowed -- this only happens when set default deny policy
        //   default deny policy should have allowNS and allowLabel be empty at the same time
        public Dictionary<string, string>? allowNamespaces;
        public Dictionary<string, string>? allowLabels;
        public Policy(string ns="default", 
            Dictionary<string, string> sLabels=null, 
            Dictionary<string, string> aNs=null, 
            Dictionary<string, string> aLabels=null, 
            bool ingress=true)
        {
            this.ingress = ingress;
            this.ns = ns;
            selectLabels = sLabels;
            allowNamespaces = aNs;
            allowLabels = aLabels;
        }
    };
    public class Selector
    {
        // namespace + label select from/to pods
        // labels is empty then select all pods in the namespace
        public Dictionary<string, string>? allowedNs;
        public Dictionary<string, string>? allowedLabel;
        public Selector(Dictionary<string, string> ns, Dictionary<string, string> lbs)
        {
            allowedNs = ns;
            allowedLabel = lbs;
        }
    };
    public class Namespace
    {
        public string name;
        public Dictionary<string, string> labels;
        public Namespace(string name, Dictionary<string, string> lbs)
        {
            this.name = name;
            labels = lbs;
        }
        public Namespace(string name) : this(name, new Dictionary<string, string>()) { }
        public bool addLabel(string key, string val)
        {
            return labels.TryAdd(key, val);
        }
        public bool removeLabel(string key)
        {
            return labels.Remove(key);
        }
        public void updateLabel(string key, string newVal)
        {
            labels[key] = newVal;
        }
    };
}
