# Udon Reflection

## What is udon reflection?

### Why use udon reflection?
- Properties or variables can be accessed dynamically by specifying their names as strings, even if they are not predefined.
- Methods can be invoked dynamically based on their names.
- Operations that are restricted in standard Udon scripts can be achieved more flexibly.

### Limitations
- Since Udon must adhere to strict security constraints within VRChat, the Reflection functionality is not as flexible as the standard C# environment in Unity.
- Performance and resource consumption may increase compared to standard Udon scripts.

## How to use

### Setup
Inherit your class from `UdonReflectionBehaviour`, that's all.

### Available Methods
UdonReflectionBehaviour
- GetProperty: `GetProperty(string propertyName)` ReturnType: `UdonPropertyInfo`
- GetField: `GetField(string fieldName)` ReturnType: `UdonFieldInfo`
- GetFields: `GetFields()` ReturnType: `UdonFieldInfo[]`
- GetMethod: `GetMethod(string methodName)` ReturnType: `UdonMethodInfo`
- GetMethods: `GetMethods()` ReturnType: `UdonMethodInfo[]`

UdonPropertyInfo
- GetSystemType: `GetSystemType()` ReturnType: `System.Type`
- GetValue: `GetValue()` ReturnType: `object`
- SetValue: `SetValue(object value)` ReturnType: `void`

UdonFieldInfo
- GetSystemType: `GetSystemType()` ReturnType: `System.Type`
- GetValue: `GetValue()` ReturnType: `object`
- SetValue: `SetValue(object value)` ReturnType: `void`

UdonMethodInfo
- GetArgTypes `GetArgTypes()` ReturnType: `System.Type[]`
- GetReturnType `GetReturnType()` ReturnType: `System.Type`
- Invoke: `Invoke(UdonReflectionBehaviour udon, object[] args)` ReturnType: `object`

## Example
```csharp
using UnityEngine;
using Yamadev.UdonReflection;

public class TestUdon : UdonReflectionBehaviour
{
    private int _field = 0;

    private int Property
    {
        get => _field;
        set => _field = value;
    }

    private int Add(int x, int y)
    {
        return x + y;
    }

    private void Start()
    {
        var field = this.GetField(nameof(_field));
        Debug.Log(field.GetName()); // _field
        Debug.Log(field.GetSystemType()); // System.Int32
        Debug.Log(field.GetValue(this)); // 0

        var property = this.GetProperty(nameof(Property));
        Debug.Log(property.GetName()); // Property
        Debug.Log(property.GetSystemType()); // System.Int32
        Debug.Log(property.GetValue(this)); // 0
        property.SetValue(this, 1);
        Debug.Log(property.GetValue(this)); // 1

        var method = this.GetMethod(nameof(Add));
        Debug.Log(method.GetReturnType()); // System.Int32
        Debug.Log(method.Invoke(this, new object[] { 1, 2 })); // 3
    }
}
```

## License
MIT