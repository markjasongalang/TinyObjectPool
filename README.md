#  TinyObjectPool
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE.txt)

By Mark Jason T. Galang ([@markjasongalang](https://github.com/markjasongalang))

## Overview
I started this project to help myself understand the [object pool pattern](https://en.wikipedia.org/wiki/Object_pool_pattern) because
it fascinated me how we could improve performance just by simply reusing objects.

Currently, I'm taking inspiration from a similar open-source project: 
[EsoxSolutions.ObjectPool](https://github.com/snoekiede/EsoxSolutions.ObjectPool) 
by [@snoekiede](https://github.com/snoekiede).

As of September 18, 2026, after reading [@snoekiede](https://github.com/snoekiede)'s implementation, I don't think there's resetting of objects when returned, so I referred to [Microsoft.Extensions.ObjectPool](https://github.com/dotnet/dotnet/tree/95017c711e6afc1085133d440e42b4bd78155701/src/aspnetcore/src/ObjectPool) instead.

## Features
- Object Pool - Rent, Return, and Dispose behavior.
- Parent Pool Tracking - Rented object tracks parent pool to avoid returning conflicts.
- Eviction - Unused objects are automatically removed in the background based on their idle time.

## Object Eviction Sample (Idle-Based)

```
[10/13/2026 4:43:10 PM] Rented an object
[10/13/2026 4:43:10 PM] Rented an object
here1
[10/13/2026 4:43:12 PM] Returned the object
[10/13/2026 4:43:12 PM] Pool object count: 1
here2
[10/13/2026 4:43:22 PM] Returned the object
[10/13/2026 4:43:22 PM] Pool object count: 2
[10/13/2026 4:44:10 PM] Eviction run completed: 0 object(s) evicted in 0ms
[10/13/2026 4:45:10 PM] Eviction run completed: 0 object(s) evicted in 0ms
[10/13/2026 4:46:10 PM] Eviction run completed: 0 object(s) evicted in 0ms
[10/13/2026 4:47:10 PM] Eviction run completed: 0 object(s) evicted in 0ms
[10/13/2026 4:48:10 PM] Eviction run completed: 0 object(s) evicted in 0ms
[10/13/2026 4:49:10 PM] An idle object was removed from the pool. Pool count now = 1
[10/13/2026 4:49:10 PM] An idle object was removed from the pool. Pool count now = 0
[10/13/2026 4:49:10 PM] Eviction run completed: 2 object(s) evicted in 1ms
[10/13/2026 4:50:10 PM] Eviction run completed: 0 object(s) evicted in 0ms
```

> Note: The idle timeout is set to 5 minutes (for each rented object).

## Inquiries

Have any questions? Feel free to send an email at <markjasongalang.work@gmail.com>.

## License
This project is licensed under the [MIT License](LICENSE.txt).
