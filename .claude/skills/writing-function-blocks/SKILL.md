---
name: writing-function-blocks
description: 'Concepts and correctness rules for writing or reviewing ViciOne runtime FunctionBlocks and drivers. USE WHEN: writing, fixing, reviewing, or debugging a FunctionBlock or driver; deciding whether a block needs a driver; questions about the FunctionBlock lifecycle (Setup, InitializeI/II, ProcessCycle, Dispose), cycle model, change tracking (.Changed), pooling, validity, settings, or persistent variables; making a block AssemblyLoadContext (ALC) safe / unloadable (no static state, no leaked event subscriptions, no leaked threads/timers/native handles); correct disposal order and idempotent Dispose; the Designer.cs vs logic-partial split; how FunctionBlockDesign is generated from the assembly. DO NOT USE FOR: contributing to the ViciOne Core engine itself.'
argument-hint: 'A question about FunctionBlock behavior/correctness, or a block you are writing/reviewing'
---

# Writing FunctionBlocks — concepts & correctness

A FunctionBlock is a small .NET class that derives from `RuntimeFunctionBlock<TSelf>`, exposes typed connectors/settings/variables, and reacts to engine cycles. Use this skill to understand *how* a block behaves and *what you must regard* to keep it correct and unloadable — most importantly **AssemblyLoadContext (ALC) safety**.

## Mental model

- The **engine** hosts the block. It drives cycles, copies setting inputs, aggregates (Pooling) multi-source inputs, persists variables, and creates/owns the connector objects. You write only the runtime class and its per-cycle logic.
- A block's **metadata** (`FunctionBlockDesign`: GUIDs, connector/setting shape, run modes, pooling, validators, defaults) is **generated separately** by the engine's design-generation tooling, which reflects your compiled block assembly. The attributes in `*.Designer.cs` are the source of truth for that generation — they are not just documentation.
- Block assemblies are loaded into a **collectible `AssemblyLoadContext`** and can be **unloaded** at runtime. Everything below about statics, events, threads and handles follows from this.

## The runtime environment

Your block never runs on its own — a **host** loads the dataflow into the engine (the runtime environment) and drives it. The engine turns the static dataflow (blocks + links) into a live system by executing it as a sequence of **cycles**; the lifecycle hooks and run modes below all exist to fit into that loop.

A **cycle** is one discrete pass over the dataflow: the engine propagates each changed value along its links (applying type conversion and Pooling where inputs merge), copies setting inputs into their settings, then invokes `ProcessCycle` on exactly the blocks that are due to run this pass (per their run mode). A block sees a consistent snapshot of its inputs for the cycle and publishes outputs that become inputs for the next one. Cycles are numbered (`cycleInfo.CycleNo`), and `cycleInfo.IsFirstCycle` flags a block's **own** first processed cycle — it is `true` the first time *that block* is invoked and is cleared once the block has run, so it does **not** track the engine's first cycle.

The engine itself has **no built-in clock** — it advances exactly one cycle each time the host calls its `ProcessCycle()`, so the cadence is the host's to set. In production the **EngineHost** drives it with a fixed-interval timer at a configured **cycle time** in milliseconds (bounded to **10 ms – 5 s**); it runs a *chain* of engines in a fixed order on each tick, each engine's cycle being one link in the chain, and all engines in a chain share the same cycle time. So a cycle's target period is that fixed cycle time — e.g. a 20 ms cycle time means the engine (and every block due that cycle) should complete ~50 times a second.

The cycle time is a **budget, not a guarantee**: all the work due in a tick — every block the engine invokes plus value propagation, Pooling and persistence — must finish within it. If a tick overruns, the host logs a *cycle-time-exceeded* warning and **skips** the ticks that were missed (`skipped = overrun / cycle time`), so the effective rate drops. This is the concrete reason every `ProcessCycle` must stay short and non-blocking (see below): one slow block blows the budget for the whole chain. Note that `RunMode.Cyclic`'s frequency is a separate concept counted in **cycles, not milliseconds** — a block with frequency *N* runs on every *N*-th engine cycle, so its real-world period is *N* × the host's cycle time.

## Designer.cs vs logic partial

A block class **derives from `RuntimeFunctionBlock<TSelf>`** (self-generic) and must have a **public parameterless constructor** — the base `new()` constraint means the engine constructs the block itself, so never add a constructor with parameters. One-time construction/wiring goes in `InitializeI()`, not a constructor.

A block is one `partial class` split across two files by responsibility:

- **`<Block>.Designer.cs`** — *shape only*: base class, `[FunctionBlockDesign(...)]` and the other class-level attributes, nested `[InputConnectorDesigns]` / `[OutputConnectorDesigns]` / `[SettingDesigns]` / `[PersistentVariableDesigns]` groups, and the `public new <Group>_ ... = new();` properties. No behavior.
- **`<Block>.cs`** — *behavior only*: a second `partial class` (no base, no attributes) overriding the lifecycle hooks. This is where ALC-relevant resource ownership lives.

Reference the **`ViciOne.Core.Runtime.Analyzers`** package (`PrivateAssets=all`) in every block project — its Roslyn analyzers catch most authoring mistakes at build time.

## Lifecycle & cycle model

Overridable hooks on `RuntimeFunctionBlock<TSelf>`. For every hook **except `Dispose`**, call `base.` **first**. `Dispose` is the exception: set the `_disposed` guard and run your teardown **before** calling `base.Dispose(disposing)` last, so a re-entrant call cannot race teardown that has not yet run (see the ALC section).

| Hook | When | Typical use |
|------|------|-------------|
| `InitializeI()` | First init phase | Acquire driver, subscribe to driver events, one-time setup |
| `InitializeII()` | Second init phase | Work that depends on all blocks/drivers being initialized |
| `ProcessCycle(IFunctionBlockCycleInfo cycleInfo)` | Only when the engine invokes it (see run modes) — on an input change (`Change` mode) or every *N*-th cycle (`Cyclic` mode), not necessarily on every engine cycle | Main logic |
| `OnFunctionBlockEnabledChanged()` | `FunctionBlockEnabled` input flips | Re-publish or stop |
| `OnSettingsChanged()` / `OnSettingChanged(setting)` | A setting's input changed | React to config |
| `OnPersistentVariableChanged(variable)` | A persistent variable changed | React to restored/edited state |
| `Dispose(bool disposing)` | Teardown / ALC unload | **Release everything** (see ALC section) |

`Setup(...)` is **engine-only** for blocks — it is `public`, called once by the engine to inject the `FunctionBlock`, driver, logger, etc., and throws if called twice. **Do not override or call it** (only *drivers* have an overridable `Setup(ILogger)`); put your one-time block init in `InitializeI()`.

**Never block in `ProcessCycle`.** It runs on the engine's cycle and must return quickly. Blocking calls — `Thread.Sleep`, `Task.Wait()`/`.Result`, blocking I/O, `lock` contention, busy-waits, or long computations — inflate the cycle time and could break the configured frequency. Do only fast, non-blocking work; offload slow or out-of-band work (hardware, network, timers) to a **background worker** — a `Thread`, `Task`, timer, or a **driver** — and just read its latest result in `ProcessCycle`. A driver is not required merely to move blocking work off the cycle; it is the right home when that work is hardware-bound or must be shared (see below). Whatever you use, you own its lifecycle and must stop and dispose it (see the ALC section).

**Exception handling.** Wrap I/O or driver calls in `try`/`catch` inside `ProcessCycle` and inside driver event handlers. On failure, mark the affected output(s) invalid with `SetConnectorValueIfNotEqual(ProcessDataOutputs.X, currentValue, Validity.Invalid)` and log via the injected `ILogger` rather than rethrowing — an exception that escapes `ProcessCycle` or a driver handler can crash the engine cycle.

`cycleInfo` exposes `IsFirstCycle`, `CycleNo`, etc. Settings are **auto-processed** by the engine each cycle (input value is copied into the setting before `ProcessCycle`); you do not copy them yourself.

Idiomatic `ProcessCycle`:

```csharp
protected override void ProcessCycle(IFunctionBlockCycleInfo cycleInfo)
{
    if (FunctionBlockEnabled == false)
        return; // guard unless the block must run while disabled
    SetConnectorValueIfNotEqual(ProcessDataOutputs.Y,
        ProcessDataInputs.A.Value && ProcessDataInputs.B.Value);
}
```

## Run modes

The class-level attributes declare which run modes the engine may use to drive the block: `[ValidRunModes(...)]`, `[DefaultRunMode(...)]`, and (for cyclic) `[DefaultCycleFrequency(n)]` where *n* is a number of engine cycles. The two run modes are:

- **`RunMode.Change`** — the engine invokes `ProcessCycle` when an input changes **or** when the block has scheduled itself by calling the base class's own `MarkAsChanged()` (distinct from `connector.MarkAsChanged()`), which makes the block run again on the next cycle. The `.Changed` guard inside `ProcessCycle` is then usually unnecessary — just recompute and publish.
- **`RunMode.Cyclic`** — the engine invokes `ProcessCycle` every *n*-th engine cycle (frequency `1` = every cycle), regardless of input changes. Here a `.Changed` guard is a useful optimization to skip redundant work.

A block may allow several modes (e.g. `[ValidRunModes(RunMode.Change, RunMode.Cyclic)]`). Read the resolved mode at runtime via `FunctionBlock.RunMode` when behavior must differ (e.g. a Random block that free-runs in Cyclic but waits for a trigger in Change).

## Connectors: change tracking, value & validity

- **`.Changed`** — set when the value/validity changed; cleared after the engine reads it (`Interlocked`-based). In `RunMode.Cyclic`, guard on relevant inputs `.Changed` to keep cycles cheap; in `RunMode.Change` the engine already gates the call, so it is usually redundant.
- **`.Value`** — the current (validated) value. **`T` can be almost any type** — primitives (`bool`, `int`, `double`, …), `string`, `enum`s, `DateTime`, arrays (`double[]` — required for `PoolingStrategy.Concat`), and custom reference types (use `[DefaultValue<TConcrete>]` for their default). Linked connectors of different `T` are bridged by the engine's `TypeConverter` when a conversion exists.
- **`.Validity`** — `Valid` / `Invalid` (default `Valid`). Propagate validity for bad/unknown inputs rather than publishing a misleading value.
- **Publish via `SetConnectorValueIfNotEqual(connector, value[, validity])`** — validates, sets only on change, marks `Changed`, and returns whether it changed. Do not assign `.Value` directly. Equivalents exist for settings/variables: `SetSettingValueIfNotEqual`, `SetPersistentVariableIfNotEqual`.
- **`connector.MarkAsChanged()`** — force a connector to be re-published even when the value is unchanged (e.g. to push the first value from a driver to downstream blocks).

## Settings, persistent variables, pooling, validators

- **Settings** survive in config and have both an input and an output connector; declare them in a `[SettingDesigns]` group with `[SettingDesign(settingGuid, inputGuid, outputGuid)]`.
- **Persistent variables** survive restarts (single value); declare them in a `[PersistentVariableDesigns]` group with `[PersistentVariableDesign(guid)]`.
- **Reading/writing at runtime.** Like connectors, each group needs a `public new <Group>_ ... = new();` property. In logic read the current value via `Settings.<Name>.Value` / `PersistentVariables.<Name>.Value`, and write persistent variables via `SetPersistentVariableIfNotEqual(PersistentVariables.<Name>, value)`. React to external edits by overriding `OnSettingChanged` / `OnPersistentVariableChanged` (e.g. to re-publish an output).
- **Pooling** controls how multiple links into one input combine — e.g. `[DefaultCrossSourcePoolingStrategy(PoolingStrategy.Queue|Concat|Aggregate)]` or `[DefaultCrossSourceAggregatingPooling<MinPooling<double>>]` (needs `ViciOne.Engine.DefaultPoolings`).
- **Validators** constrain inputs/settings — e.g. `[RangeValidator<double>(-100, 100)]`, `[InequalityValidator<double>(0, 1)]`, `[EnumValidator<TEnum>(TEnum.First)]` (needs `ViciOne.Engine.DefaultValidators`).
- **Defaults** — `[DefaultValue(...)]`, `[DefaultValue<TConcrete>]` for reference types, or a custom attribute deriving the default-value attribute for a value a literal can't express (e.g. a `DateTimeDefaultValue` attribute that constructs a `DateTime`).

Only reference `DefaultPoolings` / `DefaultValidators` when you actually use their attributes.

## When do you need a driver?

Most blocks are **pure logic** (compute outputs from inputs) and need **no driver**.

Add a driver when the block needs **shared, stateful, out-of-band resources**:

- hardware / IO (GPIO, serial, fieldbus, sockets), or
- state that must be **shared across multiple block instances** of the same kind.

Key facts about drivers:

- A driver derives `RuntimeDriver<TDriver>` (self-generic, **parameterless** constructor) and the block is annotated `[Driver<TDriver>]` (or `[Driver("Namespace.TDriver")]`).
- The engine creates **one driver instance per driver type** and shares it across all blocks that reference it. Driver state is therefore shared — guard accordingly.
- Driver hooks mirror the block: `Setup(ILogger)` (called once), `InitializeI()`, `InitializeII()`, `Dispose(bool)`. Start hardware/timers in `InitializeII()`, stop them in `Dispose`.
- Expose a strongly-typed accessor in the block: `public new MyDriver Driver => (MyDriver)base.Driver;` and subscribe to driver events in `InitializeI()`.

If the block has no out-of-band resource, do **not** add a driver — it only adds shared state and unload hazards.

## AssemblyLoadContext (ALC) awareness — the critical part

Block assemblies load into a **collectible `AssemblyLoadContext`** and be **unloaded** after stop and dispose of the engine. After unload the engine polls a `WeakReference` and forces GC; if anything outside the ALC still references anything inside it, the unload **fails** and the assembly (and all its types/statics) **leaks**. Write every block and driver to be unloadable:

- **No mutable static state.** Statics live for the lifetime of the ALC and pin it. Avoid static fields, static caches, static events, and static `Type`/`MethodInfo`/`Assembly` references. Prefer instance state.
- **Unsubscribe from every event you subscribe to**, especially **driver events**, in `Dispose(bool)`. A live subscription from a longer-lived publisher (e.g. the shared driver) to your block keeps the block — and its whole ALC — alive:

  ```csharp
  protected override void Dispose(bool disposing)
  {
      if (_disposed) return;
      _disposed = true;
      Driver.ValueChanged -= OnDriverValueChanged; // break the back-reference
      base.Dispose(disposing); // call last, after teardown
  }
  ```

- **Stop and dispose background work** — timers, threads, tasks, `CancellationTokenSource` — so no callback can run after unload. Drivers own this: start in `InitializeII`, fully stop in `Dispose` (detach handler, `Stop()`, `Dispose()`, null the field).
- **Release native/unmanaged resources** — P/Invoke handles, COM objects, `SafeHandle`s — in `Dispose`.
- **Don't hand out references that outlive the block.** Anything you register with a host outside the ALC must be unregistered in `Dispose`.
- **Always call `base.Dispose(disposing)`** (last, after your teardown) so the base can release settings/persistent variables. If you omit it, the base class never disposes settings and persistent variables, leaking those resources silently at runtime. The Roslyn analyzer does not currently catch this omission, so verify it manually.

## Threading & disposal guarantees

- **One block instance is processed single-threaded per cycle** — no concurrent `ProcessCycle` calls on the same instance. The engine may process *different* blocks concurrently.
- Connector change flags use `Interlocked`; you generally need **no locks** inside a block. Add synchronization only for state a **driver** shares across blocks/threads (e.g. a timer callback thread writing driver state read by blocks).
- **Disposal order**: the engine disposes drivers and blocks; the base `RuntimeFunctionBlock` disposes settings and persistent variables for you. Make `Dispose` **idempotent** (`_disposed` guard) and side-effect-free if called twice.

## Correctness checklist

- [ ] **Driver only when needed** — hardware/IO, background change source, or cross-instance shared state. Pure logic → no driver.
- [ ] **No mutable statics**; all state is instance state.
- [ ] **Every `+=` has a `-=` in `Dispose`**, driver events especially.
- [ ] **Timers/threads/tasks/handles started → stopped & disposed** in `Dispose`; `base.Dispose` always called; `Dispose` is idempotent.
- [ ] **`ProcessCycle` never blocks** — no `Thread.Sleep`, `Task.Wait()`/`.Result`, blocking I/O, busy-wait, or long synchronous work; offload slow/blocking work to a driver and react to its events. Blocking inflates and breaks the cycle time.
- [ ] **Exceptions can't escape `ProcessCycle` or driver handlers** — wrap I/O/driver calls in `try`/`catch`, mark affected outputs `Validity.Invalid` and log via `ILogger` instead of rethrowing.
- [ ] **`base.` call order** — call `base.` first in every hook *except* `Dispose`, which calls `base.Dispose(disposing)` last after your teardown.
- [ ] **Guard with `FunctionBlockEnabled`** at the top of `ProcessCycle` (republish in `OnFunctionBlockEnabledChanged`) unless the block must run while disabled.
- [ ] **Run mode matches the logic** — `RunMode.Change` for input-driven blocks (no `.Changed` guard needed), `RunMode.Cyclic` (+ `DefaultCycleFrequency`) for time-driven blocks; branch on `FunctionBlock.RunMode` only if the block supports both.
- [ ] **Publish via `SetConnectorValueIfNotEqual`**; in Cyclic mode guard on relevant inputs `.Changed`; set `Validity` for bad/unknown values rather than a misleading value.
- [ ] **Designer/logic split** respected; `new` on group properties; `internal set;` on members.
- [ ] **Public parameterless constructor only** (no custom ctor); one-time init in `InitializeI`, never in a constructor or by overriding `Setup`.
- [ ] **Settings/persistent variables** each have a `public new <Group>_ = new();` property; read via `.Value`, write variables via `SetPersistentVariableIfNotEqual`.
- [ ] **Reference the analyzers** (`ViciOne.Core.Runtime.Analyzers`, `PrivateAssets=all`) and only the runtime packages used (`DefaultPoolings` / `DefaultValidators` only if their attributes appear).
- [ ] **Fresh, unique GUIDs** on every block/connector/setting/variable (never reuse).

## Worked examples

These are complete and self-contained. Replace every `"<...>"` with a fresh `Guid` and adjust the namespace/names. Each block is one `partial class` split into `*.Designer.cs` (shape) and `*.cs` (behavior).

### Pure-logic block (no driver)

`And.Designer.cs`:

```csharp
using ViciOne.Core.Contracts.DataModel;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime;
using ViciOne.Core.Runtime.Attributes;

namespace MyBlocks;

[FunctionBlockDesign("<block-guid>", "<enabled-input-guid>", "<enabled-output-guid>")]
[DefaultRunMode(RunMode.Change)]
[ValidRunModes(RunMode.Change, RunMode.Cyclic)]
public partial class And : RuntimeFunctionBlock<And>
{
    public new Inputs_ ProcessDataInputs { get; } = new Inputs_();
    [InputConnectorDesigns]
    public class Inputs_
    {
        [InputConnectorDesign("<guid>")]
        public IRuntimeConnector<bool> A { get; internal set; }
        [InputConnectorDesign("<guid>")]
        public IRuntimeConnector<bool> B { get; internal set; }
    }

    public new Outputs_ ProcessDataOutputs { get; } = new Outputs_();
    [OutputConnectorDesigns]
    public class Outputs_
    {
        [OutputConnectorDesign("<guid>")]
        public IRuntimeConnector<bool> Y { get; internal set; }
    }
}
```

`And.cs`:

```csharp
using ViciOne.Core.Contracts.Runtime;

namespace MyBlocks;

public partial class And
{
    protected override void ProcessCycle(IFunctionBlockCycleInfo cycleInfo)
    {
        if (FunctionBlockEnabled)
            SetConnectorValueIfNotEqual(ProcessDataOutputs.Y,
                ProcessDataInputs.A.Value && ProcessDataInputs.B.Value);
    }
}
```

### Driver + block (ALC-safe teardown)

`MyDriver.cs` — a shared, parameterless `RuntimeDriver<TSelf>` that raises an event from a timer:

```csharp
using System;
using System.Timers;
using ViciOne.Core.Runtime;

namespace MyBlocks;

public sealed class MyDriver : RuntimeDriver<MyDriver>
{
    private Timer? _timer;

    public DateTime Time { get; private set; }
    public event EventHandler? TimeChanged;

    public override void InitializeII()
    {
        base.InitializeII();
        _timer = new Timer { Interval = 1000 };
        _timer.Elapsed += OnElapsed;
        _timer.Start();
    }

    private void OnElapsed(object? sender, ElapsedEventArgs e)
    {
        Time = DateTime.Now;
        TimeChanged?.Invoke(this, EventArgs.Empty);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && _timer is not null)
        {
            _timer.Elapsed -= OnElapsed; // stop callbacks before unload
            _timer.Stop();
            _timer.Dispose();
            _timer = null;
        }
        base.Dispose(disposing);
    }
}
```

`Clock.Designer.cs` — the block references the driver via `[Driver<TDriver>]`:

```csharp
using System;
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime;
using ViciOne.Core.Runtime.Attributes;

namespace MyBlocks;

[FunctionBlockDesign("<block-guid>", "<enabled-input-guid>", "<enabled-output-guid>")]
[Driver<MyDriver>]
// Event-driven: the driver pushes new values, so the block only needs to run on change.
[DefaultRunMode(RunMode.Change)]
[ValidRunModes(RunMode.Change)]
public partial class Clock : RuntimeFunctionBlock<Clock>
{
    public new MyDriver Driver => (MyDriver)base.Driver;

    public new Outputs_ ProcessDataOutputs { get; } = new Outputs_();
    [OutputConnectorDesigns]
    public class Outputs_
    {
        [OutputConnectorDesign("<guid>")]
        public IRuntimeConnector<DateTime> Time { get; internal set; }
    }
}
```

`Clock.cs` — subscribe in `InitializeI`, **unsubscribe in `Dispose`** (this is the ALC-critical part):

```csharp
using System;
using ViciOne.Core.Contracts.Runtime;

namespace MyBlocks;

public partial class Clock
{
    private bool _disposed;

    public override void InitializeI()
    {
        base.InitializeI();
        Driver.TimeChanged += OnDriverTimeChanged;
    }

    private void OnDriverTimeChanged(object? sender, EventArgs e)
    {
        if (FunctionBlockEnabled)
            SetConnectorValueIfNotEqual(ProcessDataOutputs.Time, Driver.Time);
    }

    protected override void Dispose(bool disposing)
    {
        if (_disposed)
            return;
        _disposed = true;
        Driver.TimeChanged -= OnDriverTimeChanged; // without this the ALC cannot unload
        base.Dispose(disposing); // call last, after teardown
    }
}
```

### Block with a setting and a persistent variable

`Incrementor.Designer.cs` — note the `public new Settings_ / PersistentVariables_` group properties, exactly like the connector groups:

```csharp
using ViciOne.Core.Contracts.Runtime;
using ViciOne.Core.Runtime;
using ViciOne.Core.Runtime.Attributes;
using ViciOne.Engine.DefaultValidators;

namespace MyBlocks;

[FunctionBlockDesign("<block-guid>", "<enabled-input-guid>", "<enabled-output-guid>")]
public partial class Incrementor : RuntimeFunctionBlock<Incrementor>
{
    public new PersistentVariables_ PersistentVariables { get; } = new PersistentVariables_();
    [PersistentVariableDesigns]
    public class PersistentVariables_
    {
        [PersistentVariableDesign("<guid>")]
        public IRuntimePersistentVariable<double> Value { get; internal set; }
    }

    public new Inputs_ ProcessDataInputs { get; } = new Inputs_();
    [InputConnectorDesigns]
    public class Inputs_
    {
        [InputConnectorDesign("<guid>")]
        public IRuntimeConnector<bool> Increment { get; internal set; }
    }

    public new Outputs_ ProcessDataOutputs { get; } = new Outputs_();
    [OutputConnectorDesigns]
    public class Outputs_
    {
        [OutputConnectorDesign("<guid>")]
        public IRuntimeConnector<double> Value { get; internal set; }
    }

    public new Settings_ Settings { get; } = new Settings_();
    [SettingDesigns]
    public class Settings_
    {
        [SettingDesign("<setting-guid>", "<input-guid>", "<output-guid>")]
        [DefaultValue(0.5)]
        public IRuntimeSetting<double> IncrementValue { get; internal set; }
    }
}
```

`Incrementor.cs` — read `Settings.<Name>.Value` / `PersistentVariables.<Name>.Value`, write the variable with `SetPersistentVariableIfNotEqual`, and re-publish on external edits via `OnPersistentVariableChanged`:

```csharp
using ViciOne.Core.Contracts.Runtime;

namespace MyBlocks;

public partial class Incrementor
{
    protected override void OnPersistentVariableChanged(IRuntimePersistentVariable variable)
        => SetConnectorValueIfNotEqual(ProcessDataOutputs.Value, PersistentVariables.Value.Value);

    protected override void ProcessCycle(IFunctionBlockCycleInfo cycleInfo)
    {
        if (FunctionBlockEnabled == false)
            return;

        if (cycleInfo.IsFirstCycle)
            SetConnectorValueIfNotEqual(ProcessDataOutputs.Value, PersistentVariables.Value.Value);

        if (ProcessDataInputs.Increment.Changed && ProcessDataInputs.Increment.Value)
        {
            if (SetPersistentVariableIfNotEqual(PersistentVariables.Value,
                    PersistentVariables.Value.Value + Settings.IncrementValue.Value))
                SetConnectorValueIfNotEqual(ProcessDataOutputs.Value, PersistentVariables.Value.Value);
        }
    }
}
```
