﻿namespace GreenPipes.Tests.Caching
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using GreenPipes.Caching;
    using GreenPipes.Caching.Internals;
    using NUnit.Framework;
    using TestValueObjects;


    [TestFixture]
    public class Using_the_node_tracker
    {
        class NodeObserver :
            ICacheValueObserver<SimpleValue>
        {
            readonly TaskCompletionSource<INode<SimpleValue>> _source;
            CancellationTokenSource _cancellation;

            public NodeObserver()
            {
                _source = new TaskCompletionSource<INode<SimpleValue>>();
                _cancellation = new CancellationTokenSource(TimeSpan.FromSeconds(30));
                _cancellation.Token.Register(() => _source.TrySetCanceled());
            }

            public void ValueAdded(INode<SimpleValue> node, SimpleValue value)
            {
                _source.TrySetResult(node);
            }

            public void ValueRemoved(INode<SimpleValue> node, SimpleValue value)
            {
            }

            public void CacheCleared()
            {
            }

            public Task<INode<SimpleValue>> Value => _source.Task;
        }


        [Test]
        public async Task Should_accept_a_completed_node()
        {
            var settings = new CacheSettings(1000, TimeSpan.FromSeconds(30), TimeSpan.FromSeconds(60));

            var tracker = new NodeTracker<SimpleValue>(settings);

            var observer = new NodeObserver();
            tracker.Connect(observer);

            var pendingValue = new PendingValue<string, SimpleValue>("Hello", SimpleValueFactory.Healthy);
            var nodeValueFactory = new NodeValueFactory<SimpleValue>(pendingValue, 100);
            var node = new FactoryNode<SimpleValue>(nodeValueFactory);

            tracker.Add(nodeValueFactory);

            var value = await node.Value;

            var observedNode = await observer.Value;

            Assert.That(observedNode, Is.InstanceOf<BucketNode<SimpleValue>>());
        }
    }
}