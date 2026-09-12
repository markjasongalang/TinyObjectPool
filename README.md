##  TinyObjectPool
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE.txt)

By Mark Jason T. Galang

### Overview
I started this project to help me understand the [object pool pattern](https://en.wikipedia.org/wiki/Object_pool_pattern) because
it fascinated me how we could improve performance just by simply reusing objects.

Currently, I'm taking inspiration from a similar open-source project: 
[EsoxSolutions.ObjectPool](https://github.com/snoekiede/EsoxSolutions.ObjectPool) 
by [@snoekiede](https://github.com/snoekiede).

### Features
- Object Pool - Rent, Return, and Dispose behavior
- Parent Pool Tracking - Rented object tracks parent pool to avoid returning conflicts

### License
This project is licensed under the [MIT License](LICENSE.txt).
