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
        public Pod(string ns) : this(ns, new Dictionary<string, string>()) { }
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
    public class Policy
    {
        public bool ingress;
        public string namespaceName;
        // in this class, one policy only select one group of pods
        public Dictionary<string, string> selectLabels;
        // ipblock selection is not supported
        public Selector[]? allowLabels;
        public Policy(string ns, Dictionary<string, string> select, Selector[] allows, bool ingress = true)
        {
            this.ingress = ingress;
            namespaceName = ns;
            selectLabels = select;
            allowLabels = allows;
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
