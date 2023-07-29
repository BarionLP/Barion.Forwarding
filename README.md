# Forwarding Source Generator by Barion

## Overview
Provides a .net SourceGenerator for  properties or methods from properties or fields
Barion.Forwarding is a .NET Source Generator which aims to simplify the process of [forwarding calls](https://en.wikipedia.org/wiki/Forwarding_(object-oriented_programming)) to a field or property in a class or structs. This generator works by identifying attributes in the source code and creating forwarding methods based on these attributes. It reduces the amount of boilerplate code you need to write, making your code cleaner and easier to manage. Especially when it comes to composition.

### Features
- forwards calls to methods
- forwards getter of properties

### Limitations
- static members (coming at somepoint)
- setter of properties (coming at somepoint) 
- always forwards all method overloads
- might have trouble with generics
- ignores members starting with `set_`/`get_` as they are typically lowered getters/setters

## Usage
- add an analyzer reference to your csproj file:
```xml
  <ItemGroup>
    <ProjectReference Include="..\Barion.Forwarding\Barion.Forwarding.csproj"
      OutputItemType="Analyzer"
      ReferenceOutputAssembly="false" />
  </ItemGroup>
```
- (optional) set `<EmitCompilerGeneratedFiles>true</EmitCompilerGeneratedFiles>` so the generator saves the files in `obj\Debug\net7.0\generated\`
- copy the `ForwardAttribute.cs` and `ForwardingAttribute.cs` files into your project
- you might have to restart your IDEA for intellisense to find the generated stuff

## Example
```csharp
[Forwarding] //mark for the source generator
public partial class A { // needs to be partial
    [Forward] private B b1; // forwards every property (as readonly) or method on B
    [Forward] private B b2 {get; set;} // works for properties too
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