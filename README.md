# ViciOne Core

## Projektstruktur

|Projekt|Inhalt|
|-|-|
|`FunctionBlockDesignGenerator`|Kommandozeilen-Tool mit dem die Funktionalität aus `ViciOne.Core.Dataflow.DataModel.Generation` verfügbar gemacht wird|
|`FunctionBlockExecutor`|Kommandozeilen-Tool mit dem die Funktionalität aus `ViciOne.Core.Runtime.Execution` verfügbar gemacht wird|
|`ViciOne.Core.Contracts`|die Vertrags-Definitionen der Kernobjekte sowie grundlegende Objekte, die an verschiedensten Stellen verwendet werden|
|`ViciOne.Core.Dataflow`|alle Objekte die den Dataflow und seine Komponenten darstellen sowie zugehörige Editoren|
|`ViciOne.Core.Dataflow.DataModel.Generation`|Analyzer und Generator um aus einem RuntimeFunctionBlock-Assembly das zugehörige FunctionBlockDesign zu generieren|
|`ViciOne.Core.Runtime`|die Basisklasse, von der alle Funktionsblöcke erben sollen sowie zugehörige Objekte|
|`ViciOne.Core.Runtime.Execution`|Funktionalität um einen FB aus einem RuntimeFunctionBlock-Assembly auszuführen|
|`ViciOne.Engine`|die Runtime um Dataflows auszuführen|
|`ViciOne.Engine.DefaultPoolings`|die Standardimplementierungen der Verrechnungsalgorithmen|
|`ViciOne.Engine.DefaultValidators`|die Standardimplementierungen der Prüfalgorithmen|
|`ViciOne.Serialization.Json`|Methoden zum (De-)Serialisieren von Objekten im JSON-Format|

## Beispiele

### samples/design-time/GenerateFBDesigns

Eine Beispiel-Anwendung mit der kompilierte Funktionsblöcke analysiert und daraus Funktionsblock-Designs erzeugt werden können.

### samples/runtime/RuntimeDemo

Eine Beispiel-Anwendung, welche die Verwendung der Engine und ihrer Abhängigkeiten zeigt.

### samples/runtime/ScaleUpSample

Eine Beispiel-Anwendung mit der eine beliebige Anzahl an Engines ausgeführt werden kann und die damit die Skalierbarkeit der Engine zeigt.

### samples/runtime/fbs

Enthält einige Beispiel-Funktionsblöcke.
