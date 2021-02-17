using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System;
using HelloZen;

namespace HelloZenTest
{
    [TestClass]
    public class KanoVerifierTest
    {
        [TestMethod]
        public void NsPodsMapTests()
        {
            KanoVerifier verifier = new KanoVerifier();
            Pod p1 = new Pod("default");
            Pod p2 = new Pod("ns1");
            Pod p3 = new Pod("ns2");
            Pod p4 = new Pod("ns2");
            var output = verifier.createNSMatrix(new Pod[]{p1, p2, p3, p4});
            var actual = new Dictionary<string, BitArray>() 
            { 
                { "default", new BitArray(new bool[4] {true, false, false, false}) } ,
                { "ns1", new BitArray(new bool[4] {false, true, false, false}) } ,
                { "ns2", new BitArray(new bool[4] {false, false, true, true}) } ,
            };
            Assert.IsTrue(new DictionaryComparer<string, BitArray>(new BitArrayComparer()).Equals(output, actual));
        }
        // 1. select all
        // 2. allow pods (current namespace)
        // 3. allow ns 
        // 4. allow ns + pods
        // 5. allow all
        // 6. deny all
        // 7?. invalid input
        [TestMethod]
        public void ReachMatrixTests_DefaultNS()
        {
            Pod[] pods = new Pod[4]{new Pod(), new Pod(), new Pod(), new Pod("ns1")};
            pods[0].addLabel("k0", "v0");
            pods[1].addLabel("k1", "v1");
            pods[2].addLabel("k2", "v2");
            pods[3].addLabel("k3", "v3");

            Namespace[] namespaces = new Namespace[2] {new Namespace("default"), new Namespace("ns1")};
            Policy[] polices = new Policy[3] { new Policy(), new Policy(), new Policy() };
            polices[0].selectLabels = new Dictionary<string, string>() { {"k0", "v0" } };
            polices[0].allowLabels = new Dictionary<string, string>() { {"k1","v1"} };

            polices[1].selectLabels = new Dictionary<string, string>() { {"k0", "v0" } };
            polices[1].allowLabels = new Dictionary<string, string>() { {"k3","v3"} };

            polices[2].selectLabels = new Dictionary<string, string>() { {"k3", "v3" } };
            polices[2].allowLabels = new Dictionary<string, string>() { {"k2","v2"} };

            KanoVerifier verifier = new KanoVerifier();
            var reachMatrix = verifier.createReachMatrix(pods, polices, namespaces);
            Assert.AreEqual(reachMatrix.Length, 4, "reach matrix should be 4*4");
            BitArrayComparer bComp = new BitArrayComparer();
            Assert.IsTrue(bComp.Equals(reachMatrix[0], new BitArray(new bool[4] { false, true, false, false})), "pod0 has wrong reachability");
            for (int i = 1; i < 3; ++i)
                Assert.IsTrue(bComp.Equals(reachMatrix[i], new BitArray(new bool[4] { true, true, true, false})), String.Format("pod{0} has wrong reachability", i));
            Assert.IsTrue(bComp.Equals(reachMatrix[3], new BitArray(new bool[4] { false, false, false, true})), "pod3 has wrong reachability");
        }
        [TestMethod]
        public void ReachMatrixTest_NSOnly()
        {
            Pod[] pods = new Pod[4]{new Pod(), new Pod(), new Pod("ns1"), new Pod("ns1")};
            pods[0].addLabel("k0", "v0");
            pods[1].addLabel("k1", "v1");
            pods[2].addLabel("k2", "v2");
            pods[3].addLabel("k3", "v3");

            Namespace[] namespaces = new Namespace[2] {new Namespace("default"), new Namespace("ns1")};
            namespaces[0].addLabel("k0", "v0");
            namespaces[1].addLabel("k1", "v1");
            Policy[] polices = new Policy[] { new Policy(), new Policy("ns1")};
            polices[0].selectLabels = new Dictionary<string, string>() { {"k0", "v0" } };
            polices[0].allowNamespaces = new Dictionary<string, string>() { { "k1", "v1" } };
            polices[1].selectLabels = new Dictionary<string, string>() { {"k2", "v2" } };
            polices[1].allowNamespaces = new Dictionary<string, string>() { { "k0", "v0" } };

            KanoVerifier verifier = new KanoVerifier();
            var reachMatrix = verifier.createReachMatrix(pods, polices, namespaces);
            Assert.AreEqual(reachMatrix.Length, 4, "reach matrix should be 4*4");
            BitArrayComparer bComp = new BitArrayComparer();
            Assert.IsTrue(bComp.Equals(reachMatrix[0], new BitArray(new bool[4] { false, false, true, true})), "pod0 has wrong reachability");
            Assert.IsTrue(bComp.Equals(reachMatrix[1], new BitArray(new bool[4] { true, true, false, false})), "pod1 has wrong reachability");
            Assert.IsTrue(bComp.Equals(reachMatrix[2], new BitArray(new bool[4] { true, true, false, false})), "pod2 has wrong reachability");
            Assert.IsTrue(bComp.Equals(reachMatrix[3], new BitArray(new bool[4] { false, false, true, true})), "pod3 has wrong reachability");
        }
        [TestMethod]
        public void ReachMatrixTest_NSPod()
        {
            Pod[] pods = new Pod[4]{new Pod(), new Pod(), new Pod("ns1"), new Pod("ns1")};
            pods[0].addLabel("k0", "v0");
            pods[1].addLabel("k1", "v1");
            pods[2].addLabel("k2", "v2");
            pods[3].addLabel("k3", "v3");

            Namespace[] namespaces = new Namespace[2] {new Namespace("default"), new Namespace("ns1")};
            namespaces[0].addLabel("k0", "v0");
            namespaces[1].addLabel("k1", "v1");
            Policy[] polices = new Policy[] { new Policy(), new Policy("ns1") };
            polices[0].selectLabels = new Dictionary<string, string>() { {"k0", "v0" } };
            polices[0].allowNamespaces = new Dictionary<string, string>() { { "k1", "v1" } };
            polices[0].allowLabels = new Dictionary<string, string>() { { "k2", "v2" } };

            polices[1].selectLabels = new Dictionary<string, string>() { {"k3", "v3" } };
            polices[1].allowNamespaces = new Dictionary<string, string>() { { "k0", "v0" } };
            polices[1].allowLabels = new Dictionary<string, string>() { { "k2", "v2" } };

            KanoVerifier verifier = new KanoVerifier();
            var reachMatrix = verifier.createReachMatrix(pods, polices, namespaces);
            Assert.AreEqual(reachMatrix.Length, 4, "reach matrix should be 4*4");
            BitArrayComparer bComp = new BitArrayComparer();
            Assert.IsTrue(bComp.Equals(reachMatrix[0], new BitArray(new bool[4] { false, false, true, false})), "pod0 has wrong reachability");
            Assert.IsTrue(bComp.Equals(reachMatrix[1], new BitArray(new bool[4] { true, true, false, false})), "pod1 has wrong reachability");
            Assert.IsTrue(bComp.Equals(reachMatrix[2], new BitArray(new bool[4] { false, false, true, true})), "pod2 has wrong reachability");
            Assert.IsTrue(bComp.Equals(reachMatrix[3], new BitArray(new bool[4] { false, false, false, false})), "pod3 has wrong reachability");
        }
        [TestMethod]
        public void ReachMatrixTest_AllAllow()
        {
            Pod[] pods = new Pod[4]{new Pod(), new Pod(), new Pod("ns1"), new Pod("ns1")};
            pods[0].addLabel("k0", "v0");
            pods[1].addLabel("k1", "v1");
            pods[2].addLabel("k2", "v2");
            pods[3].addLabel("k3", "v3");

            Namespace[] namespaces = new Namespace[2] {new Namespace("default"), new Namespace("ns1")};
            namespaces[0].addLabel("k0", "v0");
            namespaces[1].addLabel("k1", "v1");
            Policy[] polices = new Policy[] { new Policy(), new Policy("ns1") };

            polices[1].selectLabels = new Dictionary<string, string>() { {"k3", "v3" } };
            polices[1].allowNamespaces = new Dictionary<string, string>() { { "k0", "v0" } };
            polices[1].allowLabels = new Dictionary<string, string>() { { "k2", "v2" } };

            KanoVerifier verifier = new KanoVerifier();
            var reachMatrix = verifier.createReachMatrix(pods, polices, namespaces);
            Assert.AreEqual(reachMatrix.Length, 4, "reach matrix should be 4*4");
            BitArrayComparer bComp = new BitArrayComparer();
            Assert.IsTrue(bComp.Equals(reachMatrix[0], new BitArray(new bool[4] { true, true, false, false})), string.Format("pod0 has wrong reachability {0}", string.Join(", ", reachMatrix[0].OfType<bool>())));
            Assert.IsTrue(bComp.Equals(reachMatrix[1], new BitArray(new bool[4] { true, true, false, false})), "pod1 has wrong reachability");
            Assert.IsTrue(bComp.Equals(reachMatrix[2], new BitArray(new bool[4] { false, false, true, true})), "pod2 has wrong reachability");
            Assert.IsTrue(bComp.Equals(reachMatrix[3], new BitArray(new bool[4] { false, false, false, false})), "pod3 has wrong reachability");
        }
        [TestMethod]
        public void ReachMatrixTest_AllDeny()
        {
            Pod[] pods = new Pod[4]{new Pod(), new Pod(), new Pod("ns1"), new Pod("ns1")};
            pods[0].addLabel("k0", "v0");
            pods[1].addLabel("k1", "v1");
            pods[2].addLabel("k2", "v2");
            pods[3].addLabel("k3", "v3");

            Namespace[] namespaces = new Namespace[2] {new Namespace("default"), new Namespace("ns1")};
            namespaces[0].addLabel("k0", "v0");
            namespaces[1].addLabel("k1", "v1");
            Policy[] polices = new Policy[] { new Policy(), new Policy("ns1") };
            polices[0].allowNamespaces = new Dictionary<string, string>();
            polices[0].allowLabels = new Dictionary<string, string>();

            polices[1].selectLabels = new Dictionary<string, string>() { {"k3", "v3" } };
            polices[1].allowNamespaces = new Dictionary<string, string>() { { "k0", "v0" } };
            polices[1].allowLabels = new Dictionary<string, string>() { { "k2", "v2" } };

            KanoVerifier verifier = new KanoVerifier();
            var reachMatrix = verifier.createReachMatrix(pods, polices, namespaces);
            Assert.AreEqual(reachMatrix.Length, 4, "reach matrix should be 4*4");
            BitArrayComparer bComp = new BitArrayComparer();
            Assert.IsTrue(bComp.Equals(reachMatrix[0], new BitArray(new bool[4] { false, false, false, false})), string.Format("pod0 has wrong reachability {0}", string.Join(", ", reachMatrix[0].OfType<bool>())));
            Assert.IsTrue(bComp.Equals(reachMatrix[1], new BitArray(new bool[4] { false, false, false, false})), "pod1 has wrong reachability");
            Assert.IsTrue(bComp.Equals(reachMatrix[2], new BitArray(new bool[4] { false, false, true, true})), "pod2 has wrong reachability");
            Assert.IsTrue(bComp.Equals(reachMatrix[3], new BitArray(new bool[4] { false, false, false, false})), "pod3 has wrong reachability");
        }
        [TestMethod]
        public void TransposeTest()
        {
            BitArray[] x = new BitArray[3]
            {
                new BitArray(new bool[3] {true, true, false}),
                new BitArray(new bool[3] {false, false, true}),
                new BitArray(new bool[3] {true, true, false})
            };
            var y = BitsetHelper.transpose(x);
            x = new BitArray[3]
            {
                new BitArray(new bool[3] {true, false, true}),
                new BitArray(new bool[3] {true, false, true}),
                new BitArray(new bool[3] {false, true, false})
            };
            BitArrayComparer bComp = new BitArrayComparer();
            for (int i = 0; i < 3; ++i)
                Assert.IsTrue(bComp.Equals(x[i], y[i]));
        }
    }
    public class DictionaryComparer<TKey, TValue> :
    IEqualityComparer<Dictionary<TKey, TValue>>
    {
        private IEqualityComparer<TValue> valueComparer;
        public DictionaryComparer(IEqualityComparer<TValue> valueComparer = null)
        {
            this.valueComparer = valueComparer ?? EqualityComparer<TValue>.Default;
        }
        public bool Equals(Dictionary<TKey, TValue> x, Dictionary<TKey, TValue> y)
        {
            if (x.Count != y.Count)
                return false;
            if (x.Keys.Except(y.Keys).Any())
                return false;
            if (y.Keys.Except(x.Keys).Any())
                return false;
            foreach (var pair in x)
                if (!valueComparer.Equals(pair.Value, y[pair.Key]))
                    return false;
            return true;
        }

        public int GetHashCode(Dictionary<TKey, TValue> obj)
        {
            throw new NotImplementedException();
        }
    }
}
