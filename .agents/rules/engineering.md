# Engineering principles

General design principles for every change in this repo, whatever the package or language. They guide judgement rather than mechanical checks. When two of them pull in different directions, choose the one that leaves the code easier to read and to change, and say in the change description which one you chose and why. The project's own rules live in the files linked from [`AGENTS.md`](../../AGENTS.md).

## Keep it simple

- **KISS (keep it simple).** Choose the simplest design that meets the requirement. Solve the problem in front of you, not a more general version of it. Plain functions and data come before new abstractions.
- **YAGNI (you aren't gonna need it).** Don't build for a future requirement nobody has written down. An extension point that nothing uses costs maintenance and gives nothing back.
- **Rule of three.** Abstract repeated code once it has appeared three times and the shape of the repetition is clear. Two similar copies are cheaper than the wrong abstraction.

## Structure

- **Separation of concerns.** Each part handles one kind of work: input, rules, persistence, presentation. Don't mix them in one function or class. Logic that can be tested without the UI should not live in the UI.
- **Cohesion.** Code that changes together lives together, and each module serves one purpose. Test a module by describing it in one sentence without "and". If the sentence needs "and", split the module.
- **Low coupling, high information hiding.** A change in one module should not force edits in unrelated modules. Expose the smallest public surface that callers need, and keep the rest private.
- **Law of Demeter.** Talk only to direct collaborators. Ask `order.Total()` rather than reaching through `order.Customer.Account.Balance`.

## Locality

- **Locality of behaviour.** Code that is read together sits together. A behaviour's logic, its tests and the configuration it depends on should be findable from one place, so a reader does not have to follow a chain of indirections to understand one action. Prefer a visible, local definition over a registry or a lookup by string, unless the indirection buys real reuse.
- **Least surprise.** A name says what the code does, and a function does nothing its name does not promise. Side effects are visible at the call site.

## Change and failure

- **Remove duplicated knowledge, not duplicated text.** A rule stated in two places that must agree is a real duplicate. Two pieces of code that look alike but change for different reasons are not.
- **Fail where the bad input enters.** Validate at the boundary and report the problem there. Don't swallow errors or substitute defaults that hide them.
- **Small, reviewable steps.** Keep a refactoring separate from a behaviour change, so each can be reviewed and reverted on its own.

## Applying them

- Before writing code, name the single responsibility of each unit you add or change. If the name needs "and", split it.
- Before adding an abstraction, name the second real case that needs it. If there isn't one, don't add it.
- When you change a behaviour, keep its logic, its tests and its configuration close together.
- When two principles conflict, say which one won and why in the change description.
