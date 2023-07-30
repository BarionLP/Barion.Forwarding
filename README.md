# Forwarding Source Generator by Barion

## Overview
Provides a .NET Source Generator which aims to simplify the process of [forwarding calls](https://en.wikipedia.org/wiki/Forwarding_(object-oriented_programming)) to a field or property. This generator works by identifying attributes in the source code and creating forwarding methods based on these attributes. It reduces the amount of boilerplate code you need to write, making your code cleaner and easier to manage. Especially when it comes to [composition](https://en.wikipedia.org/wiki/Composition_over_inheritance).

### Features
- forwards calls to methods
- forwards properties (optionally including setters)
- includes inherited members
- adds overrides if neccessary

### Limitations
- No static members
- No indexers
- No constructors
- Ignores `required` keywords
- Always forwards all method overloads
- May encounter difficulties with generics
- Ignores methods starting with `set_`/`get_` as they are typically lowered getters/setters

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
- copy the `ForwardingAttributes.cs` file into your project
- you might have to restart your IDE for intellisense to recognize the generated files
- check the generated files for comments in case members do not get forwarded correctly 

## Examples
```csharp
[Forwarding] //signal for the source generator
public partial class A { // needs to be partial
  // forwards every property (getter only) and method on B
  [Forward] private B b1;
}
```

```csharp
[Forwarding]
public partial class A {
  // forwards Foo and Bar from B
  [Forward("Foo", "Bar")] private B b1;
  
  [Forward(nameof(B.Foo), nameof(B.Bar))] // better practice
   private B b2 {get; set;} // works for properties too
}
```
if you  want more controll use the `ForwardMethodsAttribute` and `ForwardPropertiesAttribute`
```csharp
[Forwarding]
public partial class A {
  // forwards methods named Foo from B
  [ForwardMethods(nameof(B.Foo))] private B b1;
    
  // forwards the Bar property from B including it's setter
  [ForwardProperties(true, nameof(B.Bar))] 
  private B b2 {get; set;}
}
```

## Contributions
Feel free to fork this project and submit a pull request. Your contributions will be highly appreciated.

## Notes
- please link back this repo
- GPT-4 was used in the creation of this project