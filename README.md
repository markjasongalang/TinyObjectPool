#  TinyObjectPool
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE.txt)

By Mark Jason T. Galang ([@markjasongalang](https://github.com/markjasongalang))

## Overview
I started this project to help myself understand the [object pool pattern](https://en.wikipedia.org/wiki/Object_pool_pattern) because
it fascinated me how we could improve performance just by simply reusing objects.

Currently, I'm taking inspiration from a similar open-source project: 
[EsoxSolutions.ObjectPool](https://github.com/snoekiede/EsoxSolutions.ObjectPool) 
by [@snoekiede](https://github.com/snoekiede).

Apparently, after reading snoekide's implementation, I don't think there's resetting of objects when returned, so I referred to [Microsoft.Extensions.ObjectPool](https://github.com/dotnet/dotnet/tree/95017c711e6afc1085133d440e42b4bd78155701/src/aspnetcore/src/ObjectPool) instead.

## Features
- Object Pool - Rent, Return, and Dispose behavior
- Parent Pool Tracking - Rented object tracks parent pool to avoid returning conflicts

## Inquiries

Have any questions? Feel free to send an email at <markjasongalang.work@gmail.com>.

## License
This project is licensed under the [MIT License](LICENSE.txt).
