// Copyright (c) Stefán Jökull Sigurðarson. All rights reserved.

using System;

namespace Stebet.LayeredCache
{
    public class CacheItem<T>
    {
        public T Item { get; private set; }

        public DateTime ExpiresAt { get; private set; }

        public bool IsExpired => ExpiresAt < DateTime.UtcNow;

        public CacheItem(T item, DateTime expiresAt)
        {
            Item = item;
            ExpiresAt = expiresAt;
        }
    }
}