# Changelog

## 0.52.0

### Added

- Support for tracing and metrics

## 0.51.0

### Changed

- Improve reset of link changed flags to prevent side effects of parallelization

## 0.50.0

### Changed

- Speed up search for available pooling implementations in dataflow generation

## 0.49.0

### Changed

- Improve logging categories for log messages produced by FBs and Drivers
- Speed up initialization of runtime FBs and Drivers

## 0.48.0

### Changed

- Improve start and stop of engine
- Improve initialization of persistence handling components
- Reduce memory usage if no persistence layer attached
- Replace `FastDeepCloner` with `FastCloner` library due to thread safety issues in `FastDeepCloner` library

## 0.47.0

### Added

- 'Connected' flag to runtime connector to query if connector has links connected to it

## 0.46.0

### Changed

- Update `.NET` to `9.0`

## 0.45.0

### Fixed

- Return correct value for DefaultPerSourceAggregatingPooling in connector design data model
- Return correct value for PerSourceAggregatingPooling in connector data model
- Use different object instances for per source aggregating pooling and cross source aggregating pooling

## 0.44.0

### Added

- Support for different pooling configurations for links and connectors (per source pooling and cross source pooling)

### Changed

- Set change time of connector if it´s marked as changed instead only if value is different

### Fixed

- Also process settings if they were changed by attached persistence layer

## 0.43.0

### Added

- Generic version of DefaultValueAttribute

### Fixed

- Raise OnSettingsChanged event in first cycle

## 0.42.0

### Changed

- Also signal loaded entries as changed entries to attached persistence layer
- Change attributes to use generics language feature

### Removed

- Explicit validator and aggregating pooling attributes

## 0.41.0

### Added

- Support for automatic conversion between date and time data types

## 0.40.0

### Added

- Support for links between derived and their base types

## 0.39.0

### Changed

- Change target framework to .NET 8.0

## 0.38.0

### Added

- Support for multiple validators for connectors and settings

## 0.37.0

### Added

- Support for value type determination of types that implement multiple value types

### Changed

- Allow dataflow changes while engine initialization
- Change target framework to .NET 7.0

## 0.36.0

### Added

- Class 'TypeConversionResolver' for resolving necessary type conversions between data types
- Non generic overloads for methods in 'PoolingFactory'
- Support for Poolings and Validators in 'DataTypeCompatibilityValidator'

### Changed

- Improve error messages in invalid name checks

## 0.35.0

### Changed

- Rename 'Enabled' system connectors of FB to 'FunctionBlockEnabled'
- Rename 'IsEnabled' property of runtime FB to 'FunctionBlockEnabled'

## 0.34.0

### Added

- Support for explicit specification of implementing FB type using FB property 'RuntimeTypeName'
  - Namespace and name of FB design can now be specified indepentently from name and namespace of implementing type using the according properties of FunctionBlockDesign attribute

### Changed

- Implementing type of driver is now specified by FB property 'DriverRuntimeTypeName'
- Rename 'Process' method of runtime FB to 'ProcessCycle'

### Removed

- AssemblyName properties of drivers and FBs
- No more needed dataflow object 'Driver'

### Fixed

- Execute all FBs in all existing child containers

## 0.33.0

### Added

- Support for data type conversion for external incoming links
- Non generic 'IValidator' interface

## 0.32.0

### Fixed

- Prevent multiple connector value changed events per cycle in FB-Executor
- Rename 'HandleChangedOutputLinks' method to 'HandleChangedOutgoingLinks' in communication manager interface
- Re-enable listing current values of input connectors in FB-Executor

## 0.31.0

### Added

- Support for outgoing external links from input connectors

### Changed

- Rename 'UniqueId' to 'Id'
- Dataflow object represents now only the part which has to be executed by the specific runtime engine
  - 'Engine' datamodel object is no longer needed
  - Dataflow internal links between different engines are now represented by external links
- External communication and process data logging are now both handled by external links
  - 'ProcessDataLogger' datamodel object is no longer needed
  - External outgoing links needs to be used for process data logging
- Improve naming of external links
  - Rename 'ExternalInputLinks' to 'ExternalIncomingLinks'
  - Rename 'ExternalOutputLinks' to 'ExternalOutgoingLinks'

### Removed

- No more needed dataflow objects 'Engine', 'ExternalCommunication' and 'ProcessDataLogger'

## 0.30.0

### Changed

- Improve consistency of creating settings via setting editor (never set changed time instead of setting it sometimes)
- Publish 'ValidateValue' method of setting editor to enable validation of value without setting it
- Change target framework of FB-Design-Generator and FB-Executor to .NET 6.0
- Publish default values of external output links during initialization phase of Engine

## 0.29.0

### Changed

- Change factories from static to instances with cleanup functionality
  - Cleanup factory caches when Engine is disposed
- Cleanup cache of FastDeepCloner when Engine is disposed
- Ensure Engine cycle is finished before Engine gets disposed

### Fixed

- Initialize outputs in FB-Executor with correct values

## 0.28.0

### Added

- Non generic data model base classes for links
- Extension methods for data model
- Link classes for runtime use (implementing 'IRuntimeLink')
- Queuing for links (also called 'horizontal pooling')

### Changed

- Publish classes with default values of properties
- Use non generic data model base classes in data model and editors
- Extend existing pooling functionality to pooling strategy
  - Former poolings 'Queue' and 'Internal' are now integrated in Engine as strategies 'Queue' and 'Concat'
  - All other poolings are now modeled as 'IAggregatingPooling'
  - Aggregating poolings have fewer dependecies, they now only need to provide the aggregation itself

### Removed

- All non design time relevant properties from link contracts and data model classes

## 0.27.0

### Added

- Non generic data model base classes
- Non generic pooling instance creation

### Changed

- Use FastDeepCloner instead of AutoMapper to clone Pooling in input connector editor
- Describe value validity with enumeration instead of boolean flag

## 0.26.0

### Changed

- Use explicit type instead of interface for process data logger list in dataflow

### Removed

- Unnecessary external link type parameter when adding external links

## 0.25.0

### Added

- Automatic initialization of FB components via reflection (when structured and decorated correctly)

### Changed

- Reduce ChangeProcessDataLogger and CyclicProcessDataLogger to ProcessDataLogger

### Removed

- Engine's CycleTimeSettings

## 0.24.0

### Fixed

- Provide correct Enabled value to FB between engine start and first call of Process method

## 0.23.0

### Added

- UniqueNameProvider as mandatory argument in runtime engine's constructor
- Support for connector validators in FB design generator
- Support for multiple validators via attributes

### Changed

- Extend unique name generation by more uniquely identifiable objects
- Change target framework to .NET Standard 2.1
- Enable nullable reference types

### Fixed

- Mark FB settings as changed in first cycle

## 0.22.0

### Added

- IPoolableDesign interface to input connector designs
- Ability to use more than one FB design in FB-Executor

### Changed

- Provide dataflow wide unique names to FB's
- Use AutoMapper instead of Newtonsoft.Json to clone Pooling in input connector editor
- Make unique name generation publicly available via Contracts package

## 0.21.0

### Changed

- Improve error logging by adding dataflow wide unique name of subject to log message
- Implement Process method in runtime FB's base class explicitly to remove need of base call in FB implementations

### Fixed

- Sort received entries from communication manager to ensure that all values will be passed to an eventually existing advanced destination pooling

### Removed

- RunModes Never and Always
- No more needed dataflow property 'CommunicationGroup'

## 0.20.0

### Added

- Name as mandatory argument in connector design editor's constructors
- Check name validity in setting and persistent variable editors

### Changed

- Improve TypeConverter determination in internal link editor
  - Extens type conversion information stored in data model
- Some other improvements in various editors
- Improve error logging for settings and persistent variables

### Removed

- Automatic unique name generation to ensure not to alter specified names by mistake

## 0.19.0

### Added

- Generic attributes for generic validators

### Changed

- Improve runtime engine's thread usage
  - Remove worker thread per engine, cycle must now be directly executed by calling ProcessCycle method
  - Sequential instead of parallel disposition of runtime elements

### Removed

- Specific derivations of generic validators

## 0.18.0

### Added

- Additional default validators (enumeration, integer and regular expression validation)
- Support for multiple links between one external communication and one connector

### Removed

- Unnecessary type constraint in setting design editor

## 0.17.0

### Added

- Support for settings in FB-Executor
- Support for persistable variables in FB-Executor

### Changed

- Improve FB-Executor's events
- Improve assembly loading logic in FB-Executor

## 0.16.0

### Changed

- Improve FB-Executor's events

## 0.15.0

### Added

- Release ViciOne.Core.Runtime.Execution as NuGet package

## 0.14.0

### Added

- Validator support to connectors
- Additional default validators (equality checks, combination)

### Changed

- Improve naming and structure of objects
  - Rename ViciOne.Core.Interfaces to ViciOne.Core.Contracts
  - Split contracts namespace to data model and runtime contracts
  - Move filters to Contracts assembly
- Update runtime engine interface with all functionality of its implementation
- Restrict write access to properties of runtime elements
- Improve runtime element creation

### Removed

- Unused link enabled flag
- Unnecessary default validators

## 0.13.0

### Added

- Ability to apply sets of new FB settings while Engine is running
- Ability to retrieve all current FB settings while Engine is running
- Ability to modify every connector value while Engine is running
- Ability to cancel any value transfer from link/pooling to connector or from connector to link
- Support for retrieving incoming links in FB when using internal pooling
- Support for persistable variables in FBs

### Changed

- Reduce dependencies needed to create an Engine
- Rename Runtime-FB's methods for changing value of settings or connectors
- Ensure thread safety of setting changed flag
- Improve startup validation logic

### Fixed

- Pay attention to validator when applying a previously saved setting value

### Removed

- No more needed setters of some runtime elements

## 0.12.0

### Added

- FB-Executor to execute FB defined in FB-Assembly
- Support for driver FBs in FB-Design-Generator
- Unique ID property to setting design

### Changed

- Change type of runtime FB's SettingChanged event from EventHandler to Action
- Move presentation specific classes to own project

### Fixed

- Set setting's timestamp only when value changes

### Removed

- Unused method ViciOne.Core.Interfaces.IPersistence.GetPersisted()

## 0.11.0

### Added

- Support for structured logging by using log message templates
- Ability to use value-type-determination by passing type instead of instance
- First editors for presentation data model
- SettingsDemo-Sample-FB for demonstrating use of FB settings
- FB-Design-Generator and according attributes to generate FB-Design from FB-Assembly
- Third party license notes

### Changed

- Ensure thread safety of FB-Changed-Flag
- Process output connectors whenever it is necessary, not only when FB was processed
- Change default value of Connector-Valid flag from false to true
- Use unsigned data types where appropriate
- Create new pooling instance from default pooling for every connector when using Connector editor

### Fixed

- Use correct value for initialization of FB-Design's DefaultCycleFrequency property
- Prevent removal of default RunMode from ValidRunModes collection of FB

### Removed

- Remaining occurences of DriverEnabled-System-Connector
