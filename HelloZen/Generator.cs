using System;
using System.Collections.Generic;
using System.Text;
using ZenLib;

namespace HelloZen
{
    public class Generator
    {
        public Pod[] pods;
        public Namespace[] namespaces;
        public Policy[] policies;
        public uint podLabelMax;
        public uint nsLabelMax;
        public uint keyMax;
        public uint valueMax;
        public uint selectLabelMax;
        public uint allowNsMax;
        public uint allowLabelMax;
        public Generator(uint podNum, uint nsNum, uint policyNum, uint podLblMax = 5, uint nsLblMax = 5, uint kMax = 5,
            uint valMax = 10, uint selectMax = 3, uint allowLbMax = 3, uint allowNsMax = 3)
        {
            pods = new Pod[podNum];
            namespaces = new Namespace[nsNum];
            policies = new Policy[policyNum];
            // generate keys and values
            string[] keys = new string[kMax];
            for (int i = 0; i < keys.Length; ++i)
            {
                keys[i] = "key" + i.ToString();
            }
            // ad-hoc values
            string[] values = new string[valMax*3];
            for (int i = 0; i < values.Length; ++i)
            {
                values[i] = "value" + i.ToString();
            }
            for (int i = 0; i < keys.Length; ++i)
            {
                namespaces[i] = new Namespace(keys[i]);
            }
            // TODO: assign keys and values to labels 
            var random = new Random();
            int initKey = random.Next(keys.Length);
            int initVal = random.Next(values.Length);
            for (int i = 0; i < pods.Length; ++i)
            {
                pods[i] = new Pod(namespaces[i].name);
                for (int j = 0; j < podLabelMax; ++j)
                {
                    pods[i].addLabel(keys[initKey], values[initVal]);
                    initKey = (++initKey) % keys.Length;
                    initVal = (++initVal) % values.Length;
                }
            }
            for (int i = 0; i < namespaces.Length; ++i)
            {
                for (int j = 0; j < nsLabelMax; ++j)
                {
                    namespaces[i].addLabel(keys[initKey], values[initVal]);
                    initKey = (++initKey) % keys.Length;
                    initVal = (++initVal) % values.Length;
                }
            }
            // TODO: generate policy : ns and label based
            // num of ns and label be randomly generated
            // ad-hoc: half ns based and half label based
            for (int i = 0; i < policies.Length; ++i)
            {
                int selectedLabel = random.Next((int)selectLabelMax+1);
                Dictionary<string, string> selected = new Dictionary<string, string>();
                for (int j = 0; j < selectedLabel; ++j)
                {
                    selected.TryAdd(keys[initKey], values[initVal]);
                    initKey = (++initKey) % keys.Length;
                    initVal = (++initVal) % values.Length;
                }
                policies[i] = new Policy(namespaces[i].name, selected, null);
            }

            for (int i = 0; i < policies.Length/2; ++i)
            {
                int nsAllow = random.Next((int)allowNsMax+1);
                Selector[] selectors = new Selector[1];
                Selector selector = new Selector(new Dictionary<string, string>(), null);
                for (int j = 0; j < nsAllow; ++j)
                {
                    selector.allowedNs.TryAdd(keys[initKey], values[initVal]);
                    initKey = (++initKey) % keys.Length;
                    initVal = (++initVal) % values.Length;
                }
                selectors[1] = selector;
                policies[i].allowLabels = selectors;
            }

            for (int i = policies.Length/2; i < policies.Length; ++i)
            {
                int labelAllow = random.Next((int)allowLabelMax+1);
                Selector[] selectors = new Selector[1];
                Selector selector = new Selector(null, new Dictionary<string, string>());
                for (int j = 0; j < labelAllow; ++j)
                {
                    selector.allowedLabel.TryAdd(keys[initKey], values[initVal]);
                    initKey = (++initKey) % keys.Length;
                    initVal = (++initVal) % values.Length;
                }
                selectors[1] = selector;
                policies[i].allowLabels = selectors;
            }
        }
    };

    public class Verifier
    {
        public Zen<bool> Allowed(Zen<Pod> p1, Zen<Pod> p2)
        {
            return true;
        }
    };
}
