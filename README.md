# ViciOne Core

## Project structure

|Project|Content|
|-|-|
|`FunctionBlockDesignGenerator`|Command-line tool that exposes the functionality of `ViciOne.Core.Dataflow.DataModel.Generation`|
|`FunctionBlockExecutor`|Command-line tool that exposes the functionality of `ViciOne.Core.Runtime.Execution`|
|`ViciOne.Core.Contracts`|The contract definitions of the core objects as well as fundamental objects used in a wide variety of places|
|`ViciOne.Core.Dataflow`|All objects that represent the dataflow and its components, along with the corresponding editors|
|`ViciOne.Core.Dataflow.DataModel.Generation`|Analyzer and generator that creates the corresponding FunctionBlockDesign from a RuntimeFunctionBlock assembly|
|`ViciOne.Core.Runtime`|The base class that all function blocks should inherit from, along with the corresponding objects|
|`ViciOne.Core.Runtime.Execution`|Functionality for executing an FB from a RuntimeFunctionBlock assembly|
|`ViciOne.Engine`|The runtime for executing dataflows|
|`ViciOne.Engine.DefaultPoolings`|The default implementations of the aggregation algorithms|
|`ViciOne.Engine.DefaultValidators`|The default implementations of the validation algorithms|
|`ViciOne.Serialization.Json`|Methods for (de-)serializing objects in JSON format|

## Samples

### samples/design-time/GenerateFBDesigns

A sample application that can analyze compiled function blocks and generate function block designs from them.

### samples/runtime/RuntimeDemo

A sample application that demonstrates the use of the engine and its dependencies.

### samples/runtime/ScaleUpSample

A sample application that can run any number of engines and thereby demonstrates the scalability of the engine.

### samples/runtime/fbs

Contains a few sample function blocks.
