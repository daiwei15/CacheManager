﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using CacheManager.Core;
using CacheManager.Core.Configuration;
using FluentAssertions;
using Xunit;

namespace CacheManager.Tests
{
    [ExcludeFromCodeCoverage]
#if NET40
    [Trait("Framework", "NET40")]
#else
    [Trait("Framework", "NET45")]
#endif
    public class CacheManagerExpirationTest : BaseCacheManagerTest
    {
        // Issue #9 - item still expires
        [Theory]
        [MemberData("TestCacheManagers")]
        [Trait("Unreliable", "Timing")]
        public void CacheManager_RemoveExpiration_DoesNotExpire<T>(T cache) where T : ICacheManager<object>
        {
            using (cache)
            {
                var key = Guid.NewGuid().ToString();
                cache.Add(new CacheItem<object>(key, "value", ExpirationMode.Absolute, TimeSpan.FromMilliseconds(30)))
                    .Should().BeTrue();

                var item = cache.GetCacheItem(key);
                item.Should().NotBeNull();

                cache.Put(item.WithExpiration(ExpirationMode.None, default(TimeSpan)));

                Thread.Sleep(100);

                cache.Get(key).Should().NotBeNull();
            }
        }

        [Fact]
        [Trait("Unreliable", "Timing")]
        [ReplaceCulture]
        public void CacheManager_Configuration_AbsoluteExpires()
        {
            using (var cache = CacheFactory.Build("testCache", settings =>
            {
                settings.WithSystemRuntimeCacheHandle(Guid.NewGuid().ToString())
                        .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromMilliseconds(50));
            }))
            {
                var key = Guid.NewGuid().ToString();
                cache.Put(key, "value");

                Thread.Sleep(20);

                cache.Get(key).Should().Be("value");

                Thread.Sleep(40);

                cache.Get(key).Should().BeNull("Should be expired.");
            }
        }

        [Fact]
        public void BaseCacheHandle_ExpirationInherits_Issue_1()
        {
            using (var cache = CacheFactory.Build("testCache", settings =>
            {
                settings.WithSystemRuntimeCacheHandle(Guid.NewGuid().ToString())
                        .WithExpiration(ExpirationMode.Absolute, TimeSpan.FromSeconds(10))
                    .And
                    .WithSystemRuntimeCacheHandle("handleB");
            }))
            {
                cache.Add("something", "stuip");

                var handles = cache.CacheHandles.ToArray();
                handles[0].GetCacheItem("something").ExpirationMode.Should().Be(ExpirationMode.Absolute);

                // second cache should not inherit the expiration
                handles[1].GetCacheItem("something").ExpirationMode.Should().Be(ExpirationMode.None);
                handles[1].GetCacheItem("something").ExpirationTimeout.Should().Be(default(TimeSpan));
            }
        }
    }
}