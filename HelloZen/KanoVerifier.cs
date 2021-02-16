using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace HelloZen
{
    public class KanoVerifier
    {
        /*
         currently, namespace selectors are not implemented,
         assuming all pods(selected and allowed) need to be in the same namespace as the policy's namespace
         TODO:
         1. Policy only selects allow pods with namespaces (based on ns labels)
                namespace label filter
                pod namespace check with 'labeled ns'
         2. Policy selects allow pods with namespaces + pod labels (2-step filter required)
                step 1
                apply pod label filter with chosen pods in step 1
         3. Ingress + Egress (corner case self reachability)
            a. pod selection cant block pod itself
            b. ns-involved selection is not clear
         4. All allowed and all denied
                special indicator in policy datastructure
         5. IPBlock filter is not supported
        */
        
        // create namespace - pods matrix (sparse)
        public Dictionary<string, BitArray> createNSMatrix(Pod[] pods)
        {
            var n = pods.Length;
            Dictionary<string, BitArray> matrix = new Dictionary<string, BitArray>();
            for (int i = 0; i < n; ++i)
            {
                if (!matrix.ContainsKey(pods[i].nameSpace)) matrix[pods[i].nameSpace] = new BitArray(n);
                matrix[pods[i].nameSpace].Set(i, true);
            }
            return matrix;
        }
        public Dictionary<string, BitArray> createNSLabelMatrix(Namespace[] namespaces)
        {
            var n = namespaces.Length;
            Dictionary<string, BitArray> nsMatrix = new Dictionary<string, BitArray>();
            for (int i = 0; i < n; ++i)
            {
                var labels = namespaces[i].labels;
                foreach (var label in labels)
                {
                    if (!nsMatrix.ContainsKey(label.Key)) nsMatrix[label.Key] = new BitArray(n);
                    nsMatrix[label.Key].Set(i, true);
                }
            }
            return nsMatrix;
        }
        // Reachability matrix = Ingress matrix && Egress matrix 
        //   split policy into ingress and egress and call this function twice
        public BitArray[] createReachMatrix(Pod[] pods, Policy[] policies, Namespace[] namespaces)
        {
            var n = pods.Length;
            var m = policies.Length;
            // nsname - podbit map
            var nsMatrix = createNSMatrix(pods);
            BitArray[] reachMatrix = new BitArray[n];
            for (int i = 0; i < n; ++i)
            {
                reachMatrix[i] = new BitArray(n, true);
                reachMatrix[i].And(nsMatrix[pods[i].nameSpace]);
            }

            // create label hashmap
            Dictionary<string, BitArray> labelHash = new Dictionary<string, BitArray>();
            for (int i = 0; i < n; ++i)
            {
                var labels = pods[i].labels;
                foreach (var label in labels)
                {
                    if (!labelHash.ContainsKey(label.Key)) labelHash[label.Key] = new BitArray(n);
                    labelHash[label.Key].Set(i, true);
                }
            }

            // record pods have been selected
            BitArray podsSelected = new BitArray(n);
            // label - nsbit map
            Dictionary<string, BitArray> nsLabelMatrix = createNSLabelMatrix(namespaces);
            for (int i = 0; i < m; ++i)
            {
                BitArray selectSet = new BitArray(n, true);
                // policy has a namespace while this namespace has no pods, this policy is void
                if (!nsMatrix.ContainsKey(policies[i].ns)) continue;
                // else select pods only in the namespace
                selectSet.And(nsMatrix[policies[i].ns]);

                var selectLabels = policies[i].selectLabels;
                if (selectLabels != null)
                    foreach (var label in selectLabels)
                    {
                        if (!labelHash.ContainsKey(label.Key))
                        {
                            selectSet.SetAll(false);
                            break;
                        }
                        else selectSet = selectSet.And(labelHash[label.Key]);
                    }

                BitArray allowNsSet = new BitArray(namespaces.Length, true);
                BitArray allowSet = new BitArray(n);
                var allowNs = policies[i].allowNamespaces;
                // if allowNS == null, only default namespace is allowed
                if (allowNs != null)
                {
                    // get all namespaces match ns allowed label key
                    foreach (var nsLabel in allowNs)
                    {
                        if (!nsLabelMatrix.ContainsKey(nsLabel.Key))
                        {
                            allowNsSet.SetAll(false);
                            break;
                        }
                        allowNsSet = allowNsSet.And(nsLabelMatrix[nsLabel.Key]);
                    }
                    // check ns label value with allowed label value
                    for (int j = 0; j < namespaces.Length; ++j)
                    {
                        if (allowNsSet.Get(j))
                        {
                            foreach (var label in allowNs)
                            {
                                // if label value is not matched, this ns is not allowed
                                if (!label.Value.Equals(namespaces[j].labels.GetValueOrDefault(label.Key)))
                                {
                                    allowNsSet.Set(j, false);
                                    break;
                                }
                            }
                        }
                        // if this ns is allowed, all the pods in the ns are candidates
                        if (allowNsSet.Get(j))
                        {
                            allowSet.Or(nsMatrix[namespaces[j].name]);
                        }
                    }
                }
                else allowSet.Or(nsMatrix[policies[i].ns]);

                var allowLabels = policies[i].allowLabels;
                // if allowLabel == null, all labels in allowed ns are allowed
                if (allowLabels != null)
                {
                    foreach (var label in allowLabels)
                    {
                        if (!labelHash.ContainsKey(label.Key))
                        {
                            allowSet.SetAll(false);
                            break;
                        }
                        allowSet = allowSet.And(labelHash[label.Key]);
                    }
                }

                for (int j = 0; j < n; ++j)
                {
                    if (allowSet.Get(j) && allowLabels != null)
                    {
                        // check label value
                        foreach (var label in allowLabels)
                        {
                            // if label value is not matched, this pod is not allowed
                            if (!label.Value.Equals(pods[j].labels.GetValueOrDefault(label.Key)))
                            {
                                allowSet.Set(j, false);
                                break;
                            }
                        }
                    }
                }

                // TODO: unselected pods should have all traffic allowed
                //       only set selected pods to traffic denied and udpate based on it
                for (int j = 0; j < n; ++j)
                {
                    if (selectSet.Get(j) && selectLabels != null)
                    {
                        // check label value
                        foreach (var label in selectLabels)
                        {
                            if (!label.Value.Equals(pods[j].labels.GetValueOrDefault(label.Key)))
                            {
                                // if label value is not matched, this pod is not selected
                                selectSet.Set(j, false);
                                break;
                            }
                        }
                    }
                    if (selectSet.Get(j))
                    {
                        // if this pod has not been selected before, set all its reach bits to 0
                        if (!podsSelected.Get(j))
                        {
                            podsSelected.Set(j, true);
                            reachMatrix[j].SetAll(false);
                        }
                        reachMatrix[j] = reachMatrix[j].Or(allowSet);
                    }
                }

            }
            return reachMatrix;
        }
        // TODO: violation checks
    }
}
