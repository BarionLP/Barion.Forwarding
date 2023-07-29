# Forwarding Source Generator by Barion

## Overview
Provides a .NET Source Generator which aims to simplify the process of [forwarding calls](https://en.wikipedia.org/wiki/Forwarding_(object-oriented_programming)) to a field or property. This generator works by identifying attributes in the source code and creating forwarding methods based on these attributes. It reduces the amount of boilerplate code you need to write, making your code cleaner and easier to manage. Especially when it comes to [composition](https://en.wikipedia.org/wiki/Composition_over_inheritance).

### Features
- forwards calls to methods
- forwards getter of properties
- includes inherited members (not from `System.Object`)

### Limitations
- No static members
- No setter of properties 
- No constructors
- May encounter difficulties with generics
- Always forwards all method overloads
- Ignores members starting with `set_`/`get_` as they are typically lowered getters/setters

## Usage
- add an analyzer reference to the csproj file or dll:
```xml
  <ItemGroup>
    <ProjectReference Include="..\Barion.Forwarding\Barion.Forwarding.csproj"
      OutputItemType="Analyzer"
      ReferenceOutputAssembly="false" />
  </ItemGroup>
```
- (optional) set `<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>` the generator saves the files in `{BaseIntermediateOutpath}/generated` (e.g. `obj/Debug/net7.0/generated`)
- copy the `ForwardAttribute.cs` and `ForwardingAttribute.cs` files into your project
- you might have to restart your IDE for intellisense to recognize the generated files

## Examples
```csharp
[Forwarding] //signal for the source generator
public partial class A { // needs to be partial
    [Forward] private B b1; // forwards every property (as readonly) or method on B
}
```

```csharp
[Forwarding]
public partial class A {
    [Forward("Foo", "Bar")] // forwards Foo and Bar from B
    private B b1;
    
    [Forward(nameof(B.Foo), nameof(B.Bar))] // better practice
    private B b2 {get; set;} // works for properties too
}
```

## Contributions
Feel free to fork this project and submit a pull request. Your contributions will be highly appreciated.

## Notes
- please link back this repo
- GPT-4 was used in the creation of this project